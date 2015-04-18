/*
 * (c) Copyright Microsoft Corporation.
This source is subject to the Microsoft Public License (Ms-PL).
All other rights reserved.
 */
using System;
using System.Diagnostics;
using System.Threading;
using Windows.ApplicationModel.Background;
using Windows.Media;
using Windows.Media.Playback;
using PodCatch.Common;
using Windows.Foundation.Collections;
using Windows.Storage;

/* This is the Sample background task that will start running the first time 
 * MediaPlayer singleton instance is accessed from foreground. When a new audio 
 * or video app comes into picture the task is expected to recieve the cancelled 
 * event. User can save state and shutdown MediaPlayer at that time. When foreground 
 * app is resumed or restarted check if your music is still playing or continue from
 * previous state.
 * 
 * This task also implements SystemMediaTransportControl apis for windows phone universal 
 * volume control. Unlike Windows 8.1 where there are different views in phone context, 
 * SystemMediaTransportControl is singleton in nature bound to the process in which it is 
 * initialized. If you want to hook up volume controls for the background task, do not 
 * implement SystemMediaTransportControls in foreground app process.
 */

namespace PodCatch.WindowsPhone.BackgroundAudioTask
{
    /// <summary>
    /// Enum to identify foreground app state
    /// </summary>
    enum ForegroundAppStatus
    {
        Active,
        Suspended,
        Unknown
    }

    /// <summary>
    /// Impletements IBackgroundTask to provide an entry point for app code to be run in background. 
    /// Also takes care of handling UVC and communication channel with foreground
    /// </summary>
    public sealed class BackgroundAudioTask : IBackgroundTask
    {

        private SystemMediaTransportControls m_SystemMediaTransportControl;
        private BackgroundTaskDeferral m_Deferral; // Used to keep task alive
        private ForegroundAppStatus m_ForegroundAppState = ForegroundAppStatus.Unknown;
        private AutoResetEvent m_BackgroundTaskStarted = new AutoResetEvent(false);
        private bool m_Backgroundtaskrunning = false;
        private TimeSpan m_StartPosition;
        private string m_EpisodeId;

        /// <summary>
        /// The Run method is the entry point of a background task. 
        /// </summary>
        /// <param name="taskInstance"></param>
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("Background Audio Task " + taskInstance.Task.Name + " starting...");
            m_Deferral = taskInstance.GetDeferral();
            // Initialize SMTC object to talk with UVC. 
            //Note that, this is intended to run after app is paused and 
            //hence all the logic must be written to run in background process
            m_SystemMediaTransportControl = SystemMediaTransportControls.GetForCurrentView();
            m_SystemMediaTransportControl.ButtonPressed += systemmediatransportcontrol_ButtonPressed;
            m_SystemMediaTransportControl.PropertyChanged += systemmediatransportcontrol_PropertyChanged;
            m_SystemMediaTransportControl.IsEnabled = true;
            m_SystemMediaTransportControl.IsPauseEnabled = true;
            m_SystemMediaTransportControl.IsPlayEnabled = true;
            m_SystemMediaTransportControl.IsNextEnabled = true;
            m_SystemMediaTransportControl.IsPreviousEnabled = true;

            // Associate a cancellation and completed handlers with the background task.
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);
            taskInstance.Task.Completed += Taskcompleted;

            var value = ApplicationData.Current.LocalSettings.ConsumeValue(PhoneConstants.AppState);
            if (value == null)
                m_ForegroundAppState = ForegroundAppStatus.Unknown;
            else
                m_ForegroundAppState = (ForegroundAppStatus)Enum.Parse(typeof(ForegroundAppStatus), value.ToString());

            //Add handlers for MediaPlayer
            BackgroundMediaPlayer.Current.CurrentStateChanged += Current_CurrentStateChanged;
            BackgroundMediaPlayer.Current.MediaOpened += MediaPlayer_MediaOpened;

            //Add handlers for playlist trackchanged
            //Playlist.TrackChanged += playList_TrackChanged;

            //Initialize message channel 
            BackgroundMediaPlayer.MessageReceivedFromForeground += BackgroundMediaPlayer_MessageReceivedFromForeground;

