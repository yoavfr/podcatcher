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
        private string m_ImagePath;

        public Podcast(String title, String uri, String imagePath, String description, BaseData parent)
            : this(uri, parent)
        {
            this.Title = title;
            this.Description = description;
            this.ImagePath = imagePath;
        }

        public Podcast(String uri, BaseData parent) : base (parent)
        {
            this.Episodes = new ObservableCollection<Episode>();
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
        public string ImagePath 
        {
            get { return m_ImagePath; }
            private set { m_ImagePath = value; NotifyPropertyChanged("ImagePath"); }
        }
        [DataMember]
        public ObservableCollection<Episode> Episodes {get; private set;}
        [DataMember]
        public DateTime LastUpdatedTime { get; private set; }


        public async Task LoadFromRssAsync()
        {
            
            // Update data from actual RSS feed
            SyndicationFeed syndicationFeed = new SyndicationFeed();
            XmlDocument feedXml = await XmlDocument.LoadFromUriAsync(new Uri(Uri));
            syndicationFeed.LoadFromXml(feedXml);

            if (syndicationFeed.LastUpdatedTime.DateTime > LastUpdatedTime)
            {
                Title = syndicationFeed.Title.Text;
                LastUpdatedTime = syndicationFeed.LastUpdatedTime.DateTime;

                if (syndicationFeed.Subtitle != null)
                {
                    Description = syndicationFeed.Subtitle.Text;
                }
            
                if (syndicationFeed.ImageUri != null)
                {
                    ImagePath = syndicationFeed.ImageUri.AbsoluteUri;
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
                        Episode episode = new Episode(UniqueId, item.Title.Text, item.Summary.Text, item.PublishedDate, uri, this, Episodes);
                        Episodes.Add(episode);
                    }
                    if (count>=3)
                    {
                        break;
                    }
                }
                // Store changes locally
                await StoreToCacheAsync();
                await StoreImageToCacheAsync();
            }
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
                    LastUpdatedTime = readItem.LastUpdatedTime;
                    string imagePath = readItem.ImagePath;
                    string imageExtension = Path.GetExtension(imagePath);
                    imagePath = string.Format("{0}{1}", UniqueId, imageExtension);
                    StorageFile imageFile = await localFolder.GetFileAsync(imagePath);
                    ImagePath = imageFile.Path;
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
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile xmlFile = await localFolder.CreateFileAsync(string.Format("{0}.json", UniqueId), CreationCollisionOption.ReplaceExisting);
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Podcast));
            using (Stream stream = await xmlFile.OpenStreamForWriteAsync())
            {
                serializer.WriteObject(stream, this);
            }
        }

        public async Task StoreImageToCacheAsync()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            if (!string.IsNullOrEmpty(ImagePath))
            { 
                string imageExtension = Path.GetExtension(ImagePath);
                string localImagePath = string.Format("{0}{1}", UniqueId, imageExtension);
                StorageFile localImageFile = await localFolder.CreateFileAsync(localImagePath, CreationCollisionOption.ReplaceExisting);
                BackgroundDownloader downloader = new BackgroundDownloader();
                try
                {
                    DownloadOperation downloadOperation = downloader.CreateDownload(new Uri(ImagePath), localImageFile);
                    await downloadOperation.StartAsync();
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
