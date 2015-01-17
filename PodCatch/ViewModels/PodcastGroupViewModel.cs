using PodCatch.Common;
using PodCatch.DataModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace PodCatch.ViewModels
{
    public class PodcastGroupViewModel : BaseViewModel<PodcastGroup>
    {
        private ObservableCollection<PodcastSummaryViewModel> m_Podcasts = new ObservableCollection<PodcastSummaryViewModel>();
        public ObservableCollection<PodcastSummaryViewModel> Podcasts
        {
            get
            {
                return m_Podcasts;
            }
        }
        public string Id { get; set; }
        public string TitleText { get; set; }
        public string SubtitleText { get; set; }
        public string DescriptionText { get; set; }
        public PodcastGroupViewModel(PodcastGroup podcastGroup, IServiceContext serviceContext)
            : base(podcastGroup, serviceContext)
        {
            podcastGroup.Podcasts.CollectionChanged += OnPodcastsChanged;
        }

        void OnPodcastsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdatePodcasts();
        }

        protected override void UpdateFields()
        {
            Id = Data.Id;
            TitleText = Data.TitleText;
            SubtitleText = Data.SubtitleText;
            DescriptionText = Data.DescriptionText;
            UpdatePodcasts();
        }

        private void UpdatePodcasts()
        {
            CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
            UIThread.Dispatch(() =>
                {
                    Podcasts.Clear();
                    Podcasts.AddAll(Data.Podcasts.Select((podcast) => new PodcastSummaryViewModel(podcast, ServiceContext)));
                });
        }
    }
}
