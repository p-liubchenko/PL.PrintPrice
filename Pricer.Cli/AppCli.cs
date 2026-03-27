using Pricer.Models;

using System;
using System.Linq;

namespace Pricer;

public sealed class AppCli(
	FilamentWarehouseCliDrawer filamentWarehouseDrawer,
	PrinterManagerCliDrawer printerManagerDrawer,
	CurrencyManagerCliDrawer currencyManagerDrawer,
	PrintTransactionsCliDrawer printTransactionsDrawer,
	PrintCostCalculatorCliDrawer printCostDrawer,
	ISettingsService settingsService,
	ICurrenciesService currenciesService,
	IPrintersService printersService,
	IMaterialsService materialsService)
{
	public void Run()
	{
		Console.OutputEncoding = System.Text.Encoding.UTF8;

		while (true)
		{
			Console.Clear();
			ConsoleEx.PrintHeader("3D Print Cost Calculator + Filament Warehouse");
			Console.WriteLine("1) Filament warehouse");
			Console.WriteLine("2) Print cost calculator");
			Console.WriteLine("3) Printer management");
			Console.WriteLine("4) Currency management");
			Console.WriteLine("5) Expense settings");
			Console.WriteLine("6) Print history / transactions");
			Console.WriteLine("7) Reports / overview");
			Console.WriteLine("0) Exit");
			Console.WriteLine();

			switch (ConsoleEx.ReadMenuChoice("Choose an option"))
			{
				case "1": filamentWarehouseDrawer.Menu(); break;
				case "2": PrintCostMenu(); break;
				case "3": printerManagerDrawer.Menu(); break;
				case "4": currencyManagerDrawer.Menu(); break;
				case "5": ExpenseSettingsMenu(); break;
				case "6": printTransactionsDrawer.Menu(); break;
				case "7": ReportsMenu(); break;
				case "0": return;
				default: ConsoleEx.ShowMessage("Unknown option."); break;
			}
		}
	}

	private void PrintCostMenu()
	{
		while (true)
		{
			Console.Clear();
			ConsoleEx.PrintHeader("Print Cost Calculator");
			Console.WriteLine("1) Calculate print cost");
			Console.WriteLine("2) Calculate print cost and deduct stock");
			Console.WriteLine("0) Back");
			Console.WriteLine();

			switch (ConsoleEx.ReadMenuChoice("Choose an option"))
			{
				case "1": printCostDrawer.Run(deductStock: false); break;
				case "2": printCostDrawer.Run(deductStock: true); break;
				case "0": return;
				default: ConsoleEx.ShowMessage("Unknown option."); break;
			}
		}
	}

	private void ExpenseSettingsMenu()
	{
		while (true)
		{
			var currencies = currenciesService.GetAllAsync().GetAwaiter().GetResult();
			var settings = settingsService.GetAsync().GetAwaiter().GetResult() ?? new AppSettings();
			var operatingCurrency = currencies.FirstOrDefault(c => c.Id == settings.OperatingCurrencyId);
			var printers = printersService.GetAllAsync().GetAwaiter().GetResult();
			var selectedPrinter = printers.FirstOrDefault(p => p.Id == settings.SelectedPrinterId);

			Console.Clear();
			ConsoleEx.PrintHeader("Expense Settings");
			Console.WriteLine($"Current electricity price: {MoneyFormatter.FormatPerKwh(operatingCurrency, settings.ElectricityPricePerKwhMoney.ToBase(currencies))}");
			Console.WriteLine($"Current selected printer: {(selectedPrinter is null ? "(none)" : selectedPrinter.Name)}");
			Console.WriteLine($"Current misc fixed cost per print: {MoneyFormatter.Format(operatingCurrency, settings.FixedCostPerPrintMoney.ToBase(currencies))}");
			Console.WriteLine();
			Console.WriteLine("1) Set electricity price per kWh");
			Console.WriteLine("2) Set fixed cost per print");
			Console.WriteLine("0) Back");
			Console.WriteLine();

			switch (ConsoleEx.ReadMenuChoice("Choose an option"))
			{
				case "1":
				{
					var value = ConsoleEx.ReadDecimal($"Electricity price in {operatingCurrency?.Code ?? "(base)"}/kWh", min: 0);
					settings.ElectricityPricePerKwhMoney = new Money(value, settings.OperatingCurrencyId);
					settingsService.UpsertAsync(settings).GetAwaiter().GetResult();
					ConsoleEx.ShowMessage("Electricity price updated.");
					break;
				}
				case "2":
				{
					var value = ConsoleEx.ReadDecimal($"Fixed cost per print in {operatingCurrency?.Code ?? "(base)"}", min: 0);
					settings.FixedCostPerPrintMoney = new Money(value, settings.OperatingCurrencyId);
					settingsService.UpsertAsync(settings).GetAwaiter().GetResult();
					ConsoleEx.ShowMessage("Fixed print cost updated.");
					break;
				}
				case "0": return;
				default: ConsoleEx.ShowMessage("Unknown option."); break;
			}
		}
	}

	private void ReportsMenu()
	{
		var materials = materialsService.GetAllAsync().GetAwaiter().GetResult();
		var currencies = currenciesService.GetAllAsync().GetAwaiter().GetResult();
		var settings = settingsService.GetAsync().GetAwaiter().GetResult() ?? new AppSettings();
		var operatingCurrency = currencies.FirstOrDefault(c => c.Id == settings.OperatingCurrencyId);

		Console.Clear();
		ConsoleEx.PrintHeader("Reports / Overview");

		if (!materials.Any())
		{
			Console.WriteLine("No materials stored.");
			ConsoleEx.Pause();
			return;
		}

		var totalStockKg = materials.Sum(x => x.AmountKg);
		var totalStockM = materials.Sum(x => x.EstimatedLengthMeters);
		var totalValueBase = materials.Sum(x => x.AmountKg * x.AveragePricePerKgMoney.ToBase(currencies));

		Console.WriteLine($"Material entries:         {materials.Count}");
		Console.WriteLine($"Total stock:              {totalStockKg:F3} kg");
		Console.WriteLine($"Estimated total length:   {totalStockM:F1} m");
		Console.WriteLine($"Estimated stock value:    {MoneyFormatter.Format(operatingCurrency, totalValueBase)}");
		Console.WriteLine();

		foreach (var group in materials.GroupBy(x => x.Type).OrderBy(g => g.Key))
		{
			var groupValueBase = group.Sum(x => x.AmountKg * x.AveragePricePerKgMoney.ToBase(currencies));
			Console.WriteLine($"[{group.Key}]");
			Console.WriteLine($"  Entries: {group.Count()}");
			Console.WriteLine($"  Stock:   {group.Sum(x => x.AmountKg):F3} kg");
			Console.WriteLine($"  Value:   {MoneyFormatter.Format(operatingCurrency, groupValueBase)}");
			Console.WriteLine();
		}

		ConsoleEx.Pause();
	}
}
