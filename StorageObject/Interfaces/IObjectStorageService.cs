using System.Threading.Tasks;

namespace StorageObject.Interfaces
{
    public interface IObjectStorageService
    {
        Task<IObjectStorageInfo> UploadAsync(string fileName, byte[] data);
        Task<string> GetAsync(IObjectStorageInfo storageInfo);
        Task DeleteAsync(IObjectStorageInfo storageInfo);
    }
}
