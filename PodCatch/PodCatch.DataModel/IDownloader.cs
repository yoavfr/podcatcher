using System.Threading.Tasks;
using Windows.Storage;

namespace PodCatch.DataModel
{
    public interface IDownloader
    {
        ulong GetTotalBytes();
        ulong GetBytesDownloaded();

        Task<StorageFile> Download();
    }
}
