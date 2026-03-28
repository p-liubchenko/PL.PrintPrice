using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Pricer.WebApp.Server.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
	UserManager<IdentityUser> userManager,
	RoleManager<IdentityRole> roleManager,
	IConfiguration configuration) : ControllerBase
{
	private const string AdminRole = "Administrator";
	private const string MustChangePasswordClaim = "must_change_password";

	public sealed record LoginRequest(string Username, string Password);
	public sealed record SetupRequest(string Username, string Password);
	public sealed record ChangePasswordRequest(string NewPassword, string? CurrentPassword);
	public sealed record AuthResponse(string Token, DateTimeOffset ExpiresAt, string Username, bool MustChangePassword, bool IsAdmin);

	// ── Public ───────────────────────────────────────────────────────────

	[HttpGet("setup-required")]
	public async Task<IActionResult> SetupRequired(CancellationToken ct)
		=> Ok(new { required = !await userManager.Users.AnyAsync(ct) });

	[HttpPost("setup")]
	public async Task<IActionResult> Setup([FromBody] SetupRequest request, CancellationToken ct)
	{
		if (await userManager.Users.AnyAsync(ct))
			return BadRequest("Setup has already been completed. Use the login page.");

		if (!await roleManager.RoleExistsAsync(AdminRole))
			await roleManager.CreateAsync(new IdentityRole(AdminRole));

		var user = new IdentityUser { UserName = request.Username };
		var result = await userManager.CreateAsync(user, request.Password);
		if (!result.Succeeded)
			return BadRequest(result.Errors.Select(e => e.Description));

		await userManager.AddToRoleAsync(user, AdminRole);
		return Ok(await BuildTokenAsync(user));
	}

	[HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
	{
		var user = await userManager.FindByNameAsync(request.Username);
		if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
			return Unauthorized("Invalid username or password.");

		return Ok(await BuildTokenAsync(user));
	}

	// ── Authenticated ────────────────────────────────────────────────────

	[HttpPost("change-password")]
	[Authorize]
	public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
	{
		var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
		var user = await userManager.FindByIdAsync(userId!);
		if (user is null) return Unauthorized();

		var userClaims = await userManager.GetClaimsAsync(user);
		var mustChangeClaim = userClaims.FirstOrDefault(c => c.Type == MustChangePasswordClaim);

		IdentityResult result;
		if (mustChangeClaim is not null)
		{
			// Forced first-time change — no old password needed
			var token = await userManager.GeneratePasswordResetTokenAsync(user);
			result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
		}
		else
		{
			if (string.IsNullOrWhiteSpace(request.CurrentPassword))
				return BadRequest("Current password is required.");
			result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
		}

		if (!result.Succeeded)
			return BadRequest(result.Errors.Select(e => e.Description));

		if (mustChangeClaim is not null)
			await userManager.RemoveClaimAsync(user, mustChangeClaim);

		return Ok(await BuildTokenAsync(user));
	}

	// ── Helpers ──────────────────────────────────────────────────────────

	private async Task<AuthResponse> BuildTokenAsync(IdentityUser user)
	{
		var key = configuration["Jwt:Key"]!;
		var issuer = configuration["Jwt:Issuer"] ?? "PL.PrintPrice";
		var audience = configuration["Jwt:Audience"] ?? "PL.PrintPrice";
		var hours = configuration.GetValue<double>("Jwt:ExpirationHours", 24);

		var userClaims = await userManager.GetClaimsAsync(user);
		var roles = await userManager.GetRolesAsync(user);

		var claims = new List<Claim>
		{
			new(JwtRegisteredClaimNames.Sub, user.Id),
			new(JwtRegisteredClaimNames.UniqueName, user.UserName!),
			new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
		};
		claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

		var mustChange = userClaims.FirstOrDefault(c => c.Type == MustChangePasswordClaim);
		if (mustChange is not null) claims.Add(mustChange);

		var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
		var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
		var expires = DateTimeOffset.UtcNow.AddHours(hours);

		var token = new JwtSecurityToken(
			issuer: issuer,
			audience: audience,
			claims: claims,
			expires: expires.UtcDateTime,
			signingCredentials: creds);

		return new AuthResponse(
			new JwtSecurityTokenHandler().WriteToken(token),
			expires,
			user.UserName!,
			MustChangePassword: mustChange is not null,
			IsAdmin: roles.Contains(AdminRole));
	}
}
