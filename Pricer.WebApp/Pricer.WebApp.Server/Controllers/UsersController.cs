using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Pricer.WebApp.Server.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Administrator")]
public sealed class UsersController(
	UserManager<IdentityUser> userManager) : ControllerBase
{
	private const string MustChangePasswordClaim = "must_change_password";

	public sealed record UserDto(string Id, string Username, IList<string> Roles, bool MustChangePassword);
	public sealed record CreateUserRequest(string Username, string TempPassword);
	public sealed record ResetPasswordRequest(string NewTempPassword);

	[HttpGet]
	public async Task<IActionResult> GetAll(CancellationToken ct)
	{
		var users = await userManager.Users.ToListAsync(ct);
		var result = new List<UserDto>(users.Count);
		foreach (var u in users)
		{
			var roles = await userManager.GetRolesAsync(u);
			var claims = await userManager.GetClaimsAsync(u);
			result.Add(new UserDto(
				u.Id,
				u.UserName!,
				roles,
				claims.Any(c => c.Type == MustChangePasswordClaim)));
		}
		return Ok(result);
	}

	[HttpPost]
	public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
	{
		var user = new IdentityUser { UserName = request.Username };
		var result = await userManager.CreateAsync(user, request.TempPassword);
		if (!result.Succeeded)
			return BadRequest(result.Errors.Select(e => e.Description));

		await userManager.AddClaimAsync(user, new Claim(MustChangePasswordClaim, "true"));
		return Ok(new { user.Id, user.UserName });
	}

	[HttpPost("{id}/reset-password")]
	public async Task<IActionResult> ResetPassword(string id, [FromBody] ResetPasswordRequest request)
	{
		var user = await userManager.FindByIdAsync(id);
		if (user is null) return NotFound();

		var token = await userManager.GeneratePasswordResetTokenAsync(user);
		var result = await userManager.ResetPasswordAsync(user, token, request.NewTempPassword);
		if (!result.Succeeded)
			return BadRequest(result.Errors.Select(e => e.Description));

		// Ensure must_change_password is set
		var claims = await userManager.GetClaimsAsync(user);
		if (!claims.Any(c => c.Type == MustChangePasswordClaim))
			await userManager.AddClaimAsync(user, new Claim(MustChangePasswordClaim, "true"));

		return Ok();
	}

	[HttpDelete("{id}")]
	public async Task<IActionResult> Delete(string id)
	{
		var currentUserId = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
		if (id == currentUserId)
			return BadRequest("Cannot delete your own account.");

		var user = await userManager.FindByIdAsync(id);
		if (user is null) return NotFound();

		var result = await userManager.DeleteAsync(user);
		return result.Succeeded ? Ok() : BadRequest(result.Errors.Select(e => e.Description));
	}
}
