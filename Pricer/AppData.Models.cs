using Pricer.Models;
using Pricer.Models.Transactions;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Pricer;

public sealed class AppData
{
	public List<FilamentMaterial> Materials { get; set; } = [];
	public List<Printer> Printers { get; set; } = [];
	public List<PrintTransaction> PrintTransactions { get; set; } = [];
	public List<StockTransaction> StockTransactions { get; set; } = [];
	public List<Currency> Currencies { get; set; } = [];
	/// <summary>Legacy top-level field kept for backward-compat when reading old data.json files.</summary>
	public Guid? SelectedPrinterId { get; set; }
	/// <summary>Legacy top-level field kept for backward-compat when reading old data.json files.</summary>
	public Guid? OperatingCurrencyId { get; set; }
	public AppSettings Settings { get; set; } = new();

	public Printer? GetSelectedPrinter()
	{
		var id = Settings?.SelectedPrinterId ?? SelectedPrinterId;
		return id is null ? null : Printers.FirstOrDefault(x => x.Id == id.Value);
	}
}
