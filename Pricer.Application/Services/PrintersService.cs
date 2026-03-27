using Pricer;
using Pricer.Models;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pricer.Application.Services;

public sealed class PrintersService(
	ICrudRepository<Printer, Guid> repo,
	ICrudRepository<AppSettings, Guid> settingsRepo) : IPrintersService
{
	public Task<List<Printer>> GetAllAsync(CancellationToken ct = default) => repo.GetAllAsync(ct);

	public Task UpsertAsync(Printer printer, CancellationToken ct = default) => repo.UpsertAsync(printer, ct);

	public async Task AddAsync(Printer printer, CancellationToken ct = default)
	{
		await repo.UpsertAsync(printer, ct);

		var settings = (await settingsRepo.GetAllAsync(ct)).FirstOrDefault();
		if (settings is not null && settings.SelectedPrinterId is null)
		{
			settings.SelectedPrinterId = printer.Id;
			await settingsRepo.UpsertAsync(settings, ct);
		}
	}

	public async Task SelectAsync(Guid printerId, CancellationToken ct = default)
	{
		var settings = (await settingsRepo.GetAllAsync(ct)).FirstOrDefault();
		if (settings is null) return;
		settings.SelectedPrinterId = printerId;
		await settingsRepo.UpsertAsync(settings, ct);
	}

	public async Task<(bool success, string error)> RemoveAsync(Guid id, CancellationToken ct = default)
	{
		var printers = await repo.GetAllAsync(ct);
		if (printers.All(p => p.Id != id))
			return (false, "Printer not found.");

		await repo.DeleteAsync(id, ct);

		var settings = (await settingsRepo.GetAllAsync(ct)).FirstOrDefault();
		if (settings?.SelectedPrinterId == id)
		{
			var remaining = await repo.GetAllAsync(ct);
			settings.SelectedPrinterId = remaining.FirstOrDefault()?.Id;
			await settingsRepo.UpsertAsync(settings, ct);
		}

		return (true, string.Empty);
	}
}
