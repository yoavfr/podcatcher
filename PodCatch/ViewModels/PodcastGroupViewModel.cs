using PodCatch.Common;
using PodCatch.DataModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
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

        private void OnPodcastsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdatePodcasts(e);
        }

        protected override void UpdateFields()
        {
            Id = Data.Id;
            TitleText = Data.TitleText;
            SubtitleText = Data.SubtitleText;
            DescriptionText = Data.DescriptionText;
            UpdatePodcasts(null);
        }

        private void UpdatePodcasts(NotifyCollectionChangedEventArgs e)
        {
            CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
            UIThread.Dispatch(() =>
                {
                    if (e == null)
                    {
                        Podcasts.Clear();
                        Podcasts.AddAll(Data.Podcasts.Select((podcast) => new PodcastSummaryViewModel(podcast, ServiceContext)));
                        return;
                    }

                    if (e.Action == NotifyCollectionChangedAction.Add)
                    {
                        foreach (var item in e.NewItems)
                        {
                            Podcast podcast = item as Podcast;
                            if (podcast != null)
                            {
                                Podcasts.Add(new PodcastSummaryViewModel(podcast, ServiceContext));
                            }
                            else
                            {
                                foreach (Podcast podcastItem in (IEnumerable<Podcast>)item)
                                {
                                    Podcasts.Add(new PodcastSummaryViewModel(podcastItem, ServiceContext));
                                }
                            }
                        }
                        return;
                    }
                    if (e.Action == NotifyCollectionChangedAction.Remove)
                    {
                        foreach (Podcast podcast in e.OldItems)
                        {
                            Podcasts.RemoveFirst((podcastViewModel) => podcastViewModel.Data.Id == podcast.Id);
                        }
                    }
                });
        }
    }
}