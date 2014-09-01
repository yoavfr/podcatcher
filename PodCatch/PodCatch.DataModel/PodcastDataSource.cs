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

        private Collection<PodcastGroup> LoadFavorites()
        {
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;

            if (roamingSettings.Values.ContainsKey("PodcastDataSource"))
            {
                string favoritesAsJson = roamingSettings.Values["PodcastDataSource"].ToString();
                try
                {
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
                    roamingSettings.Values["PodcastDataSource"] = null;
                    // TODO: report error 
                }
            }
            return new Collection<PodcastGroup>();
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
            foreach (PodcastGroup group in LoadFavorites())
            {
                foreach (Podcast podcast in group.Podcasts)
                {
                    AddPodcast(group.Id, podcast);
                    loadTasks.Add(podcast.Load().ContinueWith((loadTask)=>
                    {
                        podcast.DownloadEpisodes().ContinueWith(async (downloadTask) =>
                        {
                            await podcast.Store();
                        });
                    }));
                }
            }
            await Task.WhenAll(loadTasks.ToArray());
        }

        public void Store()
        {
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            Collection<PodcastGroup> favorites = new Collection<PodcastGroup>() { GetGroup(Constants.FavoritesGroupId) };
            string favoritesAsJson = JsonConvert.SerializeObject(favorites, Formatting.Indented);
            roamingSettings.Values["PodcastDataSource"] = favoritesAsJson;
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
                Task t = podcast.RefreshFromRss(false);
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
            Store();
            await podcast.RefreshFromRss(true).ContinueWith((refreshTask) =>
                {
                    podcast.DownloadEpisodes().ContinueWith(async (downalodTask) =>
                        {
                            await podcast.Store();
                        });
                });
            return true;
        }

        public void RemoveFromFavorites(Podcast podcast)
        {
            PodcastGroup favorites = GetGroup(Constants.FavoritesGroupId);
            favorites.Podcasts.Remove(podcast);
            Store();
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
