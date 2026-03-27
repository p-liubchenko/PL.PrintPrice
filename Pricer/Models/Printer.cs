using System;
using System.Collections.Generic;
using System.Text;

namespace Pricer.Models;

public sealed class Printer
{
	public Guid Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public decimal AveragePowerWatts { get; set; }
	public decimal HourlyCost { get; set; }
	public Money HourlyCostMoney { get; set; }
}