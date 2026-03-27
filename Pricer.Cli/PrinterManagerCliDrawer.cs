using Pricer.Models;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Pricer;

public sealed class PrinterManagerCliDrawer(
	IPrintersService printersService,
	ICurrenciesService currenciesService,
	ISettingsService settingsService)
{
	public void Menu()
	{
		while (true)
		{
			var printers = printersService.GetAllAsync().GetAwaiter().GetResult();
			var (currencies, settings, operatingCurrency) = LoadContext();
			var selectedPrinter = printers.FirstOrDefault(p => p.Id == settings.SelectedPrinterId);

			Console.Clear();
			ConsoleEx.PrintHeader("Printer Management");

			if (printers.Any())
			{
				Console.WriteLine($"Printers: {printers.Count}");
				ConsoleEx.ShowInline($"Selected: {(selectedPrinter is null ? "(none)" : selectedPrinter.Name)}", ConsoleEx.Severity.Safe);
				if (selectedPrinter is not null)
				{
					Console.WriteLine($"  Avg power: {selectedPrinter.AveragePowerWatts:F0} W");
					Console.WriteLine($"  Hourly cost: {MoneyFormatter.FormatPerHour(operatingCurrency, selectedPrinter.HourlyCostMoney.ToBase(currencies))}");
				}
			}
			else
			{
				Console.WriteLine("No printers added yet.");
			}

			Console.WriteLine();
			ConsoleEx.DrawMenuItem("1) List printers");
			ConsoleEx.DrawMenuItem("2) Add printer");
			ConsoleEx.DrawMenuItem("3) Select printer");
			ConsoleEx.DrawMenuItem("4) Remove printer", ConsoleEx.Severity.Unsafe);
			ConsoleEx.DrawMenuItem("0) Back");
			Console.WriteLine();

			switch (ConsoleEx.ReadMenuChoice("Choose an option"))
			{
				case "1": ListPrinters(printers, currencies, operatingCurrency, settings); break;
				case "2": AddPrinter(settings, operatingCurrency); break;
				case "3": SelectPrinter(printers); break;
				case "4": RemovePrinter(printers); break;
				case "0": return;
				default: ConsoleEx.ShowMessage("Unknown option."); break;
			}
		}
	}

	private static void ListPrinters(List<Printer> printers, List<Currency> currencies, Currency? operatingCurrency, AppSettings settings)
	{
		Console.Clear();
		ConsoleEx.PrintHeader("Printers");

		if (!printers.Any())
		{
			Console.WriteLine("No printers stored.");
			ConsoleEx.Pause();
			return;
		}

		for (int i = 0; i < printers.Count; i++)
		{
			var p = printers[i];
			var selectedMark = p.Id == settings.SelectedPrinterId ? "*" : " ";
			Console.WriteLine($"{selectedMark}{i + 1}) {p.Name} | {p.AveragePowerWatts:F0} W | {MoneyFormatter.FormatPerHour(operatingCurrency, p.HourlyCostMoney.ToBase(currencies))}");
		}

		ConsoleEx.Pause();
	}

	private void AddPrinter(AppSettings settings, Currency? operatingCurrency)
	{
		Console.Clear();
		ConsoleEx.PrintHeader("Add Printer");

		var printer = new Printer
		{
			Id = Guid.NewGuid(),
			Name = ConsoleEx.ReadRequiredString("Printer name"),
			AveragePowerWatts = ConsoleEx.ReadDecimal("Average power draw (W)", min: 0),
			HourlyCostMoney = new Money(
				ConsoleEx.ReadDecimal($"Hourly overhead cost ({operatingCurrency?.Code ?? "(base)"}/h)", min: 0),
				settings.OperatingCurrencyId)
		};

		printersService.AddAsync(printer).GetAwaiter().GetResult();
		ConsoleEx.ShowMessage("Printer added.");
	}

	private void SelectPrinter(List<Printer> printers)
	{
		Console.Clear();
		ConsoleEx.PrintHeader("Select Printer");

		if (!printers.Any())
		{
			ConsoleEx.ShowMessage("No printers to select.");
			return;
		}

		for (int i = 0; i < printers.Count; i++)
		{
			Console.WriteLine($"{i + 1}) {printers[i].Name}");
		}

		var index = ConsoleEx.ReadInt("Select printer number", 1, printers.Count) - 1;
		printersService.SelectAsync(printers[index].Id).GetAwaiter().GetResult();
		ConsoleEx.ShowMessage("Printer selected.");
	}

	private void RemovePrinter(List<Printer> printers)
	{
		Console.Clear();
		ConsoleEx.PrintHeader("Remove Printer");

		if (!printers.Any())
		{
			ConsoleEx.ShowMessage("No printers to remove.");
			return;
		}

		for (int i = 0; i < printers.Count; i++)
		{
			Console.WriteLine($"{i + 1}) {printers[i].Name}");
		}

		var index = ConsoleEx.ReadInt("Enter printer number to remove", 1, printers.Count) - 1;
		var removed = printers[index].Name;

		ConsoleEx.RequestConfirmation($"Remove printer '{removed}'?", ConsoleEx.Severity.Critical, () =>
		{
			var (success, error) = printersService.RemoveAsync(printers[index].Id).GetAwaiter().GetResult();
			ConsoleEx.ShowMessage(success ? $"Removed: {removed}" : error, ConsoleEx.Severity.Critical);
		});
	}

	private (List<Currency> currencies, AppSettings settings, Currency? operatingCurrency) LoadContext()
	{
		var currencies = currenciesService.GetAllAsync().GetAwaiter().GetResult();
		var settings = settingsService.GetAsync().GetAwaiter().GetResult() ?? new AppSettings();
		var operatingCurrency = currencies.FirstOrDefault(c => c.Id == settings.OperatingCurrencyId);
		return (currencies, settings, operatingCurrency);
	}
}
