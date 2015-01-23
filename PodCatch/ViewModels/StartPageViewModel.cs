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
using Windows.UI.Xaml.Controls;

namespace PodCatch.ViewModels
{
    public class StartPageViewModel : BaseViewModel<IPodcastDataSource>
    {
        private ObservableCollection<PodcastGroupViewModel> m_Groups = new ObservableCollection<PodcastGroupViewModel>();
        private bool m_ShowingPopUp;
        private RelayCommand m_SearchForPodcastCommand;
        private StartPage m_View;

        public StartPageViewModel(StartPage startPage, IServiceContext serviceContext)
            : base(serviceContext.GetService<IPodcastDataSource>(), serviceContext)
        {
            m_View = startPage;
            ObservableCollection<PodcastGroup> podcastGroups = Data.GetGroups();
            podcastGroups.CollectionChanged += OnPodcastGroupsChanged;

            // load from cache
            UIThread.RunInBackground(() => Data.Load(false));

            Task t = RegisterBackgroundTask();
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
                backgroundTaskRegistration.Completed += ((s, a) => Data.Load(true));
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
        }

        protected void UpdateFields(NotifyCollectionChangedEventArgs e)
        {
            UIThread.Dispatch(() =>
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

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        public void OnLoadState(object sender, LoadStateEventArgs e)
        {
            // MediaElementWrapper needs the dispatcher to conrtol the MediaElement on this thread
            MediaElementWrapper.Dispatcher = m_View.Dispatcher;
        }

        public ObservableCollection<PodcastGroupViewModel> Groups
        {
            get
            {
                return m_Groups;
            }
        }

        public RelayCommand SearchForPodcastCommand
        {
            get
            {
                if (m_SearchForPodcastCommand == null)
                {
                    m_SearchForPodcastCommand = new RelayCommand(ExecuteSearchForPodcastCommand);
                }
                return m_SearchForPodcastCommand;
            }
        }

        public async Task OnPodcastTapped(PodcastSummaryViewModel podcast, Point position)
        {
            if (m_ShowingPopUp == true)
            {
                return;
            }
            m_ShowingPopUp = true;
            try
            {
                ;

                PopupMenu popupMenu = new PopupMenu();
                // this is useful for debugging
                //popupMenu.Commands.Add(new UICommand(){Id=1, Label="Copy RSS feed URL to clipboard"});

                if (Data.IsPodcastInFavorites(podcast.Data))
                {
                    popupMenu.Commands.Add(new UICommand() { Id = 2, Label = "Remove from favorites" });
                }
                else
                {
                    popupMenu.Commands.Add(new UICommand() { Id = 3, Label = "Add to favorites" });
                }
                IUICommand selectedCommand = await popupMenu.ShowAsync(position);
                if (selectedCommand == null)
                {
                    return;
                }
                switch ((int)selectedCommand.Id)
                {
                    case 1: // Copy RSS feed to clipboard
                        DataPackage dataPackage = new DataPackage();
                        dataPackage.SetText(podcast.Data.PodcastUri);
                        Clipboard.SetContent(dataPackage);
                        break;

                    case 2: // Remove from favorites
                        Task t = Data.RemoveFromFavorites(podcast.Data);
                        m_View.NavigationHelper.GoBack();
                        break;

                    case 3: // Add to favorites
                        // Don't wait for this - It will leave the m_ShowingPopUp open
                        t = Data.AddToFavorites(podcast.Data);
                        podcast.DownloadEpisodes();
                        break;
                }
            }
            finally
            {
                m_ShowingPopUp = false;
            }
        }

        private async void ExecuteSearchForPodcastCommand()
        {
            m_View.BottomAppBar.IsOpen = false;
            // show input dialog
            InputMessageDialog dlg = new InputMessageDialog("Search term or RSS feed URL:");
            bool result = await dlg.ShowAsync();

            // cancel pressed
            if (result == false)
            {
                return;
            }

            string searchTerm = dlg.TextBox.Text;
            
            UIThread.RunInBackground(() =>
                {
                    Data.Search(searchTerm);
                });
            
        }
    }
}