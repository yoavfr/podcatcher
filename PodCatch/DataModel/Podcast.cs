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
        private string m_RssImageUrl;
        private string m_CachedImagePath;
        private string m_SearchImageUrl;

        public Podcast(String title, String uri, String tempImageUrl, String description)
            : this(uri, null)
        {
            Title = title;
            Description = description;
            SearchImageUrl = tempImageUrl;
            LastUpdatedTimeTicks = 0;
            LastStoreTimeTicks = 0;
        }

        public Podcast(String uri, BaseData parent) : base (null)
        {
            Episodes = new ObservableCollection<Episode>();
            Uri = uri;
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
        public string Uri { get; private set; }
        [DataMember]
        public string Description
        {
            get { return m_Description; }
            private set { m_Description = value; NotifyPropertyChanged("Description"); }
        }
        [DataMember]
        public string RssImageUrl 
        {
            get 
            {
                return m_RssImageUrl; 
            }
            private set 
            {
                if (m_RssImageUrl != value)
                {
                    m_RssImageUrl = value; 
                    NotifyPropertyChanged("ImagePath"); 
                }
            }
        }
        [DataMember]
        private string CachedImagePath
        {
            get
            {
                return m_CachedImagePath;
            }
            set
            {
                m_CachedImagePath = value;
                NotifyPropertyChanged("ImagePath");
            }
        }

        [DataMember]
        private string SearchImageUrl
        {
            get
            {
                return m_SearchImageUrl;
            }
            set
            {
                m_SearchImageUrl = value;
                NotifyPropertyChanged("ImagePath");
            }

        }
        [DataMember]
        public ObservableCollection<Episode> Episodes {get; private set;}
        [DataMember]
        private long LastUpdatedTimeTicks { get; set; }
        [DataMember]
        private long LastStoreTimeTicks { get; set; }

        public string ImagePath
        {
            get
            {
                if (string.IsNullOrEmpty(CachedImagePath))
                {
                    return SearchImageUrl;
                }
                return CachedImagePath;
            }
        }

        public async Task LoadFromRssAsync()
        {
            DateTime lastUpdatedTime = new DateTime(LastUpdatedTimeTicks);
            // limit refreshs to every 2 hours
            if (DateTime.UtcNow - lastUpdatedTime < TimeSpan.FromHours(2))
            {
                return;
            }

            // Update data from actual RSS feed
            SyndicationFeed syndicationFeed = new SyndicationFeed();
            XmlDocument feedXml = await XmlDocument.LoadFromUriAsync(new Uri(Uri));
            syndicationFeed.LoadFromXml(feedXml);

            // don't refresh if feed has not been updated since
            if (syndicationFeed.LastUpdatedTime != null && 
                syndicationFeed.LastUpdatedTime.DateTime > lastUpdatedTime)
            {
                Title = syndicationFeed.Title.Text;

                if (syndicationFeed.Subtitle != null)
                {
                    Description = syndicationFeed.Subtitle.Text;
                }
            
                if (syndicationFeed.ImageUri != null)
                {
                    RssImageUrl = syndicationFeed.ImageUri.AbsoluteUri;
                }
                else
                {
                    // if nothing in RSS itself use what we got from the search results
                    RssImageUrl = SearchImageUrl;
                }

                Episodes.Clear();
                int count = 0;

                foreach (SyndicationItem item in syndicationFeed.Items)
                {
                    Uri uri=null;
                    foreach (SyndicationLink link in item.Links)
                    {
                        if (link.Relationship == "enclosure" && link.MediaType == "audio/mpeg")
                        {
                            uri = link.Uri;
                            break;
                        }
                    }
                    if (uri != null && count++ <3)
                    {
                        string episodeTitle = item.Title != null ? item.Title.Text : "<No Title>";
                        string episodeSummary = item.Summary != null ? item.Summary.Text : "<No Summary>";
                        Episode episode = new Episode(UniqueId, episodeTitle, episodeSummary, item.PublishedDate, uri, this, Episodes);
                        Episodes.Add(episode);
                    }
                    if (count>=3)
                    {
                        break;
                    }
                }
            }

            // keep record of last update time
            LastUpdatedTimeTicks = DateTime.UtcNow.Ticks;

            // and store changes locally (including LastUpdateTime)
            await StoreToCacheAsync();
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
            bool failed = false;
            StorageFile xmlFile = await localFolder.CreateFileAsync(string.Format("{0}.json", UniqueId), CreationCollisionOption.OpenIfExists);
            using (Stream stream = await xmlFile.OpenStreamForReadAsync())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Podcast));
                try
                {
                    Podcast readItem = (Podcast)serializer.ReadObject(stream);
                    Title = readItem.Title;
                    Description = readItem.Description;
                    Episodes = readItem.Episodes;
                    LastUpdatedTimeTicks = readItem.LastUpdatedTimeTicks;
                    CachedImagePath = readItem.CachedImagePath;
                    RssImageUrl = readItem.RssImageUrl;
                    if (readItem.SearchImageUrl != null)
                    {
                        SearchImageUrl = readItem.SearchImageUrl;
                    }
                    
                    // load episode states
                    foreach (Episode episode in Episodes)
                    {
                        episode.Parent = this;
                        await episode.LoadStateAsync(Episodes);
                    }
                }
                catch (Exception)
                {
                    failed = true;
                }
                if (failed)
                {
                    await xmlFile.DeleteAsync();
                }
            }
        }

        override public async Task StoreToCacheAsync()
        {
            await StoreImageToCacheAsync();

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile xmlFile = await localFolder.CreateFileAsync(string.Format("{0}.json", UniqueId), CreationCollisionOption.ReplaceExisting);
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Podcast));
            using (Stream stream = await xmlFile.OpenStreamForWriteAsync())
            {
                serializer.WriteObject(stream, this);
            }
        }

        private async Task StoreImageToCacheAsync()
        {
            DateTime lastStoreTime = new DateTime(LastStoreTimeTicks);
            if (DateTime.UtcNow - lastStoreTime < TimeSpan.FromDays(1) &&
                !string.IsNullOrEmpty(CachedImagePath))
            {
                return;
            }

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            if (!string.IsNullOrEmpty(RssImageUrl))
            {
                string imageExtension = Path.GetExtension(RssImageUrl);
                string localImagePath = string.Format("{0}{1}", UniqueId, imageExtension);

                // the image we have is from the cache
                StorageFile localImageFile = await localFolder.CreateFileAsync(localImagePath, CreationCollisionOption.ReplaceExisting);
                BackgroundDownloader downloader = new BackgroundDownloader();
                try
                {
                    DownloadOperation downloadOperation = downloader.CreateDownload(new Uri(RssImageUrl), localImageFile);
                    await downloadOperation.StartAsync();
                    CachedImagePath = localImageFile.Path;
                    lastStoreTime = DateTime.UtcNow;
                }
                catch (Exception e)
                {

                }
            }
        }

        public override string ToString()
        {
            return this.Title;
        }
    }
}
