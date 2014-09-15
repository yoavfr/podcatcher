using PodCatch.DataModel;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.UI.Core;
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
        private SystemMediaTransportControls SystemMediaTransportControls { get; set;}
        public static CoreDispatcher Dispatcher { private get; set; }

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

        public async Task Play(Episode episode)
        {
            if (m_NowPlaying != null && m_NowPlaying != episode)
            {
                Pause(m_NowPlaying);
            }

            StorageFile storageFile = await episode.GetStorageFile();
            var stream = await storageFile.OpenReadAsync();
            MediaElement.SetSource(stream, storageFile.ContentType);
            Position = episode.Position;
            m_NowPlaying = episode;
            episode.PostEvent(EpisodeEvent.Play);
            MediaElement.Play();
            MediaElement.MediaEnded += MediaElement_MediaEnded;
        }

        async void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            Episode episode = m_NowPlaying;
            if (episode != null)
            {
                m_NowPlaying = null;
                episode.Played = true;
                Task t = episode.PostEvent(EpisodeEvent.DonePlaying);
                await PodcastDataSource.Instance.Store();
            }
            MediaElement.MediaEnded -= MediaElement_MediaEnded;
        }

        public void Pause(Episode episode)
        {
            MediaElement.Pause();
            episode.Position = Position;
            episode.PostEvent(EpisodeEvent.Pause);
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
            MediaElement.CurrentStateChanged += MediaElement_CurrentStateChanged;

            SystemMediaTransportControls = SystemMediaTransportControls.GetForCurrentView();
            SystemMediaTransportControls.ButtonPressed += SystemMediaTransportControls_ButtonPressed;
            SystemMediaTransportControls.IsPlayEnabled = true;
            SystemMediaTransportControls.IsPauseEnabled = true;

            // Periodically update position in playing episode and every 10 seconds save state too
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += (sender, e) =>
            {
                Episode episode = m_NowPlaying;
                if (episode != null)
                {
                    // don't update position when slider is being manipulated 
                    if (episode.State is EpisodeStatePlaying && 
                        (episode.Position - Position).Duration() < TimeSpan.FromSeconds(1)) // hack - clicking directly on the slider will cause a big difference
                    {
                        episode.Position = Position;
                    }
                    if (DateTime.UtcNow.AddSeconds(-10) > m_LastSaveTime)
                    {
                        // save location
                        Task t = PodcastDataSource.Instance.Store();
                        m_LastSaveTime = DateTime.UtcNow;
                    }
                }
            };
            timer.Start();
        }

        private void MediaElement_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
            switch (MediaElement.CurrentState)
            {
                case MediaElementState.Playing:
                    SystemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Playing;
                    break;
                case MediaElementState.Paused:
                    SystemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
                    break;
                case MediaElementState.Stopped:
                    SystemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Stopped;
                    break;
                case MediaElementState.Closed:
                    SystemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Closed;
                    break;
                default:
                    break;
            }
        }

        private async void SystemMediaTransportControls_ButtonPressed(Windows.Media.SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            try
            {
                Episode episode = m_NowPlaying;
                if (episode == null || Dispatcher == null)
                {
                    return;
                }
                switch (args.Button)
                {
                    case SystemMediaTransportControlsButton.Play:
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                Play(episode);
                            });
                        break;
                    case SystemMediaTransportControlsButton.Pause:
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                Pause(episode);
                            });
                        break;
                    case SystemMediaTransportControlsButton.Next:
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                SkipForward(episode);
                            });
                        break;
                    case SystemMediaTransportControlsButton.Previous:
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                SkipBackward(episode);
                            });
                        break;

                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("{0}",e);
            }
        }

        void MediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            ((MediaElement)sender).Position = m_Position;
            Episode episode = m_NowPlaying;
            if (episode != null)
            {
                SystemMediaTransportControlsDisplayUpdater updater = SystemMediaTransportControls.DisplayUpdater;
                updater.Type = MediaPlaybackType.Music;
                updater.MusicProperties.Title = episode.Title;
                updater.Update();
                SystemMediaTransportControls.IsNextEnabled = true;
                SystemMediaTransportControls.IsPreviousEnabled = true;
            }
        }

        public bool IsEpisodePlaying(Episode episode)
        {
            return m_NowPlaying == episode;
        }

        public void SkipForward(Episode episode)
        {
            long positionTicks = Position.Ticks;
            long durationTicks = Duration.Ticks;
            long increment = durationTicks / 10;
            positionTicks = Math.Min(durationTicks, positionTicks + increment);
            Position = TimeSpan.FromTicks(positionTicks);
            episode.Position = Position;
        }

        public void SkipBackward(Episode episode)
        {
            long positionTicks = Position.Ticks;
            long durationTicks = Duration.Ticks;
            long increment = durationTicks / 10;
            positionTicks = Math.Max(0, positionTicks - increment);
            Position = TimeSpan.FromTicks(positionTicks);
            episode.Position = Position;
        }


    }
}
