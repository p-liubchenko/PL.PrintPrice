using Pricer.Models;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Pricer;

public sealed class CurrencyManagerCliDrawer(
	ICurrenciesService currenciesService,
	ISettingsService settingsService)
{
	public void Menu()
	{
		while (true)
		{
			var currencies = currenciesService.GetAllAsync().GetAwaiter().GetResult();
			var settings = settingsService.GetAsync().GetAwaiter().GetResult() ?? new AppSettings();
			var baseCurrency = currencies.FirstOrDefault(c => c.Value == 1m);
			var operatingCurrency = currencies.FirstOrDefault(c => c.Id == settings.OperatingCurrencyId);

			Console.Clear();
			ConsoleEx.PrintHeader("Currency Management");
			ConsoleEx.ShowInline($"Base currency: {(baseCurrency is null ? "(none)" : baseCurrency.Code)}", ConsoleEx.Severity.Safe);
			ConsoleEx.ShowInline($"Operating currency: {(operatingCurrency is null ? "(none)" : operatingCurrency.Code)}", ConsoleEx.Severity.Safe);
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
				case "1": List(currencies, settings); break;
				case "2": Add(); break;
				case "3": SelectOperating(currencies); break;
				case "4": SetBase(currencies); break;
				case "5": Remove(currencies); break;
				case "0": return;
				default: ConsoleEx.ShowMessage("Unknown option."); break;
			}
		}
	}

	private static void List(List<Currency> currencies, AppSettings settings)
	{
		Console.Clear();
		ConsoleEx.PrintHeader("Currencies");

		if (!currencies.Any())
		{
			Console.WriteLine("No currencies defined.");
			ConsoleEx.Pause();
			return;
		}

		for (int i = 0; i < currencies.Count; i++)
		{
			var c = currencies[i];
			var baseMark = c.Value == 1m ? "(base)" : "";
			var opMark = settings.OperatingCurrencyId == c.Id ? "*" : " ";
			Console.WriteLine($"{opMark}{i + 1}) {c.Code} | value: {c.Value} {baseMark}");
		}

		ConsoleEx.Pause();
	}

	private void Add()
	{
		Console.Clear();
		ConsoleEx.PrintHeader("Add Currency");

		var currency = new Currency
		{
			Id = Guid.NewGuid(),
			Code = ConsoleEx.ReadRequiredString("Currency code (e.g. CZK, EUR)"),
			Value = ConsoleEx.ReadDecimal("Value (relative to base currency)", min: 0.000001m)
		};

		var (success, error) = currenciesService.AddAsync(currency).GetAwaiter().GetResult();
		ConsoleEx.ShowMessage(success ? "Currency added." : error);
	}

	private void SelectOperating(List<Currency> currencies)
	{
		Console.Clear();
		ConsoleEx.PrintHeader("Select Operating Currency");

		if (!currencies.Any())
		{
			ConsoleEx.ShowMessage("No currencies defined.");
			return;
		}

		for (int i = 0; i < currencies.Count; i++)
		{
			Console.WriteLine($"{i + 1}) {currencies[i].Code}");
		}

		var index = ConsoleEx.ReadInt("Select currency", 1, currencies.Count) - 1;
		var (success, error) = currenciesService.SetOperatingCurrencyAsync(currencies[index].Id).GetAwaiter().GetResult();
		ConsoleEx.ShowMessage(success ? "Operating currency selected." : error);
	}

	private void SetBase(List<Currency> currencies)
	{
		Console.Clear();
		ConsoleEx.PrintHeader("Set Base Currency");

		if (!currencies.Any())
		{
			ConsoleEx.ShowMessage("No currencies defined.");
			return;
		}

		for (int i = 0; i < currencies.Count; i++)
		{
			Console.WriteLine($"{i + 1}) {currencies[i].Code} (current value: {currencies[i].Value})");
		}

		var index = ConsoleEx.ReadInt("Select base currency", 1, currencies.Count) - 1;
		var (success, error) = currenciesService.SetBaseCurrencyAsync(currencies[index].Id).GetAwaiter().GetResult();
		ConsoleEx.ShowMessage(success ? "Base currency updated." : error);
	}

	private void Remove(List<Currency> currencies)
	{
		Console.Clear();
		ConsoleEx.PrintHeader("Remove Currency");

		if (!currencies.Any())
		{
			ConsoleEx.ShowMessage("No currencies to remove.");
			return;
		}

		for (int i = 0; i < currencies.Count; i++)
		{
			Console.WriteLine($"{i + 1}) {currencies[i].Code}");
		}

		var index = ConsoleEx.ReadInt("Select currency", 1, currencies.Count) - 1;
		var removedCode = currencies[index].Code;

		ConsoleEx.RequestConfirmation($"Remove currency '{removedCode}'?", ConsoleEx.Severity.Critical, () =>
		{
			var (success, error) = currenciesService.RemoveAsync(currencies[index].Id).GetAwaiter().GetResult();
			ConsoleEx.ShowMessage(success ? $"Removed: {removedCode}" : error, ConsoleEx.Severity.Critical);
		});
	}
}
