using Podcatch.StateMachine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace PodCatch.DataModel
{
    public class EpisodeStateDownloading : AbstractState<Episode, EpisodeEvent>
    {
        public override async Task OnEntry(Episode owner, IState<Episode, EpisodeEvent> fromState, IEventProcessor<Episode, EpisodeEvent> stateMachine)
        {
            owner.NotifyPropertyChanged("State");
            Progress<Downloader> progress = new Progress<Downloader>((downloader) =>
            {
                ulong totalBytesToReceive = downloader.TotalBytes;
                double at = 0;
                if (totalBytesToReceive > 0)
                {
                    at = (double)downloader.DownloadedBytes / totalBytesToReceive;
                }
                owner.DownloadProgress = at;
            });

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            try
            {
                StorageFile localFile = await localFolder.CreateFileAsync(owner.FileName, CreationCollisionOption.ReplaceExisting);
                Downloader downloader = new Downloader(owner.Uri, localFile, progress);
                await downloader.Download();

                // set duration
                MusicProperties musicProperties = await localFile.Properties.GetMusicPropertiesAsync();
                owner.Duration = musicProperties.Duration;
                // set position
                owner.Position = TimeSpan.FromMilliseconds(0);

                if (musicProperties.Duration.TotalMilliseconds > 0)
                {
                    TouchedFiles.Instance.Add(localFile.Path);
                    TouchedFiles.Instance.Add(Path.GetDirectoryName(localFile.Path));
                    Task t = stateMachine.PostEvent(EpisodeEvent.DownloadSuccess);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Episode.Download(): error downloading {0}. {1}", owner.Uri, e);
            }
            Task task = stateMachine.PostEvent(EpisodeEvent.DownloadFail);

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
                    return Task.FromResult<IState<Episode, EpisodeEvent>>(EpisodeStateFactory.Instance.GetState<EpisodeStateDownloaded>());
                case EpisodeEvent.DownloadFail:
                    return Task.FromResult<IState<Episode, EpisodeEvent>>(EpisodeStateFactory.Instance.GetState<EpisodeStatePendingDownload>());
            }
            return Task.FromResult<IState<Episode, EpisodeEvent>>(null);
        }
    }
}
