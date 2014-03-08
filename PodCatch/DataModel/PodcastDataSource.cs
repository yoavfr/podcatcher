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

// The data model defined by this file serves as a representative example of a strongly-typed
// model.  The property names chosen coincide with data bindings in the standard item templates.
//
// Applications may use this model as a starting point and build on it, or discard it entirely and
// replace it with something appropriate to their needs. If using this model, you might improve app 
// responsiveness by initiating the data loading task in the code behind for App.xaml when the app 
// is first launched.

namespace PodCatch.Data
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
        public ObservableCollection<PodcastDataGroup> Groups { get; private set; }

        private PodcastDataSource()
        {
            Groups = new ObservableCollection<PodcastDataGroup>();
        }

        public static PodcastDataSource Instance
        {
            get
            {
                return _podcastDataSource;
            }
        }
        public static async Task<IEnumerable<PodcastDataGroup>> LoadGroupsFromCacheAsync()
        {
            await _podcastDataSource.LoadFromCacheAsync();

            return _podcastDataSource.Groups;
        }

        public static async Task<IEnumerable<PodcastDataGroup>> LoadGroupsFromRssAsync()
        {
            await _podcastDataSource.LoadFromRssAsync();

            return _podcastDataSource.Groups;
        }

        public static void AddItem(string groupUniqueId, PodcastDataItem item)
        {
            var groups = _podcastDataSource.Groups.Where(g => g.UniqueId == groupUniqueId);
            if (groups.Count() > 0)
            {
                if (!groups.First<PodcastDataGroup>().Items.Any(i => i.Uri == item.Uri))
                {
                    groups.First<PodcastDataGroup>().Items.Add(item);
                }
            }
        }

        public static void RemoveItem(string groupUniqueId, PodcastDataItem item)
        {
            var groups = _podcastDataSource.Groups.Where(g => g.UniqueId == groupUniqueId);
            if (groups.Count() > 0)
            {
                groups.First<PodcastDataGroup>().Items.Remove(item);
            }
        }

        public static async Task<PodcastDataGroup> GetGroupAsync(string uniqueId)
        {
            //await _podcastDataSource.LoadFromCacheAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _podcastDataSource.Groups.Where((group) => group.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        public static async Task<PodcastDataItem> GetItemAsync(string uniqueId)
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
            string favoritesString = string.Join(",", _podcastDataSource.Groups.First(g => g.Title == "Favorites").Items.Select(item => item.Uri));
            Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            roamingSettings.Values["PodcastDataSource"] = favoritesString;
        }

        private async Task LoadFromRssAsync()
        {
            foreach (PodcastDataGroup podcastDataGroup in _podcastDataSource.Groups)
            {
                foreach (PodcastDataItem podcastDataItem in podcastDataGroup.Items)
                {
                    await podcastDataItem.LoadFromRssAsync();
                }
            }
        }

        private async Task LoadFromCacheAsync()
        {
            if (Groups.Count != 0)
                return;

             
            PodcastDataGroup favorites = new PodcastDataGroup("Favorites", "Favorites", "My favorite podcasts", "Assets/DarkGray.png", "Podcasts I have subscribed to");
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
                    PodcastDataItem item = new PodcastDataItem(uriString, null);
                    favorites.Items.Add(item);
                }
                foreach (PodcastDataGroup group in Groups)
                {
                    foreach (PodcastDataItem item in group.Items)
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

            /*PodcastDataGroup favorites = new PodcastDataGroup("Favorites", "Favorites", "My favorite podcasts", "Assets/DarkGray.png", "Podcasts I have subscribed to");
            //PodcastDataItem item1 = new PodcastDataItem(favorites.UniqueId, "The Moth", "http://feeds.themoth.org/themothpodcast", String.Empty, String.Empty);
            //favorites.Items.Add(item1);
            //PodcastDataItem item2 = new PodcastDataItem(favorites.UniqueId, "Blastoff", "http://www.npr.org/rss/podcast.php?id=510289", String.Empty, String.Empty);
            //favorites.Items.Add(item2);

            Groups.Add(favorites);
            foreach (PodcastDataGroup group in Groups)
            {
                foreach (PodcastDataItem item in group.Items)
                {
                    await item.GetPodcastDataAsync();
                }
            }
            

            /*Uri dataUri = new Uri("ms-appx:///DataModel/Podcasts.json");

            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(dataUri);
            string jsonText = await FileIO.ReadTextAsync(file);
            JsonObject jsonObject = JsonObject.Parse(jsonText);
            JsonArray jsonArray = jsonObject["Groups"].GetArray();

            foreach (JsonValue groupValue in jsonArray)
            {
                JsonObject groupObject = groupValue.GetObject();
                PodcastDataGroup group = new PodcastDataGroup(groupObject["UniqueId"].GetString(),
                                                            groupObject["Title"].GetString(),
                                                            groupObject["Subtitle"].GetString(),
                                                            groupObject["ImagePath"].GetString(),
                                                            groupObject["Description"].GetString());

                foreach (JsonValue itemValue in groupObject["Items"].GetArray())
                {
                    JsonObject itemObject = itemValue.GetObject();
                    PodcastDataItem podcast = new PodcastDataItem(group.UniqueId,
                                                       itemObject["Title"].GetString(),
                                                       itemObject["Uri"].GetString(),
                                                       itemObject["ImagePath"].GetString(),
                                                       itemObject["Description"].GetString());
                    await podcast.GetPodcastDataAsync();
                    group.Items.Add(podcast);
                }
                this.Groups.Add(group);
            }*/
        }
    }
}