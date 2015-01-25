using Podcatch.Common.StateMachine;
using PodCatch.Common;
using PodCatch.DataModel.Data;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Data.Html;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Core;

namespace PodCatch.DataModel
{
    public class Episode : ServiceConsumer, INotifyPropertyChanged
    {
        private TimeSpan m_Position;
        private TimeSpan m_Duration;
        private double m_DownloadProgress;
        private IStateMachine<Episode, EpisodeEvent> m_StateMachine;
        private string m_Title;
        private string m_Description;
        private bool m_Played;
        public IDownloadService m_DownloadService;

        public Episode(IServiceContext serviceContext, string podcastFileName, Uri uri)
            : base(serviceContext)
        {
            Uri = uri;
            PodcastFileName = podcastFileName;
            m_DownloadService = serviceContext.GetService<IDownloadService>();
            m_StateMachine = new SimpleStateMachine<Episode, EpisodeEvent>(serviceContext, this, 0);
            m_StateMachine.InitState(EpisodeStateFactory.Instance.GetState<EpisodeStateUnknown>(), true);
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

        public static Episode FromRoamingData(IServiceContext serviceContext, string podcastFileName, RoamingEpisodeData data)
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
                    NotifyPropertyChanged(() => Title);
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
                if (m_Description != value && m_Description != null)
                {
                    m_Description = value;
                    FormattedDescription = HtmlUtilities.ConvertToText(Description).Trim('\n', '\r', '\t', ' ');
                    int lineLimit = FormattedDescription.IndexOfOccurence("\n", 10);
                    if (lineLimit != -1)
                    {
                        FormattedShortDescription = FormattedDescription.Substring(0, lineLimit) + "\n...";
                    }
                }
            }
        }

        public DateTimeOffset PublishDate { get; set; }

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
                    NotifyPropertyChanged(() => Played);
                }
            }
        }

        private string m_FormattedDescription;

        public string FormattedDescription
        {
            get
            {
                return m_FormattedDescription;
            }
            set
            {
                if (value != m_FormattedDescription)
                {
                    m_FormattedDescription = value;
                    NotifyPropertyChanged(() => FormattedShortDescription);
                }
            }
        }

        private string m_FormattedShortDescription;

        public string FormattedShortDescription
        {
            get
            {
                return m_FormattedShortDescription;
            }
            set
            {
                if (m_FormattedShortDescription != value)
                {
                    m_FormattedShortDescription = value;
                    NotifyPropertyChanged(() => FormattedShortDescription);
                }
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
                    NotifyPropertyChanged(() => Position);
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
                    NotifyPropertyChanged(() => Duration);
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
                    NotifyPropertyChanged(() => DownloadProgress);
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

        public async Task Download()
        {
            await UpdateDownloadStatus();
            await PostEvent(EpisodeEvent.Download);
        }

        public Task UpdateDownloadStatus()
        {
            return PostEvent(EpisodeEvent.UpdateDownloadStatus);
        }

        public async Task<StorageFile> GetStorageFile()
        {
            var storageFolder = await GetStorageFolder();
            try
            {
                var folders = await storageFolder.GetFoldersAsync();
                var podcastFolder = folders.FirstOrDefault(f => f.Name == PodcastFileName);
                if (podcastFolder == null)
                {
                    return null;
                }
                var files = await podcastFolder.GetFilesAsync();
                return files.FirstOrDefault(f => f.Name == FileName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public string FolderAndFileName
        {
            get
            {
                if (Uri == null || PodcastFileName == null)
                {
                    return null;
                }
                return System.IO.Path.Combine(PodcastFileName, FileName);
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
                return Path.GetFileName(Uri.AbsolutePath.ToString());
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged<TValue>(Expression<Func<TValue>> propertyId)
        {
            string propertyName = ((MemberExpression)propertyId.Body).Member.Name;

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

        public override string ToString()
        {
            return String.Format("Episode Uri {0}", Uri);
        }
    }
}