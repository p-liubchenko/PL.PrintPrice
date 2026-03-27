using Pricer;
using Pricer.Models;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pricer.Application.Services;

public sealed class SettingsService(
	ICrudRepository<AppSettings, Guid> repo,
	ICrudRepository<Currency, Guid> currencyRepo,
	ICrudRepository<Printer, Guid> printerRepo) : ISettingsService
{
	public async Task<AppSettings?> GetAsync(CancellationToken ct = default)
		=> (await repo.GetAllAsync(ct)).FirstOrDefault();

	public Task UpsertAsync(AppSettings settings, CancellationToken ct = default)
		=> repo.UpsertAsync(settings, ct);

	public async Task EnsureSeedAsync(CancellationToken ct = default)
	{
		var settings = (await repo.GetAllAsync(ct)).FirstOrDefault();
		var currencies = await currencyRepo.GetAllAsync(ct);
		var printers = await printerRepo.GetAllAsync(ct);

		var isNew = settings is null;
		settings ??= new AppSettings { Id = Guid.NewGuid() };

		if (!currencies.Any())
		{
			var defaultCurrency = new Currency { Id = Guid.NewGuid(), Code = "CZK", Value = 1m };
			await currencyRepo.UpsertAsync(defaultCurrency, ct);
			currencies = [defaultCurrency];
			settings.OperatingCurrencyId ??= defaultCurrency.Id;
		}
		else
		{
			var baseCurrencies = currencies.Where(c => c.Value == 1m).ToList();
			if (baseCurrencies.Count == 0)
			{
				currencies[0].Value = 1m;
				await currencyRepo.UpsertAsync(currencies[0], ct);
			}
			else if (baseCurrencies.Count > 1)
			{
				foreach (var extra in baseCurrencies.Skip(1))
				{
					extra.Value = 0.0001m;
					await currencyRepo.UpsertAsync(extra, ct);
				}
			}
			settings.OperatingCurrencyId ??= currencies.FirstOrDefault()?.Id;
		}

		var baseCurrencyId = currencies.FirstOrDefault(c => c.Value == 1m)?.Id;
		if (settings.ElectricityPricePerKwhMoney.Amount == 0)
			settings.ElectricityPricePerKwhMoney = new Money(6.50m, baseCurrencyId);
		if (settings.FixedCostPerPrintMoney.Amount == 0)
			settings.FixedCostPerPrintMoney = new Money(0m, baseCurrencyId);

		if (!printers.Any())
		{
			var defaultPrinter = new Printer
			{
				Id = Guid.NewGuid(),
				Name = "Default printer",
				AveragePowerWatts = 120m,
				HourlyCostMoney = new Money(5.00m, baseCurrencyId)
			};
			await printerRepo.UpsertAsync(defaultPrinter, ct);
			settings.SelectedPrinterId ??= defaultPrinter.Id;
		}
		else
		{
			settings.SelectedPrinterId ??= printers.FirstOrDefault()?.Id;
		}

		await repo.UpsertAsync(settings, ct);
	}
}
