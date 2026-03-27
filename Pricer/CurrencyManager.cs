using System;
using System.Linq;

namespace Pricer;

public sealed class CurrencyManager(IAppDataStore store, string dataFilePath)
{
	private readonly IAppDataStore _store = store;
	private readonly string _dataFilePath = dataFilePath;

	public void Menu(AppData appData) => throw new NotSupportedException("CLI is hosted in Pricer.Cli");

	public bool AddCurrency(AppData appData, Currency currency, out string error)
	{
		error = string.Empty;
		if (string.IsNullOrWhiteSpace(currency.Code))
		{
			error = "Currency code is required.";
			return false;
		}

		currency.Code = currency.Code.Trim().ToUpperInvariant();
		if (appData.Currencies.Any(c => string.Equals(c.Code, currency.Code, StringComparison.OrdinalIgnoreCase)))
		{
			error = "Currency already exists.";
			return false;
		}

		if (currency.Value <= 0)
		{
			error = "Currency value must be > 0.";
			return false;
		}

		// Only one base currency (Value == 1)
		if (currency.Value == 1m && appData.Currencies.Any(c => c.Value == 1m))
		{
			error = "There is already a base currency with value 1. Edit it instead.";
			return false;
		}

		appData.Currencies.Add(currency);
		if (appData.OperatingCurrencyId is null)
		{
			appData.OperatingCurrencyId = currency.Id;
		}

		_store.Save(_dataFilePath, appData);
		return true;
	}

	public bool SetBaseCurrency(AppData appData, Guid currencyId, out string error)
	{
		error = string.Empty;
		var currency = appData.Currencies.FirstOrDefault(c => c.Id == currencyId);
		if (currency is null)
		{
			error = "Currency not found.";
			return false;
		}

		foreach (var c in appData.Currencies)
		{
			if (c.Id == currencyId)
			{
				c.Value = 1m;
			}
			else
			{
				// Ensure no other currency stays at exactly 1
				if (c.Value == 1m)
				{
					c.Value = 0.0001m;
				}
			}
		}

		_store.Save(_dataFilePath, appData);
		return true;
	}

	public bool SelectOperatingCurrency(AppData appData, int index, out string error)
	{
		error = string.Empty;
		if (index < 0 || index >= appData.Currencies.Count)
		{
			error = "Invalid selection.";
			return false;
		}

		appData.OperatingCurrencyId = appData.Currencies[index].Id;
		_store.Save(_dataFilePath, appData);
		return true;
	}

	public bool RemoveCurrency(AppData appData, int index, out string error)
	{
		error = string.Empty;
		if (index < 0 || index >= appData.Currencies.Count)
		{
			error = "Invalid selection.";
			return false;
		}

		var removed = appData.Currencies[index];
		appData.Currencies.RemoveAt(index);
		if (appData.OperatingCurrencyId == removed.Id)
		{
			appData.OperatingCurrencyId = appData.Currencies.FirstOrDefault()?.Id;
		}

		_store.Save(_dataFilePath, appData);
		return true;
	}
}
