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
        private static PodcastDataSource _podcastDataSource = new PodcastDataSource();

        [XmlIgnore]
        public ObservableCollection<PodcastGroup> Groups { get; private set; }

        private PodcastDataSource()
        {
            Groups = new ObservableCollection<PodcastGroup>();
        }

        public static PodcastDataSource Instance
        {
            get
            {
                return _podcastDataSource;
            }
        }
        public static async Task<IEnumerable<PodcastGroup>> LoadGroupsFromCacheAsync()
        {
            await _podcastDataSource.LoadFromCacheAsync();

            return _podcastDataSource.Groups;
        }

        public static async Task<IEnumerable<PodcastGroup>> LoadGroupsFromRssAsync()
        {
            await _podcastDataSource.LoadFromRssAsync();

            return _podcastDataSource.Groups;
        }

        public static void AddItem(string groupUniqueId, Podcast item)
        {
            var groups = _podcastDataSource.Groups.Where(g => g.UniqueId == groupUniqueId);
            if (groups.Count() > 0)
            {
                if (!groups.First<PodcastGroup>().Items.Any(i => i.Uri == item.Uri))
                {
                    groups.First<PodcastGroup>().Items.Add(item);
                }
            }
        }

        public static void RemoveItem(string groupUniqueId, Podcast item)
        {
            var groups = _podcastDataSource.Groups.Where(g => g.UniqueId == groupUniqueId);
            if (groups.Count() > 0)
            {
                groups.First<PodcastGroup>().Items.Remove(item);
            }
        }

        public static async Task<PodcastGroup> GetGroupAsync(string uniqueId)
        {
            //await _podcastDataSource.LoadFromCacheAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _podcastDataSource.Groups.Where((group) => group.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        public static async Task<Podcast> GetItemAsync(string uniqueId)
        {
            //await _podcastDataSource.LoadFromCacheAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _podcastDataSource.Groups.SelectMany(group => group.Items).Where((item) => item.UniqueId.Equals(uniqueId));
            if (matches.Count() > 0)
            {
                return matches.First();
            }
            return null;
        }

        public static void Store()
        {
            StringWriter stringWriter = new StringWriter();
            string favoritesString = string.Join(",", _podcastDataSource.Groups.First(g => g.UniqueId == Constants.FavoritesGroupId).Items.Select(item => item.Uri));
            Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            roamingSettings.Values["PodcastDataSource"] = favoritesString;
        }

        private async Task LoadFromRssAsync()
        {
            foreach (PodcastGroup podcastDataGroup in _podcastDataSource.Groups)
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
            // adding from itunes search term https://itunes.apple.com/search?term=freakonomics&media=podcast&entity=podcast&attribute=titleTerm

            PodcastGroup favorites = new PodcastGroup(
                Constants.FavoritesGroupId, 
                LocalizedStrings.FavoritesPodcastGroupName, 
                "My favorite podcasts", 
                "Assets/DarkGray.png", 
                "Podcasts I have subscribed to");
            _podcastDataSource.Groups.Add(favorites);

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