            //Send information to foreground that background task has been started if app is active
            if (m_ForegroundAppState != ForegroundAppStatus.Suspended)
            {
                SendStartedMessageToForeground();
            }
            m_BackgroundTaskStarted.Set();
            m_Backgroundtaskrunning = true;

            ApplicationData.Current.LocalSettings.PutValue(PhoneConstants.BackgroundTaskState, PhoneConstants.BackgroundTaskRunning);
        }

        private void SendStartedMessageToForeground()
        {
            ValueSet message = new ValueSet();
            message.Add(PhoneConstants.BackgroundTaskStarted, m_EpisodeId);
            BackgroundMediaPlayer.SendMessageToForeground(message);
        }

        /// <summary>
        /// Indicate that the background task is completed.
        /// </summary>       
        void Taskcompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            Debug.WriteLine("MyBackgroundAudioTask " + sender.TaskId + " Completed...");
            m_Deferral.Complete();
        }

        /// <summary>
        /// Handles background task cancellation. Task cancellation happens due to :
        /// 1. Another Media app comes into foreground and starts playing music 
        /// 2. Resource pressure. Your task is consuming more CPU and memory than allowed.
        /// In either case, save state so that if foreground app resumes it can know where to start.
        /// </summary>
        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            // You get some time here to save your state before process and resources are reclaimed
            Debug.WriteLine("MyBackgroundAudioTask " + sender.Task.TaskId + " Cancel Requested... Reason: {0}", reason);
            try
            {
                //save state
                //ApplicationData.Current.LocalSettings.PutValue(PhoneConstants.CurrentTrack, Playlist.CurrentTrackName);
                ApplicationData.Current.LocalSettings.PutValue(PhoneConstants.Position, BackgroundMediaPlayer.Current.Position.ToString());
                ApplicationData.Current.LocalSettings.PutValue(PhoneConstants.BackgroundTaskState, PhoneConstants.BackgroundTaskCancelled);
                ApplicationData.Current.LocalSettings.PutValue(PhoneConstants.AppState, Enum.GetName(typeof(ForegroundAppStatus), m_ForegroundAppState));
                m_Backgroundtaskrunning = false;
                //unsubscribe event handlers
                m_SystemMediaTransportControl.ButtonPressed -= systemmediatransportcontrol_ButtonPressed;
                m_SystemMediaTransportControl.PropertyChanged -= systemmediatransportcontrol_PropertyChanged;
                //Playlist.TrackChanged -= playList_TrackChanged;

                //clear objects task cancellation can happen uninterrupted
                //playlistManager.ClearPlaylist();
                //playlistManager = null;
                BackgroundMediaPlayer.Shutdown(); // shutdown media pipeline
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            m_Deferral.Complete(); // signals task completion. 
            Debug.WriteLine("MyBackgroundAudioTask Cancel complete...");
        }

        /// <summary>
        /// Update UVC using SystemMediaTransPortControl apis
        /// </summary>
        private void UpdateUVCOnNewTrack()
        {
            m_SystemMediaTransportControl.PlaybackStatus = MediaPlaybackStatus.Playing;
            m_SystemMediaTransportControl.DisplayUpdater.Type = MediaPlaybackType.Music;
            //systemmediatransportcontrol.DisplayUpdater.MusicProperties.Title = Playlist.CurrentTrackName;
            m_SystemMediaTransportControl.DisplayUpdater.Update();
        }

        /// <summary>
        /// Fires when any SystemMediaTransportControl property is changed by system or user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void systemmediatransportcontrol_PropertyChanged(SystemMediaTransportControls sender, SystemMediaTransportControlsPropertyChangedEventArgs args)
        {
            //TODO: If soundlevel turns to muted, app can choose to pause the music
        }

        /// <summary>
        /// This function controls the button events from UVC.
        /// This code if not run in background process, will not be able to handle button pressed events when app is suspended.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void systemmediatransportcontrol_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    Debug.WriteLine("UVC play button pressed");
                    // If music is in paused state, for a period of more than 5 minutes, 
                    //app will get task cancellation and it cannot run code. 
                    //However, user can still play music by pressing play via UVC unless a new app comes in clears UVC.
                    //When this happens, the task gets re-initialized and that is asynchronous and hence the wait
                    if (!m_Backgroundtaskrunning)
                    {
                        bool result = m_BackgroundTaskStarted.WaitOne(2000);
                        if (!result)
                            throw new Exception("Background Task didnt initialize in time");
                    }
                    BackgroundMediaPlayer.Current.Play();
                    //StartPlayback();
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    Debug.WriteLine("UVC pause button pressed");
                    try
                    {
                        BackgroundMediaPlayer.Current.Pause();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                    break;
                case SystemMediaTransportControlsButton.Next:
                    Debug.WriteLine("UVC next button pressed");
                    //SkipToNext();
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    Debug.WriteLine("UVC previous button pressed");
                    //SkipToPrevious();
                    break;
            }
        }

        void Current_CurrentStateChanged(MediaPlayer sender, object args)
        {
            if (sender.CurrentState == MediaPlayerState.Playing)
            {
                if (m_StartPosition > TimeSpan.FromTicks(0))
                {
                    sender.Position = m_StartPosition;
                    m_StartPosition = TimeSpan.FromTicks(0);
                    sender.PlaybackMediaMarkers.Clear();
                }
                sender.Volume = 1;
                m_SystemMediaTransportControl.PlaybackStatus = MediaPlaybackStatus.Playing;
            }
            else if (sender.CurrentState == MediaPlayerState.Paused)
            {
                m_SystemMediaTransportControl.PlaybackStatus = MediaPlaybackStatus.Paused;
            }
        }

        /// <summary>
        /// Fired when MediaPlayer is ready to play the track
        /// </summary>
        void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            // wait for media to be ready
            sender.Play();
        }

        /// <summary>
        /// Fires when a message is recieved from the foreground app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void BackgroundMediaPlayer_MessageReceivedFromForeground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            foreach (string key in e.Data.Keys)
            {
                switch (key)
                {
                    case PhoneConstants.EpisodePath:
                        string episodePath = (string)e.Data[key];
                        var storageFile = await StorageFile.GetFileFromPathAsync(episodePath);
                        var musicProperties = await storageFile.Properties.GetMusicPropertiesAsync();
                        BackgroundMediaPlayer.Current.AutoPlay = false;
                        // Set volume to 0 - this will be set back to 1 when the correct position is assumed
                        BackgroundMediaPlayer.Current.Volume = 0;
                        BackgroundMediaPlayer.Current.SetFileSource(storageFile);
                        m_SystemMediaTransportControl.PlaybackStatus = MediaPlaybackStatus.Playing;
                        m_SystemMediaTransportControl.DisplayUpdater.Type = MediaPlaybackType.Music;
                        m_SystemMediaTransportControl.DisplayUpdater.MusicProperties.Title = musicProperties.Title;
                        m_SystemMediaTransportControl.DisplayUpdater.Update();

                        ApplicationData.Current.LocalSettings.PutValue(PhoneConstants.EpisodePath, episodePath);
                        break;
                    case PhoneConstants.Position:
                        TimeSpan position = TimeSpan.FromTicks(long.Parse((string)e.Data[key]));
                        
                        // Set the start position, If not currently playing anything we set the position 
                        // once the state changes to playing, it can be possible for a fraction of second, 
                        // playback can start before we are able to seek to new start position
                        m_StartPosition = position;

                        // if we are playing - setting the position will do the trick
                        BackgroundMediaPlayer.Current.Position = position;
                        break;
                    case PhoneConstants.AppSuspended:
                        Debug.WriteLine("App suspending"); // App is suspended, you can save your task state at this point
                        m_ForegroundAppState = ForegroundAppStatus.Suspended;
                        //ApplicationData.Current.LocalSettings.PutValue(PhoneConstants.CurrentTrack, Playlist.CurrentTrackName);
                        break;
                    case PhoneConstants.AppResumed:
                        Debug.WriteLine("App resuming"); // App is resumed, now subscribe to message channel
                        m_ForegroundAppState = ForegroundAppStatus.Active;
                        SendStartedMessageToForeground();
                        break;
                    case PhoneConstants.EpisodeId:
                        m_EpisodeId = (string)e.Data[key];
                        break;
                }
            }
        }

    }
}
