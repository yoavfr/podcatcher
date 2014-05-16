using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Data.Json;
using Windows.Storage;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Newtonsoft.Json;
using System.Runtime.Serialization;

// The data model defined by this file serves as a representative example of a strongly-typed
// model.  The property names chosen coincide with data bindings in the standard item templates.
//
// Applications may use this model as a starting point and build on it, or discard it entirely and
// replace it with something appropriate to their needs. If using this model, you might improve app 
// responsiveness by initiating the data loading task in the code behind for App.xaml when the app 
// is first launched.

namespace PodCatch.DataModel
{
    /// <summary>
    /// Creates a collection of groups and items with content read from a static json file.
    /// 
    /// SampleDataSource initializes with data read from a static json file included in the 
    /// project.  This provides sample data at both design-time and run-time.
    /// </summary>
    [DataContract]
    public sealed class PodcastDataSource
    {
        private static PodcastDataSource s_PodcastDataSource = new PodcastDataSource();
        private bool m_Loaded;

        //[XmlIgnore]
        [GlobalDataMember]
        [DataMember]
        public ObservableCollection<PodcastGroup> Groups { get; private set; }

        private PodcastDataSource()
        {
            Groups = new ObservableCollection<PodcastGroup>();
        }

        public static PodcastDataSource Instance
        {
            get
            {
                return s_PodcastDataSource;
            }
        }

        public async Task Load()
        {
            if (m_Loaded)
            {
                return;
            }
            await LoadFromCacheAsync();
            Refresh();
            SetInitialEpisodeDisplay();
            m_Loaded = true;
        }

        public void Refresh()
        {
            LoadFromRssAsync();
        }

        public void AddGroup(string uniqueId, string title, string subTitle, string imagePath, string description)
        {
            var groups = Groups.Where(g => g.UniqueId == uniqueId);
            if (groups.Count() > 0 )
            {
                return;
            }
            PodcastGroup podcastGroup = new PodcastGroup(uniqueId, title, subTitle, imagePath, description);
            Groups.Add(podcastGroup);
        }

        public bool IsPodcastInGroup (string groupUniqueId, string podcastUniqueId)
        {
            return Groups.Where(g => g.UniqueId == groupUniqueId).First().Podcasts.Any(i => i.UniqueId == podcastUniqueId);
        }

        public void ClearGroup(string groupUniqueId)
        {
            PodcastGroup group = Groups.Where(g => g.UniqueId == groupUniqueId).First();
            group.Podcasts.Clear();
        }

        public void AddItem(string groupUniqueId, Podcast item)
        {
            var groups = Groups.Where(g => g.UniqueId == groupUniqueId);
            if (groups.Count() > 0)
            {
                if (!groups.First<PodcastGroup>().Podcasts.Any(i => i.Uri.ToLower() == item.Uri.ToLower()))
                {
                    groups.First<PodcastGroup>().Podcasts.Add(item);
                }
            }
        }

        public void RemoveItem(string groupUniqueId, Podcast item)
        {
            var groups = Groups.Where(g => g.UniqueId == groupUniqueId);
            if (groups.Count() > 0)
            {
                groups.First<PodcastGroup>().Podcasts.Remove(item);
            }
        }

        public PodcastGroup GetGroup(string uniqueId)
        {
            //await _podcastDataSource.LoadFromCacheAsync();
            // Simple linear search is acceptable for small data sets
            var matches = Groups.Where((group) => group.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        public async Task<Podcast> GetItemAsync(string uniqueId)
        {
            //await _podcastDataSource.LoadFromCacheAsync();
            // Simple linear search is acceptable for small data sets
            var matches = Groups.SelectMany(group => group.Podcasts).Where((item) => item.UniqueId.Equals(uniqueId));
            if (matches.Count() > 0)
            {
                return matches.First();
            }
            return null;
        }

        public void Store()
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.ContractResolver = new GlobalDataMemberContractResolver();
            PodcastGroup favorites = GetGroup(Constants.FavoritesGroupId);
            string thisAsJson = JsonConvert.SerializeObject(favorites, Formatting.Indented, settings);
            
            Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            roamingSettings.Values["PodcastDataSource"] = thisAsJson;
        }

        private async void LoadFromRssAsync()
        {
            foreach (PodcastGroup podcastDataGroup in Groups)
            {
                foreach (Podcast podcastDataItem in podcastDataGroup.Podcasts)
                {
                    Task t = podcastDataItem.LoadFromRssAsync(false);
                }
            }
        }

        private async Task LoadFromCacheAsync()
        {

            if (Groups.Count != 0)
                return;

            Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            //roamingSettings.Values["PodcastDataSource"] = null;
            if (!roamingSettings.Values.ContainsKey("PodcastDataSource"))
            {
                AddDefaultGroups();
            }
            else
            {
                string favoritesAsJson = roamingSettings.Values["PodcastDataSource"].ToString();
                try
                {
                    PodcastGroup favorites = JsonConvert.DeserializeObject<PodcastGroup>(favoritesAsJson);
                    if (favorites != null) 
                    {
                        Groups.Add(favorites);
                    }
                }
                catch (Exception e)
                {
                    AddDefaultGroups();
                    roamingSettings.Values["PodcastDataSource"] = null;
                }
            }

            if (Groups == null || Groups.Count == 0)
            {
                AddDefaultGroups();
            }

            foreach (PodcastGroup group in Groups)
            {
                foreach (Podcast item in group.Podcasts)
                {
                    try
                    {
                        await item.LoadFromCacheAsync();
                    }
                    catch (Exception e)
                    {
                        //group.Items.Remove(item);
                    }
                }
            }
        }

        private void SetInitialEpisodeDisplay()
        {
            foreach (PodcastGroup group in Groups)
            {
                foreach (Podcast podcast in group.Podcasts)
                {
                    podcast.DisplayNextEpisodes(5);
                }
            }
        }

        private void AddDefaultGroups()
        {
            AddGroup(
                Constants.FavoritesGroupId,
                "FavoritesTitle",
                "FavoritesSubtitle",
                "Assets/DarkGray.png",
                "FavoritesDescription");
        }
    }
}