using Microsoft.AspNetCore.Authorization;

namespace Pricer.WebApp.Server.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
	public string Permission => permission;
}
