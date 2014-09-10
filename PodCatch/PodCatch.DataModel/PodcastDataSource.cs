using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Popups;

namespace PodCatch.DataModel
{
    public class PodcastDataSource
    {
        private static Lazy<PodcastDataSource> s_Intance = new Lazy<PodcastDataSource>(() => new PodcastDataSource());
        private bool m_Loaded;
        public ObservableCollection<PodcastGroup> Groups { get; private set; }
        public static PodcastDataSource Instance
        {
            get
            {
                return s_Intance.Value;
            }
        }

        private PodcastDataSource()
        {
            Groups = new ObservableCollection<PodcastGroup>();
            AddDefaultGroups();
        }

        private void AddDefaultGroups()
        {
            PodcastGroup favorites = new PodcastGroup()
            {
                Id = Constants.FavoritesGroupId,
                TitleText = "FavoritesTitleText",
                SubtitleText = "FavoritesSubtitleText",
                DescriptionText = "FavoritesDescriptionText",
            };
            Groups.Add(favorites);
        }

        private PodcastGroup AddSearchResultsGroup()
        {
            PodcastGroup searchGroup = new PodcastGroup()
            {
                Id = Constants.SearchGroupId,
                TitleText = "SearchTitleText",
                SubtitleText = "SearchSubtitleText",
                DescriptionText = "SearchDescriptionText",
            };
            Groups.Add(searchGroup);
            return searchGroup;
        }

        private PodcastGroup GetGroup(string groupId)
        {
            var matches = Groups.Where((group) => group.Id.Equals(groupId));
            if (matches.Count() > 0)
            {
                return matches.First();
            }
            return null;
        }

        private void AddPodcast(string groupId, Podcast podcast)
        {
            PodcastGroup group = GetGroup(groupId);
            if (group != null)
            {
                if (!group.Podcasts.Contains(podcast))
                {
                    group.Podcasts.Add(podcast);
                }
            }
        }

        private async Task<Collection<PodcastGroup>> LoadFavorites()
        {
            StorageFile roamingFavoritesFile;
            try
            {
                roamingFavoritesFile = await ApplicationData.Current.RoamingFolder.GetFileAsync("podcatch.json");
                string favoritesAsJson = await FileIO.ReadTextAsync(roamingFavoritesFile);
                // Json.NET would be more concise, but it doesn't handle this correctly
                using (MemoryStream stream = new MemoryStream(Encoding.Unicode.GetBytes(favoritesAsJson)))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Collection<PodcastGroup>));
                    Collection<PodcastGroup> favorites = (Collection<PodcastGroup>)serializer.ReadObject(stream);
                    return favorites;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Couldn't find favorites file in roaming folder. {0}",e);
                return new Collection<PodcastGroup>();
            }
        }

        public async Task Load(bool force)
        {
            if (m_Loaded && !force)
            {
                return;
            }
            m_Loaded = true;
            TouchedFiles.Instance.Clear();
            List<Task> loadTasks = new List<Task>();
            foreach (PodcastGroup group in await LoadFavorites())
            {
                foreach (Podcast podcast in group.Podcasts)
                {
                    AddPodcast(group.Id, podcast);
                    loadTasks.Add(LoadPodcast(podcast));
                }
            }
            await Task.WhenAll(loadTasks.ToArray());
        }

        private async Task<bool> LoadPodcast(Podcast podcast)
        {
            try
            {
                await podcast.Load();
                await podcast.DownloadEpisodes();
                await podcast.Store();
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error loading {0}. {1}", podcast, e);
                return false;
            }
        }

        public async Task Store()
        {
            try
            {
                StorageFile roamingFavoritesFile = await ApplicationData.Current.RoamingFolder.CreateFileAsync("podcatch.json", CreationCollisionOption.ReplaceExisting);
                Collection<PodcastGroup> favorites = new Collection<PodcastGroup>() { GetGroup(Constants.FavoritesGroupId) };
                string favoritesAsJson = JsonConvert.SerializeObject(favorites, Formatting.Indented);
                await FileIO.WriteTextAsync(roamingFavoritesFile, favoritesAsJson);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to store favorites. {0}", e);
            }
        }

        public void ShowSearchResults(IEnumerable<Podcast> podcasts)
        {
            PodcastGroup searchGroup = GetGroup(Constants.SearchGroupId);
            if (searchGroup == null)
            {
                searchGroup = AddSearchResultsGroup();
            }

            searchGroup.Podcasts.Clear();
            searchGroup.Podcasts.AddAll(podcasts);
            foreach (Podcast podcast in searchGroup.Podcasts)
            {
                try
                {
                    Task t = podcast.RefreshFromRss(false);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("PodcastDataSource.ShowSearchResults() - failed to refresh {0}: (1}", podcast.Title, e);
                }
            }
        }

        public async Task<bool> AddToFavorites(Podcast podcast)
        {
            PodcastGroup favorites = GetGroup(Constants.FavoritesGroupId);
            if (favorites.Podcasts.Contains(podcast))
            {
                return false;
            }

            PodcastGroup search = GetGroup(Constants.SearchGroupId);
            if (search.Podcasts.Contains(podcast))
            {
                search.Podcasts.Remove(podcast);
            }

            favorites.Podcasts.Add(podcast);
            await Store();
            return await LoadPodcast(podcast);
        }

        public void RemoveFromFavorites(Podcast podcast)
        {
            PodcastGroup favorites = GetGroup(Constants.FavoritesGroupId);
            favorites.Podcasts.Remove(podcast);
            Task t = Store();
        }

        public bool IsPodcastInFavorites(Podcast podcast)
        {
            PodcastGroup favorites = GetGroup(Constants.FavoritesGroupId);
            return favorites.Podcasts.Contains(podcast);
        }

        public async Task DoHouseKeeping()
        {
            await Load(true);
            await RemoveUntouchedFiles(ApplicationData.Current.LocalFolder);
        }

        private async Task RemoveUntouchedFiles(StorageFolder folder)
        {
            foreach(IStorageItem item in await folder.GetItemsAsync())
            {
                // suspension manager session state file
                if (item.Name == "_sessionState.xml")
                {
                    continue;
                }
                if (item.Attributes == FileAttributes.Directory)
                {
                    await RemoveUntouchedFiles(await StorageFolder.GetFolderFromPathAsync(item.Path));
                }
                if (!TouchedFiles.Instance.Contains(item.Path))
                {
                    Debug.WriteLine("DoHouseKeeping() - deleting {0}", item.Path);
                    await item.DeleteAsync();
                }
            }
            
        }
    }
}
