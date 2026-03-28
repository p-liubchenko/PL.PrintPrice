using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Pricer.DAL.Security;

public static class SecurityServiceCollectionExtensions
{
	/// <summary>
	/// Registers <see cref="SecurityDbContext"/> using the provider determined by
	/// <c>DataAccess:Mode</c> in configuration:
	/// <list type="bullet">
	///   <item><c>File</c> (default) — SQLite database at <c>DataAccess:SecurityFilePath</c>
	///     (defaults to <c>security.db</c>).</item>
	///   <item><c>Mssql</c> — SQL Server using <c>ConnectionStrings:SecurityConnection</c>
	///     or <c>ConnectionStrings:DefaultConnection</c>.</item>
	/// </list>
	/// </summary>
	public static IServiceCollection AddSecurityDataAccess(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		var mode = configuration["DataAccess:Mode"] ?? "File";

		if (mode.Equals("Mssql", StringComparison.OrdinalIgnoreCase))
		{
			var connStr = configuration.GetConnectionString("SecurityConnection")
				?? configuration.GetConnectionString("DefaultConnection")
				?? throw new InvalidOperationException(
					"A connection string ('SecurityConnection' or 'DefaultConnection') is required when DataAccess:Mode is Mssql.");

			services.AddDbContext<SecurityDbContext>(opt =>
				opt.UseSqlServer(connStr, sql =>
					sql.MigrationsAssembly(typeof(SecurityDbContext).Assembly.FullName)));
		}
		else
		{
			var filePath = configuration["DataAccess:SecurityFilePath"] ?? "security.db";
			services.AddDbContext<SecurityDbContext>(opt =>
				opt.UseSqlite($"Data Source={filePath}"));
		}

		return services;
	}

	/// <summary>
	/// Applies pending SQL Server migrations, or calls <c>EnsureCreated</c> for SQLite.
	/// Call once at startup after the service provider is built.
	/// </summary>
	public static async Task InitializeSecuritySchemaAsync(this IServiceProvider serviceProvider)
	{
		using var scope = serviceProvider.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();

		if (db.Database.IsSqlite())
			await db.Database.EnsureCreatedAsync();
		else
			await db.Database.MigrateAsync();
	}
}
