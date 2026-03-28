using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Pricer.Models;
using Pricer.WebApp.Server.Authorization;

namespace Pricer.WebApp.Server.Controllers;

[ApiController]
[Route("api/transactions")]
[Authorize]
public sealed class TransactionsController(
	ITransactionsService transactions,
	ISettingsService settings,
	IPrintersService printers,
	IMaterialsService materials,
	ICurrenciesService currencies) : ControllerBase
{
	public sealed record RecordPrintRequest(
		Guid MaterialId,
		decimal FilamentGrams,
		decimal PrintHours,
		decimal ExtraFixedCost);

	[HttpGet("prints")]
	[Authorize(Policy = Permissions.TransactionsView)]
	public async Task<IActionResult> GetPrints(CancellationToken ct)
		=> Ok(await transactions.GetPrintTransactionsAsync(ct));

	[HttpGet("stock")]
	[Authorize(Policy = Permissions.TransactionsView)]
	public async Task<IActionResult> GetStock(CancellationToken ct)
		=> Ok(await transactions.GetStockTransactionsAsync(ct));

	[HttpPost("record-print")]
	[Authorize(Policy = Permissions.TransactionsManage)]
	public async Task<IActionResult> RecordPrint([FromBody] RecordPrintRequest req, CancellationToken ct)
	{
		var allMaterials = await materials.GetAllAsync(ct);
		var allPrinters = await printers.GetAllAsync(ct);
		var allCurrencies = await currencies.GetAllAsync(ct);
		var appSettings = await settings.GetAsync(ct) ?? new AppSettings();

		var material = allMaterials.FirstOrDefault(m => m.Id == req.MaterialId);
		if (material is null)
			return BadRequest("Material not found.");

		var appData = new AppData
		{
			Materials = allMaterials,
			Printers = allPrinters,
			Currencies = allCurrencies,
			Settings = appSettings,
		};

		var costRequest = new PrintCostRequest(material, req.FilamentGrams, req.PrintHours, req.ExtraFixedCost);

		if (!PrintCostCalculator.TryCalculate(appData, costRequest, out var result, out var error))
			return BadRequest(error);

		var printer = appData.GetSelectedPrinter()!;
		await transactions.RecordPrintAsync(
			costRequest, result,
			printer.Id, printer.Name,
			appSettings.OperatingCurrencyId,
			allCurrencies,
			ct);

		return Ok(result);
	}

	[HttpPost("prints/{id:guid}/revert")]
	[Authorize(Policy = Permissions.TransactionsManage)]
	public async Task<IActionResult> RevertPrint(Guid id, CancellationToken ct)
	{
		var (ok, error) = await transactions.RevertPrintAsync(id, ct);
		return ok ? Ok() : BadRequest(error);
	}

	[HttpDelete("prints/{id:guid}")]
	[Authorize(Policy = Permissions.TransactionsDelete)]
	public async Task<IActionResult> DeletePrint(Guid id, CancellationToken ct)
	{
		var (ok, error) = await transactions.DeletePrintAsync(id, ct);
		return ok ? Ok() : BadRequest(error);
	}
}
