using PodCatch.Common;
using PodCatch.DataModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace PodCatch.ViewModels
{
    public class PodcastSummaryViewModel : BaseViewModel<Podcast>
    {
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

        public PodcastSummaryViewModel(Podcast podcast, IServiceContext serviceContext)
            : base(podcast, serviceContext)
        {
            podcast.Episodes.CollectionChanged += OnEpisodesChanged;
        }

        private void OnEpisodesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateUnplayedEpisodes();
        }

        protected override void UpdateFields()
        {
            Title = Data.Title;
            Description = Data.Description;
            Image = Data.Image;
            UpdateUnplayedEpisodes();
        }

        public void UpdateUnplayedEpisodes()
        {
            NumUnplayedEpisodes = Data.Episodes.Where((episode) => !episode.Played).Count();
        }

        public void DownloadEpisodes()
        {
            List<Episode> sortedEpisodes = Data.Episodes.ToList<Episode>();
            sortedEpisodes.Sort((a, b) => { return a.PublishDate > b.PublishDate ? -1 : 1; });

            int i = 0;
            foreach (Episode episode in sortedEpisodes)
            {
                if (i++ > 3)
                {
                    break;
                }
                episode.Download();
            }
        }
    }
}