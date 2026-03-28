using System.Security.Claims;
using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

using Spooly.Application;
using Spooly.DAL;
using Spooly.DAL.Security;
using Spooly.WebApp.Server.Authorization;

namespace Spooly.WebApp.Server;

public class Program
{
	public static async Task Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		// ── Identity + Security DB ───────────────────────────────────────────
		builder.Services.AddSecurityDataAccess(builder.Configuration);

		builder.Services.AddIdentityCore<IdentityUser>(opt =>
			{
				opt.Password.RequireDigit = false;
				opt.Password.RequireLowercase = false;
				opt.Password.RequireUppercase = false;
				opt.Password.RequireNonAlphanumeric = false;
				opt.Password.RequiredLength = 6;
			})
			.AddRoles<IdentityRole>()
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
				opt.MapInboundClaims = false;
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

		// ── Permission-based authorization ───────────────────────────────────
		builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
		builder.Services.AddAuthorization(opt =>
		{
			foreach (var permission in Permissions.All)
				opt.AddPolicy(permission, policy =>
					policy.AddRequirements(new PermissionRequirement(permission)));
		});

		// ── Spooly domain services ───────────────────────────────────────────
		builder.Services.AddSpooly(builder.Configuration);

		// ── MVC / OpenAPI ────────────────────────────────────────────────────
		builder.Services.AddControllers();
		builder.Services.AddOpenApi();

		var app = builder.Build();

		// ── Auto-apply migrations / schema init ──────────────────────────────
		await app.Services.InitializeSecuritySchemaAsync();
		app.Services.ApplyPendingMigrations();
		await app.Services.MigrateDataIfNeededAsync(builder.Configuration);

		// ── Seed: Administrator role + all permissions ────────────────────────
		using (var scope = app.Services.CreateScope())
		{
			var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
			await SeedAdminRoleAsync(roleManager);
		}

		// ── Seed default app data ────────────────────────────────────────────
		using (var scope = app.Services.CreateScope())
		{
			var settings = scope.ServiceProvider.GetRequiredService<ISettingsService>();
			await settings.EnsureSeedAsync();
		}

		// ── Pipeline ─────────────────────────────────────────────────────────
		app.UseDefaultFiles();
		app.UseStaticFiles();

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

	/// <summary>
	/// Ensures the Administrator role exists and always has every defined permission.
	/// Safe to run on every startup — missing permissions are added, none are removed.
	/// </summary>
	private static async Task SeedAdminRoleAsync(RoleManager<IdentityRole> roleManager)
	{
		const string adminRole = "Administrator";

		if (!await roleManager.RoleExistsAsync(adminRole))
			await roleManager.CreateAsync(new IdentityRole(adminRole));

		var role = await roleManager.FindByNameAsync(adminRole);
		var existing = await roleManager.GetClaimsAsync(role!);

		foreach (var p in Permissions.All)
		{
			if (!existing.Any(c => c.Type == "permission" && c.Value == p))
				await roleManager.AddClaimAsync(role!, new Claim("permission", p));
		}
	}
}
