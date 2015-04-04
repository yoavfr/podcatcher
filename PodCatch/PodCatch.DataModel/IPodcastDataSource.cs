using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace PodCatch.DataModel
{
    public interface IPodcastDataSource
    {
        Task Load(bool force);

        Task Store();

        ObservableCollection<PodcastGroup> GetGroups();

        PodcastGroup GetGroup(string groupId);

        Task<bool> AddToFavorites(Podcast podcast);

        Task RemoveFromFavorites(Podcast podcast);

        bool IsPodcastInFavorites(Podcast podcast);

        Task<IEnumerable<Podcast>> Search(string searchTerm);

        void UpdateSearchResults(IEnumerable<Podcast> podcasts);

        Task RefreshSearchResults();

        Podcast GetPodcast(string podcastId);

        Episode GetEpisode(string episodeId);

        Podcast GetPodcastByEpisodeId(string episodeId);

        Task DoHouseKeeping();
    }
}