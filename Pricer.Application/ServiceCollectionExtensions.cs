using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Pricer.DAL;

namespace Pricer.Application;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPricerApplication(this IServiceCollection services)
	{
		services.AddScoped<ISettingsService, Services.SettingsService>();
		services.AddScoped<IPrintersService, Services.PrintersService>();
		services.AddScoped<IMaterialsService, Services.MaterialsService>();
		services.AddScoped<ICurrenciesService, Services.CurrenciesService>();
		services.AddScoped<ITransactionsService, Services.TransactionsService>();
		return services;
	}

	public static IServiceCollection AddPricer(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddPricerDataAccess(configuration);
		services.AddPricerApplication();
		return services;
	}
}
