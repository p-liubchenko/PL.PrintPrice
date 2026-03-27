using Pricer.Models;

using System;
using System.Linq;

// ReSharper disable once CheckNamespace

namespace Pricer;

public sealed class FilamentWarehouse(IAppDataStore store, string dataFilePath)
{
	private readonly IAppDataStore _store = store;
	private readonly string _dataFilePath = dataFilePath;

	private const decimal DefaultDiameterMm = 1.75m;

	public void Menu(AppData appData) => throw new NotSupportedException("CLI is hosted in Pricer.Cli");

	public FilamentMaterial? SelectMaterial(AppData appData) => throw new NotSupportedException("CLI is hosted in Pricer.Cli");

	public void AddSpoolPurchase(AppData appData, FilamentMaterial material, decimal totalPrice)
	{
        var avg = totalPrice / material.AmountKg;
		var currencyId = appData.OperatingCurrencyId;
		material.AveragePricePerKgMoney = new Money(avg, currencyId);
		material.AveragePricePerKg = avg;
		appData.Materials.Add(material);
		_store.Save(_dataFilePath, appData);
	}

	public bool RestockExistingMaterial(AppData appData, FilamentMaterial material, decimal addKg, decimal addMeters, decimal addTotalPrice)
	{
		if (addKg <= 0)
		{
			return false;
		}

      var currentValueBase = material.AmountKg * material.AveragePricePerKgMoney.ToBase(appData);
		var addValueBase = new Money(addTotalPrice, appData.OperatingCurrencyId).ToBase(appData);
		var newValueBase = currentValueBase + addValueBase;
		var newKg = material.AmountKg + addKg;

		material.AmountKg = newKg;
		material.EstimatedLengthMeters += addMeters;
     var newAvgBase = newKg <= 0 ? 0 : newValueBase / newKg;
		material.AveragePricePerKgMoney = Money.FromBase(appData, newAvgBase, appData.OperatingCurrencyId);
		material.AveragePricePerKg = material.AveragePricePerKgMoney.Amount;

		_store.Save(_dataFilePath, appData);
		return true;
	}

	public bool ConsumeMaterial(AppData appData, FilamentMaterial material, decimal kg, decimal meters)
	{
		if (kg <= 0)
		{
			return false;
		}

		if (kg > material.AmountKg)
		{
			return false;
		}

		if (meters > material.EstimatedLengthMeters)
		{
			return false;
		}

		material.AmountKg -= kg;
		material.EstimatedLengthMeters -= meters;

		_store.Save(_dataFilePath, appData);
		return true;
	}

	public bool RemoveMaterial(AppData appData, int index)
	{
		if (index < 0 || index >= appData.Materials.Count)
		{
			return false;
		}

		appData.Materials.RemoveAt(index);
		_store.Save(_dataFilePath, appData);
		return true;
	}

  public static decimal SuggestLengthMeters(decimal weightKg, Enums.FilamentType type, decimal diameterMm = DefaultDiameterMm)
		=> EstimateLengthMeters(weightKg, type, diameterMm);

    private static decimal EstimateLengthMeters(decimal weightKg, Enums.FilamentType type, decimal diameterMm)
	{
		var densityGcm3 = type switch
		{
          Enums.FilamentType.PLA => 1.24m,
			Enums.FilamentType.PLAPlus => 1.24m,
			Enums.FilamentType.PETG => 1.27m,
			Enums.FilamentType.ABS => 1.04m,
			Enums.FilamentType.ASA => 1.07m,
			Enums.FilamentType.TPU => 1.20m,
			_ => 1.24m
		};

		var massG = weightKg * 1000m;
		if (massG <= 0)
		{
			return 0;
		}

		// volume (cm^3) = mass (g) / density (g/cm^3)
		var volumeCm3 = massG / densityGcm3;
		// area (cm^2) for round filament with diameter (mm)
		var diameterCm = diameterMm / 10m;
		var radiusCm = diameterCm / 2m;
		var areaCm2 = (decimal)Math.PI * radiusCm * radiusCm;
		if (areaCm2 <= 0)
		{
			return 0;
		}

		// length (cm) = volume / area
		var lengthCm = volumeCm3 / areaCm2;
		return lengthCm / 100m;
	}
}