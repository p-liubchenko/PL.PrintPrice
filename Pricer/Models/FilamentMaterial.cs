using Pricer.Enums;

namespace Pricer.Models;

public sealed class FilamentMaterial
{
	public Guid Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Color { get; set; } = string.Empty;
	public FilamentType Type { get; set; } = FilamentType.PLA;
	public string Grade { get; set; } = string.Empty;
	public decimal AmountKg { get; set; }
	public decimal EstimatedLengthMeters { get; set; }
	public decimal AveragePricePerKg { get; set; }
	public Money AveragePricePerKgMoney { get; set; }
}