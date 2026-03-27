using Pricer.Models;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Pricer;

public sealed class PrintCostCalculatorCliDrawer(
	IMaterialsService materialsService,
	ITransactionsService transactionsService,
	ICurrenciesService currenciesService,
	ISettingsService settingsService,
	IPrintersService printersService)
{
	public void Run(bool deductStock)
	{
		var materials = materialsService.GetAllAsync().GetAwaiter().GetResult();
		var currencies = currenciesService.GetAllAsync().GetAwaiter().GetResult();
		var settings = settingsService.GetAsync().GetAwaiter().GetResult() ?? new AppSettings();
		var operatingCurrency = currencies.FirstOrDefault(c => c.Id == settings.OperatingCurrencyId);
		var printers = printersService.GetAllAsync().GetAwaiter().GetResult();

		Console.Clear();
		ConsoleEx.PrintHeader(deductStock ? "Calculate Print Cost and Deduct Stock" : "Calculate Print Cost");

		if (!materials.Any())
		{
			ConsoleEx.ShowMessage("No materials in storage yet.");
			return;
		}

		// Material selection
		for (int i = 0; i < materials.Count; i++)
		{
			var m = materials[i];
			Console.WriteLine($"{i + 1}) {m.Name} | {m.Color} | {m.Type} | {m.AmountKg:F3} kg | {MoneyFormatter.FormatPerKg(operatingCurrency, m.AveragePricePerKgMoney.ToBase(currencies))}");
		}

		var materialIndex = ConsoleEx.ReadInt("Select material number", 1, materials.Count) - 1;
		var material = materials[materialIndex];

		Console.WriteLine();
		Console.WriteLine($"Using: {material.Name}");
		Console.WriteLine($"Available stock: {material.AmountKg:F3} kg | ~{material.EstimatedLengthMeters:F1} m");
		Console.WriteLine($"Average price: {MoneyFormatter.FormatPerKg(operatingCurrency, material.AveragePricePerKgMoney.ToBase(currencies))}");
		Console.WriteLine();

		var grams = ConsoleEx.ReadDecimal("Filament used in grams", min: 0);
		var hours = ConsoleEx.ReadHoursDecimal("Print time (hours) (examples: 1h10m, 15m35s, 1:10, 01:10, 1:10:05, 15:35, 1.12)", min: 0);
		var extraFixed = ConsoleEx.ReadDecimal($"Any extra one-time cost for this print in {operatingCurrency?.Code ?? "(base)"} (Enter for 0)", min: 0, defaultValue: 0m);

		var extraFixedBase = new Money(extraFixed, settings.OperatingCurrencyId).ToBase(currencies);
		var request = new PrintCostRequest(material, grams, hours, extraFixedBase);

		// Build AppData for the calculator
		var appData = new AppData
		{
			Currencies = currencies,
			Printers = printers,
			Materials = materials,
			Settings = settings
		};

		if (!PrintCostCalculator.TryCalculate(appData, request, out var result, out var error))
		{
			ConsoleEx.ShowMessage(error);
			return;
		}

		Console.WriteLine();
		Console.WriteLine("----- Cost Breakdown -----");
		Console.WriteLine($"Material cost:           {MoneyFormatter.Format(operatingCurrency, result.MaterialCost)}");
		Console.WriteLine($"Electricity cost:        {MoneyFormatter.Format(operatingCurrency, result.ElectricityCost)}");
		Console.WriteLine($"Printer hourly cost:     {MoneyFormatter.Format(operatingCurrency, result.PrinterWearCost)}");
		Console.WriteLine($"Fixed cost per print:    {MoneyFormatter.Format(operatingCurrency, result.FixedCost)}");
		Console.WriteLine($"Extra one-time cost:     {MoneyFormatter.Format(operatingCurrency, result.ExtraFixedCost)}");
		Console.WriteLine("--------------------------");
		Console.WriteLine($"TOTAL:                   {MoneyFormatter.Format(operatingCurrency, result.Total)}");
		Console.WriteLine();
		Console.WriteLine($"Estimated electricity:   {result.ElectricityKwh:F3} kWh");
		Console.WriteLine($"Estimated filament use:  {result.FilamentKg:F3} kg | ~{result.EstimatedMetersUsed:F1} m");
		Console.WriteLine();

		if (deductStock)
		{
			var (consumeOk, consumeError) = materialsService.ConsumeAsync(material.Id, result.FilamentKg, result.EstimatedMetersUsed).GetAwaiter().GetResult();
			if (!consumeOk)
			{
				ConsoleEx.ShowMessage(consumeError);
				return;
			}

			var selectedPrinter = printers.FirstOrDefault(p => p.Id == settings.SelectedPrinterId);
			if (selectedPrinter is not null)
			{
				transactionsService.RecordPrintAsync(
					request, result,
					selectedPrinter.Id, selectedPrinter.Name,
					settings.OperatingCurrencyId, currencies).GetAwaiter().GetResult();
			}

			Console.WriteLine("Stock was deducted.");
			Console.WriteLine();
		}

		ConsoleEx.Pause();
	}
}
