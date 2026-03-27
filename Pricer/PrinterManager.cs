using Pricer.Models;

using System;
using System.Linq;

namespace Pricer;

public sealed class PrinterManager(IAppDataStore store, string dataFilePath)
{
	private readonly IAppDataStore _store = store;
	private readonly string _dataFilePath = dataFilePath;

	public void Menu(AppData appData) => throw new NotSupportedException("CLI is hosted in Pricer.Cli");

	public void AddPrinter(AppData appData, Printer printer)
	{
      printer.HourlyCostMoney = new Money(printer.HourlyCost, appData.OperatingCurrencyId);
		appData.Printers.Add(printer);
		if (appData.SelectedPrinterId is null)
		{
			appData.SelectedPrinterId = printer.Id;
		}

		_store.Save(_dataFilePath, appData);
	}

   public bool SelectPrinter(AppData appData, int index, out string error)
	{
       error = string.Empty;
		if (index < 0 || index >= appData.Printers.Count)
		{
           error = "Invalid selection.";
			return false;
		}

		appData.SelectedPrinterId = appData.Printers[index].Id;
		_store.Save(_dataFilePath, appData);
		return true;
	}

   public bool RemovePrinter(AppData appData, int index, out string error)
	{
       error = string.Empty;
		if (index < 0 || index >= appData.Printers.Count)
		{
           error = "Invalid selection.";
			return false;
		}

		var removed = appData.Printers[index];
		appData.Printers.RemoveAt(index);
		if (appData.SelectedPrinterId == removed.Id)
		{
			appData.SelectedPrinterId = appData.Printers.FirstOrDefault()?.Id;
		}

		_store.Save(_dataFilePath, appData);
		return true;
	}
}
