using System;
using System.Linq;

namespace Pricer.Models;

public sealed class Currency
{
	public Guid Id { get; set; }
	public string Code { get; set; } = string.Empty;
	public decimal Value { get; set; } = 1m;
}

public static class CurrencyExtensions
{
	public static Currency? GetBaseCurrency(this AppData data)
		=> data.Currencies.FirstOrDefault(c => c.Value == 1m);

	public static Currency? GetOperatingCurrency(this AppData data)
	{
		var id = data.Settings?.OperatingCurrencyId ?? data.OperatingCurrencyId;
		if (id is null)
			return null;
		return data.Currencies.FirstOrDefault(c => c.Id == id.Value);
	}
}
