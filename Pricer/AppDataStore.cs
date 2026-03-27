using Pricer.Models;
using Pricer.Models.Transactions;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pricer;

public interface ICrudRepository<TEntity, in TKey>
	where TEntity : class
{
	Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);
	Task<List<TEntity>> GetAllAsync(CancellationToken ct = default);
	Task UpsertAsync(TEntity entity, CancellationToken ct = default);
	Task DeleteAsync(TKey id, CancellationToken ct = default);
}

public interface ISettingsService
{
	Task<AppSettings?> GetAsync(CancellationToken ct = default);
	Task UpsertAsync(AppSettings settings, CancellationToken ct = default);
	Task EnsureSeedAsync(CancellationToken ct = default);
}

public interface IPrintersService
{
	Task<List<Printer>> GetAllAsync(CancellationToken ct = default);
	Task AddAsync(Printer printer, CancellationToken ct = default);
	Task UpsertAsync(Printer printer, CancellationToken ct = default);
	Task SelectAsync(Guid printerId, CancellationToken ct = default);
	Task<(bool success, string error)> RemoveAsync(Guid id, CancellationToken ct = default);
}

public interface IMaterialsService
{
	Task<List<FilamentMaterial>> GetAllAsync(CancellationToken ct = default);
	Task UpsertAsync(FilamentMaterial material, CancellationToken ct = default);
	Task AddSpoolAsync(FilamentMaterial material, decimal totalPrice, Guid? operatingCurrencyId, IReadOnlyList<Currency> currencies, CancellationToken ct = default);
	Task<(bool success, string error)> RestockAsync(Guid materialId, decimal addKg, decimal addMeters, decimal addTotalPrice, Guid? operatingCurrencyId, IReadOnlyList<Currency> currencies, CancellationToken ct = default);
	Task<(bool success, string error)> ConsumeAsync(Guid materialId, decimal kg, decimal meters, CancellationToken ct = default);
	Task<(bool success, string error)> RemoveAsync(Guid id, CancellationToken ct = default);
}

public interface ICurrenciesService
{
	Task<List<Currency>> GetAllAsync(CancellationToken ct = default);
	Task UpsertAsync(Currency currency, CancellationToken ct = default);
	Task<(bool success, string error)> AddAsync(Currency currency, CancellationToken ct = default);
	Task<(bool success, string error)> SetBaseCurrencyAsync(Guid currencyId, CancellationToken ct = default);
	Task<(bool success, string error)> SetOperatingCurrencyAsync(Guid currencyId, CancellationToken ct = default);
	Task<(bool success, string error)> RemoveAsync(Guid currencyId, CancellationToken ct = default);
}

public interface ITransactionsService
{
	Task<List<PrintTransaction>> GetPrintTransactionsAsync(CancellationToken ct = default);
	Task<List<StockTransaction>> GetStockTransactionsAsync(CancellationToken ct = default);
	Task RecordPrintAsync(PrintCostRequest request, PrintCostResult result, Guid printerId, string printerName, Guid? operatingCurrencyId, IReadOnlyList<Currency> currencies, CancellationToken ct = default);
	Task RecordSpoolPurchaseAsync(FilamentMaterial material, decimal kgAdded, decimal metersAdded, Money totalCost, CancellationToken ct = default);
	Task<(bool success, string error)> RevertPrintAsync(Guid transactionId, CancellationToken ct = default);
	Task<(bool success, string error)> DeletePrintAsync(Guid transactionId, CancellationToken ct = default);
}
