using Podcatch.Common.StateMachine;
using PodCatch.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace PodCatch.DataModel
{
    public class EpisodeStateDownloading : AbstractState<Episode, EpisodeEvent>
    {
        public EpisodeStateDownloading(IServiceContext serviceContext)
            : base(serviceContext)
        {
        }

        public override async Task OnEntry(Episode owner, IState<Episode, EpisodeEvent> fromState, IEventProcessor<Episode, EpisodeEvent> stateMachine)
        {
            owner.NotifyPropertyChanged(() => owner.State);
            Progress<IDownloader> progress = new Progress<IDownloader>((downloader) =>
            {
                ulong totalBytesToReceive = downloader.GetTotalBytes();
                double at = 0;
                if (totalBytesToReceive > 0)
                {
                    at = (double)downloader.GetBytesDownloaded() / totalBytesToReceive;
                }
                owner.DownloadProgress = at;
            });

            try
            {
                var downloader = owner.DownloadService.CreateDownloader(owner.Uri, await owner.GetStorageFolder(), owner.FolderAndFileName, progress);
                var localFile = await downloader.Download();

                // set duration
                var musicProperties = await localFile.Properties.GetMusicPropertiesAsync();
                owner.Duration = musicProperties.Duration;
                // set position
                owner.Position = TimeSpan.FromMilliseconds(0);

                TouchedFiles.Instance.Add(localFile.Path);
                TouchedFiles.Instance.Add(Path.GetDirectoryName(localFile.Path));
                Task t = stateMachine.PostEvent(EpisodeEvent.DownloadSuccess);
            }
            catch (Exception e)
            {
                Tracer.TraceWarning("EpisodeStateDownloading.OnEntry(): error downloading {0}. {1}", owner.Uri, e);
                Task task = stateMachine.PostEvent(EpisodeEvent.DownloadFail);
            }
        }

        public override Task OnExit(Episode owner, IState<Episode, EpisodeEvent> toState, IEventProcessor<Episode, EpisodeEvent> stateMachine)
        {
            return Task.FromResult<object>(null);
        }

        public override Task<IState<Episode, EpisodeEvent>> OnEvent(Episode owner, EpisodeEvent anEvent, IEventProcessor<Episode, EpisodeEvent> stateMachine)
        {
            switch (anEvent)
            {
                case EpisodeEvent.DownloadSuccess:
                    return Task.FromResult<IState<Episode, EpisodeEvent>>(GetState<EpisodeStateDownloaded>());

                case EpisodeEvent.DownloadFail:
                    return Task.FromResult<IState<Episode, EpisodeEvent>>(GetState<EpisodeStatePendingDownload>());
            }
            return Task.FromResult<IState<Episode, EpisodeEvent>>(null);
        }
    }
}