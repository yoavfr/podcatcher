using PodCatch.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.Storage;
using PodCatch.Common;
using Windows.Foundation.Collections;
using Windows.Foundation;
using PodCatch.WindowsPhone.BackgroundAudioTask;
using Windows.UI.Xaml;

namespace PodCatch.WindowsPhone
{
    public class ForegroundMediaPlayer : ServiceConsumer, IMediaPlayer
    {
        private AutoResetEvent m_ServerInitialized = new AutoResetEvent(false);
        private bool m_IsBackgroundTaskRunning = false;
        private IPodcastDataSource m_PodcastDataSource;
        private DateTime m_LastSaveTime;
        private string m_CurrentEpisodeId;


        public ForegroundMediaPlayer(IServiceContext serviceContext): base (serviceContext)
        {

            m_PodcastDataSource = serviceContext.GetService<IPodcastDataSource>();
            
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

        public void Connect()
        {
            Tracer.TraceInformation("ForegroundMediaPlayer - Connecting");
            // start the background audio task
            StartBackgroundAudioTask();

            // If the background task is already playing an episode - update that episode's status 
            if (m_CurrentEpisodeId != null)
            {
                var currentEpisode = m_PodcastDataSource.GetEpisode(m_CurrentEpisodeId);
                if (currentEpisode != null)
                {
                    NowPlaying = currentEpisode;
                    NowPlaying.Position = BackgroundMediaPlayer.Current.Position;
                    if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing)
                    {
                        NowPlaying.PostEvent(EpisodeEvent.Play);
                    }
                }
            }

            // If media ended when we were not running - sync this
            var endedEpisodeId = (string)ApplicationData.Current.LocalSettings.ConsumeValue(PhoneConstants.MediaEnded);
            if (endedEpisodeId != null)
            {
                var endedEpisode = m_PodcastDataSource.GetEpisode(endedEpisodeId);
                if (endedEpisode != null)
                {
                    OnMediaEnded(endedEpisode);
                }
            }

            //Adding App suspension handlers here so that we can unsubscribe handlers 
            //that access to BackgroundMediaPlayer events
            App.Current.Suspending += ForegroundApp_Suspending;
            App.Current.Resuming += ForegroundApp_Resuming;
            ApplicationData.Current.LocalSettings.PutValue(PhoneConstants.AppState, PhoneConstants.ForegroundAppActive);
        }

        private bool IsMyBackgroundTaskRunning
        {
            get
            {
                if (m_IsBackgroundTaskRunning)
                    return true;

                object value = ApplicationData.Current.LocalSettings.ConsumeValue(PhoneConstants.BackgroundTaskState);
                if (value == null)
                {
                    return false;
                }
                else
                {
                    m_IsBackgroundTaskRunning = ((String)value).Equals(PhoneConstants.BackgroundTaskRunning);
                    return m_IsBackgroundTaskRunning;
                }
            }
        }

        /// <summary>
        /// Sends message to background informing app has resumed
        /// Subscribe to MediaPlayer events
        /// </summary>
        void ForegroundApp_Resuming(object sender, object e)
        {
            ApplicationData.Current.LocalSettings.PutValue(PhoneConstants.AppState, PhoneConstants.ForegroundAppActive);

            // Verify if the task was running before
            if (IsMyBackgroundTaskRunning)
            {
                // if yes, reconnect to media play handlers
                AddMediaPlayerEventHandlers();

                //send message to background task that app is resumed, so it can start sending notifications
                ValueSet messageDictionary = new ValueSet();
                messageDictionary.Add(PhoneConstants.AppResumed, DateTime.Now.ToString());
                BackgroundMediaPlayer.SendMessageToBackground(messageDictionary);
            }
        }

        /// <summary>
        /// Send message to Background process that app is to be suspended
        /// Stop clock and slider when suspending
        /// Unsubscribe handlers for MediaPlayer events
        /// </summary>
        void ForegroundApp_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            ValueSet messageDictionary = new ValueSet();
            messageDictionary.Add(PhoneConstants.AppSuspended, DateTime.Now.ToString());
            BackgroundMediaPlayer.SendMessageToBackground(messageDictionary);
            RemoveMediaPlayerEventHandlers();
            ApplicationData.Current.LocalSettings.PutValue(PhoneConstants.AppState, PhoneConstants.ForegroundAppSuspended);
            deferral.Complete();
        }

        /// <summary>
        /// MediaPlayer state changed event handlers. 
        /// Note that we can subscribe to events even if Media Player is playing media in background
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        async void OnCurrentStateChanged(MediaPlayer sender, object args)
        {
            switch (sender.CurrentState)
            {
                case MediaPlayerState.Playing:
                    if (NowPlaying != null)
                    {
                        NowPlaying.PostEvent(EpisodeEvent.Play);
                    }
                    break;
                case MediaPlayerState.Paused:
                    if (NowPlaying != null)
                    {
                        NowPlaying.PostEvent(EpisodeEvent.Pause);
                    }
                    break;
            }
        }


