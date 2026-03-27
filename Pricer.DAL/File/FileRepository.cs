using Pricer;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pricer.DAL.File;

internal sealed class FileRepository<TEntity, TKey>(
	string filePath,
	Func<AppData, List<TEntity>> selector,
	Func<TEntity, TKey> getId) : ICrudRepository<TEntity, TKey>
	where TEntity : class
{
	public Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default)
	{
		var data = AppDataSerializer.Load(filePath);
		return Task.FromResult(selector(data).FirstOrDefault(e => Equals(getId(e), id)));
	}

	public Task<List<TEntity>> GetAllAsync(CancellationToken ct = default)
	{
		var data = AppDataSerializer.Load(filePath);
		return Task.FromResult(selector(data).ToList());
	}

	public Task UpsertAsync(TEntity entity, CancellationToken ct = default)
	{
		var data = AppDataSerializer.Load(filePath);
		var list = selector(data);
		var id = getId(entity);
		var idx = list.FindIndex(e => Equals(getId(e), id));
		if (idx >= 0)
			list[idx] = entity;
		else
			list.Add(entity);
		AppDataSerializer.Save(filePath, data);
		return Task.CompletedTask;
	}

	public Task DeleteAsync(TKey id, CancellationToken ct = default)
	{
		var data = AppDataSerializer.Load(filePath);
		var list = selector(data);
		var idx = list.FindIndex(e => Equals(getId(e), id));
		if (idx >= 0)
		{
			list.RemoveAt(idx);
			AppDataSerializer.Save(filePath, data);
		}
		return Task.CompletedTask;
	}
}
