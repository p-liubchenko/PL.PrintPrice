using Pricer.Enums;
using Pricer.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Pricer;

public sealed class AppData
{
	public List<FilamentMaterial> Materials { get; set; } = new();
	public List<Printer> Printers { get; set; } = new();
	public List<PrintTransaction> PrintTransactions { get; set; } = new();
	public List<Currency> Currencies { get; set; } = new();
	public Guid? SelectedPrinterId { get; set; }
	public Guid? OperatingCurrencyId { get; set; }
	public AppSettings Settings { get; set; } = new();

	public Printer? GetSelectedPrinter()
        => SelectedPrinterId is null ? null : Printers.FirstOrDefault(x => x.Id == SelectedPrinterId.Value);
}

public static class DataStore
{
	public static AppData Load(string filePath)
	{
		try
		{
			if (!File.Exists(filePath))
			{
				return new AppData();
			}

			var json = File.ReadAllText(filePath);
			if (string.IsNullOrWhiteSpace(json))
			{
				return new AppData();
			}

			var data = JsonSerializer.Deserialize<AppData>(json, ProgramJson.Options);
			return data ?? new AppData();
		}
		catch
		{
			return new AppData();
		}
	}

	public static void Save(string filePath, AppData data)
	{
		var json = JsonSerializer.Serialize(data, ProgramJson.Options);
		File.WriteAllText(filePath, json);
	}
}

public static class ProgramJson
{
	public static JsonSerializerOptions Options { get; } = new()
	{
		WriteIndented = true,
		PropertyNameCaseInsensitive = true
	};
}
