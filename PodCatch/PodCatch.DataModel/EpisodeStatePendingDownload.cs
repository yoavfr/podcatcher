using Podcatch.Common.StateMachine;
using PodCatch.Common;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using System;
using System.Diagnostics;

namespace PodCatch.DataModel
{
    public class EpisodeStatePendingDownload : AbstractState<Episode, EpisodeEvent>
    {
        public override Task OnEntry(Episode owner, IState<Episode, EpisodeEvent> fromState, IEventProcessor<Episode, EpisodeEvent> stateMachine)
        {
            owner.NotifyPropertyChanged(() => owner.State);
            return Task.FromResult<object>(null);
        }

        public override Task OnExit(Episode owner, IState<Episode, EpisodeEvent> toState, IEventProcessor<Episode, EpisodeEvent> stateMachine)
        {
            return Task.FromResult<object>(null);
        }

        public override async Task<IState<Episode, EpisodeEvent>> OnEvent(Episode owner, EpisodeEvent anEvent, IEventProcessor<Episode, EpisodeEvent> stateMachine)
        {
            switch (anEvent)
            {
                case EpisodeEvent.Download:
                    {
                        return EpisodeStateFactory.Instance.GetState<EpisodeStateDownloading>();
                    }

                case EpisodeEvent.UpdateDownloadStatus:
                    {
                        try
                        {
                            StorageFile file = await owner.GetStorageFile();
                            if (file == null)
                            {
                                return null;
                            }
                            MusicProperties musicProperties = await file.Properties.GetMusicPropertiesAsync();

                            owner.Duration = musicProperties.Duration;
                            TouchedFiles.Instance.Add(file.Path);
                            TouchedFiles.Instance.Add(Path.GetDirectoryName(file.Path));
                            return EpisodeStateFactory.Instance.GetState<EpisodeStateDownloaded>();
                        }
                        catch (FileNotFoundException e )
                        {
                            Debug.WriteLine("EpisodeStatePendingDownload.OnEvent() - ", e);
                            return null;
                        }
                    }
            }

            return null;
        }
    }
}