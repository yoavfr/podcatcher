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

namespace PodCatch.WindowsPhone
{
    public class ForegroundMediaPlayer : ServiceConsumer, IMediaPlayer
    {
        private AutoResetEvent SererInitialized = new AutoResetEvent(false);
        private bool isMyBackgroundTaskRunning = false;


        public ForegroundMediaPlayer(IServiceContext serviceContext): base (serviceContext)
        {
            StartBackgroundAudioTask();
        }

        private bool IsMyBackgroundTaskRunning
        {
            get
            {
                if (isMyBackgroundTaskRunning)
                    return true;

                object value = ApplicationData.Current.LocalSettings.ConsumeValue(PhoneConstants.BackgroundTaskState);
                if (value == null)
                {
                    return false;
                }
                else
                {
                    isMyBackgroundTaskRunning = ((String)value).Equals(PhoneConstants.BackgroundTaskRunning);
                    return isMyBackgroundTaskRunning;
                }
            }
        }

        private string CurrentTrack
        {
            get
            {
                object value = ApplicationData.Current.LocalSettings.ConsumeValue(PhoneConstants.CurrentTrack);
                if (value != null)
                {
                    return (String)value;
                }
                else
                    return String.Empty;
            }
        }

        public void OnForegroundActivated()
        {
            App.Current.Suspending += ForegroundApp_Suspending;
            App.Current.Resuming += ForegroundApp_Resuming;
            ApplicationData.Current.LocalSettings.PutValue(PhoneConstants.AppState, PhoneConstants.ForegroundAppActive);
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
                //if yes, reconnect to media play handlers
                AddMediaPlayerEventHandlers();

                //send message to background task that app is resumed, so it can start sending notifications
                ValueSet messageDictionary = new ValueSet();
                messageDictionary.Add(PhoneConstants.AppResumed, DateTime.Now.ToString());
                BackgroundMediaPlayer.SendMessageToBackground(messageDictionary);

                /*if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing)
                {
                    playButton.Content = "| |";     // Change to pause button
                }
                else
                {
                    playButton.Content = ">";     // Change to play button
                }
                txtCurrentTrack.Text = CurrentTrack;*/
            }
            else
            {
                /*playButton.Content = ">";     // Change to play button
                txtCurrentTrack.Text = "";*/
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
        async void MediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
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
        async void BackgroundMediaPlayer_MessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            foreach (string key in e.Data.Keys)
            {
                switch (key)
                {
                    case PhoneConstants.Trackchanged:
                        //When foreground app is active change track based on background message
                        /*await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            txtCurrentTrack.Text = (string)e.Data[key];
                        }
                        );*/
                        break;
                    case PhoneConstants.BackgroundTaskStarted:
                        //Wait for Background Task to be initialized before starting playback
                        //Debug.WriteLine("Background Task started");
                        SererInitialized.Set();
                        break;
                }
            }
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
            var message = new ValueSet();
            message.Add(PhoneConstants.EpisodePath, episodePath);
            BackgroundMediaPlayer.SendMessageToBackground(message);
            if (NowPlaying != null && NowPlaying != episode)
            {
                NowPlaying.PostEvent(EpisodeEvent.Pause);
            }
            NowPlaying = episode;
        }

        private void RemoveMediaPlayerEventHandlers()
        {
            BackgroundMediaPlayer.Current.CurrentStateChanged -= MediaPlayer_CurrentStateChanged;
            BackgroundMediaPlayer.MessageReceivedFromBackground -= BackgroundMediaPlayer_MessageReceivedFromBackground;
        }

        /// <summary>
        /// Subscribes to MediaPlayer events
        /// </summary>
        private void AddMediaPlayerEventHandlers()
        {
            BackgroundMediaPlayer.Current.CurrentStateChanged += MediaPlayer_CurrentStateChanged;
            BackgroundMediaPlayer.MessageReceivedFromBackground += BackgroundMediaPlayer_MessageReceivedFromBackground;
        }

        private async Task StartBackgroundAudioTask()
        {
            AddMediaPlayerEventHandlers();
            await ThreadManager.DispatchOnUIthread(() =>
            {
                bool result = SererInitialized.WaitOne(2000);
                if (result != true)
                {
                    throw new Exception("Background Audio Task didn't start in expected time");
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
        }

        public void SkipBackward(Episode episode)
        {
        }
    }
}
