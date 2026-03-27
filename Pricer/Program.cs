using Pricer.Enums;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
namespace Pricer;

public static class Program
{
	private const string DataFileName = "data.json";

 private static readonly FilamentWarehouse Warehouse = new(FileBackedStore.Default, DataFileName);
	private static readonly PrinterManager PrinterManager = new(FileBackedStore.Default, DataFileName);
	private static readonly PrintCostCalculatorCliDrawer PrintCostDrawer = new();
	private static readonly PrintTransactionsManager PrintTransactions = new(FileBackedStore.Default, DataFileName);
	private static readonly CurrencyManager CurrencyManager = new(FileBackedStore.Default, DataFileName);

	public static void Main()
	{
		Console.OutputEncoding = System.Text.Encoding.UTF8;

		var store = DataStore.Load(DataFileName);
		SeedDefaults(store);

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
				case "1":
					Warehouse.Menu(store);
					break;
				case "2":
					PrintCostMenu(store);
					break;
               case "3":
                   PrinterManager.Menu(store);
					break;
                case "4":
					CurrencyManager.Menu(store);
					break;
				case "5":
					ExpenseSettingsMenu(store);
					break;
               case "6":
                 PrintTransactions.Menu(store);
					break;
               case "7":
					ReportsMenu(store);
					break;
				case "0":
					DataStore.Save(DataFileName, store);
					return;
				default:
                 ConsoleEx.ShowMessage("Unknown option.");
					break;
			}
		}
	}


	private static void PrintCostMenu(AppData store)
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
				case "1":
                        PrintCostDrawer.Run(store, Warehouse, deductStock: false, dataFilePath: DataFileName);
					break;
				case "2":
                        PrintCostDrawer.Run(
							store,
							Warehouse,
							deductStock: true,
							dataFilePath: DataFileName,
							onDeducted: (req, res) => PrintTransactions.RecordCompletedPrint(store, req, res));
					break;
				case "0":
					DataStore.Save(DataFileName, store);
					return;
				default:
                 ConsoleEx.ShowMessage("Unknown option.");
					break;
			}
		}
	}

	private static void ExpenseSettingsMenu(AppData store)
	{
		while (true)
		{
			Console.Clear();
            ConsoleEx.PrintHeader("Expense Settings");
            Console.WriteLine($"Current electricity price: {MoneyFormatter.FormatPerKwh(store, store.Settings.ElectricityPricePerKwh)}");
           var selected = store.GetSelectedPrinter();
			Console.WriteLine($"Current selected printer: {(selected is null ? "(none)" : selected.Name)}");
         Console.WriteLine($"Current misc fixed cost per print: {MoneyFormatter.Format(store, store.Settings.FixedCostPerPrint)}");
			Console.WriteLine();
			Console.WriteLine("1) Set electricity price per kWh");
         Console.WriteLine("2) Set fixed cost per print");
			Console.WriteLine("4) Set fixed cost per print");
			Console.WriteLine("0) Back");
			Console.WriteLine();

            switch (ConsoleEx.ReadMenuChoice("Choose an option"))
			{
				case "1":
                      {
							var value = ConsoleEx.ReadDecimal($"Electricity price in {store.GetOperatingCurrency()?.Code ?? "(base)"}/kWh", min: 0);
							store.Settings.ElectricityPricePerKwhMoney = new Money(value, store.OperatingCurrencyId);
							store.Settings.ElectricityPricePerKwh = value;
						}
					DataStore.Save(DataFileName, store);
                    ConsoleEx.ShowMessage("Electricity price updated.");
					break;
				case "2":
                       {
							var value = ConsoleEx.ReadDecimal($"Fixed cost per print in {store.GetOperatingCurrency()?.Code ?? "(base)"}", min: 0);
							store.Settings.FixedCostPerPrintMoney = new Money(value, store.OperatingCurrencyId);
							store.Settings.FixedCostPerPrint = value;
						}
						DataStore.Save(DataFileName, store);
						ConsoleEx.ShowMessage("Fixed print cost updated.");
					break;
				case "4":
                        // kept for backwards-compat in case users have muscle memory
						store.Settings.FixedCostPerPrint = ConsoleEx.ReadDecimal("Fixed cost per print in CZK", min: 0);
						DataStore.Save(DataFileName, store);
						ConsoleEx.ShowMessage("Fixed print cost updated.");
					break;
				case "0":
					return;
				default:
             ConsoleEx.ShowMessage("Unknown option.");
					break;
			}
		}
	}

	private static void ReportsMenu(AppData store)
	{
		Console.Clear();
        ConsoleEx.PrintHeader("Reports / Overview");

		if (!store.Materials.Any())
		{
			Console.WriteLine("No materials stored.");
            ConsoleEx.Pause();
			return;
		}

		var totalStockKg = store.Materials.Sum(x => x.AmountKg);
		var totalStockM = store.Materials.Sum(x => x.EstimatedLengthMeters);
		var totalValue = store.Materials.Sum(x => x.AmountKg * x.AveragePricePerKg);

		Console.WriteLine($"Material entries:         {store.Materials.Count}");
		Console.WriteLine($"Total stock:              {totalStockKg:F3} kg");
		Console.WriteLine($"Estimated total length:   {totalStockM:F1} m");
        Console.WriteLine($"Estimated stock value:    {MoneyFormatter.Format(store, totalValue)}");
		Console.WriteLine();

		foreach (var group in store.Materials
					 .GroupBy(x => x.Type)
					 .OrderBy(g => g.Key))
		{
			Console.WriteLine($"[{group.Key}]");
			Console.WriteLine($"  Entries: {group.Count()}");
			Console.WriteLine($"  Stock:   {group.Sum(x => x.AmountKg):F3} kg");
         Console.WriteLine($"  Value:   {MoneyFormatter.Format(store, group.Sum(x => x.AmountKg * x.AveragePricePerKg))}");
			Console.WriteLine();
		}

        ConsoleEx.Pause();
	}



	private static void SeedDefaults(AppData store)
	{
		if (store.Settings == null)
		{
			store.Settings = new AppSettings();
		}

		if (store.Settings.ElectricityPricePerKwh <= 0)
		{
			store.Settings.ElectricityPricePerKwh = 6.50m;
		}

       if (!store.Printers.Any())
		{
			var defaultPrinter = new Printer
			{
				Id = Guid.NewGuid(),
				Name = "Default printer",
				AveragePowerWatts = 120m,
				HourlyCost = 5.00m
			};
			store.Printers.Add(defaultPrinter);
			store.SelectedPrinterId = defaultPrinter.Id;
		}

		foreach (var m in store.Materials)
		{
			if (m.Id == Guid.Empty)
			{
				m.Id = Guid.NewGuid();
			}
		}

		if (!store.Currencies.Any())
		{
			var defaultCurrency = new Currency
			{
				Id = Guid.NewGuid(),
				Code = "CZK",
				Value = 1m
			};
			store.Currencies.Add(defaultCurrency);
			store.OperatingCurrencyId = defaultCurrency.Id;
		}
		else
		{
			// Ensure there is exactly one base currency (Value == 1)
			if (store.Currencies.Count(c => c.Value == 1m) == 0)
			{
				store.Currencies[0].Value = 1m;
			}
			else if (store.Currencies.Count(c => c.Value == 1m) > 1)
			{
				var first = true;
				foreach (var c in store.Currencies.Where(c => c.Value == 1m))
				{
					if (first)
					{
						first = false;
						continue;
					}
					c.Value = 0.0001m;
				}
			}

			if (store.OperatingCurrencyId is null || store.GetOperatingCurrency() is null)
			{
				store.OperatingCurrencyId = store.Currencies.FirstOrDefault()?.Id;
			}
		}

		DataTreatment(store);
		DataStore.Save(DataFileName, store);
	}

	private static void DataTreatment(AppData store)
	{
		var baseCurrency = store.GetBaseCurrency();
		var baseCurrencyId = baseCurrency?.Id;

		// Settings
		if (store.Settings.ElectricityPricePerKwhMoney.Amount == 0 && store.Settings.ElectricityPricePerKwh > 0)
		{
			store.Settings.ElectricityPricePerKwhMoney = new Money(store.Settings.ElectricityPricePerKwh, baseCurrencyId);
		}
		store.Settings.ElectricityPricePerKwh = store.Settings.ElectricityPricePerKwhMoney.Amount;

		if (store.Settings.FixedCostPerPrintMoney.Amount == 0 && store.Settings.FixedCostPerPrint != 0)
		{
			store.Settings.FixedCostPerPrintMoney = new Money(store.Settings.FixedCostPerPrint, baseCurrencyId);
		}
		store.Settings.FixedCostPerPrint = store.Settings.FixedCostPerPrintMoney.Amount;

		// Printers
		foreach (var p in store.Printers)
		{
			if (p.HourlyCostMoney.Amount == 0 && p.HourlyCost != 0)
			{
				p.HourlyCostMoney = new Money(p.HourlyCost, baseCurrencyId);
			}
			p.HourlyCost = p.HourlyCostMoney.Amount;
		}

		// Materials
		foreach (var m in store.Materials)
		{
			if (m.AveragePricePerKgMoney.Amount == 0 && m.AveragePricePerKg != 0)
			{
				m.AveragePricePerKgMoney = new Money(m.AveragePricePerKg, baseCurrencyId);
			}
			m.AveragePricePerKg = m.AveragePricePerKgMoney.Amount;
		}
	}
}

