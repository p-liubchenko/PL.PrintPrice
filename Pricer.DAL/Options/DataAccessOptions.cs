namespace Pricer.DAL.Options;

public sealed class DataAccessOptions
{
	public const string SectionName = "DataAccess";

	public DataAccessMode Mode { get; set; } = DataAccessMode.File;
	public string? ConnectionString { get; set; }
	public string? FilePath { get; set; }
}
