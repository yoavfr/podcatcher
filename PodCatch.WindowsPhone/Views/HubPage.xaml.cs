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
using Windows.UI.Input;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace PodCatch.WindowsPhone
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HubPage : Page
    {
        private HubPageViewModel m_ViewModel;
        private IServiceContext m_ServiceContext;
        private NavigationHelper m_NavigationHelper;
        private ITracer m_Tracer;
        public HubPage()
        {
            m_ServiceContext = PhoneServiceContext.Instance;
            m_Tracer = m_ServiceContext.GetService<ITracer>();
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

        public void OnLoadState(object sender, LoadStateEventArgs e)
        {
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
            var searchResults = await ThreadManager.RunInBackground<IEnumerable<Podcast>>(async () =>
            {
                return await m_ViewModel.Data.Search(searchTerm);
            });
            m_ViewModel.Data.UpdateSearchResults(searchResults);
            await ThreadManager.RunInBackground(async () =>
            {
                await m_ViewModel.Data.RefreshSearchResults();
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
            Grid grid = (Grid)sender;
            if (e.HoldingState == HoldingState.Started)
            {
                PodcastSummaryViewModel selectedPodcast = (PodcastSummaryViewModel)grid.DataContext;
                await OnPodcastHolding(selectedPodcast, e.GetPosition(this));
            }
            e.Handled = true;
        }

        private async Task OnPodcastHolding(PodcastSummaryViewModel podcast, Point position)
        {
            PopupMenu popupMenu = new PopupMenu();

            if (m_ViewModel.Data.IsPodcastInFavorites(podcast.Data))
            {
                popupMenu.Commands.Add(new UICommand() { Id = 1, Label = "Remove from favorites" });
            }
            else
            {
                popupMenu.Commands.Add(new UICommand() { Id = 2, Label = "Add to favorites" });
            }

            m_Tracer.TraceInformation("OnPodcastHolding - before popup");
            IUICommand selectedCommand = await popupMenu.ShowAsync(position);
            m_Tracer.TraceInformation("OnPodcastHolding - after popup");

            if (selectedCommand == null)
            {
                return;
            }
            switch ((int)selectedCommand.Id)
            {
                case 1: // Remove from favorites
                    await ThreadManager.RunInBackground(() => m_ViewModel.Data.RemoveFromFavorites(podcast.Data));
                    break;

                case 2: // Add to favorites
                    // Don't wait for this - It will leave the m_ShowingPopUp open
                    await ThreadManager.RunInBackground(async () => 
                        {
                            await m_ViewModel.Data.AddToFavorites(podcast.Data);
                            podcast.DownloadEpisodes();
                        });
                    break;
            }
        }

        private void OnPodcastTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            Grid grid = (Grid)sender;
            PodcastSummaryViewModel selectedPodcast = (PodcastSummaryViewModel)grid.DataContext;
            Frame.Navigate(typeof(PodcastPage), selectedPodcast.Id);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            m_NavigationHelper.OnNavigatedFrom(e);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            m_NavigationHelper.OnNavigatedTo(e);
        }
    }
}
