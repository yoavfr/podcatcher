using Podcatch.Common.StateMachine;
using PodCatch.Common;
using PodCatch.DataModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodCatch.ViewModels
{
    public class EpisodePageViewModel : BaseViewModel<IPodcastDataSource>
    {
        private Episode m_Episode;
        private Podcast m_Podcast;
        private IMediaPlayer m_MediaPlayer;
        private bool m_Scanning;

        public EpisodePageViewModel(IPodcastDataSource podcastDataSource, IServiceContext serviceContext) : base (podcastDataSource, serviceContext)
        {
        }

        public void Load(string episodeId)
        {
            m_Episode = Data.GetEpisode(episodeId);
            m_Podcast = Data.GetPodcastByEpisodeId(episodeId);
            m_MediaPlayer = ServiceContext.GetService<IMediaPlayer>();
            UpdateFields();
            m_Episode.PropertyChanged += OnDataChanged;
            m_Podcast.PropertyChanged += OnDataChanged;
        }

        private void OnDataChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateFields();
        }


        private string m_EpisodeTitle;
        public string EpisodeTitle
        {
            get
            {
                return m_EpisodeTitle;
            }
            set
            {
                if (m_EpisodeTitle != value)
                {
                    m_EpisodeTitle = value;
                    NotifyPropertyChanged(() => EpisodeTitle);
                }
            }
        }

        private string m_PodcastTitle;
        public string PodcastTitle
        {
            get
            {
                return m_PodcastTitle;
            }
            set
            {
                if (m_PodcastTitle != value)
                {
                    m_PodcastTitle = value;
                    NotifyPropertyChanged(() => PodcastTitle);
                }
            }
        }

        private string m_Image;
        public string Image
        {
            get
            {
                return m_Image;
            }
            set
            {
                if (m_Image != value)
                {
                    m_Image = value;
                    NotifyPropertyChanged(() => Image);
                }
            }
        }

        private string m_PublishDate;

        public string PublishDate
        {
            get
            {
                return m_PublishDate;
            }
            set
            {
                if (m_PublishDate != value)
                {
                    m_PublishDate = value;
                    NotifyPropertyChanged(() => PublishDate);
                }
            }
        }

        private string m_EpisodeDescription;
        public string EpisodeDescription
        {
            get
            {
                return m_EpisodeDescription;
            }
            set
            {
                if (m_EpisodeDescription != value)
                {
                    m_EpisodeDescription = value;
                    NotifyPropertyChanged(() => EpisodeDescription);
                }
            }
        }

        private IState<Episode, EpisodeEvent> m_EpisodeState;
        public IState<Episode, EpisodeEvent> EpisodeState
        {
            get
            {
                return m_EpisodeState;
            }
            set
            {
                if (m_EpisodeState != value)
                {
                    m_EpisodeState = value;
                    NotifyPropertyChanged(() => EpisodeState);
                }
            }
        }

        private TimeSpan m_EpisodeDuration;
        public TimeSpan EpisodeDuration
        {
            get
            {
                return m_EpisodeDuration;
            }
            set
            {
                if (m_EpisodeDuration != value)
                {
                    m_EpisodeDuration = value;
                    NotifyPropertyChanged(() => EpisodeDuration);
                }
            }
        }

        private TimeSpan m_EpisodePosition;
        public TimeSpan EpisodePosition
        {
            get
            {
                return m_EpisodePosition;
            }
            set
            {
                if (m_EpisodePosition != value)
                {
                    m_EpisodePosition = value;
                    // don't update position when scanning
                    if (! m_Scanning)
                    {
                        NotifyPropertyChanged(() => EpisodePosition);
                    }
                }
            }
        }

        private double m_DownloadProgress;

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

        private bool m_Played;

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

        public void TogglePlayState()
        {
            if (m_Episode.State is EpisodeStatePendingDownload)
            {
                m_Episode.Download();
            }
            else if (m_Episode.State is EpisodeStateDownloaded)
            {
                if (m_Episode.Played)
                {
                    m_Episode.Position = TimeSpan.FromSeconds(0);
                    m_Episode.Played = false;
                }
                Task t = m_MediaPlayer.Play(m_Episode);
            }
            else if (m_Episode.State is EpisodeStatePlaying)
            {
                m_MediaPlayer.Pause(m_Episode);
            }
        }

        protected override void UpdateFields()
        {
            if (m_Episode != null)
            {
                EpisodeTitle = m_Episode.Title;
                PublishDate = m_Episode.PublishDate.ToString("D");
                EpisodeDescription = m_Episode.FormattedDescription;
                EpisodeState = m_Episode.State;
                EpisodeDuration = m_Episode.Duration;
                EpisodePosition = m_Episode.Position;
                DownloadProgress = m_Episode.DownloadProgress;
                Played = m_Episode.Played;
            }
            if (m_Podcast != null)
            {
                Image = m_Podcast.Image;
                PodcastTitle = m_Podcast.Title;
            }
        }

        public void SkipForward()
        {
            m_MediaPlayer.SkipForward(m_Episode);
        }

        public void SkipBackward()
        {
            m_MediaPlayer.SkipBackward(m_Episode);
        }

        internal void ScanStart(EpisodePageViewModel episode)
        {
            m_Scanning = true;
            if (m_MediaPlayer.IsEpisodePlaying(m_Episode))
            {
                m_Episode.PostEvent(EpisodeEvent.Scan);
            }
        }

        internal void ScanDone(EpisodePageViewModel episode, long sliderValue)
        {
            m_Scanning = false;
            if (m_MediaPlayer.IsEpisodePlaying(m_Episode))
            {
                m_Episode.Position = m_MediaPlayer.Position = TimeSpan.FromTicks(sliderValue);
                m_Episode.PostEvent(EpisodeEvent.Play);
            }
        }
    }
}
