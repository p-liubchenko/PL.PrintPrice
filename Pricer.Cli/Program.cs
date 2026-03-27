using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Pricer;
using Pricer.Application;
using Pricer.DAL;

namespace Pricer.Cli;

internal static class Program
{
	static async Task Main(string[] args)
	{
		var configuration = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json", optional: true)
			.AddJsonFile("appsettings.Development.json", optional: true)
			.AddEnvironmentVariables(prefix: "PRICER_")
			.AddUserSecrets(typeof(Program).Assembly, optional: true)
			.AddCommandLine(args)
			.Build();

		var services = new ServiceCollection();
		services.AddSingleton<IConfiguration>(configuration);
		services.AddPricer(configuration);

		services.AddScoped<FilamentWarehouseCliDrawer>();
		services.AddScoped<PrinterManagerCliDrawer>();
		services.AddScoped<CurrencyManagerCliDrawer>();
		services.AddScoped<PrintTransactionsCliDrawer>();
		services.AddScoped<PrintCostCalculatorCliDrawer>();
		services.AddScoped<AppCli>();

		var provider = services.BuildServiceProvider();
		provider.ApplyPendingMigrations();

		using var scope = provider.CreateScope();
		var sp = scope.ServiceProvider;

		await sp.GetRequiredService<ISettingsService>().EnsureSeedAsync();
		sp.GetRequiredService<AppCli>().Run();
	}
}
