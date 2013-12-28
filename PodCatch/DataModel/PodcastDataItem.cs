using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Windows.Data.Html;
using Windows.Data.Xml.Dom;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Web.Syndication;

namespace PodCatch.Data
{
    [DataContract]
    public class PodcastDataItem
    {
        public PodcastDataItem(String title, String uri, String imagePath, String description)
            : this(uri)
        {
            this.Title = title;
            this.Description = description;
            this.ImagePath = imagePath;
        }

        public PodcastDataItem(String uri)
        {
            this.Episodes = new ObservableCollection<EpisodeDataItem>();
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
        public string Title { get; private set; }
        public string Uri { get; private set; }
        [DataMember]
        public string Description { get; private set; }
        [DataMember]
        public string ImagePath { get; private set; }
        [DataMember]
        public ObservableCollection<EpisodeDataItem> Episodes {get; private set;}

        public async Task GetPodcastDataAsync()
        {
            await GetLocalPodcastDataAsync();

            // Update data from actual RSS feed
            SyndicationFeed syndicationFeed = new SyndicationFeed();
            XmlDocument feedXml = await XmlDocument.LoadFromUriAsync(new Uri(Uri));
            syndicationFeed.LoadFromXml(feedXml);

            if (syndicationFeed.Title.Text != Title ||
                syndicationFeed.Subtitle.Text != Description ||
                syndicationFeed.Items.Count != Episodes.Count) // TODO: check for image changes
            {
                Title = syndicationFeed.Title.Text;

                if (syndicationFeed.Subtitle != null)
                {
                    Description = syndicationFeed.Subtitle.Text;
                }
            
                if (syndicationFeed.ImageUri != null)
                {
                    ImagePath = syndicationFeed.ImageUri.AbsoluteUri;
                }

                Episodes.Clear();

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
                    if (uri != null)
                    {
                        EpisodeDataItem episode = new EpisodeDataItem(UniqueId, item.Title.Text, HtmlUtilities.ConvertToText(item.Summary.Text), item.PublishedDate, uri, Episodes);
                        Episodes.Add(episode);
                    }
                }

                // Store changes locally
                await StoreLocalPodcastDataAsync();
            }
        }

        private async Task GetLocalPodcastDataAsync()
        {
            // use cached data if we have it
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile xmlFile = await localFolder.CreateFileAsync(string.Format("{0}.xml", UniqueId), CreationCollisionOption.OpenIfExists);
            using (Stream stream = await xmlFile.OpenStreamForReadAsync())
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(PodcastDataItem));
                try
                {
                    PodcastDataItem readItem = (PodcastDataItem)serializer.ReadObject(stream);
                    Title = readItem.Title;
                    Description = readItem.Description;
                    Episodes = readItem.Episodes;
                    string imagePath = readItem.ImagePath;
                    string imageExtension = Path.GetExtension(imagePath);
                    imagePath = string.Format("{0}{1}", UniqueId, imageExtension);
                    StorageFile imageFile = await localFolder.GetFileAsync(imagePath);
                    ImagePath = imagePath;
                }
                catch (Exception)
                { }
            }
        }

        private async Task StoreLocalPodcastDataAsync()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile xmlFile = await localFolder.CreateFileAsync(string.Format("{0}.xml", UniqueId), CreationCollisionOption.ReplaceExisting);
            DataContractSerializer serializer = new DataContractSerializer(typeof(PodcastDataItem));
            using (Stream stream = await xmlFile.OpenStreamForWriteAsync())
            {
                serializer.WriteObject(stream, this);
            }

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
