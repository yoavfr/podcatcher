using PodCatch.Common;
using PodCatch.DataModel;
using PodCatch.Strings;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Networking.BackgroundTransfer;
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
    public sealed partial class PodcatchPath : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private MediaElementWrapper MediaPlayer
        {
            get
            {
                return MediaElementWrapper.Instance;
            }
        }
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

        public PodcatchPath()
        {
            this.InitializeComponent();
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
            Podcast item = await PodcastDataSource.Instance.GetItemAsync((String)e.NavigationParameter); 
            this.DefaultViewModel["Item"] = item;
            this.DefaultViewModel["Episodes"] = item.Episodes;

            bool inFavorites = PodcastDataSource.Instance.IsPodcastInGroup(Constants.FavoritesGroupId, item.UniqueId);
            if (inFavorites)
            {
                AddToFavoritesAppBarButton.IsEnabled = false;
                RemoveFromFavoritesAppBarButton.IsEnabled = true;
            }
            else
            {
                AddToFavoritesAppBarButton.IsEnabled = true;
                RemoveFromFavoritesAppBarButton.IsEnabled = false;
            }
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
            PodcastDataSource.Instance.RemoveItem("Favorites", (Podcast)DefaultViewModel["Item"]);
            PodcastDataSource.Instance.Store();
            NavigationHelper.GoBack();
        }

        private void RssFeedToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            BottomAppBar.IsOpen = false;
            Podcast podcastDataItem = (Podcast)DefaultViewModel["Item"];
            DataPackage dataPackage = new DataPackage();
            dataPackage.SetText(podcastDataItem.Uri);
            Clipboard.SetContent(dataPackage);
        }


        private void PlayButton_Clicked (object sender, RoutedEventArgs e)
        {
            AppBarButton playButton = (AppBarButton)sender;
            Episode episode = (Episode)playButton.DataContext;
            switch (episode.State)
            {
                case EpisodeState.PendingDownload:
                    {
                        Task t = episode.DownloadAsync();
                        break;
                    }
                case (EpisodeState.Downloaded):
                    {
                        Task t = MediaPlayer.PlayAsync(episode);
                        break;
                    }
                case (EpisodeState.Playing):
                    {
                        Task t = MediaPlayer.PauseAsync(episode);
                        break;
                    }
            }
        }

        private void PlayEpisodeSlider_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            Slider slider = (Slider)sender;
            Episode episode = (Episode)slider.DataContext;
            if (MediaPlayer.IsEpisodePlaying(episode))
            {
                episode.StartScan();
            }
        }

        private void PlayEpisodeSlider_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            Slider slider = (Slider)sender;
            Episode episode = (Episode)slider.DataContext;
            if (MediaPlayer.IsEpisodePlaying(episode))
            {
                episode.EndScan();
                MediaPlayer.Position = TimeSpan.FromTicks((long)slider.Value);
            }
        }

        private void PlayEpisodeSlider_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            Slider slider = (Slider)sender;
            Episode episode = (Episode)slider.DataContext;
            if (MediaPlayer.IsEpisodePlaying(episode))
            {
                MediaPlayer.Position = TimeSpan.FromTicks((long)slider.Value);
                episode.EndScan();
            }
        }
    }
}