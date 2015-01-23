using System;
using Windows.Storage;

namespace PodCatch.DataModel
{
    public class DownloadService : IDownloadService
    {
        public IDownloader CreateDownloader(Uri sourceUri, StorageFolder destinationStorageFolder, string destinationFileName, Progress<IDownloader> progress)
        {
            return new Downloader(sourceUri, destinationStorageFolder, destinationFileName, progress);
        }
    }
}