using PodCatch.Common;
using PodCatch.DataModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodCatch.ViewModels
{
    public class PodcastPageViewModel : BaseViewModel<IPodcastDataSource>
    {
        private PodcastPage m_View;
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

        private int m_NumUnplayedEpisodes;
        public int NumUnplayedEpisodes
        {
            get
            {
                return m_NumUnplayedEpisodes;
            }
            private set
            {
                if (m_NumUnplayedEpisodes != value)
                {
                    m_NumUnplayedEpisodes = value;
                    NotifyPropertyChanged(() => NumUnplayedEpisodes);
                }
            }
        }


        private Collection<Episode> m_AllEpisodes = new ObservableCollection<Episode>();
        private Collection<Episode> AllEpisodes
        {
            get
            {
                return m_AllEpisodes;
            }
        }

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
            Podcast.Episodes.CollectionChanged += OnEpisodesChanged;
            Podcast.PropertyChanged += OnPodcastChanged;
        }

        private void OnPodcastChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateFields();
        }

        void OnEpisodesChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateAllEpisodes();
        }

        protected override void UpdateFields()
        {
            if (Podcast == null)
            {
                return;
            }
            Title = Podcast.Title;
            Description = Podcast.Description;
            Image = Podcast.Image;
            UpdateAllEpisodes();
        }

        private void UpdateAllEpisodes()
        {
            List<Episode> sortedEpisodes = Podcast.Episodes.ToList<Episode>();
            sortedEpisodes.Sort((a, b) => { return a.PublishDate > b.PublishDate ? -1 : 1; });
            if (AllEpisodes.SequenceEqual(sortedEpisodes))
            {
                return;
            }
            AllEpisodes.Clear();
            AllEpisodes.AddAll(sortedEpisodes);
            UpdateVisibleEpisodes();
        }


        private void UpdateVisibleEpisodes()
        {
            m_Episodes.Clear();
            int i = 0;
            foreach (Episode episode in AllEpisodes)
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

        public bool IsAddToFavoritesEnabled()
        {
            bool inFavorites = Data.IsPodcastInFavorites(Podcast);
            return !inFavorites;
        }

        public bool IsRemoveFromFavoritesEnabled()
        {
            bool inFavorites = Data.IsPodcastInFavorites(Podcast);
            return inFavorites;
        }

        public void RemoveFromFavorites()
        {
            Data.RemoveFromFavorites(Podcast);
        }

        public Task AddToFavorites()
        {
            return Data.AddToFavorites(Podcast);
        }
    }
}