        /// <summary>
        /// This event fired when a message is recieved from Background Process
        /// </summary>
        async void OnMessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            foreach (string key in e.Data.Keys)
            {
                switch (key)
                {
                    case PhoneConstants.BackgroundTaskStarted:
                        //Wait for Background Task to be initialized before starting playback
                        Tracer.TraceInformation("Background Media task started");
                        m_ServerInitialized.Set();
                        m_CurrentEpisodeId = (string)e.Data[key];
                        break;
                    case PhoneConstants.MediaEnded:
                        Tracer.TraceInformation("Media ended");
                        await OnMediaEnded(NowPlaying);
                        break;
                }
            }
        }

        private async Task OnMediaEnded(Episode endedEpisode)
        {
            await ThreadManager.RunInBackground(async () =>
            {
                await endedEpisode.PostEvent(EpisodeEvent.DonePlaying);
                await m_PodcastDataSource.Store();
            });
        }

        public Episode NowPlaying { get; private set;}

        public TimeSpan Duration
        {
            get 
            {
                return  BackgroundMediaPlayer.Current.NaturalDuration; 
            }
        }

        public TimeSpan Position
        {
            get
            {
                return BackgroundMediaPlayer.Current.Position;
            }
            set
            {
                BackgroundMediaPlayer.Current.Position = value;
            }
        }

        public async Task Play(Episode episode)
        {
            if (!IsMyBackgroundTaskRunning)
            {
                await StartBackgroundAudioTask();
            }

            var file = await episode.GetStorageFile();
            var episodePath = file.Path;
            
            // set position
            var positionMessage = new ValueSet();
            positionMessage.Add(PhoneConstants.Position, episode.Position.Ticks.ToString());
            BackgroundMediaPlayer.SendMessageToBackground(positionMessage);

            // send episode path to background + id of current episode so that when we resume we can reconstruct our state
            var startMessage = new ValueSet();
            startMessage.Add(PhoneConstants.EpisodePath, episodePath);
            startMessage.Add(PhoneConstants.EpisodeId, episode.Id);
            BackgroundMediaPlayer.SendMessageToBackground(startMessage);
            
            // Switching to a new podcast - mark the previously played as paused
            if (NowPlaying != null && NowPlaying != episode)
            {
                NowPlaying.PostEvent(EpisodeEvent.Pause);
            }

            // Keep what we are playing now
            NowPlaying = episode;
        }

        private void RemoveMediaPlayerEventHandlers()
        {
            BackgroundMediaPlayer.Current.CurrentStateChanged -= OnCurrentStateChanged;
            BackgroundMediaPlayer.MessageReceivedFromBackground -= OnMessageReceivedFromBackground;
        }

        /// <summary>
        /// Subscribes to MediaPlayer events
        /// </summary>
        private void AddMediaPlayerEventHandlers()
        {
            BackgroundMediaPlayer.Current.CurrentStateChanged += OnCurrentStateChanged;
            BackgroundMediaPlayer.MessageReceivedFromBackground += OnMessageReceivedFromBackground;
        }

        private async Task StartBackgroundAudioTask()
        {
            AddMediaPlayerEventHandlers();
            
            ValueSet message = new ValueSet();
            message.Add(PhoneConstants.AppResumed, "");
            BackgroundMediaPlayer.SendMessageToBackground(message);

            await ThreadManager.DispatchOnUIthread(() =>
            {
                bool result = m_ServerInitialized.WaitOne(2000);
                if (result != true)
                {
                    Tracer.TraceWarning("Background Audio Task didn't start in expected time");
                }
            });
        }

        private void BackgroundTaskInitializationCompleted(IAsyncAction action, AsyncStatus status)
        {
            if (status == AsyncStatus.Completed)
            {
                //Debug.WriteLine("Background Audio Task initialized");
            }
            else if (status == AsyncStatus.Error)
            {
                //Debug.WriteLine("Background Audio Task could not initialized due to an error ::" + action.ErrorCode.ToString());
            }
        }

        public void Pause(Episode episode)
        {
            BackgroundMediaPlayer.Current.Pause();
        }

        public bool IsEpisodePlaying(Episode episode)
        {
            return NowPlaying == episode;
        }

        public void SkipForward(Episode episode)
        {
            long positionTicks = Position.Ticks;
            long durationTicks = Duration.Ticks;
            long increment = durationTicks / 20;
            positionTicks = Math.Min(durationTicks, positionTicks + increment);
            Position = TimeSpan.FromTicks(positionTicks);
            episode.Position = Position;
        }

        public void SkipBackward(Episode episode)
        {
            long positionTicks = Position.Ticks;
            long durationTicks = Duration.Ticks;
            long increment = durationTicks / 20;
            positionTicks = Math.Max(0, positionTicks - increment);
            Position = TimeSpan.FromTicks(positionTicks);
            episode.Position = Position;
        }
    }
}
