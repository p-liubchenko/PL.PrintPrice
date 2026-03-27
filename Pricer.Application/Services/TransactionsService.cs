using Pricer;
using Pricer.Models;
using Pricer.Models.Transactions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pricer.Application.Services;

public sealed class TransactionsService(
	ICrudRepository<PrintTransaction, Guid> printRepo,
	ICrudRepository<StockTransaction, Guid> stockRepo,
	ICrudRepository<FilamentMaterial, Guid> materialRepo) : ITransactionsService
{
	public Task<List<PrintTransaction>> GetPrintTransactionsAsync(CancellationToken ct = default)
		=> printRepo.GetAllAsync(ct);

	public Task<List<StockTransaction>> GetStockTransactionsAsync(CancellationToken ct = default)
		=> stockRepo.GetAllAsync(ct);

	public async Task RecordPrintAsync(
		PrintCostRequest request,
		PrintCostResult result,
		Guid printerId,
		string printerName,
		Guid? operatingCurrencyId,
		IReadOnlyList<Currency> currencies,
		CancellationToken ct = default)
	{
		var totalOriginal = Money.FromBase(currencies, result.Total, operatingCurrencyId);
		var materialName = string.IsNullOrWhiteSpace(request.Material.Color)
			? request.Material.Name
			: $"{request.Material.Name} ({request.Material.Color})";

		var tx = new PrintTransaction
		{
			Id = Guid.NewGuid(),
			CreatedAt = DateTimeOffset.UtcNow,
			Status = PrintTransactionStatus.Completed,
			MaterialId = request.Material.Id,
			MaterialNameSnapshot = materialName,
			FilamentKg = result.FilamentKg,
			EstimatedMetersUsed = result.EstimatedMetersUsed,
			PrinterId = printerId,
			PrinterNameSnapshot = printerName,
			PrintHours = request.PrintHours,
			MaterialCost = result.MaterialCost,
			ElectricityKwh = result.ElectricityKwh,
			ElectricityCost = result.ElectricityCost,
			PrinterWearCost = result.PrinterWearCost,
			FixedCost = result.FixedCost,
			ExtraFixedCost = result.ExtraFixedCost,
			OriginalCost = totalOriginal,
			TotalCost = totalOriginal
		};

		await printRepo.UpsertAsync(tx, ct);
	}

	public async Task RecordSpoolPurchaseAsync(
		FilamentMaterial material,
		decimal kgAdded,
		decimal metersAdded,
		Money totalCost,
		CancellationToken ct = default)
	{
		var materialName = string.IsNullOrWhiteSpace(material.Color)
			? material.Name
			: $"{material.Name} ({material.Color})";

		var tx = new StockTransaction
		{
			Id = Guid.NewGuid(),
			CreatedAt = DateTimeOffset.UtcNow,
			Type = StockTransactionType.SpoolPurchase,
			MaterialId = material.Id,
			MaterialNameSnapshot = materialName,
			KgDelta = kgAdded,
			MetersDelta = metersAdded,
			TotalCost = totalCost
		};

		await stockRepo.UpsertAsync(tx, ct);
	}

	public async Task<(bool success, string error)> RevertPrintAsync(Guid transactionId, CancellationToken ct = default)
	{
		var tx = await printRepo.GetByIdAsync(transactionId, ct);
		if (tx is null)
			return (false, "Transaction not found.");

		if (tx.Status == PrintTransactionStatus.Reverted)
			return (false, "Transaction already reverted.");

		var material = await materialRepo.GetByIdAsync(tx.MaterialId, ct);
		if (material is null)
			return (false, "Material for this transaction no longer exists.");

		material.AmountKg += tx.FilamentKg;
		material.EstimatedLengthMeters += tx.EstimatedMetersUsed;
		await materialRepo.UpsertAsync(material, ct);

		tx.Status = PrintTransactionStatus.Reverted;
		tx.RevertedAt = DateTimeOffset.UtcNow;
		await printRepo.UpsertAsync(tx, ct);

		return (true, string.Empty);
	}

	public async Task<(bool success, string error)> DeletePrintAsync(Guid transactionId, CancellationToken ct = default)
	{
		var tx = await printRepo.GetByIdAsync(transactionId, ct);
		if (tx is null)
			return (false, "Transaction not found.");

		await printRepo.DeleteAsync(transactionId, ct);
		return (true, string.Empty);
	}
}
