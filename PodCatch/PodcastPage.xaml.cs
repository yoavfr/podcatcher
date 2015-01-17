using PodCatch.Common;
using PodCatch.DataModel;
using PodCatch.ViewModels;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Networking.BackgroundTransfer;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Item Detail Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234232

namespace PodCatch
{
    /// <summary>
    /// A page that displays details for a single item within a group.
    /// </summary>
    public sealed partial class PodcastPage : Page
    {
        private NavigationHelper navigationHelper;
        PodcastPageViewModel m_ViewModel;
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
        public PodcastPageViewModel DefaultViewModel
        {
            get 
            { 
                if (m_ViewModel == null)
                {
                    m_ViewModel = new PodcastPageViewModel(this, m_ServiceContext);
                }
                return m_ViewModel; 
            }
        }

        public PodcastPage()
        {
            m_ServiceContext = ApplicationServiceContext.Instance;
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += m_ViewModel.OnLoadState;
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

        #endregion

        private void RemoveFromFavoritesButtonClicked(object sender, RoutedEventArgs e)
        {
            BottomAppBar.IsOpen = false;
            m_ViewModel.RemoveFromFavorites();
            NavigationHelper.GoBack();
        }
        private async void AddToFavoritesAppBarButtonClicked(object sender, RoutedEventArgs e)
        {
            BottomAppBar.IsOpen = false;
            await m_ViewModel.AddToFavorites();
            NavigationHelper.GoBack();
        }

        private void PlayButton_Clicked (object sender, RoutedEventArgs e)
        {
            AppBarButton playButton = (AppBarButton)sender;
            EpisodeViewModel episode = (EpisodeViewModel)playButton.DataContext;
            m_ViewModel.TogglePlayState(episode);
        }

        private void PlayEpisodeSlider_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            Slider slider = (Slider)sender;
            EpisodeViewModel episode = (EpisodeViewModel)slider.DataContext;
            m_ViewModel.ExecuteManipulateSliderCommand(episode);
        }

        private void PlayEpisodeSlider_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            Slider slider = (Slider)sender;
            EpisodeViewModel episode = (EpisodeViewModel)slider.DataContext;
            long sliderValue = (long)slider.Value;
            m_ViewModel.ExecuteReleaseSliderCommand(episode, sliderValue);
        }

        private void PlayEpisodeSlider_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            Slider slider = (Slider)sender;
            EpisodeViewModel episode = (EpisodeViewModel)slider.DataContext;
            long sliderValue = (long)slider.Value;
            m_ViewModel.ExecuteReleaseSliderCommand(episode, sliderValue);

        }

        private void ShowMoreButtonClicked(object sender, RoutedEventArgs e)
        {
            BottomAppBar.IsOpen = false;
        }

        private void Grid_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            e.Handled = true;
            EpisodeViewModel selectedEpisode = (EpisodeViewModel)((Grid)sender).DataContext;
            Point point = e.GetPosition(this);
            m_ViewModel.ExecuteEpisodeRightClickedCommand(selectedEpisode, point);
        }

        private void episodesListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            EpisodeViewModel episode = (EpisodeViewModel)e.ClickedItem;
            m_ViewModel.TogglePlayState(episode);
        }

        private void RefreshButtonClicked(object sender, RoutedEventArgs e)
        {
            BottomAppBar.IsOpen = false;
        }
    }
}