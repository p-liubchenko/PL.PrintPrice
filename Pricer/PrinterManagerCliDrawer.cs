using System;
using System.Linq;

namespace Pricer;

public sealed class PrinterManagerCliDrawer
{
	public void Menu(AppData store, PrinterManager manager)
	{
		while (true)
		{
			Console.Clear();
			ConsoleEx.PrintHeader("Printer Management");

			if (store.Printers.Any())
			{
				var selected = store.GetSelectedPrinter();
				Console.WriteLine($"Printers: {store.Printers.Count}");
				Console.WriteLine($"Selected: {(selected is null ? "(none)" : selected.Name)}");
				if (selected is not null)
				{
					Console.WriteLine($"  Avg power: {selected.AveragePowerWatts:F0} W");
					Console.WriteLine($"  Hourly cost: {selected.HourlyCost:F2} CZK/h");
				}
			}
			else
			{
				Console.WriteLine("No printers added yet.");
			}

			Console.WriteLine();
			Console.WriteLine("1) List printers");
			Console.WriteLine("2) Add printer");
			Console.WriteLine("3) Select printer");
			Console.WriteLine("4) Remove printer");
			Console.WriteLine("0) Back");
			Console.WriteLine();

			switch (ConsoleEx.ReadMenuChoice("Choose an option"))
			{
				case "1":
					ListPrinters(store);
					break;
				case "2":
					AddPrinter(store, manager);
					break;
				case "3":
					SelectPrinter(store, manager);
					break;
				case "4":
					RemovePrinter(store, manager);
					break;
				case "0":
					return;
				default:
					ConsoleEx.ShowMessage("Unknown option.");
					break;
			}
		}
	}

	private static void ListPrinters(AppData store)
	{
		Console.Clear();
		ConsoleEx.PrintHeader("Printers");

		if (!store.Printers.Any())
		{
			Console.WriteLine("No printers stored.");
			ConsoleEx.Pause();
			return;
		}

		var selectedId = store.SelectedPrinterId;
		for (int i = 0; i < store.Printers.Count; i++)
		{
			var p = store.Printers[i];
			var selectedMark = p.Id == selectedId ? "*" : " ";
           Console.WriteLine($"{selectedMark}{i + 1}) {p.Name} | {p.AveragePowerWatts:F0} W | {MoneyFormatter.FormatPerHour(store, p.HourlyCost)}");
		}

		ConsoleEx.Pause();
	}

	private static void AddPrinter(AppData store, PrinterManager manager)
	{
		Console.Clear();
		ConsoleEx.PrintHeader("Add Printer");

		var printer = new Printer
		{
			Id = Guid.NewGuid(),
			Name = ConsoleEx.ReadRequiredString("Printer name"),
			AveragePowerWatts = ConsoleEx.ReadDecimal("Average power draw (W)", min: 0),
			HourlyCost = ConsoleEx.ReadDecimal("Hourly overhead cost (CZK/h)", min: 0)
		};

		manager.AddPrinter(store, printer);
		ConsoleEx.ShowMessage("Printer added.");
	}

	private static void SelectPrinter(AppData store, PrinterManager manager)
	{
		Console.Clear();
		ConsoleEx.PrintHeader("Select Printer");

		if (!store.Printers.Any())
		{
			ConsoleEx.ShowMessage("No printers to select.");
			return;
		}

		for (int i = 0; i < store.Printers.Count; i++)
		{
			Console.WriteLine($"{i + 1}) {store.Printers[i].Name}");
		}

		var index = ConsoleEx.ReadInt("Select printer number", 1, store.Printers.Count) - 1;
       if (!manager.SelectPrinter(store, index))
		{
			ConsoleEx.ShowMessage("Invalid selection.");
			return;
		}

		ConsoleEx.ShowMessage("Printer selected.");
	}

	private static void RemovePrinter(AppData store, PrinterManager manager)
	{
		Console.Clear();
		ConsoleEx.PrintHeader("Remove Printer");

		if (!store.Printers.Any())
		{
			ConsoleEx.ShowMessage("No printers to remove.");
			return;
		}

		for (int i = 0; i < store.Printers.Count; i++)
		{
			Console.WriteLine($"{i + 1}) {store.Printers[i].Name}");
		}

		var index = ConsoleEx.ReadInt("Enter printer number to remove", 1, store.Printers.Count) - 1;
       var removed = store.Printers[index].Name;
		if (!manager.RemovePrinter(store, index))
		{
			ConsoleEx.ShowMessage("Invalid selection.");
			return;
		}

		ConsoleEx.ShowMessage($"Removed: {removed}");
	}
}
