using Newtonsoft.Json;
using PodCatch.DataModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Windows.Data.Html;
using Windows.Data.Xml.Dom;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Web.Syndication;

namespace PodCatch.DataModel
{
    [DataContract]
    public class Podcast : BaseData
    {
        private string m_Title;
        private string m_Description;

        public Podcast(String title, String uri, string searchImage, String description)
            : this(uri)
        {
            Title = title;
            Description = description;
            PodcastImage.Update(searchImage, ImageSource.Search);
            LastUpdatedTimeTicks = 0;
            LastStoreTimeTicks = 0;
        }

        [JsonConstructor]
        public Podcast(String uri) : base (null)
        {
            Episodes = new ObservableCollection<Episode>();
            Uri = uri;
            PodcastImage = new PodcastImage(null, ImageSource.NotSet, UniqueId);
        }

        public string UniqueId 
        { 
            get
            {
                return String.Format("0x{0:X8}", Uri.GetHashCode());
            }
        }
        [DataMember]
        public string Title
        {
            get { return m_Title; }
            private set { m_Title = value; NotifyPropertyChanged("Title"); }
        }
        [GlobalDataMember]
        [DataMember]
        public string Uri { get; private set; }
        [DataMember]
        public string Description
        {
            get { return m_Description; }
            private set { m_Description = value; NotifyPropertyChanged("Description"); }
        }
        [DataMember]
        public PodcastImage PodcastImage { get; private set; }

        public string Image
        {
            get
            {
                return PodcastImage.Image;
            }
        }

        public ObservableCollection<Episode> Episodes {get; private set;}
        [DataMember]
        private List<Episode> AllEpisodes { get; set; }
        [DataMember]
        private long LastUpdatedTimeTicks { get; set; }
        [DataMember]
        private long LastStoreTimeTicks { get; set; }
        public int NumberOfEpisodesDisplayed { get; private set; }
        public int NumberOfAvailableEpisodes { get; private set; }

        public async Task LoadFromRssAsync(bool force)
        {
            DateTime lastUpdatedTime = new DateTime(LastUpdatedTimeTicks);
            // limit refreshs to every 2 hours
            if (DateTime.UtcNow - lastUpdatedTime < TimeSpan.FromHours(2) && ! force)
            {
                return;
            }

            // Update data from actual RSS feed
            try
            {
                SyndicationFeed syndicationFeed = new SyndicationFeed();
                XmlDocument feedXml = await XmlDocument.LoadFromUriAsync(new Uri(Uri));
                syndicationFeed.LoadFromXml(feedXml);

                // don't refresh if feed has not been updated since
                if (syndicationFeed.LastUpdatedTime != null &&
                    syndicationFeed.LastUpdatedTime.DateTime > lastUpdatedTime)
                {
                    NumberOfAvailableEpisodes = syndicationFeed.Items.Count();
                    Title = syndicationFeed.Title.Text;

                    if (syndicationFeed.Subtitle != null)
                    {
                        Description = syndicationFeed.Subtitle.Text;
                    }

                    if (syndicationFeed.ImageUri != null)
                    {
                        PodcastImage.Update(syndicationFeed.ImageUri.AbsoluteUri, ImageSource.Rss);
                    }

                    LoadEpisodes(syndicationFeed);
                }

                // keep record of last update time
                LastUpdatedTimeTicks = DateTime.UtcNow.Ticks;

                // and store changes locally (including LastUpdateTime)
                await StoreToCacheAsync();
            }
            catch (Exception e)
            {

            }
        }

        private void LoadEpisodes(SyndicationFeed syndicationFeed)
        {
            AllEpisodes = new List<Episode>();
            foreach (SyndicationItem item in syndicationFeed.Items)
            {
                Uri uri = null;
                foreach (SyndicationLink link in item.Links)
                {
                    if (link.Relationship == "enclosure" && link.MediaType == "audio/mpeg")
                    {
                        uri = link.Uri;
                        break;
                    }
                }
                if (uri != null)
                {
                    string episodeTitle = item.Title != null ? item.Title.Text : "<No Title>";
                    string episodeSummary = item.Summary != null ? item.Summary.Text : "<No Summary>";
                    Episode episode = new Episode(UniqueId, episodeTitle, episodeSummary, item.PublishedDate, uri, this, Episodes);
                    AllEpisodes.Add(episode);
                }
            }
            NumberOfAvailableEpisodes = AllEpisodes.Count();
        }

        public int DisplayNextEpisodes(int increment)
        {
            if (NumberOfEpisodesDisplayed >= NumberOfAvailableEpisodes)
            {
                return NumberOfEpisodesDisplayed;
            }
            int target = NumberOfEpisodesDisplayed + increment;
            for (; NumberOfEpisodesDisplayed < NumberOfAvailableEpisodes && NumberOfEpisodesDisplayed < target; NumberOfEpisodesDisplayed++)
            {
                Episode next = AllEpisodes[NumberOfEpisodesDisplayed];
                if (!Episodes.Contains(next))
                {
                    Episodes.Insert(NumberOfEpisodesDisplayed, next);
                }
            }
            return NumberOfEpisodesDisplayed;
        }

        /// <summary>
        /// 
        /// Get cached data from local storage
        /// </summary>
        /// <returns></returns>
        public async Task LoadFromCacheAsync()
        {
            // use cached data if we have it
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            bool failed = true;
            StorageFile jsonFile = await localFolder.CreateFileAsync(string.Format("{0}.json", UniqueId), CreationCollisionOption.OpenIfExists);
            using (Stream stream = await jsonFile.OpenStreamForReadAsync())
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    using (JsonTextReader jsonReader = new JsonTextReader(reader))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        try
                        {
                            Podcast readItem = serializer.Deserialize<Podcast>(jsonReader);
                            if (readItem != null)
                            {
                                Title = readItem.Title;
                                Description = readItem.Description;
                                AllEpisodes = readItem.AllEpisodes;
                                NumberOfAvailableEpisodes = AllEpisodes.Count();
                                LastUpdatedTimeTicks = readItem.LastUpdatedTimeTicks;
                                PodcastImage.Update(readItem.PodcastImage.Image, readItem.PodcastImage.ImageSource);
                                NotifyPropertyChanged("Image");

                                failed = false;

                                // load episode states
                                foreach (Episode episode in Episodes)
                                {
                                    episode.Parent = this;
                                    await episode.LoadStateAsync(Episodes);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            failed = true;
                        }
                    }
                }
                if (failed)
                {
                    await jsonFile.DeleteAsync();
                }
            }
        }

        override public async Task StoreToCacheAsync()
        {
            await PodcastImage.StoreToCacheAsync();
            if (PodcastImage.Changed)
            {
                NotifyPropertyChanged("Image");
            }

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile jsonFile = await localFolder.CreateFileAsync(string.Format("{0}.json", UniqueId), CreationCollisionOption.ReplaceExisting);
            using (Stream stream = await jsonFile.OpenStreamForWriteAsync())
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    using (JsonWriter jsonWriter = new JsonTextWriter(writer))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Formatting = Formatting.Indented;
                        serializer.Serialize(jsonWriter, this);
                    }
                }
            }
        }

        public override string ToString()
        {
            return this.Title;
        }
    }
}
