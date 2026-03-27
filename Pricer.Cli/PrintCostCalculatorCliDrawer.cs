using Pricer.Models;

using System;

namespace Pricer;

public sealed class PrintCostCalculatorCliDrawer
{
	public void Run(AppData store, FilamentWarehouse warehouse, bool deductStock, string dataFilePath, Func<AppData, FilamentMaterial?> selectMaterial, Action<PrintCostRequest, PrintCostResult>? onDeducted = null)
	{
		Console.Clear();
		ConsoleEx.PrintHeader(deductStock ? "Calculate Print Cost and Deduct Stock" : "Calculate Print Cost");

		var material = selectMaterial(store);
		if (material is null)
		{
			return;
		}

		Console.WriteLine();
		Console.WriteLine($"Using: {material.Name}");
		Console.WriteLine($"Available stock: {material.AmountKg:F3} kg | ~{material.EstimatedLengthMeters:F1} m");
		Console.WriteLine($"Average price: {MoneyFormatter.FormatPerKg(store, material.AveragePricePerKgMoney.ToBase(store))}");
		Console.WriteLine();

		var grams = ConsoleEx.ReadDecimal("Filament used in grams", min: 0);
		var hours = ConsoleEx.ReadHoursDecimal("Print time (hours) (examples: 1h10m, 15m35s, 1:10, 01:10, 1:10:05, 15:35, 1.12)", min: 0);
		var extraFixed = ConsoleEx.ReadDecimal($"Any extra one-time cost for this print in {store.GetOperatingCurrency()?.Code ?? "(base)"} (Enter for 0)", min: 0, defaultValue: 0m);

		var request = new PrintCostRequest(material, grams, hours, new Money(extraFixed, store.OperatingCurrencyId).ToBase(store));
		if (!PrintCostCalculator.TryCalculate(store, request, out var result, out var error))
		{
			ConsoleEx.ShowMessage(error);
			return;
		}

		Console.WriteLine();
		Console.WriteLine("----- Cost Breakdown -----");
		Console.WriteLine($"Material cost:           {MoneyFormatter.Format(store, result.MaterialCost)}");
		Console.WriteLine($"Electricity cost:        {MoneyFormatter.Format(store, result.ElectricityCost)}");
		Console.WriteLine($"Printer hourly cost:     {MoneyFormatter.Format(store, result.PrinterWearCost)}");
		Console.WriteLine($"Fixed cost per print:    {MoneyFormatter.Format(store, result.FixedCost)}");
		Console.WriteLine($"Extra one-time cost:     {MoneyFormatter.Format(store, result.ExtraFixedCost)}");
		Console.WriteLine("--------------------------");
		Console.WriteLine($"TOTAL:                   {MoneyFormatter.Format(store, result.Total)}");
		Console.WriteLine();
		Console.WriteLine($"Estimated electricity:   {result.ElectricityKwh:F3} kWh");
		Console.WriteLine($"Estimated filament use:  {result.FilamentKg:F3} kg | ~{result.EstimatedMetersUsed:F1} m");
		Console.WriteLine();

		if (deductStock)
		{
			warehouse.ConsumeMaterial(store, material, result.FilamentKg, result.EstimatedMetersUsed);
			onDeducted?.Invoke(request, result);
			Console.WriteLine("Stock was deducted.");
			Console.WriteLine();
		}

		ConsoleEx.Pause();
	}
}
