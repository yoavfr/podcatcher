using PodCatch.Common;
using PodCatch.Common.Collections;
using PodCatch.DataModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Popups;

namespace PodCatch.ViewModels
{
    public class PodcastPageViewModel : BaseViewModel<IPodcastDataSource>
    {
        private PodcastPage m_View;
        private IMediaPlayer m_MediaPlayer;
        private RelayCommand m_RefreshCommand;
        private RelayCommand m_ShowMoreCommand;
        private RelayCommand m_AllPlayedCommand;
        private RelayCommand m_AllUnplayedCommand;
        private bool m_Loaded;


        public Podcast Podcast { get; set; }

        private ObservableCollection<EpisodeViewModel> m_Episodes = new ObservableCollection<EpisodeViewModel>();
        private int m_NumEpisodesToShow = 10;

        private string m_Image;

        public string Image
        {
            get { return m_Image; }
            set
            {
                if (m_Image != value)
                {
                    m_Image = value;
                    NotifyPropertyChanged(() => Image);
                }
            }
        }

        private string m_Description;

        public string Description
        {
            get { return m_Description; }
            set
            {
                if (m_Description != value)
                {
                    m_Description = value;
                    NotifyPropertyChanged(() => Description);
                }
            }
        }

        private string m_Title;

        public string Title
        {
            get { return m_Title; }
            set
            {
                if (m_Title != value)
                {
                    m_Title = value;
                    NotifyPropertyChanged(() => Title);
                }
            }
        }

        private ConcurrentObservableCollection<Episode> m_AllEpisodes = new ConcurrentObservableCollection<Episode>(episode => -episode.PublishDate.Ticks);

        /// <summary>
        /// Episodes that are visible == true. Ideally we would do the filtering in a binding converter, but this is difficult in WinRT's ICollectionView
        /// </summary>
        public ObservableCollection<EpisodeViewModel> Episodes
        {
            get
            {
                return m_Episodes;
            }
        }

        public PodcastPageViewModel(PodcastPage podcastPage, IServiceContext serviceContext)
            : base(serviceContext.GetService<IPodcastDataSource>(), serviceContext)
        {
            m_View = podcastPage;
            m_MediaPlayer = serviceContext.GetService<IMediaPlayer>();
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
            Podcast = Data.GetPodcast((String)e.NavigationParameter);
            UpdateFields();
        }

        private void OnPodcastChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateFields();
        }

