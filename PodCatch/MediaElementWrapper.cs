using PodCatch.DataModel;
using System;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace PodCatch.Common
{
    public class MediaElementWrapper : ServiceConsumer, IMediaPlayer
    {
        private Episode m_NowPlaying;
        private TimeSpan m_Position;
        private DateTime m_LastSaveTime;
        private MediaElement m_MediaElement;

        public string EndedMediaId { get; private set; }

        public string NowPlaying { get; private set; }

        public event MediaPlayerStateChangedHandler MediaPlayerStateChanged;

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

        public async Task Play(string mediaPath, TimeSpan position, string mediaId)
        {
            // Notify on swapping out old
            if (mediaId != NowPlaying)
            {
                NotifyMediaPlayerStateChanged(MediaPlayerEvent.SwappedOut, NowPlaying);
            }

            // Keep what we are playing now
            NowPlaying = mediaId;

            StorageFile storageFile = await StorageFile.GetFileFromPathAsync(mediaPath);
            if (storageFile == null)
            {
                Tracer.TraceInformation("MediaElementWrapper.Play() - can't find file {0}", storageFile);
                // TODO: error message to user
                return;
            }

            var stream = await storageFile.OpenReadAsync();
            NowPlaying = mediaId;
            await ThreadManager.DispatchOnUIthread(() =>
            {
                Position = position;
                MediaElement.SetSource(stream, storageFile.ContentType);
                MediaElement.Play();
                MediaElement.MediaEnded += OnMediaEnded;
            });
        }

        private void OnMediaEnded(object sender, RoutedEventArgs e)
        {
            NotifyMediaPlayerStateChanged(MediaPlayerEvent.Ended, NowPlaying);

            MediaElement.MediaEnded -= OnMediaEnded;
        }

        public async void Pause()
        {
            await ThreadManager.DispatchOnUIthread(() =>
            {
                MediaElement.Pause();
            });
            NotifyMediaPlayerStateChanged(MediaPlayerEvent.Pause, NowPlaying);
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
                if (MediaElement.CurrentState == MediaElementState.Playing)
                {
                    NotifyMediaPlayerStateChanged(MediaPlayerEvent.Tick, Position);
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
                if (NowPlaying == null || Dispatcher == null)
                {
                    return;
                }
                switch (args.Button)
                {
                    case SystemMediaTransportControlsButton.Play:
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                MediaElement.Play();
                            });
                        break;

                    case SystemMediaTransportControlsButton.Pause:
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                Pause();
                            });
                        break;

                    case SystemMediaTransportControlsButton.Next:
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                SkipForward();
                            });
                        break;

                    case SystemMediaTransportControlsButton.Previous:
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                SkipBackward();
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
            NotifyMediaPlayerStateChanged(MediaPlayerEvent.Play, NowPlaying);

            SystemMediaTransportControlsDisplayUpdater updater = SystemMediaTransportControls.DisplayUpdater;
            updater.Type = MediaPlaybackType.Music;

            /*if (episode.Title != null)
            {
                updater.MusicProperties.Title = episode.Title;
            }*/

            updater.Update();
            SystemMediaTransportControls.IsNextEnabled = true;
            SystemMediaTransportControls.IsPreviousEnabled = true;
        }

        public bool IsMediaPlaying(string episodeId)
        {
            return NowPlaying == episodeId;
        }

        public void SkipForward()
        {
            long positionTicks = Position.Ticks;
            long durationTicks = Duration.Ticks;
            long increment = durationTicks / 20;
            positionTicks = Math.Min(durationTicks, positionTicks + increment);
            Position = TimeSpan.FromTicks(positionTicks);
            NotifyMediaPlayerStateChanged(MediaPlayerEvent.Tick, Position);
        }

        public void SkipBackward()
        {
            long positionTicks = Position.Ticks;
            long durationTicks = Duration.Ticks;
            long increment = durationTicks / 20;
            positionTicks = Math.Max(0, positionTicks - increment);
            Position = TimeSpan.FromTicks(positionTicks);
            NotifyMediaPlayerStateChanged(MediaPlayerEvent.Tick, Position);
        }

        private void NotifyMediaPlayerStateChanged(MediaPlayerEvent eventType, object parameter)
        {
            var handler = MediaPlayerStateChanged;
            if (handler != null)
            {
                handler(eventType, parameter);
            }
        }
    }
}