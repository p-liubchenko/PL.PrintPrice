using System;
using System.Collections.Generic;
using System.Text;

namespace Pricer.Models;

public sealed class AppSettings
{
	public decimal ElectricityPricePerKwh { get; set; } = 6.50m;
	public decimal FixedCostPerPrint { get; set; } = 0m;
	public Money ElectricityPricePerKwhMoney { get; set; }
	public Money FixedCostPerPrintMoney { get; set; }
}