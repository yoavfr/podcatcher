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
        public string EndedMediaId { get; private set; } 

        public event MediaPlayerStateChangedHandler MediaPlayerStateChanged;

        public ForegroundMediaPlayer(IServiceContext serviceContext): base (serviceContext)
        {
            // Periodically update position in playing media
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += (sender, e) =>
            {
                if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing)
                {
                    NotifyMediaPlayerStateChanged(MediaPlayerEvent.Tick, Position);
                }
            };
            timer.Start();
           
            Tracer.TraceInformation("ForegroundMediaPlayer - Starting");
            // start the background audio task
            StartBackgroundAudioTask();

            //Adding App suspension handlers 
            App.Current.Suspending += ForegroundApp_Suspending;
            App.Current.Resuming += ForegroundApp_Resuming;
            ApplicationData.Current.LocalSettings.PutValue(PhoneConstants.AppState, PhoneConstants.ForegroundAppActive);

            // if media played all the way when we were suspended, we should find it in here
            EndedMediaId = (string)ApplicationData.Current.LocalSettings.ConsumeValue(PhoneConstants.MediaEnded);
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
                    NotifyMediaPlayerStateChanged(MediaPlayerEvent.Play, NowPlaying);
                    break;
                case MediaPlayerState.Paused:
                    NotifyMediaPlayerStateChanged(MediaPlayerEvent.Pause, NowPlaying);
                    break;
                case MediaPlayerState.Stopped:
                    NotifyMediaPlayerStateChanged(MediaPlayerEvent.Ended, NowPlaying);    
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
                        NowPlaying = (string)e.Data[key];
                        NotifyMediaPlayerStateChanged(MediaPlayerEvent.Play, NowPlaying);
                        break;
                    case PhoneConstants.MediaEnded:
                        Tracer.TraceInformation("Media ended");
                        NotifyMediaPlayerStateChanged(MediaPlayerEvent.Ended, NowPlaying);
                        break;
                }
            }
        }

        public string NowPlaying { get; private set;}

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

        public async Task Play(string mediaPath, TimeSpan position, string mediaId)
        {
            if (!IsMyBackgroundTaskRunning)
            {
                await StartBackgroundAudioTask();
            }

            // Notify on swapping out old
            if (mediaId != NowPlaying)
            {
                NotifyMediaPlayerStateChanged(MediaPlayerEvent.SwappedOut, NowPlaying);
            }

            // Keep what we are playing now
            NowPlaying = mediaId;
            
            // set position
            var positionMessage = new ValueSet();
            positionMessage.Add(PhoneConstants.Position, position.Ticks.ToString());
            BackgroundMediaPlayer.SendMessageToBackground(positionMessage);

            // send episode path to background + id of current episode so that when we resume we can reconstruct our state
            var startMessage = new ValueSet();
            startMessage.Add(PhoneConstants.MediaPath, mediaPath);
            startMessage.Add(PhoneConstants.MediaId, mediaId);
            BackgroundMediaPlayer.SendMessageToBackground(startMessage);
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

        public void Pause()
        {
            BackgroundMediaPlayer.Current.Pause();
        }

        public bool IsMediaPlaying(string mediaId)
        {
            return NowPlaying == mediaId;
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
