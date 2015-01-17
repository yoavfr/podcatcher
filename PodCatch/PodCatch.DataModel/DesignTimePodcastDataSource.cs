using PodCatch.Common;
using PodCatch.Common.Collections;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodCatch.DataModel
{
    public class DesignTimePodcastDataSource : ServiceConsumer, IPodcastDataSource
    {
        ObservableCollection<PodcastGroup> m_Groups;

        public DesignTimePodcastDataSource(IServiceContext serviceContext) : base (serviceContext)
        {

        }
        public Task Load(bool force)
        {
            return VoidTask.Completed;
        }

        public Task Store()
        {
            return VoidTask.Completed;
        }

        public ObservableCollection<PodcastGroup> GetGroups()
        {
            if (m_Groups == null)
            {
                Podcast podcast = new Podcast(ServiceContext)
                {
                    Title = "Podcast Title",
                    Description = "Podcast Descirption"
                };
                PodcastGroup group = new PodcastGroup(ServiceContext)
                {
                    Podcasts = new ObservableConcurrentCollection<Podcast>() { podcast },
                    Id = "Favorites",
                    TitleText = "Favorites"
                };
                m_Groups = new ObservableCollection<PodcastGroup>() { group };
            }
            return m_Groups;
        }

        public PodcastGroup GetGroup(string groupId)
        {
            return m_Groups.First((group) => group.Id == groupId);
        }

        public Task<bool> AddToFavorites(Podcast podcast)
        {
            GetGroup("Favorites").Podcasts.Add(podcast);
            return Task.FromResult(true);
        }

        public Task RemoveFromFavorites(Podcast podcast)
        {
            GetGroup("Favorites").Podcasts.Remove(podcast);
            return Task.FromResult(true);
        }

        public bool IsPodcastInFavorites(Podcast podcast)
        {
            return true;
        }

        public Task SetSearchResults(IEnumerable<Podcast> podcasts)
        {
            return VoidTask.Completed;
        }

        public Podcast GetPodcast(string podcastId)
        {
            return null;
        }

        public Task DoHouseKeeping()
        {
            return VoidTask.Completed;
        }
    }
}
