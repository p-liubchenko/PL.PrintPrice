using Microsoft.EntityFrameworkCore;

using Pricer.DAL.Ef;
using Pricer.DAL.File;
using Pricer.Models;
using Pricer.Models.Transactions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pricer.DAL;

/// <summary>
/// Handles one-way data migration between the file store and the database.
/// Merge rule: entities that already exist in the destination (matched by ID) are skipped.
/// </summary>
internal static class DataMigrator
{
	/// <summary>
	/// Inserts into <paramref name="db"/> every entity from <paramref name="filePath"/>
	/// that is not already present in the database (matched by primary key).
	/// Settings are merged field-by-field: DB wins on conflict; file fills nulls/zeros.
	/// </summary>
	public static async Task MigrateFileToDbAsync(string filePath, PricerDbContext db, CancellationToken ct = default)
	{
		var data = AppDataSerializer.Load(filePath);

		// Load existing IDs so we only insert new ones.
		var existingCurrencyIds = (await db.Currencies.AsNoTracking().ToListAsync(ct)).Select(e => e.Id).ToHashSet();
		var existingPrinterIds = (await db.Printers.AsNoTracking().ToListAsync(ct)).Select(e => e.Id).ToHashSet();
		var existingMaterialIds = (await db.Materials.AsNoTracking().ToListAsync(ct)).Select(e => e.Id).ToHashSet();
		var existingPrintTxIds = (await db.PrintTransactions.AsNoTracking().ToListAsync(ct)).Select(e => e.Id).ToHashSet();
		var existingStockTxIds = (await db.StockTransactions.AsNoTracking().ToListAsync(ct)).Select(e => e.Id).ToHashSet();

		foreach (var e in data.Currencies.Where(e => !existingCurrencyIds.Contains(e.Id)))
			db.Currencies.Add(e);

		foreach (var e in data.Printers.Where(e => !existingPrinterIds.Contains(e.Id)))
			db.Printers.Add(e);

		foreach (var e in data.Materials.Where(e => !existingMaterialIds.Contains(e.Id)))
			db.Materials.Add(e);

		foreach (var e in data.PrintTransactions.Where(e => !existingPrintTxIds.Contains(e.Id)))
			db.PrintTransactions.Add(e);

		foreach (var e in data.StockTransactions.Where(e => !existingStockTxIds.Contains(e.Id)))
			db.StockTransactions.Add(e);

		await db.SaveChangesAsync(ct);

		// Settings: merge file values into DB, DB values take priority.
		var dbSettings = await db.Settings.FirstOrDefaultAsync(ct);
		if (dbSettings is null)
		{
			db.Settings.Add(data.Settings);
		}
		else
		{
			// Fill nulls/zeros in DB settings from file.
			dbSettings.SelectedPrinterId ??= data.Settings.SelectedPrinterId;
			dbSettings.OperatingCurrencyId ??= data.Settings.OperatingCurrencyId;
			if (dbSettings.ElectricityPricePerKwhMoney == default)
				dbSettings.ElectricityPricePerKwhMoney = data.Settings.ElectricityPricePerKwhMoney;
			if (dbSettings.FixedCostPerPrintMoney == default)
				dbSettings.FixedCostPerPrintMoney = data.Settings.FixedCostPerPrintMoney;
		}

		await db.SaveChangesAsync(ct);
	}

	/// <summary>
	/// Reads all data from <paramref name="db"/> and merges it into <paramref name="filePath"/>.
	/// Entities already present in the file (matched by ID) are skipped.
	/// Settings are replaced by the DB values.
	/// </summary>
	public static async Task MigrateDbToFileAsync(PricerDbContext db, string filePath, CancellationToken ct = default)
	{
		var data = AppDataSerializer.Load(filePath);

		var dbCurrencies = await db.Currencies.AsNoTracking().ToListAsync(ct);
		var dbPrinters = await db.Printers.AsNoTracking().ToListAsync(ct);
		var dbMaterials = await db.Materials.AsNoTracking().ToListAsync(ct);
		var dbPrintTxs = await db.PrintTransactions.AsNoTracking().ToListAsync(ct);
		var dbStockTxs = await db.StockTransactions.AsNoTracking().ToListAsync(ct);
		var dbSettings = await db.Settings.AsNoTracking().FirstOrDefaultAsync(ct);

		MergeIntoList(data.Currencies, dbCurrencies, e => e.Id);
		MergeIntoList(data.Printers, dbPrinters, e => e.Id);
		MergeIntoList(data.Materials, dbMaterials, e => e.Id);
		MergeIntoList(data.PrintTransactions, dbPrintTxs, e => e.Id);
		MergeIntoList(data.StockTransactions, dbStockTxs, e => e.Id);

		if (dbSettings is not null)
			data.Settings = dbSettings;

		AppDataSerializer.Save(filePath, data);
	}

	private static void MergeIntoList<T>(List<T> target, List<T> source, Func<T, Guid> getId)
	{
		var existingIds = target.Select(getId).ToHashSet();
		target.AddRange(source.Where(e => !existingIds.Contains(getId(e))));
	}
}
