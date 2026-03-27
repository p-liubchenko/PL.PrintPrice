using System;

namespace Pricer;

public static class FilamentSizeEstimator
{
	private const decimal DefaultDiameterMm = 1.75m;

	public static decimal SuggestLengthMeters(Enums.FilamentType type, decimal weightKg, decimal diameterMm = DefaultDiameterMm)
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
		if (massG <= 0) return 0;

		var volumeCm3 = massG / densityGcm3;
		var diameterCm = diameterMm / 10m;
		var radiusCm = diameterCm / 2m;
		var areaCm2 = (decimal)Math.PI * radiusCm * radiusCm;
		if (areaCm2 <= 0) return 0;

		var lengthCm = volumeCm3 / areaCm2;
		return lengthCm / 100m;
	}
}
