using System;

namespace Pricer;

public static class MoneyFormatter
{
	public static (decimal Amount, string Code) ConvertFromBase(AppData data, decimal baseAmount)
	{
		var currency = data.GetOperatingCurrency();
		if (currency is null)
		{
			return (baseAmount, "");
		}

		return (baseAmount * currency.Value, currency.Code);
	}

	public static string Format(AppData data, decimal baseAmount, string format = "F2")
	{
		var (amount, code) = ConvertFromBase(data, baseAmount);
		if (string.IsNullOrWhiteSpace(code))
		{
			return amount.ToString(format);
		}

		return $"{amount.ToString(format)} {code}";
	}

	public static string FormatPerKwh(AppData data, decimal baseAmount, string format = "F2")
		=> $"{Format(data, baseAmount, format)}/kWh";

	public static string FormatPerHour(AppData data, decimal baseAmount, string format = "F2")
		=> $"{Format(data, baseAmount, format)}/h";

	public static string FormatPerKg(AppData data, decimal baseAmount, string format = "F2")
		=> $"{Format(data, baseAmount, format)}/kg";

}