        private void OnEpisodesChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateAllEpisodes();
        }

        protected override void UpdateFields()
        {
            if (Podcast == null)
            {
                return;
            }
            if (!m_Loaded)
            {
                Podcast.Episodes.CollectionChanged += OnEpisodesChanged;
                Podcast.PropertyChanged += OnPodcastChanged;
                m_Loaded = true;
            }
            Title = Podcast.Title;
            Description = Podcast.Description;
            Image = Podcast.Image;
            UpdateAllEpisodes();
        }

        private void UpdateAllEpisodes()
        {
            lock (m_AllEpisodes)
            {
                if (m_AllEpisodes.SequenceEqual(Podcast.Episodes))
                {
                    return;
                }
                m_AllEpisodes.Clear();
                m_AllEpisodes.AddAll(Podcast.Episodes);
            }
            UpdateVisibleEpisodes();
        }

        private void UpdateVisibleEpisodes()
        {
            UIThread.Dispatch(() =>
                {
                    m_Episodes.Clear();
                    int i = 0;
                    lock (m_AllEpisodes)
                    {
                        foreach (Episode episode in m_AllEpisodes)
                        {
                            if (i >= m_NumEpisodesToShow)
                            {
                                break;
                            }
                            EpisodeViewModel viewModel = new EpisodeViewModel(episode, ServiceContext);
                            viewModel.Index = i++;
                            viewModel.Data.UpdateDownloadStatus();
                            m_Episodes.Add(viewModel);
                        }
                    }
                });
        }

        public bool IsAddToFavoritesEnabled
        {
            get
            {
                bool inFavorites = Data.IsPodcastInFavorites(Podcast);
                return !inFavorites;
            }
        }

        public bool IsRemoveFromFavoritesEnabled
        {
            get
            {
                bool inFavorites = Data.IsPodcastInFavorites(Podcast);
                return inFavorites;
            }
        }

        public void RemoveFromFavorites()
        {
            Data.RemoveFromFavorites(Podcast);
        }

        public Task AddToFavorites()
        {
            return Data.AddToFavorites(Podcast);
        }

        public async void ExecuteEpisodeRightClickedCommand(EpisodeViewModel episode, Point point)
        {
            PopupMenu popupMenu = new PopupMenu();
            if (episode.Played)
            {
                popupMenu.Commands.Add(new UICommand() { Id = 1, Label = "Mark as unplayed" });
            }
            else
            {
                popupMenu.Commands.Add(new UICommand() { Id = 2, Label = "Mark as played" });
            }

            if (episode.Data.State is EpisodeStateDownloaded)
            {
                popupMenu.Commands.Add(new UICommand() { Id = 3, Label = "Download again" });
            }
            try
            {
                IUICommand selectedCommand = await popupMenu.ShowAsync(point);
                if (selectedCommand == null)
                {
                    return;
                }
                switch ((int)selectedCommand.Id)
                {
                    case 1:
                        episode.Data.Played = false;
                        UIThread.RunInBackground(() => Data.Store());
                        break;

                    case 2:
                        episode.Data.Played = true;
                        UIThread.RunInBackground(() => Data.Store());

                        break;

                    case 3:
                        Task t = episode.Data.PostEvent(EpisodeEvent.Refresh);
                        break;
                }
            }
            catch (Exception ex)
            {
                Tracer.TraceError("PodcastPage.xaml.Grid_RightTapped() - Error occured displaying popup menu {0}", ex);
            }
        }


        public RelayCommand RefreshCommand
        {
            get
            {
                if (m_RefreshCommand == null)
                {
                    m_RefreshCommand = new RelayCommand(ExecuteRefreshCommand);
                }
                return m_RefreshCommand;
            }
        }

        private async void ExecuteRefreshCommand()
        {
            MessageDialog dlg = null;
            Podcast podcastDataItem = Podcast;
            try
            {
                await UIThread.RunInBackground(async () => await podcastDataItem.RefreshFromRss(true));
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

        public RelayCommand ShowMoreCommand
        {
            get
            {
                if (m_ShowMoreCommand == null)
                {
                    m_ShowMoreCommand = new RelayCommand(ExecuteShowMoreCommand);
                }
                return m_ShowMoreCommand;
            }
        }

        private void ExecuteShowMoreCommand()
        {
            m_NumEpisodesToShow += 10;
            UpdateVisibleEpisodes();
        }

        public RelayCommand AllPlayedCommand
        {
            get
            {
                if (m_AllPlayedCommand == null)
                {
                    m_AllPlayedCommand = new RelayCommand(ExecuteAllPlayedCommand);
                }
                return m_AllPlayedCommand;
            }
        }

        private void ExecuteAllPlayedCommand()
        {
            foreach (Episode episode in m_AllEpisodes)
            {
                episode.Played = true;
            }
            UIThread.RunInBackground(() => Data.Store());
        }

        public RelayCommand AllUnplayedCommand
        {
            get
            {
                if (m_AllUnplayedCommand == null)
                {
                    m_AllUnplayedCommand = new RelayCommand(ExecuteAllUnplayedCommand);
                }
                return m_AllUnplayedCommand;
            }
        }

        private void ExecuteAllUnplayedCommand()
        {
            foreach (Episode episode in m_AllEpisodes)
            {
                episode.Played = false;
            }
            UIThread.RunInBackground(() => Data.Store());
        }

        public void ExecuteReleaseSliderCommand(EpisodeViewModel episode, long sliderValue)
        {
            if (m_MediaPlayer.IsEpisodePlaying(episode.Data))
            {
                episode.Data.Position = m_MediaPlayer.Position = TimeSpan.FromTicks(sliderValue);
                episode.Data.PostEvent(EpisodeEvent.Play);
            }
        }

        public void ExecuteManipulateSliderCommand(EpisodeViewModel episode)
        {
            if (m_MediaPlayer.IsEpisodePlaying(episode.Data))
            {
                episode.Data.PostEvent(EpisodeEvent.Scan);
            }
        }
    }
}