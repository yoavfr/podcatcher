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
        private IPodcastDataSource m_PodcastDataSource; 

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
            m_PodcastDataSource = m_ServiceContext.GetService<IPodcastDataSource>();
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
            TogglePlayState(episode);
        }

        private void TogglePlayState(EpisodeViewModel episodeViewModel)
        {
            Episode episode = episodeViewModel.Data;
            if (episode.State is EpisodeStatePendingDownload)
            {
                episode.Download();
            }
            else if (episode.State is EpisodeStateDownloaded)
            {
                if (episode.Played)
                {
                    episode.Position = TimeSpan.FromSeconds(0);
                    episode.Played = false;
                }
                Task t = MediaPlayer.Play(episode);
            }
            else if (episode.State is EpisodeStatePlaying)
            {
                MediaPlayer.Pause(episode);
            }
        }

        private void PlayEpisodeSlider_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            Slider slider = (Slider)sender;
            Episode episode = (Episode)slider.DataContext;
            if (MediaPlayer.IsEpisodePlaying(episode))
            {
                episode.PostEvent(EpisodeEvent.Scan);
            }
        }

        private void PlayEpisodeSlider_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            Slider slider = (Slider)sender;
            Episode episode = (Episode)slider.DataContext;
            if (MediaPlayer.IsEpisodePlaying(episode))
            {
                episode.PostEvent(EpisodeEvent.Play);
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
                episode.PostEvent(EpisodeEvent.Play);
            }
        }

        private void ShowMoreButtonClicked(object sender, RoutedEventArgs e)
        {
            BottomAppBar.IsOpen = false;
            //m_ViewModel.Podcast.Data.DisplayNextEpisodes(10);
        }

        private async void RefreshButtonClicked(object sender, RoutedEventArgs e)
        {
            BottomAppBar.IsOpen = false;
            MessageDialog dlg = null;
            Podcast podcastDataItem = m_ViewModel.Podcast;
            try
            {
                await podcastDataItem.RefreshFromRss(true);
                await podcastDataItem.Store();
            }
            catch (Exception ex)
            {
                dlg = new MessageDialog(string.Format("Unable to refresh {0}. {1}", podcastDataItem.Title, ex.Message));
            }
            if (dlg != null) 
            {
                await dlg.ShowAsync();
            }
        }

        private async void Grid_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            e.Handled = true;

            EpisodeViewModel selectedEpisode = (EpisodeViewModel)((Grid)sender).DataContext;
            
            PopupMenu popupMenu = new PopupMenu();
            if (selectedEpisode.Played)
            {
                popupMenu.Commands.Add(new UICommand() { Id = 1, Label = "Mark as unplayed" });
            }
            else
            {
                popupMenu.Commands.Add(new UICommand() { Id = 2, Label = "Mark as played" });
            }

            if (selectedEpisode.Data.State is EpisodeStateDownloaded)
            {
                popupMenu.Commands.Add(new UICommand() { Id = 3, Label = "Download again" });
            }
            try
            {
                IUICommand selectedCommand = await popupMenu.ShowAsync(e.GetPosition(this));
                if (selectedCommand == null)
                {
                    return;
                }
                switch ((int)selectedCommand.Id)
                {
                    case 1:
                        selectedEpisode.Played = false;
                        await m_PodcastDataSource.Store();
                        break;
                    case 2:
                        selectedEpisode.Played = true;
                        await m_PodcastDataSource.Store();
                        break;
                    case 3:
                        Task t = selectedEpisode.Data.PostEvent(EpisodeEvent.Refresh);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("PodcastPage.xaml.Grid_RightTapped() - Error occured displaying popup menu {0}", ex);
            }
        }

        private void episodesListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            EpisodeViewModel episode = (EpisodeViewModel)e.ClickedItem;
            TogglePlayState(episode);
        }
    }
}