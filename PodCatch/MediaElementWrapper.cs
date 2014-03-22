using PodCatch.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace PodCatch
{
    public class MediaElementWrapper
    {
        private static MediaElementWrapper s_Insatnce;
        private Episode m_NowPlaying; 
        private TimeSpan m_Position;
        private DateTime m_LastSaveTime;
        private MediaElement MediaElement { get; set; }
        public TimeSpan Position 
        {
            get
            {
                return MediaElement.Position;
            }
            set
            {
                m_Position = value;
                MediaElement.Position = value;
            }
        }

        public TimeSpan Duration 
        { 
            get
            {
                return MediaElement.NaturalDuration.TimeSpan;
            }
        }

        public Uri Source 
        {
            get
            {
                return MediaElement.Source;
            }
            set
            {
                MediaElement.Source = value;
            }
        }
        public async Task PlayAsync(Episode episode)
        {
            if (m_NowPlaying != null && m_NowPlaying != episode)
            {
                await PauseAsync(m_NowPlaying);
            }
            Uri episodeUri = new Uri(episode.FullFileName);
            if (Source == null || !Source.Equals(episodeUri))
            {
                Source = episodeUri;
            }
            Position = episode.Position;
            m_NowPlaying = episode;
            episode.Play();
            MediaElement.Play();
        }

        public async Task PauseAsync(Episode episode)
        {
            MediaElement.Pause();
            episode.Position = Position;
            await episode.PauseAsync();
            if (m_NowPlaying == episode)
            {
                m_NowPlaying = null;
            }
        }

        public static MediaElementWrapper Instance
        {
            get
            {
                if (s_Insatnce == null)
                {
                    DependencyObject rootGrid = VisualTreeHelper.GetChild(Window.Current.Content, 0);
                    MediaElement mediaElement = (MediaElement)VisualTreeHelper.GetChild(rootGrid, 0);
                    mediaElement.AutoPlay = true;
                    s_Insatnce = new MediaElementWrapper(mediaElement);
                }
                return s_Insatnce;
            }
        }

        private MediaElementWrapper(MediaElement mediaElement)
        {
            MediaElement = mediaElement;
            MediaElement.MediaOpened += MediaElement_MediaOpened;
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += (sender, e) =>
            {
                Episode episode = m_NowPlaying;
                if (episode != null)
                {
                    episode.Duration = Duration;
                    episode.Position = Position;
                    if (DateTime.UtcNow.AddSeconds(-10) > m_LastSaveTime)
                    {
                        episode.StoreToCacheAsync();
                        m_LastSaveTime = DateTime.UtcNow;
                    }
                }
            };
            timer.Start();
        }

        void MediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            ((MediaElement)sender).Position = m_Position;
        }
    }
}
