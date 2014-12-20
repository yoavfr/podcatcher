using PodCatch.Common;
using PodCatch.DataModel;
using PodCatch.Search;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Foundation;

// The Grouped Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234231

namespace PodCatch
{
    /// <summary>
    /// A page that displays a grouped collection of items.
    /// </summary>
    public sealed partial class StartPage : Page
    {
        private bool m_ShowingPopUp;
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private IServiceContext m_ServiceContext;
        private IPodcastDataSource m_PodcastDataSource; 
        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        public StartPage()
        {
            this.InitializeComponent();
            m_ServiceContext = ApplicationServiceContext.Instance;
            m_PodcastDataSource = m_ServiceContext.GetService<IPodcastDataSource>();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
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
        private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // MediaElementWrapper needs the dispatcher to conrtol the MediaElement on this thread
            MediaElementWrapper.Dispatcher = Dispatcher;
            
            // load from cache
            this.DefaultViewModel["Groups"] = m_PodcastDataSource.GetGroups();
            m_PodcastDataSource.Load(false);
        }

        /// <summary>
        /// Invoked when a group header is clicked.
        /// </summary>
        /// <param name="sender">The Button used as a group header for the selected group.</param>
        /// <param name="e">Event data that describes how the click was initiated.</param>
        void Header_Click(object sender, RoutedEventArgs e)
        {
            // Determine what group the Button instance represents
            var group = (sender as FrameworkElement).DataContext;

            // Navigate to the appropriate destination page, configuring the new page
            // by passing required information as a navigation parameter
            this.Frame.Navigate(typeof(GroupPage), ((PodcastGroup)group).Id);
        }

        /// <summary>
        /// Invoked when an item within a group is clicked.
        /// </summary>
        /// <param name="sender">The GridView (or ListView when the application is snapped)
        /// displaying the item clicked.</param>
        /// <param name="e">Event data that describes the item clicked.</param>
        void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Navigate to the appropriate destination page, configuring the new page
            // by passing required information as a navigation parameter
            string podcastId = ((Podcast)e.ClickedItem).Id;
            this.Frame.Navigate(typeof(PodcastPage), podcastId);
        }

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            MessageDialog dialog = null;
            navigationHelper.OnNavigatedTo(e);
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
                backgroundTaskRegistration.Completed += OnBackgroundTask_Completed;
            }
            catch (Exception ex)
            {
                dialog = new MessageDialog(string.Format("Failed to create PodCatch background task. {0}", ex));
            }
            if (dialog != null)
            {
                //await dialog.ShowAsync();
            }
        }

        private async void OnBackgroundTask_Completed(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            await m_PodcastDataSource.Load(true);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private async void SearchForPodcastButtonClicked(object sender, RoutedEventArgs e)
        {
            BottomAppBar.IsOpen = false;

            // show input dialog
            InputMessageDialog dlg = new InputMessageDialog("Search term or RSS feed URL:");
            bool result = await dlg.ShowAsync();

            // cancel pressed
            if (result == false)
            {
                return;
            }

            // this is the search term
            string searchTerm = dlg.TextBox.Text;
            if (string.IsNullOrEmpty(searchTerm))
            {
                return;
            }
            
            IEnumerable<Podcast> matches;

            // RSS feed URL
            Uri validUri;
            if (Uri.TryCreate(searchTerm, UriKind.Absolute, out validUri) && 
                (validUri.Scheme == "http" || validUri.Scheme == "https"))
            {
                Podcast newItem = new Podcast(m_ServiceContext)
                {
                    PodcastUri = searchTerm
                };
                matches = new List<Podcast>() { newItem };
            }
            else
            {
                // Search term
                BottomAppBar.IsOpen = false;
                ITunesSearch iTunesSearch = new ITunesSearch(m_ServiceContext);
                matches = await iTunesSearch.FindAsync(searchTerm, 50);
                matches = matches.Where(podcast => !m_PodcastDataSource.IsPodcastInFavorites(podcast));
            }
            
            // add podcasts shell to data source
            m_PodcastDataSource.SetSearchResults(matches);
        }

        private async void PodcastRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            e.Handled = true;
            await ShowPodcastPopUpMenue(sender, e.GetPosition(this));
        }

        private async void HoldingPodcast(object sender, HoldingRoutedEventArgs e)
        {
            e.Handled = true;
            await ShowPodcastPopUpMenue(sender, e.GetPosition(this));
        }

        private async Task ShowPodcastPopUpMenue(object sender, Point position)
        {
            if (m_ShowingPopUp == true)
            {
                return;
            }
            m_ShowingPopUp = true;
            try
            {
                Grid grid = (Grid)sender;
                Podcast selectedPodcast = (Podcast)grid.DataContext;

                PopupMenu popupMenu = new PopupMenu();
                // this is useful for debugging
                //popupMenu.Commands.Add(new UICommand(){Id=1, Label="Copy RSS feed URL to clipboard"});

                if (m_PodcastDataSource.IsPodcastInFavorites(selectedPodcast))
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
                        dataPackage.SetText(selectedPodcast.PodcastUri);
                        Clipboard.SetContent(dataPackage);
                        break;
                    case 2: // Remove from favorites
                        m_PodcastDataSource.RemoveFromFavorites(selectedPodcast);
                        NavigationHelper.GoBack();
                        break;
                    case 3: // Add to favorites
                        // Don't wait for this - It will leave the m_ShowingPopUp open
                        Task t = m_PodcastDataSource.AddToFavorites(selectedPodcast);
                        break;
                }
            }
            finally
            {
                m_ShowingPopUp = false;
            }
        }
    }
}