using Microsoft.Extensions.DependencyInjection;
using Pricer.DAL;

namespace Pricer.Cli;

internal static class Program
{
	private const string DataFileName = "data.json";

	static void Main(string[] args)
	{
		var services = new ServiceCollection();
		services.AddSingleton<IAppDataStore, FileBackedStore>();
		services.AddSingleton(sp => new AppStartup(sp.GetRequiredService<IAppDataStore>()));
		services.AddSingleton(sp => new FilamentWarehouse(sp.GetRequiredService<IAppDataStore>(), DataFileName));
		services.AddSingleton(sp => new PrinterManager(sp.GetRequiredService<IAppDataStore>(), DataFileName));
		services.AddSingleton(sp => new CurrencyManager(sp.GetRequiredService<IAppDataStore>(), DataFileName));
		services.AddSingleton(sp => new PrintTransactionsManager(sp.GetRequiredService<IAppDataStore>(), DataFileName));

		services.AddSingleton<FilamentWarehouseCliDrawer>();
		services.AddSingleton<PrinterManagerCliDrawer>();
		services.AddSingleton<CurrencyManagerCliDrawer>();
		services.AddSingleton<PrintTransactionsCliDrawer>();
		services.AddSingleton<PrintCostCalculatorCliDrawer>();
		services.AddSingleton<AppCli>();

		var provider = services.BuildServiceProvider();
		var startup = provider.GetRequiredService<AppStartup>();
		var appData = startup.LoadAndTreat(DataFileName);
		provider.GetRequiredService<AppCli>().Run(appData, DataFileName);
	}
}
