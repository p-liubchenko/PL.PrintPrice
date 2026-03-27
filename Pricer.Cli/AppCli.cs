namespace Pricer;

public sealed class AppCli(
	FilamentWarehouseCliDrawer filamentWarehouseDrawer,
	PrinterManagerCliDrawer printerManagerDrawer,
	CurrencyManagerCliDrawer currencyManagerDrawer,
	PrintTransactionsCliDrawer printTransactionsDrawer,
	PrintCostCalculatorCliDrawer printCostDrawer,
	FilamentWarehouse filamentWarehouse,
	PrinterManager printerManager,
	CurrencyManager currencyManager,
	PrintTransactionsManager printTransactionsManager)
{
	private readonly FilamentWarehouseCliDrawer _filamentWarehouseDrawer = filamentWarehouseDrawer;
	private readonly PrinterManagerCliDrawer _printerManagerDrawer = printerManagerDrawer;
	private readonly CurrencyManagerCliDrawer _currencyManagerDrawer = currencyManagerDrawer;
	private readonly PrintTransactionsCliDrawer _printTransactionsDrawer = printTransactionsDrawer;
	private readonly PrintCostCalculatorCliDrawer _printCostDrawer = printCostDrawer;
	private readonly FilamentWarehouse _filamentWarehouse = filamentWarehouse;
	private readonly PrinterManager _printerManager = printerManager;
	private readonly CurrencyManager _currencyManager = currencyManager;
	private readonly PrintTransactionsManager _printTransactionsManager = printTransactionsManager;

	public void Run(AppData store, string dataFilePath)
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
				case "1":
					_filamentWarehouseDrawer.Menu(store, _filamentWarehouse);
					break;
				case "2":
					PrintCostMenu(store, dataFilePath);
					break;
				case "3":
					_printerManagerDrawer.Menu(store, _printerManager);
					break;
				case "4":
					_currencyManagerDrawer.Menu(store, _currencyManager);
					break;
				case "5":
					ExpenseSettingsMenu(store, dataFilePath);
					break;
				case "6":
					_printTransactionsDrawer.Menu(store, _printTransactionsManager);
					break;
				case "7":
					ReportsMenu(store);
					break;
				case "0":
					DataStore.Save(dataFilePath, store);
					return;
				default:
					ConsoleEx.ShowMessage("Unknown option.");
					break;
			}
		}
	}

	private void PrintCostMenu(AppData store, string dataFilePath)
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
					_printCostDrawer.Run(store, _filamentWarehouse, deductStock: false, dataFilePath, selectMaterial: _filamentWarehouseDrawer.SelectMaterial);
					break;
				case "2":
					_printCostDrawer.Run(
						store,
						_filamentWarehouse,
						deductStock: true,
						dataFilePath,
						selectMaterial: _filamentWarehouseDrawer.SelectMaterial,
						onDeducted: (req, res) => _printTransactionsManager.RecordCompletedPrint(store, req, res));
					break;
				case "0":
					DataStore.Save(dataFilePath, store);
					return;
				default:
					ConsoleEx.ShowMessage("Unknown option.");
					break;
			}
		}
	}

	private static void ExpenseSettingsMenu(AppData store, string dataFilePath)
	{
		while (true)
		{
			Console.Clear();
			ConsoleEx.PrintHeader("Expense Settings");
			Console.WriteLine($"Current electricity price: {MoneyFormatter.FormatPerKwh(store, store.Settings.ElectricityPricePerKwhMoney.ToBase(store))}");
			var selected = store.GetSelectedPrinter();
			Console.WriteLine($"Current selected printer: {(selected is null ? "(none)" : selected.Name)}");
			Console.WriteLine($"Current misc fixed cost per print: {MoneyFormatter.Format(store, store.Settings.FixedCostPerPrintMoney.ToBase(store))}");
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
					DataStore.Save(dataFilePath, store);
					ConsoleEx.ShowMessage("Electricity price updated.");
					break;
				}
				case "2":
				{
					var value = ConsoleEx.ReadDecimal($"Fixed cost per print in {store.GetOperatingCurrency()?.Code ?? "(base)"}", min: 0);
					store.Settings.FixedCostPerPrintMoney = new Money(value, store.OperatingCurrencyId);
					store.Settings.FixedCostPerPrint = value;
					DataStore.Save(dataFilePath, store);
					ConsoleEx.ShowMessage("Fixed print cost updated.");
					break;
				}
				case "4":
					store.Settings.FixedCostPerPrint = ConsoleEx.ReadDecimal("Fixed cost per print", min: 0);
					DataStore.Save(dataFilePath, store);
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
		var totalValueBase = store.Materials.Sum(x => x.AmountKg * x.AveragePricePerKgMoney.ToBase(store));

		Console.WriteLine($"Material entries:         {store.Materials.Count}");
		Console.WriteLine($"Total stock:              {totalStockKg:F3} kg");
		Console.WriteLine($"Estimated total length:   {totalStockM:F1} m");
		Console.WriteLine($"Estimated stock value:    {MoneyFormatter.Format(store, totalValueBase)}");
		Console.WriteLine();

		foreach (var group in store.Materials
				 .GroupBy(x => x.Type)
				 .OrderBy(g => g.Key))
		{
			var groupValueBase = group.Sum(x => x.AmountKg * x.AveragePricePerKgMoney.ToBase(store));
			Console.WriteLine($"[{group.Key}]");
			Console.WriteLine($"  Entries: {group.Count()}");
			Console.WriteLine($"  Stock:   {group.Sum(x => x.AmountKg):F3} kg");
			Console.WriteLine($"  Value:   {MoneyFormatter.Format(store, groupValueBase)}");
			Console.WriteLine();
		}

		ConsoleEx.Pause();
	}
}
