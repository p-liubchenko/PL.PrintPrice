using Pricer.Models;

using System;

namespace Pricer;

public sealed record PrintCostRequest(
	FilamentMaterial Material,
	decimal FilamentGrams,
	decimal PrintHours,
	decimal ExtraFixedCost);

public sealed record PrintCostResult(
	decimal FilamentKg,
	decimal EstimatedMetersUsed,
	decimal MaterialCost,
	decimal ElectricityKwh,
	decimal ElectricityCost,
	decimal PrinterWearCost,
	decimal FixedCost,
	decimal ExtraFixedCost,
	decimal Total);

public static class PrintCostCalculator
{
	public static bool TryCalculate(AppData store, PrintCostRequest request, out PrintCostResult result, out string error)
	{
		result = default!;
		error = string.Empty;

		if (request.FilamentGrams < 0)
		{
			error = "Filament grams must be >= 0.";
			return false;
		}

		if (request.PrintHours < 0)
		{
			error = "Print time must be >= 0.";
			return false;
		}

		if (request.ExtraFixedCost < 0)
		{
			error = "Extra cost must be >= 0.";
			return false;
		}

		var printer = store.GetSelectedPrinter();
		if (printer is null)
		{
			error = "No printer selected. Add/select a printer in Printer management.";
			return false;
		}

		var kg = request.FilamentGrams / 1000m;
		if (kg > request.Material.AmountKg)
		{
			error = "Not enough filament stock for this calculation/deduction.";
			return false;
		}

		var currencies = store.Currencies;
		var materialCost = kg * request.Material.AveragePricePerKgMoney.ToBase(currencies);
		var electricityKwh = (printer.AveragePowerWatts / 1000m) * request.PrintHours;
		var electricityCost = electricityKwh * store.Settings.ElectricityPricePerKwhMoney.ToBase(currencies);
		var printerWearCost = request.PrintHours * printer.HourlyCostMoney.ToBase(currencies);
		var fixedCost = store.Settings.FixedCostPerPrintMoney.ToBase(currencies);
		var total = materialCost + electricityCost + printerWearCost + fixedCost + request.ExtraFixedCost;

		decimal estimatedMetersUsed = 0;
		if (request.Material.AmountKg > 0 && request.Material.EstimatedLengthMeters > 0)
		{
			estimatedMetersUsed = (kg / request.Material.AmountKg) * request.Material.EstimatedLengthMeters;
		}

		result = new PrintCostResult(
			FilamentKg: kg,
			EstimatedMetersUsed: estimatedMetersUsed,
			MaterialCost: materialCost,
			ElectricityKwh: electricityKwh,
			ElectricityCost: electricityCost,
			PrinterWearCost: printerWearCost,
			FixedCost: fixedCost,
			ExtraFixedCost: request.ExtraFixedCost,
			Total: total);
		return true;
	}
}
