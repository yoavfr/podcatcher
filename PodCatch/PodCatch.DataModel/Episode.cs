﻿using Podcatch.StateMachine;
using PodCatch.Common;
using PodCatch.DataModel.Data;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Data.Html;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Core;

namespace PodCatch.DataModel
{
    public class Episode : ServiceConsumer, INotifyPropertyChanged, IBasicLogger 
    {
        private TimeSpan m_Position;
        private TimeSpan m_Duration;
        private double m_DownloadProgress;
        private IStateMachine<Episode, EpisodeEvent> m_StateMachine;
        private string m_Title;
        private string m_Description;
        private Uri m_Uri;
        private bool m_Played;
        private bool m_Visible;
        public IDownloadService m_DownloadService;

        public Episode(IServiceContext serviceContext, string podcastFileName, Uri uri) : base(serviceContext)
        {
            Uri = uri;
            PodcastFileName = podcastFileName;
            m_DownloadService = serviceContext.GetService<IDownloadService>();
            m_StateMachine = new SimpleStateMachine<Episode, EpisodeEvent>(this, this, 0);
            m_StateMachine.InitState(EpisodeStateFactory.Instance.GetState<EpisodeStatePendingDownload>(), true);
            m_StateMachine.StartPumpEvents();
        }

        public static Episode FromData(IServiceContext serviceContext, string podcastFileName, EpisodeData data)
        {
            Episode episode = new Episode(serviceContext, podcastFileName, data.Uri);
            episode.Uri = data.Uri;
            episode.Title = data.Title;
            episode.Description = data.Description;
            episode.PublishDate = new DateTimeOffset(data.PublishDateTicks, TimeSpan.FromTicks(0));
            return episode;
        }

        public static Episode FromRoamingData(IServiceContext serviceContext, string podcastFileName,RoamingEpisodeData data)
        {
            Episode episode = new Episode(serviceContext, podcastFileName, new Uri(data.Uri));
            // backward compatibility with m_PositionTicks
            if (data.PositionTicks == 0)
            {
                episode.Position = TimeSpan.FromTicks(data.m_PositionTicks);
            }
            else
            {
                episode.Position = TimeSpan.FromTicks(data.PositionTicks);
            }
            episode.Played = data.Played;
            return episode;
        }

        public EpisodeData ToData()
        {
            return new EpisodeData()
            {
                Uri = Uri,
                Title = Title,
                Description = Description,
                PublishDateTicks = PublishDate.Ticks
            };
        }

        public RoamingEpisodeData ToRoamingData()
        {
            return new RoamingEpisodeData()
            {
                Uri = Uri.ToString(),
                Played = Played,
                PositionTicks = Position.Ticks
            };
        }

        public string PodcastFileName { get; set; }

        public Uri Uri { get; set; }
        
        public string Title
        {
            get
            {
                return m_Title;
            }
            set
            {
                if (m_Title != value)
                {
                    m_Title = value;
                    NotifyPropertyChanged("Title");
                }
            }
        }

        public string Description
        {
            get
            {
                return m_Description;
            }
            set
            {
                if (m_Description != value)
                {
                    m_Description = value;
                    NotifyPropertyChanged("FormattedShortDescription");
                }
            }
        }

        public DateTimeOffset PublishDate { get; set; }
        
        // index for alternate coloring in UI
        public int Index { get; set; }

        public bool Played 
        { 
            get
            {
                return m_Played;
            }
            set
            {
                if (m_Played != value)
                {
                    m_Played = value;
                    NotifyPropertyChanged("Played");
                }
            } 
        }

        public bool Visible
        {
            get
            {
                return m_Visible;
            }
            set
            {
                if (value)
                {
                    PostEvent(EpisodeEvent.UpdateDownloadStatus);
                }
                m_Visible = value;
            }
        }

        public string FormattedDescription
        {
            get
            {
                return HtmlUtilities.ConvertToText(Description).Trim('\n', '\r', '\t', ' ');
            }
        }
        public string FormattedShortDescription
        {
            get
            {
                string descriptionAsPlainString = FormattedDescription;
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
                if (m_Position != value)
                {
                    m_Position = value;
                    NotifyPropertyChanged("Position");
                }
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
                if (m_Duration != value)
                {
                    m_Duration = value;
                    NotifyPropertyChanged("Duration");
                }
            }
        }

        public IState<Episode, EpisodeEvent> State
        {
            get
            {
                return m_StateMachine.State;
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
                if (m_DownloadProgress != value)
                {
                    m_DownloadProgress = value;
                    NotifyPropertyChanged("DownloadProgress");
                }
            }
        }

        public void UpdateFromCache(Episode fromCache)
        {
            Title = fromCache.Title;
            Duration = fromCache.Duration;
            Uri = fromCache.Uri;
            Description = fromCache.Description;
            PublishDate = fromCache.PublishDate;
        }

        public async Task<StorageFolder> GetStorageFolder()
        {
            return await Windows.Storage.KnownFolders.MusicLibrary.CreateFolderAsync(Constants.ApplicationName, CreationCollisionOption.OpenIfExists);
        }

        public async Task<StorageFile> GetStorageFile()
        {
            var folder = await GetStorageFolder();
            try
            {
                return await folder.GetFileAsync(FileName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public string FileName
        {
            get
            {
                if (Uri == null || PodcastFileName == null)
                {
                    return null;
                }
                return System.IO.Path.Combine(PodcastFileName, Path.GetFileName(Uri.AbsolutePath.ToString()));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler == null || CoreApplication.Views.Count == 0)
            {
                return;
            }
            CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;

            if (dispatcher.HasThreadAccess)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
            else
            {
                IAsyncAction t = dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    handler(this, new PropertyChangedEventArgs(propertyName));
                });
            }
        }

        public bool IsDownloaded()
        {
            return !(m_StateMachine.State is EpisodeStateDownloading ||
                m_StateMachine.State is EpisodeStatePendingDownload);
        }

        public Task<IState<Episode, EpisodeEvent>> PostEvent(EpisodeEvent anEvent)
        {
            return m_StateMachine.PostEvent(anEvent);
        }

        public void LogInfo(string msg, params object[] args)
        {
            Debug.WriteLine("Info: {0}", String.Format(msg, args));
        }

        public void LogWarning(string msg, params object[] args)
        {
            Debug.WriteLine("Warning: {0}", String.Format(msg, args));
        }

        public void LogError(string msg, params object[] args)
        {
            Debug.WriteLine("Error: {0}", String.Format(msg, args));
        }

        public override string ToString()
        {
            return String.Format("Episode Uri {0}", Uri);
        }
    }
}
