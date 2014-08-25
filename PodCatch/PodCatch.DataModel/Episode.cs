using Podcatch.StateMachine;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.Data.Html;
using Windows.Foundation;
using Windows.Storage;

namespace PodCatch.DataModel
{
    [DataContract]
    public class Episode : INotifyPropertyChanged, IBasicLogger 
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

        public Episode(Uri uri)
        {
            Uri = uri;
            m_StateMachine = new SimpleStateMachine<Episode, EpisodeEvent>(this, this, 0);
            m_StateMachine.InitState(EpisodeStateFactory.Instance.GetState<EpisodeStatePendingDownload>(), true);
            m_StateMachine.StartPumpEvents();
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

        public bool Played 
        { 
            get
            {
                return m_Played;
            }
            set
            {
                m_Played = value;
                NotifyPropertyChanged("Played");
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
                m_DownloadProgress = value;
                NotifyPropertyChanged("DownloadProgress");
            }
        }

        public void UpdateFromCache(Episode fromCache)
        {
            Title = fromCache.Title;
            Duration = fromCache.Duration;
            Uri = fromCache.Uri;
            Description = fromCache.Description;
        }

        public string FullFileName
        {
            get
            {
                string s = System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, FileName);
                return s;
            }
        }

        public string FileName
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

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
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
            return String.Format("Episode Id {0}. Uri {1}", Id, Uri);
        }
    }
}
