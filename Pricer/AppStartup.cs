using Pricer.Models;

using System;
using System.Linq;

namespace Pricer;

public sealed class AppStartup(IAppDataStore store)
{
	private readonly IAppDataStore _store = store;

	public AppData LoadAndTreat(string dataFilePath)
	{
		var appData = _store.Load(dataFilePath);
		SeedDefaultsAndMigrate(appData);
		_store.Save(dataFilePath, appData);
		return appData;
	}

	public static void SeedDefaultsAndMigrate(AppData store)
	{
		if (store.Settings is null)
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
			defaultPrinter.HourlyCostMoney = new Money(defaultPrinter.HourlyCost, store.OperatingCurrencyId);
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
	}

	private static void DataTreatment(AppData store)
	{
		var baseCurrencyId = store.GetBaseCurrency()?.Id;

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

		foreach (var p in store.Printers)
		{
			if (p.HourlyCostMoney.Amount == 0 && p.HourlyCost != 0)
			{
				p.HourlyCostMoney = new Money(p.HourlyCost, baseCurrencyId);
			}
			p.HourlyCost = p.HourlyCostMoney.Amount;
		}

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
