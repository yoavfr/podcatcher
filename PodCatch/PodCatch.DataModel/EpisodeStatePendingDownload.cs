using Podcatch.StateMachine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace PodCatch.DataModel
{
    public class EpisodeStatePendingDownload : AbstractState<Episode, EpisodeEvent>
    {
        public override async Task OnEntry(Episode owner, IState<Episode, EpisodeEvent> fromState, IEventProcessor<Episode, EpisodeEvent> stateMachine)
        {
            owner.NotifyPropertyChanged("State");
        }

        public override async Task OnExit(Episode owner, IState<Episode, EpisodeEvent> toState, IEventProcessor<Episode, EpisodeEvent> stateMachine)
        {
        }

        public override async Task<IState<Episode, EpisodeEvent>> OnEvent(Episode owner, EpisodeEvent anEvent, IEventProcessor<Episode, EpisodeEvent> stateMachine)
        {
            switch (anEvent)
            {
                case EpisodeEvent.UpdateDownloadStatus:
                    { 
                        StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                        try
                        {
                            StorageFile file = await localFolder.GetFileAsync(owner.FileName);
                            MusicProperties musicProperties = await file.Properties.GetMusicPropertiesAsync();

                            owner.Duration = musicProperties.Duration;
                            // When file is partially downloaded the only indication we have that it has not completed is that the Duration is 0
                            if (musicProperties.Duration.TotalMilliseconds > 0)
                            {
                                // We have a fully downloaded file - mark is as downloaded
                                TouchedFiles.Instance.Add(file.Path);
                                TouchedFiles.Instance.Add(Path.GetDirectoryName(file.Path));
                                return EpisodeStateFactory.Instance.GetState<EpisodeStateDownloaded>();
                            }
                        }
                        catch (FileNotFoundException e)
                        {
                        }
                        break;
                    }
                case EpisodeEvent.Download:
                    {
                        return EpisodeStateFactory.Instance.GetState<EpisodeStateDownloading>();
                    }
            }

            return null;
        }
    }
}
