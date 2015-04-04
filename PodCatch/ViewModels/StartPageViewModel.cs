using PodCatch.Common;
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
    public class StartPageViewModel : BaseViewModel<IPodcastDataSource>
    {
        private ObservableCollection<PodcastGroupViewModel> m_Groups = new ObservableCollection<PodcastGroupViewModel>();
        private EpisodeViewModel m_NowPlaying;
        private bool m_ShowNowPlaying;
        private IMediaPlayer m_MediaPlayer;

        public StartPageViewModel(IServiceContext serviceContext)
            : base(serviceContext.GetService<IPodcastDataSource>(), serviceContext)
        {
            m_MediaPlayer = serviceContext.GetService<IMediaPlayer>();
            ObservableCollection<PodcastGroup> podcastGroups = Data.GetGroups();
            podcastGroups.CollectionChanged += OnPodcastGroupsChanged;

            // load from cache
            ThreadManager.RunInBackground(() => Data.Load(false));

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

        private PodcastGroupViewModel PodcastGroupViewModelConstructor(PodcastGroup podcastGroup)
        {
            return new PodcastGroupViewModel(podcastGroup, ServiceContext);
        }

        protected override void UpdateFields()
        {
            UpdateFields(null);
            if (m_MediaPlayer != null && m_MediaPlayer.NowPlaying != null)
            {
                NowPlaying = new EpisodeViewModel(m_MediaPlayer.NowPlaying, ServiceContext);
            }
            ShowNowPlaying = NowPlaying != null;
        }

        protected void UpdateFields(NotifyCollectionChangedEventArgs e)
        {
            ThreadManager.DispatchOnUIthread(() =>
                {
                    if (e == null)
                    {
                        m_Groups.Clear();
                        m_Groups.AddAll(Data.GetGroups().Select(PodcastGroupViewModelConstructor));
                    }
                    else if (e.Action == NotifyCollectionChangedAction.Add)
                    {
                        foreach (PodcastGroup group in e.NewItems)
                        {
                            m_Groups.Add(new PodcastGroupViewModel(group, ServiceContext));
                        }
                    }
                });
        }

        public ObservableCollection<PodcastGroupViewModel> Groups
        {
            get
            {
                return m_Groups;
            }
        }

        internal void OnSkipForward()
        {
            m_MediaPlayer.SkipForward(m_MediaPlayer.NowPlaying);
        }

        internal void OnSkipBackward()
        {
            m_MediaPlayer.SkipBackward(m_MediaPlayer.NowPlaying);
        }
    }
}