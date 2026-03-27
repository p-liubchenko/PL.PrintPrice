using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Pricer.DAL.Ef;
using Pricer.DAL.File;
using Pricer.DAL.Options;
using Pricer.DAL.Repositories;
using Pricer.Models;
using Pricer.Models.Transactions;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pricer.DAL;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPricerDataAccess(this IServiceCollection services, IConfiguration configuration)
	{
		var options = configuration.GetSection(DataAccessOptions.SectionName).Get<DataAccessOptions>()
				  ?? new DataAccessOptions();

		switch (options.Mode)
		{
			case DataAccessMode.File:
			{
				var filePath = options.FilePath ?? "data.json";
				RegisterFileRepositories(services, filePath);
				return services;
			}

			case DataAccessMode.Mssql:
			{
				var connectionString = configuration.GetConnectionString("DefaultConnection");
				if (string.IsNullOrWhiteSpace(connectionString))
					throw new InvalidOperationException("Connection string 'DefaultConnection' is required when Mode is Mssql.");

				services.AddDbContext<PricerDbContext>(db =>
					db.UseSqlServer(connectionString, sql =>
						sql.MigrationsAssembly(typeof(PricerDbContext).Assembly.FullName)));

				RegisterEfRepositories(services);
				return services;
			}

			default:
				throw new NotSupportedException($"Unsupported data access mode: '{options.Mode}'.");
		}
	}

	private static void RegisterFileRepositories(IServiceCollection services, string filePath)
	{
		services.AddScoped<ICrudRepository<AppSettings, Guid>>(
			_ => new FileSettingsRepository(filePath));
		services.AddScoped<ICrudRepository<Currency, Guid>>(
			_ => new FileRepository<Currency, Guid>(filePath, d => d.Currencies, e => e.Id));
		services.AddScoped<ICrudRepository<Printer, Guid>>(
			_ => new FileRepository<Printer, Guid>(filePath, d => d.Printers, e => e.Id));
		services.AddScoped<ICrudRepository<FilamentMaterial, Guid>>(
			_ => new FileRepository<FilamentMaterial, Guid>(filePath, d => d.Materials, e => e.Id));
		services.AddScoped<ICrudRepository<PrintTransaction, Guid>>(
			_ => new FileRepository<PrintTransaction, Guid>(filePath, d => d.PrintTransactions, e => e.Id));
		services.AddScoped<ICrudRepository<StockTransaction, Guid>>(
			_ => new FileRepository<StockTransaction, Guid>(filePath, d => d.StockTransactions, e => e.Id));
	}

	private static void RegisterEfRepositories(IServiceCollection services)
	{
		services.AddScoped<ICrudRepository<AppSettings, Guid>>(
			sp => new EfCrudRepository<AppSettings, Guid>(sp.GetRequiredService<PricerDbContext>()));
		services.AddScoped<ICrudRepository<Currency, Guid>>(
			sp => new EfCrudRepository<Currency, Guid>(sp.GetRequiredService<PricerDbContext>()));
		services.AddScoped<ICrudRepository<Printer, Guid>>(
			sp => new EfCrudRepository<Printer, Guid>(sp.GetRequiredService<PricerDbContext>()));
		services.AddScoped<ICrudRepository<FilamentMaterial, Guid>>(
			sp => new EfCrudRepository<FilamentMaterial, Guid>(sp.GetRequiredService<PricerDbContext>()));
		services.AddScoped<ICrudRepository<PrintTransaction, Guid>>(
			sp => new EfCrudRepository<PrintTransaction, Guid>(sp.GetRequiredService<PricerDbContext>()));
		services.AddScoped<ICrudRepository<StockTransaction, Guid>>(
			sp => new EfCrudRepository<StockTransaction, Guid>(sp.GetRequiredService<PricerDbContext>()));
	}

	public static void ApplyPendingMigrations(this IServiceProvider serviceProvider)
	{
		using var scope = serviceProvider.CreateScope();
		if (scope.ServiceProvider.GetService<PricerDbContext>() is not PricerDbContext dbContext)
			return;
		if (dbContext.Database.GetPendingMigrations().Any())
			dbContext.Database.Migrate();
	}

	/// <summary>
	/// Detects a backend switch and migrates data automatically:
	/// <list type="bullet">
	///   <item>MSSQL mode + data.json present → merges file data into DB, archives file to .bak</item>
	///   <item>File mode + connection string configured + file empty → pulls DB data into file</item>
	/// </list>
	/// </summary>
	public static async Task MigrateDataIfNeededAsync(
		this IServiceProvider serviceProvider,
		IConfiguration configuration,
		CancellationToken ct = default)
	{
		var options = configuration.GetSection(DataAccessOptions.SectionName).Get<DataAccessOptions>()
				  ?? new DataAccessOptions();

		var filePath = options.FilePath ?? "data.json";
		var connectionString = configuration.GetConnectionString("DefaultConnection");

		if (options.Mode == DataAccessMode.Mssql && System.IO.File.Exists(filePath))
		{
			Console.WriteLine($"[Migration] data.json detected in MSSQL mode — merging into database...");
			using var scope = serviceProvider.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<PricerDbContext>();
			await DataMigrator.MigrateFileToDbAsync(filePath, db, ct);
			System.IO.File.Move(filePath, filePath + ".bak", overwrite: true);
			Console.WriteLine($"[Migration] Done. File archived as {filePath}.bak");
			return;
		}

		if (options.Mode == DataAccessMode.File && !string.IsNullOrWhiteSpace(connectionString))
		{
			var fileData = AppDataSerializer.Load(filePath);
			bool fileIsEmpty = !fileData.Currencies.Any()
							&& !fileData.Materials.Any()
							&& !fileData.Printers.Any()
							&& !fileData.PrintTransactions.Any()
							&& !fileData.StockTransactions.Any();

			if (!fileIsEmpty)
				return;

			Console.WriteLine("[Migration] File is empty, connection string configured — checking database...");
			try
			{
				await using var db = CreateRawDbContext(connectionString);
				bool dbHasData = await db.Currencies.AnyAsync(ct) || await db.Materials.AnyAsync(ct);
				if (!dbHasData)
					return;

				await DataMigrator.MigrateDbToFileAsync(db, filePath, ct);
				Console.WriteLine($"[Migration] Done. Data written to {filePath}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[Migration] Could not connect to database ({ex.Message}). Starting with empty file.");
			}
		}
	}

	private static PricerDbContext CreateRawDbContext(string connectionString)
	{
		var opts = new DbContextOptionsBuilder<PricerDbContext>()
			.UseSqlServer(connectionString, sql =>
				sql.MigrationsAssembly(typeof(PricerDbContext).Assembly.FullName))
			.Options;
		return new PricerDbContext(opts);
	}
}
