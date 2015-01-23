using System;
using Windows.Storage;

namespace PodCatch.DataModel
{
    public interface IDownloadService
    {
        IDownloader CreateDownloader(Uri sourceUri, StorageFolder destinationStorageFolder, string destinationFileName, Progress<IDownloader> progress);
    }
}