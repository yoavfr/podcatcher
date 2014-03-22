﻿using PodCatch.Common;
using PodCatch.DataModel;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.Data.Html;
using Windows.Foundation;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;

namespace PodCatch.DataModel
{
    [DataContract]
    public class Episode : BaseData
    {
        
        private EpisodeState m_State;
        private TimeSpan m_Position;
        public Episode(
            string podcastUniqueId, 
            string title, 
            string description, 
            DateTimeOffset publishDate, 
            Uri uri, 
            BaseData parent, 
            ObservableCollection<Episode> parentCollection) : base(parent)
        {
            PodcastUniqueId = podcastUniqueId;
            Title = title;
            string descriptionAsPlainString = HtmlUtilities.ConvertToText(description).Trim('\n','\r','\t',' ');
            int lineLimit = descriptionAsPlainString.IndexOfOccurence("\n", 10);
            if (lineLimit != -1)
            {
                descriptionAsPlainString = descriptionAsPlainString.Substring(0, lineLimit)+"\n...";
            }
            Description = descriptionAsPlainString;
            Uri = uri;
            ParentCollection = parentCollection;

        }

        public async Task LoadStateAsync(ObservableCollection<Episode> parentCollection)
        {
            bool failed = false;
            ParentCollection = parentCollection;
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            try
            {
                await localFolder.GetFileAsync(FileName);
                SetState(EpisodeState.Downloaded);
            }
            catch (FileNotFoundException e)
            {
                failed = true;
            }
            if (failed)
            {
                SetState(EpisodeState.PendingDownload);
            }
        }

        public async Task DownloadAsync(Progress<DownloadOperation> progress)
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile localFile = await localFolder.CreateFileAsync(FileName, CreationCollisionOption.ReplaceExisting);
            BackgroundDownloader downloader = new BackgroundDownloader();
            DownloadOperation downloadOperation = downloader.CreateDownload(Uri, localFile);
            await downloadOperation.StartAsync().AsTask(progress);
            SetState(EpisodeState.Downloaded);
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
                return System.IO.Path.Combine(PodcastUniqueId, System.IO.Path.GetFileName(Uri.ToString()));
            }
        }

        private ObservableCollection<Episode> ParentCollection { get; set; }
        [DataMember]
        public string Title { get; private set; }
        [DataMember]
        public DateTimeOffset PublishDate { get; private set; }
        [DataMember]
        public string PodcastUniqueId { get; private set; }
        [DataMember]
        public string Description { get; private set; }
        public EpisodeState State
        {
            get
            {
                return m_State;
            }
        }
        private void SetState(EpisodeState state)
        {
            m_State = state;
            NotifyPropertyChanged("State");
        }

        public void Play()
        {
            if (State != EpisodeState.Downloaded)
            {
                return;
            }
            SetState(EpisodeState.Playing);
        }

        public async Task PauseAsync()
        {
            if (State != EpisodeState.Playing)
            {
                return;
            }
            SetState(EpisodeState.Downloaded);
            await StoreToCacheAsync();
        }

        public string UniqueId
        {
            get
            {
                return String.Format(@"{0}\{1}", PodcastUniqueId, Title);
            }
        }
        [DataMember]
        public Uri Uri { get; private set; }
        [DataMember]
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
                NotifyPropertyChanged("Progress");
            }
        }
        [DataMember]
        public TimeSpan Duration { get; set; }

        public double Progress
        {
            get
            {
                long durationTicks = Duration.Ticks;
                if (Duration.Ticks > 0)
                {
                    return (double)Position.Ticks / Duration.Ticks;
                }
                return 0;
            }
        }

        public int Index
        {
            get
            {
                return ParentCollection.IndexOf(this);
            }
        }

        public override string ToString()
        {
            return this.Title;
        }
    }
}
