using PodCatch.Common;
using PodCatch.DataModel;
using PodCatch.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using System;
using Windows.UI.Input;

// The Grouped Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234231

namespace PodCatch
{
    /// <summary>
    /// A page that displays a grouped collection of items.
    /// </summary>
    public sealed partial class StartPage : Page
    {
        private StartPageViewModel m_ViewModel;
        private NavigationHelper navigationHelper;
        private IServiceContext m_ServiceContext;
        private bool m_ShowingPopUp;

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        public StartPageViewModel DefaultViewModel
        {
            get
            {
                if (m_ViewModel == null)
                {
                    m_ViewModel = new StartPageViewModel(m_ServiceContext);
                }
                return m_ViewModel;
            }
        }

        public EpisodeViewModel EpisodeViewModel
        {
            get
            {
                return m_ViewModel.NowPlaying;
            }
        }

        public StartPage()
        {
            m_ServiceContext = ApplicationServiceContext.Instance;
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += OnLoadState;
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
        }

        /// <summary>
        /// Invoked when a group header is clicked.
        /// </summary>
        /// <param name="sender">The Button used as a group header for the selected group.</param>
        /// <param name="e">Event data that describes how the click was initiated.</param>
        private void Header_Click(object sender, RoutedEventArgs e)
        {
            // Determine what group the Button instance represents
            var group = (sender as FrameworkElement).DataContext;

            // Navigate to the appropriate destination page, configuring the new page
            // by passing required information as a navigation parameter
            this.Frame.Navigate(typeof(GroupPage), ((PodcastGroupViewModel)group).Id);
        }

        /// <summary>
        /// Invoked when an item within a group is clicked.
        /// </summary>
        /// <param name="sender">The GridView (or ListView when the application is snapped)
        /// displaying the item clicked.</param>
        /// <param name="e">Event data that describes the item clicked.</param>
        private void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Navigate to the appropriate destination page, configuring the new page
            // by passing required information as a navigation parameter
            string podcastId = ((PodcastSummaryViewModel)e.ClickedItem).Id;
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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion NavigationHelper registration

        private async void PodcastRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            e.Handled = true;
            Grid grid = (Grid)sender;
            PodcastSummaryViewModel selectedPodcast = (PodcastSummaryViewModel)grid.DataContext;
            await OnPodcastTapped(selectedPodcast, e.GetPosition(this));
        }

        private async void HoldingPodcast(object sender, HoldingRoutedEventArgs e)
        {
            Grid grid = (Grid)sender;
            if(e.HoldingState == HoldingState.Started)
            {
                PodcastSummaryViewModel selectedPodcast = (PodcastSummaryViewModel)grid.DataContext;
                await OnPodcastTapped(selectedPodcast, e.GetPosition(this));
            }
            e.Handled = true;
        }

        private void OnPlayClicked(object sender, RoutedEventArgs e)
        {
            m_ViewModel.NowPlaying.TogglePlayState();
        }

        private void OnSkipForward(object sender, RoutedEventArgs e)
        {
            m_ViewModel.OnSkipForward();
        }

        private void OnSkipBackward(object sender, RoutedEventArgs e)
        {
            m_ViewModel.OnSkipBackward();
        }

        private async void OnSearchForPodcast(object sender, RoutedEventArgs e)
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

            string searchTerm = dlg.TextBox.Text;
            IEnumerable<Podcast> searchResults;

            searchResults = await ThreadManager.RunInBackground<IEnumerable<Podcast>>(async () =>
            {
                return await m_ViewModel.Data.Search(searchTerm);
            });
            m_ViewModel.Data.UpdateSearchResults(searchResults);
            await ThreadManager.RunInBackground(async () =>
            {
                await m_ViewModel.Data.RefreshSearchResults();
            });
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
                PopupMenu popupMenu = new PopupMenu();
                // this is useful for debugging
                //popupMenu.Commands.Add(new UICommand(){Id=1, Label="Copy RSS feed URL to clipboard"});

                if (m_ViewModel.Data.IsPodcastInFavorites(podcast.Data))
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
                    /*case 1: // Copy RSS feed to clipboard
                        DataPackage dataPackage = new DataPackage();
                        dataPackage.SetText(podcast.Data.PodcastUri);
                        Clipboard.SetContent(dataPackage);
                        break;*/

                    case 2: // Remove from favorites
                        Task t = m_ViewModel.Data.RemoveFromFavorites(podcast.Data);
                        NavigationHelper.GoBack();
                        break;

                    case 3: // Add to favorites
                        // Don't wait for this - It will leave the m_ShowingPopUp open
                        await ThreadManager.RunInBackground(() => m_ViewModel.Data.AddToFavorites(podcast.Data));
                        podcast.DownloadEpisodes();
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