using Newtonsoft.Json;
using PodCatch.Common;
using PodCatch.DataModel.Data;
using PodCatch.DataModel.Search;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace PodCatch.DataModel
{
    public class PodcastDataSource : ServiceConsumer, IPodcastDataSource
    {
        private bool m_Loaded;
        private ISearch m_Search;

        private ObservableCollection<PodcastGroup> Groups { get; set; }

        public PodcastDataSource(IServiceContext serviceContext)
            : base(serviceContext)
        {
            Groups = new ObservableCollection<PodcastGroup>();
            m_Search = serviceContext.GetService<ISearch>();
            AddDefaultGroups();
        }

        public ObservableCollection<PodcastGroup> GetGroups()
        {
            return Groups;
        }

        private void AddDefaultGroups()
        {
            PodcastGroup favorites = new PodcastGroup(ServiceContext)
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
            PodcastGroup searchGroup = new PodcastGroup(ServiceContext)
            {
                Id = Constants.SearchGroupId,
                TitleText = "SearchTitleText",
                SubtitleText = "SearchSubtitleText",
                DescriptionText = "SearchDescriptionText",
            };
            Groups.Add(searchGroup);
            return searchGroup;
        }

        public PodcastGroup GetGroup(string groupId)
        {
            var matches = Groups.Where((group) => group.Id.Equals(groupId));
            if (matches.Count() > 0)
            {
                return matches.First();
            }
            return null;
        }

        public Podcast GetPodcast(string podcastId)
        {
            var matches = Groups.SelectMany(group => group.Podcasts).Where((podcast) => podcast.Id.Equals(podcastId));
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

        private async Task<IEnumerable<PodcastGroup>> LoadFavorites()
        {
            StorageFile roamingFavoritesFile;
            try
            {
                roamingFavoritesFile = await ApplicationData.Current.RoamingFolder.GetFileAsync("podcatch.json");
                using (Stream stream = await roamingFavoritesFile.OpenStreamForReadAsync())
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(IEnumerable<PodcastGroupData>));
                    IEnumerable<PodcastGroupData> favoritesData = (IEnumerable<PodcastGroupData>)serializer.ReadObject(stream);
                    List<PodcastGroup> favorites = new List<PodcastGroup>();
                    foreach (PodcastGroupData groupData in favoritesData)
                    {
                        favorites.Add(PodcastGroup.FromData(ServiceContext, groupData));
                    }
                    return favorites;
                }
            }
            catch (Exception e)
            {
                Tracer.TraceInformation("Couldn't find favorites file in roaming folder. {0}", e);
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
                await podcast.Store();
                return true;
            }
            catch (Exception e)
            {
                Tracer.TraceInformation("Error loading {0}. {1}", podcast, e);
                return false;
            }
        }

        public async Task Store()
        {
            try
            {
                StorageFile roamingFavoritesFile = await ApplicationData.Current.RoamingFolder.CreateFileAsync("podcatch.json.tmp", CreationCollisionOption.ReplaceExisting);
                Collection<PodcastGroupData> favorites = new Collection<PodcastGroupData>() { GetGroup(Constants.FavoritesGroupId).ToData() };
                string favoritesAsJson = JsonConvert.SerializeObject(favorites, Formatting.Indented);
                await FileIO.WriteTextAsync(roamingFavoritesFile, favoritesAsJson);
                await roamingFavoritesFile.RenameAsync("podcatch.json", NameCollisionOption.ReplaceExisting);
            }
            catch (Exception e)
            {
                Tracer.TraceInformation("Failed to store favorites. {0}", e);
            }
        }

        public async Task Search(string searchTerm)
        {
            // this is the search term
            if (string.IsNullOrEmpty(searchTerm))
            {
                return;
            }

            IEnumerable<Podcast> matches;
            List<Podcast> filtered;

            // RSS feed URL
            Uri validUri;
            if (Uri.TryCreate(searchTerm, UriKind.Absolute, out validUri) &&
                (validUri.Scheme == "http" || validUri.Scheme == "https"))
            {
                Podcast newItem = new Podcast(ServiceContext)
                {
                    PodcastUri = searchTerm
                };
                matches = new List<Podcast>() { newItem };
            }
            else
            {
                // Search term
                matches = await m_Search.FindAsync(searchTerm, 50);
            }

            filtered = new List<Podcast>();
            foreach (Podcast podcast in matches)
            {
                if (!IsPodcastInFavorites(podcast))
                {
                    filtered.Add(podcast);
                }
            }

            PodcastGroup searchGroup = GetGroup(Constants.SearchGroupId);
            if (searchGroup == null)
            {
                searchGroup = AddSearchResultsGroup();
            }
            // TODO: this needs to happen on UIThread
            searchGroup.Podcasts.Clear();
            searchGroup.Podcasts.AddAll(filtered);
            foreach (Podcast podcast in searchGroup.Podcasts)
            {
                try
                {
                    await podcast.RefreshFromRss(false);
                }
                catch (Exception e)
                {
                    Tracer.TraceInformation("PodcastDataSource.Search() - failed to refresh {0}: {1}", podcast.Title, e);
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

        public Task RemoveFromFavorites(Podcast podcast)
        {
            PodcastGroup favorites = GetGroup(Constants.FavoritesGroupId);
            favorites.Podcasts.Remove(podcast);
            return Store();
        }

        public bool IsPodcastInFavorites(Podcast podcast)
        {
            PodcastGroup favorites = GetGroup(Constants.FavoritesGroupId);
            return favorites.Podcasts.Contains(podcast);
        }

        public async Task DoHouseKeeping()
        {
            await Load(true);
            foreach (PodcastGroup group in Groups)
            {
                foreach (Podcast podcast in group.Podcasts)
                {
                    if (IsPodcastInFavorites(podcast))
                    {
                        int i = 0;
                        foreach (Episode episode in podcast.Episodes)
                        {
                            if (i++ > 3)
                            {
                                break;
                            }
                            await episode.Download();
                        }
                    }
                }
            }

            await RemoveUntouchedFiles(ApplicationData.Current.LocalFolder);
            await RemoveUntouchedFiles(await Windows.Storage.KnownFolders.MusicLibrary.GetFolderAsync(Constants.ApplicationName));
        }

        private async Task RemoveUntouchedFiles(StorageFolder folder)
        {
            foreach (IStorageItem item in await folder.GetItemsAsync())
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
                    Tracer.TraceInformation("DoHouseKeeping() - deleting {0}", item.Path);
                    await item.DeleteAsync();
                }
            }
        }
    }
}