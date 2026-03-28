using Pricer.Models;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Pricer;

public sealed class FilamentWarehouseCliDrawer(
	IMaterialsService materialsService,
	ITransactionsService transactionsService,
	ICurrenciesService currenciesService,
	ISettingsService settingsService)
{
	public void Menu()
	{
		while (true)
		{
			Console.Clear();
			ConsoleEx.PrintHeader("Filament Warehouse");
			ConsoleEx.DrawMenuItem("1) List spools");
			ConsoleEx.DrawMenuItem("2) Add spool purchase");
			ConsoleEx.DrawMenuItem("3) Add stock to existing material (weighted average price)");
			ConsoleEx.DrawMenuItem("4) Consume material manually");
			ConsoleEx.DrawMenuItem("5) Remove spool/material entry", ConsoleEx.Severity.Unsafe);
			ConsoleEx.DrawMenuItem("0) Back");
			Console.WriteLine();

			switch (ConsoleEx.ReadMenuChoice("Choose an option"))
			{
				case "1": ListSpools(); break;
				case "2": AddSpoolPurchase(); break;
				case "3": RestockExistingMaterial(); break;
				case "4": ConsumeMaterialManually(); break;
				case "5": RemoveMaterial(); break;
				case "0": return;
				default: ConsoleEx.ShowMessage("Unknown option."); break;
			}
		}
	}

	public FilamentMaterial? SelectMaterial()
	{
		var materials = materialsService.GetAllAsync().GetAwaiter().GetResult();
		if (!materials.Any())
		{
			ConsoleEx.ShowMessage("No materials in storage yet.");
			return null;
		}

		var (currencies, settings, operatingCurrency) = LoadContext();
		ListMaterialsCompact(materials, currencies, operatingCurrency);
		var index = ConsoleEx.ReadInt("Select material number", 1, materials.Count) - 1;
		return materials[index];
	}

	private void ListSpools()
	{
		var materials = materialsService.GetAllAsync().GetAwaiter().GetResult();
		var (currencies, _, operatingCurrency) = LoadContext();

		Console.Clear();
		ConsoleEx.PrintHeader("Stored Materials");

		if (!materials.Any())
		{
			Console.WriteLine("No spools/materials in storage yet.");
			ConsoleEx.Pause();
			return;
		}

		for (int i = 0; i < materials.Count; i++)
		{
			var m = materials[i];
			Console.WriteLine($"{i + 1}) {m.Name}");
			Console.WriteLine($"   Color: {m.Color}");
			Console.WriteLine($"   Type: {m.Type}");
			Console.WriteLine($"   Grade: {m.Grade}");
			Console.WriteLine($"   Stock: {m.AmountKg:F3} kg | ~{m.EstimatedLengthMeters:F1} m");
			Console.WriteLine($"   Avg price: {MoneyFormatter.FormatPerKg(operatingCurrency, m.AveragePricePerKgMoney.ToBase(currencies))}");
			Console.WriteLine($"   Value: {MoneyFormatter.Format(operatingCurrency, m.AmountKg * m.AveragePricePerKgMoney.ToBase(currencies))}");
			Console.WriteLine();
		}

		ConsoleEx.Pause();
	}

	private void AddSpoolPurchase()
	{
		var (currencies, settings, operatingCurrency) = LoadContext();

		Console.Clear();
		ConsoleEx.PrintHeader("Add Spool Purchase");

		var type = ReadFilamentType();

		var material = new FilamentMaterial
		{
			Id = Guid.NewGuid(),
			Name = ConsoleEx.ReadRequiredString("Name / label (example: Bambu PLA Basic Green)"),
			Color = ConsoleEx.ReadRequiredString("Color"),
			Type = type,
			Grade = ConsoleEx.ReadRequiredString("Grade / quality / note"),
			AmountKg = ConsoleEx.ReadDecimal("Amount in kg", min: 0.001m),
		};

		var suggestedLength = FilamentSizeEstimator.SuggestLengthMeters(material.Type, material.AmountKg);
		Console.WriteLine();
		Console.WriteLine($"Suggested length for {material.AmountKg:F3} kg ({material.Type}): ~{suggestedLength:F1} m");
		material.EstimatedLengthMeters = ConsoleEx.ReadDecimal($"Estimated length in meters (Enter to accept {suggestedLength:F1})", min: 0, defaultValue: suggestedLength);

		var totalPrice = ConsoleEx.ReadDecimal($"Total purchase price in {operatingCurrency?.Code ?? "(base)"}", min: 0);

		materialsService.AddSpoolAsync(material, totalPrice, settings.OperatingCurrencyId, currencies).GetAwaiter().GetResult();
		transactionsService.RecordSpoolPurchaseAsync(
			material, material.AmountKg, material.EstimatedLengthMeters,
			new Money(totalPrice, settings.OperatingCurrencyId)).GetAwaiter().GetResult();

		// Reload to get computed average
		var saved = materialsService.GetAllAsync().GetAwaiter().GetResult().FirstOrDefault(m => m.Id == material.Id);
		var avgText = saved is null ? "?" : MoneyFormatter.FormatPerKg(operatingCurrency, saved.AveragePricePerKgMoney.ToBase(currencies));
		ConsoleEx.ShowMessage($"Material added. Average price: {avgText}");
	}

	private void RestockExistingMaterial()
	{
		var material = SelectMaterial();
		if (material is null) return;

		var (currencies, settings, operatingCurrency) = LoadContext();

		Console.WriteLine();
		Console.WriteLine($"Selected: {material.Name}");
		Console.WriteLine($"Current stock: {material.AmountKg:F3} kg | ~{material.EstimatedLengthMeters:F1} m");
		Console.WriteLine($"Current average: {MoneyFormatter.FormatPerKg(operatingCurrency, material.AveragePricePerKgMoney.ToBase(currencies))}");
		Console.WriteLine();

		var addKg = ConsoleEx.ReadDecimal("Added amount in kg", min: 0.001m);
		var suggestedMeters = FilamentSizeEstimator.SuggestLengthMeters(material.Type, addKg);
		Console.WriteLine($"Suggested added length for {addKg:F3} kg ({material.Type}): ~{suggestedMeters:F1} m");
		var addMeters = ConsoleEx.ReadDecimal($"Added estimated length in meters (Enter to accept {suggestedMeters:F1})", min: 0, defaultValue: suggestedMeters);
		var addTotalPrice = ConsoleEx.ReadDecimal($"Added purchase price in {operatingCurrency?.Code ?? "(base)"}", min: 0);

		var (success, error) = materialsService.RestockAsync(material.Id, addKg, addMeters, addTotalPrice, settings.OperatingCurrencyId, currencies).GetAwaiter().GetResult();
		if (!success)
		{
			ConsoleEx.ShowMessage(error);
			return;
		}

		var updated = materialsService.GetAllAsync().GetAwaiter().GetResult().FirstOrDefault(m => m.Id == material.Id);
		var newAvg = updated is null ? "?" : MoneyFormatter.FormatPerKg(operatingCurrency, updated.AveragePricePerKgMoney.ToBase(currencies));
		ConsoleEx.ShowMessage($"Material restocked. New average: {newAvg}");
	}

	private void ConsumeMaterialManually()
	{
		var material = SelectMaterial();
		if (material is null) return;

		Console.WriteLine();
		Console.WriteLine($"Selected: {material.Name}");
		Console.WriteLine($"Available: {material.AmountKg:F3} kg | ~{material.EstimatedLengthMeters:F1} m");
		Console.WriteLine();

		var kg = ConsoleEx.ReadDecimal("How many kg to consume", min: 0.001m);
		var suggestedLength = FilamentSizeEstimator.SuggestLengthMeters(material.Type, kg);
		var meters = ConsoleEx.ReadDecimal("How many meters to consume (~)", min: 0, suggestedLength);

		var (success, error) = materialsService.ConsumeAsync(material.Id, kg, meters).GetAwaiter().GetResult();
		ConsoleEx.ShowMessage(success ? "Material deducted from stock." : error);
	}

	private void RemoveMaterial()
	{
		var materials = materialsService.GetAllAsync().GetAwaiter().GetResult();
		var (currencies, _, operatingCurrency) = LoadContext();

		Console.Clear();
		ConsoleEx.PrintHeader("Remove Material");

		if (!materials.Any())
		{
			ConsoleEx.ShowMessage("No materials to remove.");
			return;
		}

		ListMaterialsCompact(materials, currencies, operatingCurrency);
		var index = ConsoleEx.ReadInt("Enter material number to remove", 1, materials.Count) - 1;
		var removed = materials[index];

		ConsoleEx.RequestConfirmation($"Remove material '{removed.Name}'?", ConsoleEx.Severity.Critical, () =>
		{
			var (success, error) = materialsService.RemoveAsync(removed.Id).GetAwaiter().GetResult();
			ConsoleEx.ShowMessage(success ? $"Removed: {removed.Name}" : error, ConsoleEx.Severity.Critical);
		});
	}

	private (List<Currency> currencies, AppSettings settings, Currency? operatingCurrency) LoadContext()
	{
		var currencies = currenciesService.GetAllAsync().GetAwaiter().GetResult();
		var settings = settingsService.GetAsync().GetAwaiter().GetResult() ?? new AppSettings();
		var operatingCurrency = currencies.FirstOrDefault(c => c.Id == settings.OperatingCurrencyId);
		return (currencies, settings, operatingCurrency);
	}

	private static void ListMaterialsCompact(List<FilamentMaterial> materials, List<Currency> currencies, Currency? operatingCurrency)
	{
		for (int i = 0; i < materials.Count; i++)
		{
			var m = materials[i];
			Console.WriteLine($"{i + 1}) {m.Name} | {m.Color} | {m.Type} | {m.AmountKg:F3} kg | {MoneyFormatter.FormatPerKg(operatingCurrency, m.AveragePricePerKgMoney.ToBase(currencies))}");
		}
	}

	private static Enums.FilamentType ReadFilamentType()
	{
		Console.WriteLine("Select filament type:");
		var values = Enum.GetValues<Enums.FilamentType>();
		for (int i = 0; i < values.Length; i++)
		{
			Console.WriteLine($"{i + 1}) {values[i]}");
		}

		var index = ConsoleEx.ReadInt("Type", 1, values.Length) - 1;
		return values[index];
	}
}
