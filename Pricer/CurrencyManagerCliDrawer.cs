using System;
using System.Linq;

namespace Pricer;

public sealed class CurrencyManagerCliDrawer
{
	public void Menu(AppData appData, CurrencyManager manager)
	{
		while (true)
		{
			Console.Clear();
			ConsoleEx.PrintHeader("Currency Management");

			var baseCurrency = appData.Currencies.FirstOrDefault(c => c.Value == 1m);
			var operating = appData.GetOperatingCurrency();
			Console.WriteLine($"Base currency: {(baseCurrency is null ? "(none)" : baseCurrency.Code)}");
			Console.WriteLine($"Operating currency: {(operating is null ? "(none)" : operating.Code)}");
			Console.WriteLine();

			Console.WriteLine("1) List currencies");
			Console.WriteLine("2) Add currency");
			Console.WriteLine("3) Select operating currency");
			Console.WriteLine("4) Set base currency (value=1)");
			Console.WriteLine("5) Remove currency");
			Console.WriteLine("0) Back");
			Console.WriteLine();

			switch (ConsoleEx.ReadMenuChoice("Choose an option"))
			{
				case "1":
					List(appData);
					break;
				case "2":
					Add(appData, manager);
					break;
				case "3":
					SelectOperating(appData, manager);
					break;
				case "4":
					SetBase(appData, manager);
					break;
				case "5":
					Remove(appData, manager);
					break;
				case "0":
					return;
				default:
					ConsoleEx.ShowMessage("Unknown option.");
					break;
			}
		}
	}

	private static void List(AppData appData)
	{
		Console.Clear();
		ConsoleEx.PrintHeader("Currencies");

		if (!appData.Currencies.Any())
		{
			Console.WriteLine("No currencies defined.");
			ConsoleEx.Pause();
			return;
		}

		var operating = appData.GetOperatingCurrency();
		for (int i = 0; i < appData.Currencies.Count; i++)
		{
			var c = appData.Currencies[i];
			var baseMark = c.Value == 1m ? "(base)" : "";
			var opMark = operating?.Id == c.Id ? "*" : " ";
			Console.WriteLine($"{opMark}{i + 1}) {c.Code} | value: {c.Value} {baseMark}");
		}

		ConsoleEx.Pause();
	}

	private static void Add(AppData appData, CurrencyManager manager)
	{
		Console.Clear();
		ConsoleEx.PrintHeader("Add Currency");

		var currency = new Currency
		{
			Id = Guid.NewGuid(),
			Code = ConsoleEx.ReadRequiredString("Currency code (e.g. CZK, EUR)"),
			Value = ConsoleEx.ReadDecimal("Value (relative to base currency)", min: 0.000001m)
		};

		if (!manager.AddCurrency(appData, currency, out var error))
		{
			ConsoleEx.ShowMessage(error);
			return;
		}

		ConsoleEx.ShowMessage("Currency added.");
	}

	private static void SelectOperating(AppData appData, CurrencyManager manager)
	{
		Console.Clear();
		ConsoleEx.PrintHeader("Select Operating Currency");

		if (!appData.Currencies.Any())
		{
			ConsoleEx.ShowMessage("No currencies defined.");
			return;
		}

		for (int i = 0; i < appData.Currencies.Count; i++)
		{
			Console.WriteLine($"{i + 1}) {appData.Currencies[i].Code}");
		}

		var index = ConsoleEx.ReadInt("Select currency", 1, appData.Currencies.Count) - 1;
		if (!manager.SelectOperatingCurrency(appData, index, out var error))
		{
			ConsoleEx.ShowMessage(error);
			return;
		}

		ConsoleEx.ShowMessage("Operating currency selected.");
	}

	private static void SetBase(AppData appData, CurrencyManager manager)
	{
		Console.Clear();
		ConsoleEx.PrintHeader("Set Base Currency");

		if (!appData.Currencies.Any())
		{
			ConsoleEx.ShowMessage("No currencies defined.");
			return;
		}

		for (int i = 0; i < appData.Currencies.Count; i++)
		{
			Console.WriteLine($"{i + 1}) {appData.Currencies[i].Code} (current value: {appData.Currencies[i].Value})");
		}

		var index = ConsoleEx.ReadInt("Select base currency", 1, appData.Currencies.Count) - 1;
		var currencyId = appData.Currencies[index].Id;
		if (!manager.SetBaseCurrency(appData, currencyId, out var error))
		{
			ConsoleEx.ShowMessage(error);
			return;
		}

		ConsoleEx.ShowMessage("Base currency updated.");
	}

	private static void Remove(AppData appData, CurrencyManager manager)
	{
		Console.Clear();
		ConsoleEx.PrintHeader("Remove Currency");

		if (!appData.Currencies.Any())
		{
			ConsoleEx.ShowMessage("No currencies to remove.");
			return;
		}

		for (int i = 0; i < appData.Currencies.Count; i++)
		{
			Console.WriteLine($"{i + 1}) {appData.Currencies[i].Code}");
		}

		var index = ConsoleEx.ReadInt("Select currency", 1, appData.Currencies.Count) - 1;
		var removedCode = appData.Currencies[index].Code;
		if (!manager.RemoveCurrency(appData, index, out var error))
		{
			ConsoleEx.ShowMessage(error);
			return;
		}

		ConsoleEx.ShowMessage($"Removed: {removedCode}");
	}
}
