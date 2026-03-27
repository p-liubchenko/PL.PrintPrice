using System;
using System.Collections.Generic;
using System.Linq;

namespace Pricer.Models;

public readonly record struct Money(decimal Amount, Guid? CurrencyId)
{
	public static Money Zero => new(0m, null);
	public static Money Base(decimal amount) => new(amount, null);

	public decimal ToBase(IReadOnlyList<Currency> currencies)
	{
		if (CurrencyId is null)
			return Amount;

		var id = CurrencyId.Value;
		var currency = currencies.FirstOrDefault(c => c.Id == id);
		if (currency is null || currency.Value == 0)
			return Amount;

		return Amount / currency.Value;
	}

	public static Money FromBase(IReadOnlyList<Currency> currencies, decimal baseAmount, Guid? targetCurrencyId)
	{
		if (targetCurrencyId is null)
			return Base(baseAmount);

		var currency = currencies.FirstOrDefault(c => c.Id == targetCurrencyId.Value);
		if (currency is null)
			return Base(baseAmount);

		return new Money(baseAmount * currency.Value, currency.Id);
	}
}
