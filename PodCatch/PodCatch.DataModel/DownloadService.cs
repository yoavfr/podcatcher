using PodCatch.Common;
using System;
using Windows.Storage;

namespace PodCatch.DataModel
{
    public class DownloadService : ServiceConsumer, IDownloadService
    {
        public DownloadService(IServiceContext serviceContext)
            : base(serviceContext)
        {
        }

        public IDownloader CreateDownloader(Uri sourceUri, StorageFolder destinationStorageFolder, string destinationFileName, Progress<IDownloader> progress)
        {
            Tracer.TraceInformation("Creating downloader for {0} to {1}/{2}", sourceUri, destinationStorageFolder, destinationFileName);
            return new Downloader(sourceUri, destinationStorageFolder, destinationFileName, progress);
        }
    }
}