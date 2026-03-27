using Pricer;

namespace Pricer.DAL;

public sealed class FileBackedStore : IAppDataStore
{
    public AppData Load(string filePath) => DataStore.Load(filePath);
    public void Save(string filePath, AppData data) => DataStore.Save(filePath, data);
}
