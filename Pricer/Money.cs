using System;

namespace Pricer;

public readonly record struct Money(decimal Amount, Guid? CurrencyId)
{
	public static Money Base(decimal amount) => new(amount, null);

	public decimal ToBase(AppData data)
	{
		if (CurrencyId is null)
		{
			return Amount;
		}

		var id = CurrencyId.Value;

       var currency = data.Currencies.FirstOrDefault(c => c.Id == id);
		if (currency is null || currency.Value == 0)
		{
			return Amount;
		}

		return Amount / currency.Value;
	}

	public static Money FromBase(AppData data, decimal baseAmount, Guid? targetCurrencyId)
	{
		if (targetCurrencyId is null)
		{
			return Base(baseAmount);
		}

		var currency = data.Currencies.FirstOrDefault(c => c.Id == targetCurrencyId.Value);
		if (currency is null)
		{
			return Base(baseAmount);
		}

		return new Money(baseAmount * currency.Value, currency.Id);
	}
}
