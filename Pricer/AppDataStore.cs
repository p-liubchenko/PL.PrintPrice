namespace Pricer;

public interface IAppDataStore
{
	AppData Load(string filePath);
	void Save(string filePath, AppData data);
}
