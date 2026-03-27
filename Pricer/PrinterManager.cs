using System;
using System.Linq;

namespace Pricer;

public sealed class PrinterManager(IAppDataStore store, string dataFilePath)
{
	private readonly IAppDataStore _store = store;
	private readonly string _dataFilePath = dataFilePath;
	private readonly PrinterManagerCliDrawer _drawer = new();

	public void Menu(AppData appData) => _drawer.Menu(appData, this);

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

	public bool SelectPrinter(AppData appData, int index)
	{
		if (index < 0 || index >= appData.Printers.Count)
		{
			return false;
		}

		appData.SelectedPrinterId = appData.Printers[index].Id;
		_store.Save(_dataFilePath, appData);
		return true;
	}

	public bool RemovePrinter(AppData appData, int index)
	{
		if (index < 0 || index >= appData.Printers.Count)
		{
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
