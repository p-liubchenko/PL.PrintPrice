using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Pricer.DAL.Security;

/// <summary>
/// Design-time factory used by EF migrations tooling (SQL Server only).
/// Run migrations with:
///   dotnet ef migrations add &lt;name&gt; --project Pricer.DAL.Security --startup-project Pricer.WebApp/Pricer.WebApp.Server --context SecurityDbContext
///
/// Note: SQLite uses EnsureCreated at startup and does not use this factory or migrations.
/// </summary>
public sealed class SecurityDbContextFactory : IDesignTimeDbContextFactory<SecurityDbContext>
{
	public SecurityDbContext CreateDbContext(string[] args)
	{
		var config = new ConfigurationBuilder()
			.SetBasePath(FindWebServerDirectory())
			.AddJsonFile("appsettings.json", optional: true)
			.AddJsonFile("appsettings.Development.json", optional: true)
			.AddUserSecrets<SecurityDbContextFactory>(optional: true)
			.AddEnvironmentVariables()
			.Build();

		var connectionString = config.GetConnectionString("SecurityConnection")
			?? config.GetConnectionString("DefaultConnection")
			?? "Server=(localdb)\\mssqllocaldb;Database=PricerSecurity;Trusted_Connection=True;MultipleActiveResultSets=true";

		var options = new DbContextOptionsBuilder<SecurityDbContext>()
			.UseSqlServer(connectionString, sql =>
				sql.MigrationsAssembly(typeof(SecurityDbContext).Assembly.FullName))
			.Options;

		return new SecurityDbContext(options);
	}

	private static string FindWebServerDirectory()
	{
		var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
		while (dir != null)
		{
			var candidate = Path.Combine(dir.FullName, "Pricer.WebApp", "Pricer.WebApp.Server");
			if (Directory.Exists(candidate))
				return candidate;
			dir = dir.Parent;
		}
		return Directory.GetCurrentDirectory();
	}
}
