using PodCatch.Common;
using PodCatch.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.Input;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace PodCatch.WindowsPhone
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PodcastPage : Page
    {
        private NavigationHelper m_NavigationHelper;
        PodcastPageViewModel m_ViewModel;
        IServiceContext m_ServiceContext;

        public PodcastPage()
        {
            m_ServiceContext = PhoneServiceContext.Instance;
            this.InitializeComponent();

            this.m_NavigationHelper = new NavigationHelper(this);
            m_NavigationHelper.LoadState += DefaultViewModel.OnLoadState;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.m_NavigationHelper; }
        }

        public PodcastPageViewModel DefaultViewModel
        {
            get
            {
                if (m_ViewModel == null)
                {
                    m_ViewModel = new PodcastPageViewModel(m_ServiceContext);
                }
                return m_ViewModel;
            }
        }

        private void OnEpisodeClicked(object sender, ItemClickEventArgs e)
        {
            EpisodeViewModel episodeViewModel = (EpisodeViewModel)e.ClickedItem;
            Frame.Navigate(typeof(EpisodePage), episodeViewModel.Id);
        }
        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.m_NavigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.m_NavigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void OnEpisodePlay(object sender, RoutedEventArgs e)
        {
            EpisodeViewModel episode = (EpisodeViewModel)((AppBarButton)sender).DataContext;
            episode.TogglePlayState();
        }

        private void RefreshButtonClicked(object sender, RoutedEventArgs e)
        {

        }

        private void MarkAllAsPlayedClicked(object sender, RoutedEventArgs e)
        {

        }

        private void MarkAllAsUnplayedClicked(object sender, RoutedEventArgs e)
        {

        }

        private void ShowMoreButtonClicked(object sender, RoutedEventArgs e)
        {

        }

        private async void OnPodcastHolding(object sender, HoldingRoutedEventArgs e)
        {
            var element = (FrameworkElement)sender;
            if (e.HoldingState == HoldingState.Started)
            {
                var selectedPodcast = (PodcastPageViewModel)element.DataContext;
                await OnPodcastHolding(selectedPodcast, e.GetPosition(this));
            }
            e.Handled = true;
        }

        private async Task OnPodcastHolding(PodcastPageViewModel podcastViewModel, Point position)
        {
            PopupMenu popupMenu = new PopupMenu();

            if (m_ViewModel.Data.IsPodcastInFavorites(podcastViewModel.Podcast))
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
                    await ThreadManager.RunInBackground(() => m_ViewModel.Data.RemoveFromFavorites(podcastViewModel.Podcast));
                    break;

                case 2: // Add to favorites
                    // Don't wait for this - It will leave the m_ShowingPopUp open
                    await ThreadManager.RunInBackground(async () =>
                    {
                        await m_ViewModel.Data.AddToFavorites(podcastViewModel.Podcast);
                        podcastViewModel.DownloadEpisodes();
                    });
                    break;
            }
        }
    }
}
