using Pricer;
using Pricer.Models;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pricer.Application.Services;

public sealed class CurrenciesService(
	ICrudRepository<Currency, Guid> repo,
	ICrudRepository<AppSettings, Guid> settingsRepo) : ICurrenciesService
{
	public Task<List<Currency>> GetAllAsync(CancellationToken ct = default) => repo.GetAllAsync(ct);

	public Task UpsertAsync(Currency currency, CancellationToken ct = default) => repo.UpsertAsync(currency, ct);

	public async Task<(bool success, string error)> AddAsync(Currency currency, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(currency.Code))
			return (false, "Currency code is required.");

		currency.Code = currency.Code.Trim().ToUpperInvariant();

		if (currency.Value <= 0)
			return (false, "Currency value must be > 0.");

		var existing = await repo.GetAllAsync(ct);

		if (existing.Any(c => string.Equals(c.Code, currency.Code, StringComparison.OrdinalIgnoreCase)))
			return (false, "Currency already exists.");

		if (currency.Value == 1m && existing.Any(c => c.Value == 1m))
			return (false, "There is already a base currency with value 1. Edit it instead.");

		await repo.UpsertAsync(currency, ct);

		// Auto-select operating currency if none is set
		var settings = (await settingsRepo.GetAllAsync(ct)).FirstOrDefault();
		if (settings is not null && settings.OperatingCurrencyId is null)
		{
			settings.OperatingCurrencyId = currency.Id;
			await settingsRepo.UpsertAsync(settings, ct);
		}

		return (true, string.Empty);
	}

	public async Task<(bool success, string error)> SetBaseCurrencyAsync(Guid currencyId, CancellationToken ct = default)
	{
		var currencies = await repo.GetAllAsync(ct);
		var target = currencies.FirstOrDefault(c => c.Id == currencyId);
		if (target is null)
			return (false, "Currency not found.");

		foreach (var c in currencies)
		{
			if (c.Id == currencyId)
				c.Value = 1m;
			else if (c.Value == 1m)
				c.Value = 0.0001m;
		}

		foreach (var c in currencies)
			await repo.UpsertAsync(c, ct);

		return (true, string.Empty);
	}

	public async Task<(bool success, string error)> SetOperatingCurrencyAsync(Guid currencyId, CancellationToken ct = default)
	{
		var currencies = await repo.GetAllAsync(ct);
		if (currencies.All(c => c.Id != currencyId))
			return (false, "Currency not found.");

		var settings = (await settingsRepo.GetAllAsync(ct)).FirstOrDefault();
		if (settings is null)
			return (false, "Settings not found.");

		settings.OperatingCurrencyId = currencyId;
		await settingsRepo.UpsertAsync(settings, ct);
		return (true, string.Empty);
	}

	public async Task<(bool success, string error)> RemoveAsync(Guid currencyId, CancellationToken ct = default)
	{
		var currencies = await repo.GetAllAsync(ct);
		if (currencies.All(c => c.Id != currencyId))
			return (false, "Currency not found.");

		await repo.DeleteAsync(currencyId, ct);

		var settings = (await settingsRepo.GetAllAsync(ct)).FirstOrDefault();
		if (settings?.OperatingCurrencyId == currencyId)
		{
			var remaining = await repo.GetAllAsync(ct);
			settings.OperatingCurrencyId = remaining.FirstOrDefault()?.Id;
			await settingsRepo.UpsertAsync(settings, ct);
		}

		return (true, string.Empty);
	}
}
