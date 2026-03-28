using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Pricer.Models;

namespace Pricer.WebApp.Server.Controllers;

[ApiController]
[Route("api/printers")]
[Authorize]
public sealed class PrintersController(IPrintersService printers) : ControllerBase
{
	[HttpGet]
	public async Task<IActionResult> GetAll(CancellationToken ct)
		=> Ok(await printers.GetAllAsync(ct));

	[HttpPost]
	public async Task<IActionResult> Add([FromBody] Printer printer, CancellationToken ct)
	{
		printer.Id = Guid.NewGuid();
		await printers.AddAsync(printer, ct);
		return Ok(printer);
	}

	[HttpPut("{id:guid}")]
	public async Task<IActionResult> Upsert(Guid id, [FromBody] Printer printer, CancellationToken ct)
	{
		printer.Id = id;
		await printers.UpsertAsync(printer, ct);
		return Ok();
	}

	[HttpPost("{id:guid}/select")]
	public async Task<IActionResult> Select(Guid id, CancellationToken ct)
	{
		await printers.SelectAsync(id, ct);
		return Ok();
	}

	[HttpDelete("{id:guid}")]
	public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
	{
		var (ok, error) = await printers.RemoveAsync(id, ct);
		return ok ? Ok() : BadRequest(error);
	}
}
