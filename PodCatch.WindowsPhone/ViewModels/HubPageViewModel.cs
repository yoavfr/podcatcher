﻿using PodCatch.Common;
using PodCatch.DataModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PodCatch.ViewModels
{
    public class HubPageViewModel : BaseViewModel<IPodcastDataSource>
    {
        private EpisodeViewModel m_NowPlaying;
        private bool m_ShowNowPlaying;
        private IMediaPlayer m_MediaPlayer;
        private IPodcastDataSource m_PodcastDataSource;

        public HubPageViewModel(IServiceContext serviceContext)
            : base(serviceContext.GetService<IPodcastDataSource>(), serviceContext)
        {
            m_MediaPlayer = serviceContext.GetService<IMediaPlayer>();
            m_PodcastDataSource = serviceContext.GetService<IPodcastDataSource>();
            ObservableCollection<PodcastGroup> podcastGroups = Data.GetGroups();
            podcastGroups.CollectionChanged += OnPodcastGroupsChanged;

            // load from cache
            ThreadManager.RunInBackground(async () => {
                await Data.Load(false);
                // Connect the currently playing episode from the media player after done loading all episodes
                m_MediaPlayer.Connect();
            });

            Task t = RegisterBackgroundTask();
            UpdateFields();
        }

        public EpisodeViewModel NowPlaying
        {
            get
            {
                return m_NowPlaying;
            }
            set
            {
                if (m_NowPlaying != value)
                {
                    m_NowPlaying = value;
                }
                NotifyPropertyChanged(() => NowPlaying);
            }
        }

        public bool ShowNowPlaying
        {
            get
            {
                return m_ShowNowPlaying;
            }
            set
            {
                if (m_ShowNowPlaying != value)
                {
                    m_ShowNowPlaying = value;
                    NotifyPropertyChanged(() => ShowNowPlaying);
                }
            }
        }

        PodcastGroupViewModel m_Favorites;
        public PodcastGroupViewModel Favorites
        {
            get
            {
                return m_Favorites;
            }
            set
            {
                if (m_Favorites != value)
                {
                    m_Favorites = value;
                    NotifyPropertyChanged(() => Favorites);
                }
            }
        }

        public PodcastGroupViewModel m_SearchResults;
        public PodcastGroupViewModel SearchResults
        {
            get
            {
                return m_SearchResults;
            }
            set
            {
                if (m_SearchResults != value)
                {
                    m_SearchResults = value;
                    NotifyPropertyChanged(() => SearchResults);
                }
            }
        }

        private void OnPodcastGroupsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateFields(e);
        }

        private async Task RegisterBackgroundTask()
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                task.Value.Unregister(true);
            }

            BackgroundTaskBuilder builder = new BackgroundTaskBuilder();
            builder.Name = "PodCatchHouseKeeping";
            builder.TaskEntryPoint = "PodCatch.BackgroundTasks.BackgroundTask";
            builder.SetTrigger(new MaintenanceTrigger(15, false));
            builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
            builder.AddCondition(new SystemCondition(SystemConditionType.UserNotPresent));
            try
            {
                BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();
                IBackgroundTaskRegistration backgroundTaskRegistration = builder.Register();
                backgroundTaskRegistration.Completed += ((s, a) => ThreadManager.RunInBackground(() => Data.Load(true)));
            }
            catch (Exception ex)
            {
                Tracer.TraceError("Error registering background task: {0}", ex);
            }
        }

        protected override void UpdateFields()
        {
            UpdateFields(null);
            if (m_MediaPlayer != null && m_MediaPlayer.NowPlaying != null)
            {
                Episode episode = m_PodcastDataSource.GetEpisode(m_MediaPlayer.NowPlaying);
                if (episode != null)
                {
                    NowPlaying = new EpisodeViewModel(episode, ServiceContext);
                }
            }
            ShowNowPlaying = NowPlaying != null;
        }

        protected void UpdateFields(NotifyCollectionChangedEventArgs e)
        {
            ThreadManager.DispatchOnUIthread(() =>
                {
                    if (Favorites == null)
                    {
                        var favoritesGroup = Data.GetGroups().FirstOrDefault((group) => group.Id == Constants.FavoritesGroupId);
                        if (favoritesGroup != null)
                        {
                            Favorites = new PodcastGroupViewModel(favoritesGroup, ServiceContext);
                        }
                    }

                    var searchResults = Data.GetGroups().FirstOrDefault((group) => group.Id == Constants.SearchGroupId);
                    if (searchResults != null)
                    {
                        SearchResults = new PodcastGroupViewModel(searchResults, ServiceContext);
                    }
                });
        }

        internal void OnSkipForward()
        {
            m_NowPlaying.SkipForward();
        }

        internal void OnSkipBackward()
        {
            m_NowPlaying.SkipBackward();
        }
    }
}