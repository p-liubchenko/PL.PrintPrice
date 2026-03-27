using Pricer.Models;

namespace Pricer;

public static class MoneyFormatter
{
	public static string Format(Currency? operatingCurrency, decimal baseAmount, string format = "F2")
	{
		if (operatingCurrency is null)
			return baseAmount.ToString(format);

		var amount = baseAmount * operatingCurrency.Value;
		return $"{amount.ToString(format)} {operatingCurrency.Code}";
	}

	public static string FormatPerKwh(Currency? operatingCurrency, decimal baseAmount, string format = "F2")
		=> $"{Format(operatingCurrency, baseAmount, format)}/kWh";

	public static string FormatPerHour(Currency? operatingCurrency, decimal baseAmount, string format = "F2")
		=> $"{Format(operatingCurrency, baseAmount, format)}/h";

	public static string FormatPerKg(Currency? operatingCurrency, decimal baseAmount, string format = "F2")
		=> $"{Format(operatingCurrency, baseAmount, format)}/kg";
}