public sealed class AppData
{
	public List<FilamentMaterial> Materials { get; set; } = new();
  public List<Printer> Printers { get; set; } = new();
	public List<PrintTransaction> PrintTransactions { get; set; } = new();
    public List<Currency> Currencies { get; set; } = new();
	public Guid? SelectedPrinterId { get; set; }
  public Guid? OperatingCurrencyId { get; set; }
	public AppSettings Settings { get; set; } = new();

	public Printer? GetSelectedPrinter()
	{
		var id = SelectedPrinterId;
		if (id is null)
		{
			return null;
		}

		return Printers.FirstOrDefault(x => x.Id == id.Value);
	}
}

public sealed class AppSettings
{
    public decimal ElectricityPricePerKwh { get; set; } = 6.50m;
	public decimal FixedCostPerPrint { get; set; } = 0m;
	public Money ElectricityPricePerKwhMoney { get; set; }
	public Money FixedCostPerPrintMoney { get; set; }
}

public sealed class Printer
{
	public Guid Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public decimal AveragePowerWatts { get; set; }
 public decimal HourlyCost { get; set; }
	public Money HourlyCostMoney { get; set; }
}

public sealed class FilamentMaterial
{
    public Guid Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Color { get; set; } = string.Empty;
    public FilamentType Type { get; set; } = FilamentType.PLA;
	public string Grade { get; set; } = string.Empty;
	public decimal AmountKg { get; set; }
	public decimal EstimatedLengthMeters { get; set; }
  public decimal AveragePricePerKg { get; set; }
	public Money AveragePricePerKgMoney { get; set; }
}

public static class DataStore
{
	public static AppData Load(string filePath)
	{
		try
		{
			if (!File.Exists(filePath))
			{
				return new AppData();
			}

			var json = File.ReadAllText(filePath);
			if (string.IsNullOrWhiteSpace(json))
			{
				return new AppData();
			}

			var data = JsonSerializer.Deserialize<AppData>(json, ProgramJson.Options);
			return data ?? new AppData();
		}
		catch
		{
			return new AppData();
		}
	}

	public static void Save(string filePath, AppData data)
	{
		var json = JsonSerializer.Serialize(data, ProgramJson.Options);
		File.WriteAllText(filePath, json);
	}
}

public static class ProgramJson
{
	public static JsonSerializerOptions Options { get; } = new()
	{
		WriteIndented = true,
		PropertyNameCaseInsensitive = true
	};
}
