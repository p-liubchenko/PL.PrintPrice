using System;

namespace Pricer.Models;

public sealed class AppSettings
{
	public Guid Id { get; set; }
	public Money ElectricityPricePerKwhMoney { get; set; }
	public Money FixedCostPerPrintMoney { get; set; }
	public Guid? SelectedPrinterId { get; set; }
	public Guid? OperatingCurrencyId { get; set; }
}
