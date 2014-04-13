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
using PodCatch.Strings;

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
    public sealed class PodcastDataSource
    {
        private static PodcastDataSource s_PodcastDataSource = new PodcastDataSource();

        [XmlIgnore]
        private ObservableCollection<PodcastGroup> Groups { get; set; }

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
        public async Task<IEnumerable<PodcastGroup>> LoadGroupsFromCacheAsync()
        {
            await LoadFromCacheAsync();

            return Groups;
        }

        public async Task<IEnumerable<PodcastGroup>> LoadGroupsFromRssAsync()
        {
            await LoadFromRssAsync();

            return Groups;
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
            return Groups.Where(g => g.UniqueId == groupUniqueId).First().Items.Any(i => i.UniqueId == podcastUniqueId);
        }

        public void ClearGroup(string groupUniqueId)
        {
            PodcastGroup group = Groups.Where(g => g.UniqueId == groupUniqueId).First();
            group.Items.Clear();
        }

        public void AddItem(string groupUniqueId, Podcast item)
        {
            var groups = Groups.Where(g => g.UniqueId == groupUniqueId);
            if (groups.Count() > 0)
            {
                if (!groups.First<PodcastGroup>().Items.Any(i => i.Uri.ToLower() == item.Uri.ToLower()))
                {
                    groups.First<PodcastGroup>().Items.Add(item);
                }
            }
        }

        public void RemoveItem(string groupUniqueId, Podcast item)
        {
            var groups = Groups.Where(g => g.UniqueId == groupUniqueId);
            if (groups.Count() > 0)
            {
                groups.First<PodcastGroup>().Items.Remove(item);
            }
        }

        public async Task<PodcastGroup> GetGroupAsync(string uniqueId)
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
            var matches = Groups.SelectMany(group => group.Items).Where((item) => item.UniqueId.Equals(uniqueId));
            if (matches.Count() > 0)
            {
                return matches.First();
            }
            return null;
        }

        public void Store()
        {
            StringWriter stringWriter = new StringWriter();
            string favoritesString = string.Join(",", Groups.First(g => g.UniqueId == Constants.FavoritesGroupId).Items.Select(item => item.Uri));
            Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            roamingSettings.Values["PodcastDataSource"] = favoritesString;
        }

        private async Task LoadFromRssAsync()
        {
            foreach (PodcastGroup podcastDataGroup in Groups)
            {
                foreach (Podcast podcastDataItem in podcastDataGroup.Items)
                {
                    await podcastDataItem.LoadFromRssAsync();
                }
            }
        }

        private async Task LoadFromCacheAsync()
        {
            if (Groups.Count != 0)
                return;

            PodcastGroup favorites = new PodcastGroup(
                Constants.FavoritesGroupId, 
                LocalizedStrings.FavoritesPodcastGroupName, 
                "My favorite podcasts", 
                "Assets/DarkGray.png", 
                "Podcasts I have subscribed to");
            Groups.Add(favorites);

            Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            if (roamingSettings.Values.ContainsKey("PodcastDataSource"))
            {
                string podcastDataSourceString = roamingSettings.Values["PodcastDataSource"].ToString();
                
                foreach (string uriString in podcastDataSourceString.Split(','))
                {
                    if (string.IsNullOrEmpty(uriString))
                    {
                        continue;
                    }
                    Podcast item = new Podcast(uriString, null);
                    favorites.Items.Add(item);
                }
                foreach (PodcastGroup group in Groups)
                {
                    foreach (Podcast item in group.Items)
                    {
                        try
                        {
                            await item.LoadFromCacheAsync();
                        }
                        catch (Exception e)
                        {
                            favorites.Items.Remove(item);
                        }
                    }
                }
            }
        }
    }
}