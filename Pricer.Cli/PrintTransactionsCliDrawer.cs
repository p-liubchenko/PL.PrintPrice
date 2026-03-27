using Pricer.Models;
using Pricer.Models.Transactions;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Pricer;

public sealed class PrintTransactionsCliDrawer(
	ITransactionsService transactionsService,
	IMaterialsService materialsService,
	ICurrenciesService currenciesService,
	ISettingsService settingsService)
{
	public void Menu()
	{
		while (true)
		{
			var printTxs = transactionsService.GetPrintTransactionsAsync().GetAwaiter().GetResult();

			Console.Clear();
			ConsoleEx.PrintHeader("Print History / Transactions");
			Console.WriteLine($"Transactions: {printTxs.Count}");
			Console.WriteLine();
			ConsoleEx.DrawMenuItem("1) List transactions");
			ConsoleEx.DrawMenuItem("2) Revert transaction");
			ConsoleEx.DrawMenuItem("3) Delete transaction", ConsoleEx.Severity.Critical);
			ConsoleEx.DrawMenuItem("0) Back");
			Console.WriteLine();

			switch (ConsoleEx.ReadMenuChoice("Choose an option"))
			{
				case "1": ListAll(); break;
				case "2": Revert(printTxs); break;
				case "3": Delete(printTxs); break;
				case "0": return;
				default: ConsoleEx.ShowMessage("Unknown option."); break;
			}
		}
	}

	private void ListAll()
	{
		var printTxs = transactionsService.GetPrintTransactionsAsync().GetAwaiter().GetResult();
		var stockTxs = transactionsService.GetStockTransactionsAsync().GetAwaiter().GetResult();
		var materials = materialsService.GetAllAsync().GetAwaiter().GetResult();
		var (currencies, settings, operatingCurrency) = LoadContext();

		Console.Clear();
		ConsoleEx.PrintHeader("Transactions");

		if (!printTxs.Any() && !stockTxs.Any())
		{
			Console.WriteLine("No transactions recorded.");
			ConsoleEx.Pause();
			return;
		}

		var printLines = printTxs.Select(tx => BuildPrintLine(tx, currencies, operatingCurrency, materials));
		var stockLines = stockTxs.Select(tx => BuildStockLine(tx, currencies, operatingCurrency, materials));

		var merged = printLines
			.Concat(stockLines)
			.OrderByDescending(x => x.CreatedAt)
			.Take(50)
			.ToList();

		for (int i = 0; i < merged.Count; i++)
		{
			RenderMergedLine(i + 1, merged[i]);
		}

		Console.WriteLine();
		Console.WriteLine("Showing latest 50.");
		ConsoleEx.Pause();
	}

	private static void RenderMergedLine(int index, TransactionLine line)
	{
		Console.Write($"{index}) {line.Prefix} | ");
		ConsoleEx.WriteInline(line.KgText.PadLeft(9), line.KgSeverity);
		Console.Write(" | ");
		Console.Write(line.Tail);
		Console.Write(" | ");
		ConsoleEx.WriteInline(line.MoneyText, line.MoneySeverity);
		Console.WriteLine();
	}

	private readonly record struct TransactionLine(
		DateTimeOffset CreatedAt,
		string Prefix,
		string KgText,
		ConsoleEx.Severity KgSeverity,
		string Tail,
		string MoneyText,
		ConsoleEx.Severity MoneySeverity);

	private static TransactionLine BuildPrintLine(PrintTransaction tx, List<Currency> currencies, Currency? operatingCurrency, List<FilamentMaterial> materials)
	{
		var status = tx.Status == PrintTransactionStatus.Reverted ? "REVERTED" : "OK";
		var totalBase = tx.TotalCost?.ToBase(currencies) ?? 0;
		var moneyText = MoneyFormatter.Format(operatingCurrency, totalBase);
		var materialDisplay = GetMaterialDisplayName(materials, tx.MaterialId, tx.MaterialNameSnapshot);

		return new TransactionLine(
			CreatedAt: tx.CreatedAt,
			Prefix: $"{tx.CreatedAt.LocalDateTime:yyyy-MM-dd HH:mm} | PRINT | {status} | {materialDisplay}",
			KgText: $"-{tx.FilamentKg:F3} kg",
			KgSeverity: ConsoleEx.Severity.Critical,
			Tail: $"{tx.PrintHours:F2} h",
			MoneyText: moneyText,
			MoneySeverity: ConsoleEx.Severity.Unsafe);
	}

	private static TransactionLine BuildStockLine(StockTransaction tx, List<Currency> currencies, Currency? operatingCurrency, List<FilamentMaterial> materials)
	{
		var costBase = tx.TotalCost?.ToBase(currencies) ?? 0;
		var moneyText = MoneyFormatter.Format(operatingCurrency, costBase);
		var materialDisplay = GetMaterialDisplayName(materials, tx.MaterialId, tx.MaterialNameSnapshot);
		var signKg = tx.KgDelta >= 0 ? "+" : "";
		var signM = tx.MetersDelta >= 0 ? "+" : "";
		var kgSeverity = tx.KgDelta >= 0 ? ConsoleEx.Severity.Safe : ConsoleEx.Severity.Critical;
		var moneySeverity = tx.Type == StockTransactionType.SpoolPurchase || tx.Type == StockTransactionType.Restock
			? ConsoleEx.Severity.Unsafe
			: ConsoleEx.Severity.Safe;

		return new TransactionLine(
			CreatedAt: tx.CreatedAt,
			Prefix: $"{tx.CreatedAt.LocalDateTime:yyyy-MM-dd HH:mm} | STOCK | {tx.Type} | {materialDisplay}",
			KgText: $"{signKg}{tx.KgDelta:F3} kg",
			KgSeverity: kgSeverity,
			Tail: $"{signM}{tx.MetersDelta:F1} m",
			MoneyText: moneyText,
			MoneySeverity: moneySeverity);
	}

	private static string GetMaterialDisplayName(List<FilamentMaterial> materials, Guid materialId, string fallback)
	{
		var material = materials.FirstOrDefault(m => m.Id == materialId);
		if (material is null) return fallback;
		return string.IsNullOrWhiteSpace(material.Color) ? material.Name : $"{material.Name} ({material.Color})";
	}

	private void Revert(List<PrintTransaction> printTxs)
	{
		Console.Clear();
		ConsoleEx.PrintHeader("Revert Transaction");

		if (!printTxs.Any())
		{
			ConsoleEx.ShowMessage("No transactions to revert.");
			return;
		}

		var list = printTxs.OrderByDescending(x => x.CreatedAt).ToList();
		for (int i = 0; i < list.Count; i++)
		{
			Console.WriteLine($"{i + 1}) {list[i].CreatedAt.LocalDateTime:yyyy-MM-dd HH:mm} | {list[i].Status} | {list[i].MaterialNameSnapshot}");
		}

		var index = ConsoleEx.ReadInt("Select transaction", 1, list.Count) - 1;
		var tx = list[index];

		ConsoleEx.RequestConfirmation("Revert this transaction and restore stock?", ConsoleEx.Severity.Unsafe, () =>
		{
			var (success, error) = transactionsService.RevertPrintAsync(tx.Id).GetAwaiter().GetResult();
			ConsoleEx.ShowMessage(success ? "Transaction reverted (stock restored)." : error, ConsoleEx.Severity.Unsafe);
		});
	}

	private void Delete(List<PrintTransaction> printTxs)
	{
		Console.Clear();
		ConsoleEx.PrintHeader("Delete Transaction");

		if (!printTxs.Any())
		{
			ConsoleEx.ShowMessage("No transactions to delete.");
			return;
		}

		var list = printTxs.OrderByDescending(x => x.CreatedAt).ToList();
		for (int i = 0; i < list.Count; i++)
		{
			Console.WriteLine($"{i + 1}) {list[i].CreatedAt.LocalDateTime:yyyy-MM-dd HH:mm} | {list[i].Status} | {list[i].MaterialNameSnapshot}");
		}

		var index = ConsoleEx.ReadInt("Select transaction", 1, list.Count) - 1;
		var tx = list[index];

		ConsoleEx.RequestConfirmation("Delete this transaction?", ConsoleEx.Severity.Critical, () =>
		{
			var (success, error) = transactionsService.DeletePrintAsync(tx.Id).GetAwaiter().GetResult();
			ConsoleEx.ShowMessage(success ? "Transaction deleted." : error, ConsoleEx.Severity.Critical);
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
