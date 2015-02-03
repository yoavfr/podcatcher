using PodCatch.Common;
using PodCatch.DataModel;
using System;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace PodCatch
{
    public class MediaElementWrapper : ServiceConsumer
    {
        private static MediaElementWrapper s_Insatnce;
        private Episode m_NowPlaying;
        private TimeSpan m_Position;
        private DateTime m_LastSaveTime;
        private MediaElement m_MediaElement;

        private MediaElement MediaElement
        {
            get
            {
                if (m_MediaElement == null)
                {
                    DependencyObject rootGrid = VisualTreeHelper.GetChild(Window.Current.Content, 0);
                    m_MediaElement = (MediaElement)VisualTreeHelper.GetChild(rootGrid, 0);
                    m_MediaElement.AutoPlay = true;
                    m_MediaElement.MediaOpened += MediaElement_MediaOpened;
                    m_MediaElement.CurrentStateChanged += MediaElement_CurrentStateChanged;
                }
                return m_MediaElement;
            }
        }

        private SystemMediaTransportControls SystemMediaTransportControls { get; set; }

        public static CoreDispatcher Dispatcher { private get; set; }

        private IPodcastDataSource m_PodcastDataSource;

        public Episode NowPlaying { get; private set; }

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
            if (NowPlaying != null && NowPlaying != episode)
            {
                Pause(NowPlaying);
            }

            StorageFile storageFile = await episode.GetStorageFile();
            if (storageFile == null)
            {
                Tracer.TraceInformation("MediaElementWrapper.Play() - can't find file {0}", storageFile);
                // TODO: error message to user
                return;
            }
            var stream = await storageFile.OpenReadAsync();
            MediaElement.SetSource(stream, storageFile.ContentType);
            Position = episode.Position;
            NowPlaying = episode;
            Task t = episode.PostEvent(EpisodeEvent.Play);
            MediaElement.Play();
            MediaElement.MediaEnded += MediaElement_MediaEnded;
        }

        private async void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            Episode episode = NowPlaying;
            if (episode != null)
            {
                NowPlaying = null;
                episode.Played = true;
                Task t = episode.PostEvent(EpisodeEvent.DonePlaying);
                await m_PodcastDataSource.Store();
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
                    s_Insatnce = new MediaElementWrapper(ApplicationServiceContext.Instance);
                }
                return s_Insatnce;
            }
        }

        private MediaElementWrapper(IServiceContext serviceContext)
            : base(serviceContext)
        {
            m_PodcastDataSource = serviceContext.GetService<IPodcastDataSource>();

            SystemMediaTransportControls = SystemMediaTransportControls.GetForCurrentView();
            SystemMediaTransportControls.ButtonPressed += SystemMediaTransportControls_ButtonPressed;
            SystemMediaTransportControls.IsPlayEnabled = true;
            SystemMediaTransportControls.IsPauseEnabled = true;

            // Periodically update position in playing episode and every 10 seconds save state too
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += (sender, e) =>
            {
                Episode episode = NowPlaying;
                if (episode != null)
                {
                    // don't update position when slider is being manipulated.
                    if (!(episode.State is EpisodeStateScanning))
                    {
                        episode.Position = Position;
                    }
                    if (DateTime.UtcNow.AddSeconds(-10) > m_LastSaveTime)
                    {
                        // save location
                        Task t = m_PodcastDataSource.Store();
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
                Episode episode = NowPlaying;
                if (episode == null || Dispatcher == null)
                {
                    return;
                }
                switch (args.Button)
                {
                    case SystemMediaTransportControlsButton.Play:
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                Task t = Play(episode);
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
                Tracer.TraceInformation("{0}", e);
            }
        }

        private void MediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            ((MediaElement)sender).Position = m_Position;
            Episode episode = NowPlaying;
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
            return NowPlaying == episode;
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