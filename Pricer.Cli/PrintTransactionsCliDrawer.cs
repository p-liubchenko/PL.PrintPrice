using System;
using System.Linq;

namespace Pricer;

public sealed class PrintTransactionsCliDrawer
{
	public void Menu(AppData appData, PrintTransactionsManager manager)
	{
		while (true)
		{
			Console.Clear();
			ConsoleEx.PrintHeader("Print History / Transactions");

			Console.WriteLine($"Transactions: {appData.PrintTransactions.Count}");
			Console.WriteLine();
			Console.WriteLine("1) List transactions");
			Console.WriteLine("2) Revert transaction");
			Console.WriteLine("3) Delete transaction");
			Console.WriteLine("0) Back");
			Console.WriteLine();

			switch (ConsoleEx.ReadMenuChoice("Choose an option"))
			{
				case "1":
					List(appData);
					break;
				case "2":
					Revert(appData, manager);
					break;
				case "3":
					Delete(appData, manager);
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
		ConsoleEx.PrintHeader("Print Transactions");

		if (!appData.PrintTransactions.Any())
		{
			Console.WriteLine("No transactions recorded.");
			ConsoleEx.Pause();
			return;
		}

		var list = appData.PrintTransactions
			.OrderByDescending(x => x.CreatedAt)
			.Take(50)
			.ToList();

		for (int i = 0; i < list.Count; i++)
		{
			var tx = list[i];
			var status = tx.Status == PrintTransactionStatus.Reverted ? "REVERTED" : "OK";
			Console.WriteLine($"{i + 1}) {tx.CreatedAt.LocalDateTime:yyyy-MM-dd HH:mm} | {status} | {tx.MaterialNameSnapshot} | {tx.FilamentKg:F3} kg | {tx.PrintHours:F2} h | {MoneyFormatter.Format(appData, tx.TotalCost)}");
		}

		Console.WriteLine();
		Console.WriteLine("Showing latest 50.");
		ConsoleEx.Pause();
	}

	private static void Revert(AppData appData, PrintTransactionsManager manager)
	{
		Console.Clear();
		ConsoleEx.PrintHeader("Revert Transaction");

		if (!appData.PrintTransactions.Any())
		{
			ConsoleEx.ShowMessage("No transactions to revert.");
			return;
		}

		var list = appData.PrintTransactions.OrderByDescending(x => x.CreatedAt).ToList();
		for (int i = 0; i < list.Count; i++)
		{
			Console.WriteLine($"{i + 1}) {list[i].CreatedAt.LocalDateTime:yyyy-MM-dd HH:mm} | {list[i].Status} | {list[i].MaterialNameSnapshot}");
		}

		var index = ConsoleEx.ReadInt("Select transaction", 1, list.Count) - 1;
		var tx = list[index];
		if (!manager.TryRevert(appData, tx.Id, out var error))
		{
			ConsoleEx.ShowMessage(error);
			return;
		}

		ConsoleEx.ShowMessage("Transaction reverted (stock restored).");
	}

	private static void Delete(AppData appData, PrintTransactionsManager manager)
	{
		Console.Clear();
		ConsoleEx.PrintHeader("Delete Transaction");

		if (!appData.PrintTransactions.Any())
		{
			ConsoleEx.ShowMessage("No transactions to delete.");
			return;
		}

		var list = appData.PrintTransactions.OrderByDescending(x => x.CreatedAt).ToList();
		for (int i = 0; i < list.Count; i++)
		{
			Console.WriteLine($"{i + 1}) {list[i].CreatedAt.LocalDateTime:yyyy-MM-dd HH:mm} | {list[i].Status} | {list[i].MaterialNameSnapshot}");
		}

		var index = ConsoleEx.ReadInt("Select transaction", 1, list.Count) - 1;
		var tx = list[index];

		if (!manager.TryDelete(appData, tx.Id, out var error))
		{
			ConsoleEx.ShowMessage(error);
			return;
		}

		ConsoleEx.ShowMessage("Transaction deleted.");
	}
}
