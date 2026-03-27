using Pricer;

using System.IO;
using System.Text.Json;

namespace Pricer.DAL.File;

internal static class AppDataSerializer
{
	private static readonly JsonSerializerOptions Options = new()
	{
		WriteIndented = true,
		PropertyNameCaseInsensitive = true
	};

	public static AppData Load(string filePath)
	{
		try
		{
			if (!System.IO.File.Exists(filePath))
				return new AppData();

			var json = System.IO.File.ReadAllText(filePath);
			if (string.IsNullOrWhiteSpace(json))
				return new AppData();

			var data = JsonSerializer.Deserialize<AppData>(json, Options) ?? new AppData();
			MigrateLegacyFields(data);
			return data;
		}
		catch
		{
			return new AppData();
		}
	}

	public static void Save(string filePath, AppData data)
	{
		var json = JsonSerializer.Serialize(data, Options);
		System.IO.File.WriteAllText(filePath, json);
	}

	/// <summary>
	/// One-time migration: copies legacy top-level SelectedPrinterId / OperatingCurrencyId
	/// into AppSettings when loading old data.json files that predate the Settings object.
	/// </summary>
	private static void MigrateLegacyFields(AppData data)
	{
		if (data.SelectedPrinterId.HasValue && data.Settings.SelectedPrinterId is null)
			data.Settings.SelectedPrinterId = data.SelectedPrinterId;

		if (data.OperatingCurrencyId.HasValue && data.Settings.OperatingCurrencyId is null)
			data.Settings.OperatingCurrencyId = data.OperatingCurrencyId;
	}
}
