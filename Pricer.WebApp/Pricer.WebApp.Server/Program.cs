using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

using Pricer.Application;
using Pricer.DAL;
using Pricer.DAL.Security;

namespace Pricer.WebApp.Server;

public class Program
{
	public static async Task Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		// ── Identity + Security DB ───────────────────────────────────────────
		builder.Services.AddSecurityDataAccess(builder.Configuration);

		builder.Services.AddIdentity<IdentityUser, IdentityRole>(opt =>
			{
				opt.Password.RequireDigit = false;
				opt.Password.RequireLowercase = false;
				opt.Password.RequireUppercase = false;
				opt.Password.RequireNonAlphanumeric = false;
				opt.Password.RequiredLength = 6;
			})
			.AddEntityFrameworkStores<SecurityDbContext>()
			.AddDefaultTokenProviders();

		// ── JWT ──────────────────────────────────────────────────────────────
		var jwtKey = builder.Configuration["Jwt:Key"]
			?? throw new InvalidOperationException("Jwt:Key is not configured.");
		var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "PL.PrintPrice";
		var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "PL.PrintPrice";

		builder.Services.AddAuthentication(opt =>
			{
				opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer(opt =>
			{
				opt.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidateAudience = true,
					ValidateLifetime = true,
					ValidateIssuerSigningKey = true,
					ValidIssuer = jwtIssuer,
					ValidAudience = jwtAudience,
					IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
				};
			});

		builder.Services.AddAuthorization();

		// ── Pricer domain services ───────────────────────────────────────────
		builder.Services.AddPricer(builder.Configuration);

		// ── MVC / OpenAPI ────────────────────────────────────────────────────
		builder.Services.AddControllers();
		builder.Services.AddOpenApi();

		var app = builder.Build();

		// ── Auto-apply migrations / schema init ──────────────────────────────
		await app.Services.InitializeSecuritySchemaAsync();
		app.Services.ApplyPendingMigrations();
		await app.Services.MigrateDataIfNeededAsync(builder.Configuration);

		// ── Seed default app data ────────────────────────────────────────────
		using (var scope = app.Services.CreateScope())
		{
			var settings = scope.ServiceProvider.GetRequiredService<ISettingsService>();
			await settings.EnsureSeedAsync();
		}

		// ── Pipeline ─────────────────────────────────────────────────────────
		app.UseDefaultFiles();
		app.MapStaticAssets();

		if (app.Environment.IsDevelopment())
		{
			app.MapOpenApi();
		}

		app.UseHttpsRedirection();
		app.UseAuthentication();
		app.UseAuthorization();
		app.MapControllers();
		app.MapFallbackToFile("/index.html");

		await app.RunAsync();
	}
}
