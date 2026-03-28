using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Pricer.WebApp.Server.Authorization;

namespace Pricer.WebApp.Server.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(
	UserManager<IdentityUser> userManager,
	RoleManager<IdentityRole> roleManager) : ControllerBase
{
	private const string MustChangePasswordClaim = "must_change_password";

	public sealed record UserDto(string Id, string Username, IList<string> Roles, bool MustChangePassword);
	public sealed record CreateUserRequest(string Username, string TempPassword);
	public sealed record ResetPasswordRequest(string NewTempPassword);

	[HttpGet]
	[Authorize(Policy = Permissions.UsersManage)]
	public async Task<IActionResult> GetAll(CancellationToken ct)
	{
		var users = await userManager.Users.ToListAsync(ct);
		var result = new List<UserDto>(users.Count);
		foreach (var u in users)
		{
			var roles = await userManager.GetRolesAsync(u);
			var claims = await userManager.GetClaimsAsync(u);
			result.Add(new UserDto(u.Id, u.UserName!, roles, claims.Any(c => c.Type == MustChangePasswordClaim)));
		}
		return Ok(result);
	}

	[HttpPost]
	[Authorize(Policy = Permissions.UsersManage)]
	public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
	{
		var user = new IdentityUser { UserName = request.Username };
		var result = await userManager.CreateAsync(user, request.TempPassword);
		if (!result.Succeeded)
			return BadRequest(result.Errors.Select(e => e.Description));

		await userManager.AddClaimAsync(user, new Claim(MustChangePasswordClaim, "true"));
		return Ok(new UserDto(user.Id, user.UserName!, [], true));
	}

	[HttpPost("{id}/reset-password")]
	[Authorize(Policy = Permissions.UsersManage)]
	public async Task<IActionResult> ResetPassword(string id, [FromBody] ResetPasswordRequest request)
	{
		var user = await userManager.FindByIdAsync(id);
		if (user is null) return NotFound();

		var token = await userManager.GeneratePasswordResetTokenAsync(user);
		var result = await userManager.ResetPasswordAsync(user, token, request.NewTempPassword);
		if (!result.Succeeded)
			return BadRequest(result.Errors.Select(e => e.Description));

		var claims = await userManager.GetClaimsAsync(user);
		if (!claims.Any(c => c.Type == MustChangePasswordClaim))
			await userManager.AddClaimAsync(user, new Claim(MustChangePasswordClaim, "true"));

		return Ok();
	}

	[HttpDelete("{id}")]
	[Authorize(Policy = Permissions.UsersDelete)]
	public async Task<IActionResult> Delete(string id)
	{
		var currentUserId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
		if (id == currentUserId)
			return BadRequest("Cannot delete your own account.");

		var user = await userManager.FindByIdAsync(id);
		if (user is null) return NotFound();

		var result = await userManager.DeleteAsync(user);
		return result.Succeeded ? Ok() : BadRequest(result.Errors.Select(e => e.Description));
	}

	// ── Role management ───────────────────────────────────────────────────

	/// <summary>All available role names (for the role assignment dropdown).</summary>
	[HttpGet("roles")]
	[Authorize(Policy = Permissions.UsersManage)]
	public IActionResult GetAvailableRoles()
		=> Ok(roleManager.Roles.Select(r => r.Name).OrderBy(n => n).ToList());

	[HttpPost("{id}/roles/{roleName}")]
	[Authorize(Policy = Permissions.UsersManage)]
	public async Task<IActionResult> AssignRole(string id, string roleName)
	{
		var user = await userManager.FindByIdAsync(id);
		if (user is null) return NotFound();

		if (!await roleManager.RoleExistsAsync(roleName))
			return BadRequest($"Role '{roleName}' does not exist.");

		if (await userManager.IsInRoleAsync(user, roleName))
			return Ok(); // already assigned — idempotent

		var result = await userManager.AddToRoleAsync(user, roleName);
		return result.Succeeded ? Ok() : BadRequest(result.Errors.Select(e => e.Description));
	}

	[HttpDelete("{id}/roles/{roleName}")]
	[Authorize(Policy = Permissions.UsersManage)]
	public async Task<IActionResult> RemoveRole(string id, string roleName)
	{
		var user = await userManager.FindByIdAsync(id);
		if (user is null) return NotFound();

		var result = await userManager.RemoveFromRoleAsync(user, roleName);
		return result.Succeeded ? Ok() : BadRequest(result.Errors.Select(e => e.Description));
	}
}
