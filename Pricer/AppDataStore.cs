namespace Pricer;

public interface IAppDataStore
{
	AppData Load(string filePath);
	void Save(string filePath, AppData data);
}

public sealed class FileBackedStore : IAppDataStore
{
	public static FileBackedStore Default { get; } = new();

	public AppData Load(string filePath) => DataStore.Load(filePath);
	public void Save(string filePath, AppData data) => DataStore.Save(filePath, data);
}
