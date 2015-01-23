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

        Task Search(string searchTerm);

        Podcast GetPodcast(string podcastId);

        Task DoHouseKeeping();
    }
}