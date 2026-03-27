using Pricer;
using Pricer.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pricer.Application.Services;

public sealed class MaterialsService(ICrudRepository<FilamentMaterial, Guid> repo) : IMaterialsService
{
	public Task<List<FilamentMaterial>> GetAllAsync(CancellationToken ct = default) => repo.GetAllAsync(ct);

	public Task UpsertAsync(FilamentMaterial material, CancellationToken ct = default) => repo.UpsertAsync(material, ct);

	public async Task AddSpoolAsync(
		FilamentMaterial material,
		decimal totalPrice,
		Guid? operatingCurrencyId,
		IReadOnlyList<Currency> currencies,
		CancellationToken ct = default)
	{
		var avg = material.AmountKg > 0 ? totalPrice / material.AmountKg : 0m;
		material.AveragePricePerKgMoney = new Money(avg, operatingCurrencyId);
		await repo.UpsertAsync(material, ct);
	}

	public async Task<(bool success, string error)> RestockAsync(
		Guid materialId,
		decimal addKg,
		decimal addMeters,
		decimal addTotalPrice,
		Guid? operatingCurrencyId,
		IReadOnlyList<Currency> currencies,
		CancellationToken ct = default)
	{
		if (addKg <= 0)
			return (false, "Amount to add must be > 0.");

		var material = await repo.GetByIdAsync(materialId, ct);
		if (material is null)
			return (false, "Material not found.");

		var currentValueBase = material.AmountKg * material.AveragePricePerKgMoney.ToBase(currencies);
		var addValueBase = new Money(addTotalPrice, operatingCurrencyId).ToBase(currencies);
		var newKg = material.AmountKg + addKg;
		var newAvgBase = newKg <= 0 ? 0m : (currentValueBase + addValueBase) / newKg;

		material.AmountKg = newKg;
		material.EstimatedLengthMeters += addMeters;
		material.AveragePricePerKgMoney = Money.FromBase(currencies, newAvgBase, operatingCurrencyId);

		await repo.UpsertAsync(material, ct);
		return (true, string.Empty);
	}

	public async Task<(bool success, string error)> ConsumeAsync(
		Guid materialId,
		decimal kg,
		decimal meters,
		CancellationToken ct = default)
	{
		if (kg <= 0)
			return (false, "Amount to consume must be > 0.");

		var material = await repo.GetByIdAsync(materialId, ct);
		if (material is null)
			return (false, "Material not found.");

		if (kg > material.AmountKg)
			return (false, "Not enough filament stock.");

		if (meters > material.EstimatedLengthMeters)
			return (false, "Not enough estimated length.");

		material.AmountKg -= kg;
		material.EstimatedLengthMeters -= meters;

		await repo.UpsertAsync(material, ct);
		return (true, string.Empty);
	}

	public async Task<(bool success, string error)> RemoveAsync(Guid id, CancellationToken ct = default)
	{
		var materials = await repo.GetAllAsync(ct);
		if (materials.All(m => m.Id != id))
			return (false, "Material not found.");

		await repo.DeleteAsync(id, ct);
		return (true, string.Empty);
	}
}
