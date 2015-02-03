using Podcatch.Common.StateMachine;
using PodCatch.Common;
using PodCatch.DataModel;
using System;
using System.Threading.Tasks;

namespace PodCatch.ViewModels
{
    public class EpisodeViewModel : BaseViewModel<Episode>
    {
        private string m_Title;

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

        private string m_Description;

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
                    NotifyPropertyChanged(() => Description);
                }
            }
        }

        private string m_ShortDescription;

        public string ShortDescription
        {
            get
            {
                return m_ShortDescription;
            }
            set
            {
                if (m_ShortDescription != value)
                {
                    m_ShortDescription = value;
                    NotifyPropertyChanged(() => ShortDescription);
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

        private TimeSpan m_Position;

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

        private TimeSpan m_Duration;

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

        private IState<Episode, EpisodeEvent> m_State;

        public IState<Episode, EpisodeEvent> State
        {
            get
            {
                return m_State;
            }
            set
            {
                if (value != m_State)
                {
                    m_State = value;
                    NotifyPropertyChanged(() => State);
                }
            }
        }

        // index for alternate coloring in UI
        public int Index { get; set; }

        public EpisodeViewModel(Episode episode, IServiceContext serviceContext)
            : base(episode, serviceContext)
        {
            episode.PropertyChanged += OnEpisodeChanged;
        }

        private void OnEpisodeChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateFields();
        }

        protected override void UpdateFields()
        {
            Title = Data.Title;
            Description = Data.FormattedDescription;
            ShortDescription = Data.FormattedShortDescription;
            Played = Data.Played;
            Position = Data.Position;
            Duration = Data.Duration;
            State = Data.State;
            DownloadProgress = Data.DownloadProgress;
        }

        public void TogglePlayState()
        {
            if (Data.State is EpisodeStatePendingDownload)
            {
                Data.Download();
            }
            else if (Data.State is EpisodeStateDownloaded)
            {
                if (Data.Played)
                {
                    Data.Position = TimeSpan.FromSeconds(0);
                    Data.Played = false;
                }
                Task t = MediaElementWrapper.Instance.Play(Data);
            }
            else if (Data.State is EpisodeStatePlaying)
            {
                MediaElementWrapper.Instance.Pause(Data);
            }
        }

        public Task Download()
        {
            return Data.Download();
        }
    }
}