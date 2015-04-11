using Podcatch.Common.StateMachine;
using PodCatch.Common;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace PodCatch.DataModel
{
    public class EpisodeStateUnknown : AbstractState<Episode, EpisodeEvent>
    {
        public EpisodeStateUnknown(IServiceContext serviceContext)
            : base(serviceContext)
        {
        }

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
                case EpisodeEvent.UpdateDownloadStatus:
                    {
                        try
                        {
                            StorageFile file = await owner.GetStorageFile();
                            if (file == null)
                            {
                                return GetState<EpisodeStatePendingDownload>();
                            }
                            MusicProperties musicProperties = await file.Properties.GetMusicPropertiesAsync();

                            owner.Duration = musicProperties.Duration;
                            if (owner.Title == null)
                            {
                                owner.Title = musicProperties.Title;
                            }
                            TouchedFiles.Instance.Add(file.Path);
                            TouchedFiles.Instance.Add(Path.GetDirectoryName(file.Path));
                            return GetState<EpisodeStateDownloaded>();
                        }
                        catch (FileNotFoundException)
                        {
                            return GetState<EpisodeStatePendingDownload>();
                        }
                    }
                case EpisodeEvent.Play:
                    {
                        // this will happen after restarting when already playing in the background
                        return GetState<EpisodeStatePlaying>();
                    }
            }
            return null;
        }
    }
}