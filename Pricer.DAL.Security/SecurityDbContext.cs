using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Pricer.DAL.Security;

public sealed class SecurityDbContext(DbContextOptions<SecurityDbContext> options)
	: IdentityDbContext<IdentityUser>(options)
{
	protected override void OnModelCreating(ModelBuilder builder)
	{
		base.OnModelCreating(builder);
		// SQLite does not support schemas; apply only for SQL Server
		if (Database.IsSqlServer())
			builder.HasDefaultSchema("security");
	}
}
