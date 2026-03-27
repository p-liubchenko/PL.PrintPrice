using System;
using Pricer.Enums;

namespace Pricer;

public sealed class FilamentWarehouseCliDrawer
{
	public void Menu(AppData appData, FilamentWarehouse warehouse)
	{
		while (true)
		{
			Console.Clear();
			ConsoleEx.PrintHeader("Filament Warehouse");
			Console.WriteLine("1) List spools");
			Console.WriteLine("2) Add spool purchase");
			Console.WriteLine("3) Add stock to existing material (weighted average price)");
			Console.WriteLine("4) Consume material manually");
			Console.WriteLine("5) Remove spool/material entry");
			Console.WriteLine("0) Back");
			Console.WriteLine();

			switch (ConsoleEx.ReadMenuChoice("Choose an option"))
			{
				case "1":
					ListSpools(appData);
					break;
				case "2":
					AddSpoolPurchase(appData, warehouse);
					break;
				case "3":
					RestockExistingMaterial(appData, warehouse);
					break;
				case "4":
					ConsumeMaterialManually(appData, warehouse);
					break;
				case "5":
					RemoveMaterial(appData, warehouse);
					break;
				case "0":
					return;
				default:
					ConsoleEx.ShowMessage("Unknown option.");
					break;
			}
		}
	}

	public FilamentMaterial? SelectMaterial(AppData appData)
	{
		if (!appData.Materials.Any())
		{
			ConsoleEx.ShowMessage("No materials in storage yet.");
			return null;
		}

		ListMaterialsCompact(appData);
		var index = ConsoleEx.ReadInt("Select material number", 1, appData.Materials.Count) - 1;
		return appData.Materials[index];
	}

	private static void ListSpools(AppData appData)
	{
		Console.Clear();
		ConsoleEx.PrintHeader("Stored Materials");

		if (!appData.Materials.Any())
		{
			Console.WriteLine("No spools/materials in storage yet.");
			ConsoleEx.Pause();
			return;
		}

		for (int i = 0; i < appData.Materials.Count; i++)
		{
			var m = appData.Materials[i];
			Console.WriteLine($"{i + 1}) {m.Name}");
			Console.WriteLine($"   Color: {m.Color}");
			Console.WriteLine($"   Type: {m.Type}");
			Console.WriteLine($"   Grade: {m.Grade}");
			Console.WriteLine($"   Stock: {m.AmountKg:F3} kg | ~{m.EstimatedLengthMeters:F1} m");
            Console.WriteLine($"   Avg price: {MoneyFormatter.FormatPerKg(appData, m.AveragePricePerKg)}");
			Console.WriteLine($"   Value: {MoneyFormatter.Format(appData, (m.AmountKg * m.AveragePricePerKg))}");
			Console.WriteLine();
		}

		ConsoleEx.Pause();
	}

	private static void AddSpoolPurchase(AppData appData, FilamentWarehouse warehouse)
	{
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

		var suggestedLength = FilamentWarehouse.SuggestLengthMeters(material.AmountKg, material.Type);
		Console.WriteLine();
		Console.WriteLine($"Suggested length for {material.AmountKg:F3} kg ({material.Type}): ~{suggestedLength:F1} m");
		material.EstimatedLengthMeters = ConsoleEx.ReadDecimal($"Estimated length in meters (Enter to accept {suggestedLength:F1})", min: 0, defaultValue: suggestedLength);

		var totalPrice = ConsoleEx.ReadDecimal("Total purchase price in CZK", min: 0);
		warehouse.AddSpoolPurchase(appData, material, totalPrice);

		ConsoleEx.ShowMessage($"Material added. Average price: {material.AveragePricePerKg:F2} CZK/kg");
	}

	private static void RestockExistingMaterial(AppData appData, FilamentWarehouse warehouse)
	{
		Console.Clear();
		ConsoleEx.PrintHeader("Restock Existing Material");

		var material = warehouse.SelectMaterial(appData);
		if (material is null)
		{
			return;
		}

		Console.WriteLine();
		Console.WriteLine($"Selected: {material.Name}");
		Console.WriteLine($"Current stock: {material.AmountKg:F3} kg | ~{material.EstimatedLengthMeters:F1} m");
		Console.WriteLine($"Current average: {material.AveragePricePerKg:F2} CZK/kg");
		Console.WriteLine();

		var addKg = ConsoleEx.ReadDecimal("Added amount in kg", min: 0.001m);
		var suggestedMeters = FilamentWarehouse.SuggestLengthMeters(addKg, material.Type);
		Console.WriteLine($"Suggested added length for {addKg:F3} kg ({material.Type}): ~{suggestedMeters:F1} m");
		var addMeters = ConsoleEx.ReadDecimal($"Added estimated length in meters (Enter to accept {suggestedMeters:F1})", min: 0, defaultValue: suggestedMeters);
		var addTotalPrice = ConsoleEx.ReadDecimal("Added purchase price in CZK", min: 0);

		warehouse.RestockExistingMaterial(appData, material, addKg, addMeters, addTotalPrice);
		ConsoleEx.ShowMessage($"Material restocked. New average: {material.AveragePricePerKg:F2} CZK/kg");
	}

	private static void ConsumeMaterialManually(AppData appData, FilamentWarehouse warehouse)
	{
		Console.Clear();
		ConsoleEx.PrintHeader("Consume Material Manually");

		var material = warehouse.SelectMaterial(appData);
		if (material is null)
		{
			return;
		}

		Console.WriteLine();
		Console.WriteLine($"Selected: {material.Name}");
		Console.WriteLine($"Available: {material.AmountKg:F3} kg | ~{material.EstimatedLengthMeters:F1} m");
		Console.WriteLine();

		var kg = ConsoleEx.ReadDecimal("How many kg to consume", min: 0.001m);
		var meters = ConsoleEx.ReadDecimal("How many meters to consume (~)", min: 0);

		if (!warehouse.ConsumeMaterial(appData, material, kg, meters))
		{
			ConsoleEx.ShowMessage("Not enough stock.");
			return;
		}

		ConsoleEx.ShowMessage("Material deducted from stock.");
	}

	private static void RemoveMaterial(AppData appData, FilamentWarehouse warehouse)
	{
		Console.Clear();
		ConsoleEx.PrintHeader("Remove Material");

		if (!appData.Materials.Any())
		{
			ConsoleEx.ShowMessage("No materials to remove.");
			return;
		}

		ListMaterialsCompact(appData);
		var index = ConsoleEx.ReadInt("Enter material number to remove", 1, appData.Materials.Count) - 1;
		var removed = appData.Materials[index];
		warehouse.RemoveMaterial(appData, index);
		ConsoleEx.ShowMessage($"Removed: {removed.Name}");
	}

	private static void ListMaterialsCompact(AppData appData)
	{
		for (int i = 0; i < appData.Materials.Count; i++)
		{
			var m = appData.Materials[i];
			Console.WriteLine($"{i + 1}) {m.Name} | {m.Color} | {m.Type} | {m.AmountKg:F3} kg | {m.AveragePricePerKg:F2} CZK/kg");
		}
	}

  private static Pricer.Enums.FilamentType ReadFilamentType()
	{
		Console.WriteLine("Select filament type:");
        var values = Enum.GetValues<Pricer.Enums.FilamentType>();
		for (int i = 0; i < values.Length; i++)
		{
			Console.WriteLine($"{i + 1}) {values[i]}");
		}

		var index = ConsoleEx.ReadInt("Type", 1, values.Length) - 1;
		return values[index];
	}
}
