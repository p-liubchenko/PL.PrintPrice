using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using Pricer.WebApp.Server.Authorization;

namespace Pricer.WebApp.Server.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
public sealed class RolesController(RoleManager<IdentityRole> roleManager) : ControllerBase
{
	private const string AdminRole = "Administrator";
	private const string PermissionClaimType = "permission";

	public sealed record RoleDto(string Id, string Name, IReadOnlyList<string> Permissions, bool IsSystem);
	public sealed record CreateRoleRequest(string Name, IList<string> Permissions);
	public sealed record UpdateRoleRequest(IList<string> Permissions);

	/// <summary>All roles with their assigned permissions.</summary>
	[HttpGet]
	[Authorize(Policy = Permissions.RolesManage)]
	public async Task<IActionResult> GetAll()
	{
		var result = new List<RoleDto>();
		foreach (var role in roleManager.Roles.ToList())
		{
			var claims = await roleManager.GetClaimsAsync(role);
			var perms = claims
				.Where(c => c.Type == PermissionClaimType)
				.Select(c => c.Value)
				.OrderBy(v => v)
				.ToList();
			result.Add(new RoleDto(role.Id, role.Name!, perms, role.Name == AdminRole));
		}
		return Ok(result);
	}

	/// <summary>Full list of all defined permissions (for the UI permission picker).</summary>
	[HttpGet("permissions")]
	[Authorize(Policy = Permissions.RolesManage)]
	public IActionResult GetAllPermissions() => Ok(Permissions.All);

	[HttpPost]
	[Authorize(Policy = Permissions.RolesManage)]
	public async Task<IActionResult> Create([FromBody] CreateRoleRequest req)
	{
		if (string.IsNullOrWhiteSpace(req.Name))
			return BadRequest("Role name is required.");

		var role = new IdentityRole(req.Name.Trim());
		var result = await roleManager.CreateAsync(role);
		if (!result.Succeeded)
			return BadRequest(result.Errors.Select(e => e.Description));

		foreach (var p in req.Permissions.Where(p => Permissions.All.Contains(p)))
			await roleManager.AddClaimAsync(role, new Claim(PermissionClaimType, p));

		var claims = await roleManager.GetClaimsAsync(role);
		return Ok(new RoleDto(
			role.Id, role.Name!,
			claims.Where(c => c.Type == PermissionClaimType).Select(c => c.Value).OrderBy(v => v).ToList(),
			false));
	}

	/// <summary>Replace the permission set of a role. Administrator is immutable.</summary>
	[HttpPut("{id}")]
	[Authorize(Policy = Permissions.RolesManage)]
	public async Task<IActionResult> Update(string id, [FromBody] UpdateRoleRequest req)
	{
		var role = await roleManager.FindByIdAsync(id);
		if (role is null) return NotFound();
		if (role.Name == AdminRole)
			return BadRequest("The Administrator role permissions are managed automatically and cannot be changed.");

		var existing = await roleManager.GetClaimsAsync(role);
		foreach (var c in existing.Where(c => c.Type == PermissionClaimType))
			await roleManager.RemoveClaimAsync(role, c);

		foreach (var p in req.Permissions.Where(p => Permissions.All.Contains(p)))
			await roleManager.AddClaimAsync(role, new Claim(PermissionClaimType, p));

		return Ok();
	}

	/// <summary>Delete a role. Administrator cannot be deleted.</summary>
	[HttpDelete("{id}")]
	[Authorize(Policy = Permissions.RolesDelete)]
	public async Task<IActionResult> Delete(string id)
	{
		var role = await roleManager.FindByIdAsync(id);
		if (role is null) return NotFound();
		if (role.Name == AdminRole)
			return BadRequest("The Administrator role cannot be deleted.");

		var result = await roleManager.DeleteAsync(role);
		return result.Succeeded ? Ok() : BadRequest(result.Errors.Select(e => e.Description));
	}
}
