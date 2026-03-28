using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Pricer.Models;

namespace Pricer.WebApp.Server.Controllers;

[ApiController]
[Route("api/currencies")]
[Authorize]
public sealed class CurrenciesController(ICurrenciesService currencies) : ControllerBase
{
	[HttpGet]
	public async Task<IActionResult> GetAll(CancellationToken ct)
		=> Ok(await currencies.GetAllAsync(ct));

	[HttpPost]
	public async Task<IActionResult> Add([FromBody] Currency currency, CancellationToken ct)
	{
		var (ok, error) = await currencies.AddAsync(currency, ct);
		return ok ? Ok() : BadRequest(error);
	}

	[HttpPut("{id:guid}")]
	public async Task<IActionResult> Upsert(Guid id, [FromBody] Currency currency, CancellationToken ct)
	{
		currency.Id = id;
		await currencies.UpsertAsync(currency, ct);
		return Ok();
	}

	[HttpPost("{id:guid}/set-base")]
	public async Task<IActionResult> SetBase(Guid id, CancellationToken ct)
	{
		var (ok, error) = await currencies.SetBaseCurrencyAsync(id, ct);
		return ok ? Ok() : BadRequest(error);
	}

	[HttpPost("{id:guid}/set-operating")]
	public async Task<IActionResult> SetOperating(Guid id, CancellationToken ct)
	{
		var (ok, error) = await currencies.SetOperatingCurrencyAsync(id, ct);
		return ok ? Ok() : BadRequest(error);
	}

	[HttpDelete("{id:guid}")]
	public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
	{
		var (ok, error) = await currencies.RemoveAsync(id, ct);
		return ok ? Ok() : BadRequest(error);
	}
}
