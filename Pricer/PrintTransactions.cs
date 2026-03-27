using System;
using System.Linq;

namespace Pricer;

public enum PrintTransactionStatus
{
	Completed,
	Reverted
}

public sealed class PrintTransaction
{
	public Guid Id { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public PrintTransactionStatus Status { get; set; }

	public Guid MaterialId { get; set; }
	public string MaterialNameSnapshot { get; set; } = string.Empty;
	public decimal FilamentKg { get; set; }
	public decimal EstimatedMetersUsed { get; set; }

	public Guid PrinterId { get; set; }
	public string PrinterNameSnapshot { get; set; } = string.Empty;
	public decimal PrintHours { get; set; }

	public decimal MaterialCost { get; set; }
	public decimal ElectricityKwh { get; set; }
	public decimal ElectricityCost { get; set; }
	public decimal PrinterWearCost { get; set; }
	public decimal FixedCost { get; set; }
	public decimal ExtraFixedCost { get; set; }
	public decimal TotalCost { get; set; }

	public string? Note { get; set; }
	public Guid? RevertedByTransactionId { get; set; }
	public DateTimeOffset? RevertedAt { get; set; }
}

public static class PrintTransactionExtensions
{
	public static PrintTransaction? GetTransaction(this AppData data, Guid id)
		=> data.PrintTransactions.FirstOrDefault(x => x.Id == id);

	public static FilamentMaterial? FindMaterialById(this AppData data, Guid id)
		=> data.Materials.FirstOrDefault(m => m.Id == id);

	public static Printer? FindPrinterById(this AppData data, Guid id)
		=> data.Printers.FirstOrDefault(p => p.Id == id);
}
