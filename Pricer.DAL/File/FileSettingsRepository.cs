using Pricer;
using Pricer.Models;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pricer.DAL.File;

/// <summary>
/// Handles AppSettings which is stored as a single object (not an array) in data.json.
/// A generic FileRepository cannot update d.Settings via a list selector, so this dedicated
/// implementation directly assigns data.Settings on upsert.
/// </summary>
internal sealed class FileSettingsRepository(string filePath) : ICrudRepository<AppSettings, Guid>
{
	public Task<AppSettings?> GetByIdAsync(Guid id, CancellationToken ct = default)
	{
		var data = AppDataSerializer.Load(filePath);
		var settings = data.Settings;
		return Task.FromResult<AppSettings?>(settings.Id == id ? settings : null);
	}

	public Task<List<AppSettings>> GetAllAsync(CancellationToken ct = default)
	{
		var data = AppDataSerializer.Load(filePath);
		return Task.FromResult<List<AppSettings>>([data.Settings]);
	}

	public Task UpsertAsync(AppSettings entity, CancellationToken ct = default)
	{
		var data = AppDataSerializer.Load(filePath);
		data.Settings = entity;
		AppDataSerializer.Save(filePath, data);
		return Task.CompletedTask;
	}

	public Task DeleteAsync(Guid id, CancellationToken ct = default)
	{
		// Settings cannot be deleted — there is always exactly one settings object.
		return Task.CompletedTask;
	}
}
