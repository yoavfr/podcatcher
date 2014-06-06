using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.Data.Html;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace PodCatch.DataModel
{
    [DataContract]
    public class Episode : INotifyPropertyChanged
    {
        private TimeSpan m_Position;
        private TimeSpan m_Duration;
        private double m_DownloadProgress;
        private EpisodeState m_State;
        private string m_Title;
        private string m_Description;
        private Uri m_Uri;

        public Episode()
        {
            State = EpisodeState.PendingDownload;
        }

        [DataMember]
        public string Id { get; set; }
        public string PodcastId { get; set; }

        [DataMember]
        public Uri Uri
        {
            get
            {
                return m_Uri;
            }
            set
            {
                m_Uri = value;
                Id = m_Uri.GetHashCode().ToString();
            }
        }
        [DataMember]
        public string Title
        {
            get
            {
                return m_Title;
            }
            set
            {
                m_Title = value;
                NotifyPropertyChanged("Title");
            }
        }
        [DataMember]
        public string Description
        {
            get
            {
                return m_Description;
            }
            set
            {
                m_Description = value;
                NotifyPropertyChanged("FormattedDescription");
            }
        }

        public string FormattedDescription
        {
            get
            {
                string descriptionAsPlainString = HtmlUtilities.ConvertToText(Description).Trim('\n', '\r', '\t', ' ');
                int lineLimit = descriptionAsPlainString.IndexOfOccurence("\n", 10);
                if (lineLimit != -1)
                {
                    descriptionAsPlainString = descriptionAsPlainString.Substring(0, lineLimit) + "\n...";
                }
                return descriptionAsPlainString;
            }
        }

        public TimeSpan Position
        {
            get
            {
                return m_Position;
            }
            set
            {
                m_Position = value;
                NotifyPropertyChanged("Position");
            }
        }
        public TimeSpan Duration
        {
            get
            {
                return m_Duration;
            }
            set
            {
                m_Duration = value;
                NotifyPropertyChanged("Duration");
            }
        }

        public EpisodeState State
        {
            get
            {
                return m_State;
            }
            set
            {
                m_State = value;
                NotifyPropertyChanged("State");
            }
        }

        public double DownloadProgress
        {
            get
            {
                return m_DownloadProgress;
            }
            set
            {
                m_DownloadProgress = value;
                NotifyPropertyChanged("DownloadProgress");
            }
        }

        public async Task UpdateFromCache(Episode fromCache)
        {
            Title = fromCache.Title;
            Duration = fromCache.Duration;
            Uri = fromCache.Uri;
            Description = fromCache.Description;
            await UpdateDownloadStatus();
        }

        private async Task UpdateDownloadStatus()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            try
            {
                StorageFile file = await localFolder.GetFileAsync(FileName);
                MusicProperties musicProperties = await file.Properties.GetMusicPropertiesAsync();

                Duration = musicProperties.Duration;
                State = EpisodeState.Downloaded;
            }
            catch (FileNotFoundException e)
            {
                State = EpisodeState.PendingDownload;
            }

        }

        public string FullFileName
        {
            get
            {
                string s = System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, FileName);
                return s;
            }
        }

        private string FileName
        {
            get
            {
                if (Uri == null || PodcastId == null)
                {
                    return null;
                }
                return System.IO.Path.Combine(PodcastId, System.IO.Path.GetFileName(Uri.ToString()));
            }
        }

        public async Task Download()
        {
            if (State != EpisodeState.PendingDownload)
            {
                return;
            }

            Progress<Downloader> progress = new Progress<Downloader>((downloader) =>
            {
                ulong totalBytesToReceive = downloader.TotalBytes;
                double at = 0;
                if (totalBytesToReceive > 0)
                {
                    at = (double)downloader.DownloadedBytes / totalBytesToReceive;
                }
                DownloadProgress = at;
            });

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            try
            {
                StorageFile localFile = await localFolder.CreateFileAsync(FileName, CreationCollisionOption.ReplaceExisting);
                State = EpisodeState.Downloading;
                Downloader downloader = new Downloader(Uri, localFile, progress);
                await downloader.Download();
                
                // set duration
                MusicProperties musicProperties = await localFile.Properties.GetMusicPropertiesAsync();
                Duration = musicProperties.Duration;
                
                //set position
                Position = TimeSpan.FromMilliseconds(0);
                State = EpisodeState.Downloaded;
            }
            catch (Exception e)
            {
                State = EpisodeState.PendingDownload;
                Debug.WriteLine("Episode.Download(): error downloading {0}. {1}", Id, e);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler == null)
            {
                return;
            }
            IAsyncAction t = Dispatcher.Instance.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                handler(this,
                    new PropertyChangedEventArgs(propertyName));
            });
        }

    }
}
