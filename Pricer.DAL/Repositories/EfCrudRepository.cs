using Microsoft.EntityFrameworkCore;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pricer.DAL.Repositories;

public sealed class EfCrudRepository<TEntity, TKey>(DbContext db) : Pricer.ICrudRepository<TEntity, TKey>
	where TEntity : class
{
	public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default)
		=> await db.Set<TEntity>().FindAsync([id!], ct);

	public async Task<List<TEntity>> GetAllAsync(CancellationToken ct = default)
		=> await db.Set<TEntity>().AsNoTracking().ToListAsync(ct);

	public async Task UpsertAsync(TEntity entity, CancellationToken ct = default)
	{
		// Resolve primary key values via EF metadata so we can check existence.
		var keyValues = db.Model.FindEntityType(typeof(TEntity))!
			.FindPrimaryKey()!
			.Properties
			.Select(p => p.PropertyInfo!.GetValue(entity))
			.ToArray();

		// FindAsync checks the local tracking cache first, then the DB.
		var existing = await db.Set<TEntity>().FindAsync(keyValues, ct);
		if (existing is null)
		{
			db.Add(entity);
		}
		else
		{
			// Detach the just-loaded instance so we can attach the caller's version.
			db.Entry(existing).State = EntityState.Detached;
			db.Update(entity);
		}

		await db.SaveChangesAsync(ct);
	}

	public async Task DeleteAsync(TKey id, CancellationToken ct = default)
	{
		var entity = await db.Set<TEntity>().FindAsync([id!], ct);
		if (entity is null)
			return;
		db.Remove(entity);
		await db.SaveChangesAsync(ct);
	}
}
