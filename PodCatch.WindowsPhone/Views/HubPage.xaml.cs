using PodCatch.Common;
using PodCatch.DataModel;
using PodCatch.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace PodCatch
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HubPage : Page
    {
        private HubPageViewModel m_ViewModel;
        private IServiceContext m_ServiceContext;
        private NavigationHelper m_NavigationHelper;
        private bool m_ShowingPopUp;
        public HubPage()
        {
            m_ServiceContext = PhoneServiceContext.Instance;
            this.InitializeComponent();
            this.m_NavigationHelper = new NavigationHelper(this);
            this.m_NavigationHelper.LoadState += OnLoadState;

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        public HubPageViewModel DefaultViewModel
        {
            get
            {
                if (m_ViewModel == null)
                {
                    m_ViewModel = new HubPageViewModel(m_ServiceContext);
                }
                return m_ViewModel;
            }
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
        }

        public void OnLoadState(object sender, LoadStateEventArgs e)
        {
            // MediaElementWrapper needs the dispatcher to conrtol the MediaElement on this thread
            MediaElementWrapper.Dispatcher = Dispatcher;
        }

        private void OnSearchBoxKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                this.Focus(FocusState.Programmatic); // dismiss the keyboard
                OnSearch(((TextBox)sender).Text);
            }
        }

        private async void OnSearch(string searchTerm)
        {
            var searchResults = await UIThread.RunInBackground<IEnumerable<Podcast>>(async () =>
            {
                return await m_ViewModel.Data.Search(searchTerm);
            });
            UIThread.Dispatch(async () =>
            {
                await m_ViewModel.Data.UpdateSearchResults(searchResults);
            });
        }

        private void OnSearchButtonClick(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var searchTerm = ((TextBox)button.Tag).Text;
            OnSearch(searchTerm);
        }

        private async void OnPodcastHolding(object sender, HoldingRoutedEventArgs e)
        {
            e.Handled = true;
            Grid grid = (Grid)sender;
            PodcastSummaryViewModel selectedPodcast = (PodcastSummaryViewModel)grid.DataContext;
            await OnPodcastHolding(selectedPodcast, e.GetPosition(this));
        }

        private async void OnPodcastRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            e.Handled = true;
            Grid grid = (Grid)sender;
            PodcastSummaryViewModel selectedPodcast = (PodcastSummaryViewModel)grid.DataContext;
            await OnPodcastHolding(selectedPodcast, e.GetPosition(this));
        }

        private async Task OnPodcastHolding(PodcastSummaryViewModel podcast, Point position)
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
                    popupMenu.Commands.Add(new UICommand() { Id = 1, Label = "Remove from favorites" });
                }
                else
                {
                    popupMenu.Commands.Add(new UICommand() { Id = 2, Label = "Add to favorites" });
                }
                IUICommand selectedCommand = await popupMenu.ShowAsync(position);
                if (selectedCommand == null)
                {
                    return;
                }
                switch ((int)selectedCommand.Id)
                {
                    case 1: // Remove from favorites
                        Task t = m_ViewModel.Data.RemoveFromFavorites(podcast.Data);
                        break;

                    case 2: // Add to favorites
                        // Don't wait for this - It will leave the m_ShowingPopUp open
                        await UIThread.RunInBackground(() => m_ViewModel.Data.AddToFavorites(podcast.Data));
                        podcast.DownloadEpisodes();
                        break;
                }
            }
            finally
            {
                m_ShowingPopUp = false;
            }
        }

        private void OnPodcastTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            Grid grid = (Grid)sender;
            PodcastSummaryViewModel selectedPodcast = (PodcastSummaryViewModel)grid.DataContext;
            this.Frame.Navigate(typeof(PodcastPage), selectedPodcast.Id);
        }
    }
}
