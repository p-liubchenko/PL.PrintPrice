using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Pricer.Models;
using Pricer.WebApp.Server.Authorization;

namespace Pricer.WebApp.Server.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize]
public sealed class SettingsController(ISettingsService settings) : ControllerBase
{
	[HttpGet]
	[Authorize(Policy = Permissions.SettingsView)]
	public async Task<IActionResult> Get(CancellationToken ct)
	{
		var s = await settings.GetAsync(ct);
		return s is null ? NotFound() : Ok(s);
	}

	[HttpPut]
	[Authorize(Policy = Permissions.SettingsManage)]
	public async Task<IActionResult> Update([FromBody] AppSettings updated, CancellationToken ct)
	{
		await settings.UpsertAsync(updated, ct);
		return Ok();
	}
}
