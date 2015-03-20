using PodCatch.Common;
using PodCatch.DataModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodCatch.ViewModels
{
    public class EpisodePageViewModel : BaseViewModel<IPodcastDataSource>
    {
        private Episode m_Episode;
        private Podcast m_Podcast;
        public EpisodePageViewModel(IPodcastDataSource podcastDataSource, IServiceContext serviceContext) : base (podcastDataSource, serviceContext)
        {
        }

        public void Load(string episodeId)
        {
            m_Episode = Data.GetEpisode(episodeId);
            m_Podcast = Data.GetPodcastByEpisodeId(episodeId);
            UpdateFields();
            m_Episode.PropertyChanged += OnDataChanged;
            m_Podcast.PropertyChanged += OnDataChanged;
        }

        private void OnDataChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateFields();
        }


        private string m_EpisodeTitle;
        public string EpisodeTitle
        {
            get
            {
                return m_EpisodeTitle;
            }
            set
            {
                if (m_EpisodeTitle != value)
                {
                    m_EpisodeTitle = value;
                    NotifyPropertyChanged(() => EpisodeTitle);
                }
            }
        }

        private string m_PodcastTitle;
        public string PodcastTitle
        {
            get
            {
                return m_PodcastTitle;
            }
            set
            {
                if (m_PodcastTitle != value)
                {
                    m_PodcastTitle = value;
                    NotifyPropertyChanged(() => PodcastTitle);
                }
            }
        }

        private string m_Image;
        public string Image
        {
            get
            {
                return m_Image;
            }
            set
            {
                if (m_Image != value)
                {
                    m_Image = value;
                    NotifyPropertyChanged(() => Image);
                }
            }
        }

        private string m_PublishDate;

        public string PublishDate
        {
            get
            {
                return m_PublishDate;
            }
            set
            {
                if (m_PublishDate != value)
                {
                    m_PublishDate = value;
                    NotifyPropertyChanged(() => PublishDate);
                }
            }
        }

        protected override void UpdateFields()
        {
            if (m_Episode != null)
            {
                EpisodeTitle = m_Episode.Title;
                PublishDate = m_Episode.PublishDate.ToString("D");
            }
            if (m_Podcast != null)
            {
                Image = m_Podcast.Image;
                PodcastTitle = m_Podcast.Title;
            }
        }
    }
}
