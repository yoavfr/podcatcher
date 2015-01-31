using PodCatch.Common;
using PodCatch.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

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
        public StartPageViewModel DefaultViewModel
        {
            get
            {
                if (m_ViewModel == null)
                {
                    m_ViewModel = new StartPageViewModel(this, m_ServiceContext);
                }
                return m_ViewModel;
            }
        }

        public StartPage()
        {
            m_ServiceContext = ApplicationServiceContext.Instance;
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += m_ViewModel.OnLoadState;
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
            await m_ViewModel.OnPodcastTapped(selectedPodcast, e.GetPosition(this));
        }

        private async void HoldingPodcast(object sender, HoldingRoutedEventArgs e)
        {
            e.Handled = true;
            Grid grid = (Grid)sender;
            PodcastSummaryViewModel selectedPodcast = (PodcastSummaryViewModel)grid.DataContext;
            await m_ViewModel.OnPodcastTapped(selectedPodcast, e.GetPosition(this));
        }
    }
}