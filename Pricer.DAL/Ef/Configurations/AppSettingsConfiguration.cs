using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Pricer.Models;

namespace Pricer.DAL.Ef.Configurations;

internal sealed class AppSettingsConfiguration : IEntityTypeConfiguration<AppSettings>
{
	public void Configure(EntityTypeBuilder<AppSettings> builder)
	{
		builder.ToTable("Settings");
		builder.HasKey(x => x.Id);

		builder.Property(x => x.ElectricityPricePerKwhMoney)
			.HasConversion(new Converters.MoneyJsonConverter())
			.HasColumnType("nvarchar(max)");

		builder.Property(x => x.FixedCostPerPrintMoney)
			.HasConversion(new Converters.MoneyJsonConverter())
			.HasColumnType("nvarchar(max)");

		builder.Property(x => x.SelectedPrinterId).IsRequired(false);
		builder.Property(x => x.OperatingCurrencyId).IsRequired(false);
	}
}
