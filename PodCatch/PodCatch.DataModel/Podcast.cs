using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Web.Http;
using Windows.Web.Syndication;

namespace PodCatch.DataModel
{
    [DataContract]
    public class Podcast : INotifyPropertyChanged
    {
        private bool m_Loaded;
        private string m_Title;
        private string m_Description;
        private string m_Image;

        public Podcast()
        {
            AllEpisodes = new List<Episode>();
            Episodes = new ObservableCollection<Episode>();
        }

        public string Uri { get; set; }
        [DataMember]
        public List<Episode> AllEpisodes { get; set; }

        public ObservableCollection<Episode> Episodes { get; private set; }

        public string Id
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
            set { m_Title = value; NotifyPropertyChanged("Title"); }
        }
        [DataMember]
        public string Description
        {
            get { return m_Description; }
            private set { m_Description = value; NotifyPropertyChanged("Description"); }
        }
        [DataMember]
        public string Image
        {
            get { return m_Image; }
            set { m_Image = value; NotifyPropertyChanged("Image"); }
        }
        [DataMember]
        private long LastRefreshTimeTicks { get; set; }

        public async Task Load()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            try
            {
                StorageFile file = await localFolder.GetFileAsync(CacheFileName);
                if (file != null)
                {
                    using (Stream stream = await file.OpenStreamForReadAsync())
                    {
                        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Podcast));
                        Podcast readPodcast = (Podcast)serializer.ReadObject(stream);

                        await UpdateFields (readPodcast);
                    }
                }
            }
            catch (Exception e)
            {

            }
            await RefreshFromRss(false);
            DisplayNextEpisodes(3);
        }

        public async Task RefreshFromRss(bool force)
        {
            DateTime lastRefreshTime = new DateTime(LastRefreshTimeTicks);
            if (DateTime.UtcNow - lastRefreshTime < TimeSpan.FromHours(2) && !force && AllEpisodes.Count > 0)
            {
                return;
            }

            try
            {
                SyndicationFeed syndicationFeed = new SyndicationFeed();

                HttpClient httpClient = new HttpClient();
                string xmlString = await httpClient.GetStringAsync(new Uri(Uri));
                XmlDocument feedXml = new XmlDocument();
                feedXml.LoadXml(xmlString);

                syndicationFeed.LoadFromXml(feedXml);

                // don't refresh if feed has not been updated since
                if (syndicationFeed.LastUpdatedTime != null &&
                    syndicationFeed.LastUpdatedTime.DateTime > lastRefreshTime)
                {
                    Title = syndicationFeed.Title.Text;

                    if (syndicationFeed.Subtitle != null)
                    {
                        Description = syndicationFeed.Subtitle.Text;
                    }

                    if (syndicationFeed.ImageUri != null)
                    {
                        await LoadImage(syndicationFeed.ImageUri);
                    }

                    ReadRssEpisodes(syndicationFeed);

                    Episodes.Clear();
                    DisplayNextEpisodes(3);
                }

                // keep record of last update time
                LastRefreshTimeTicks = DateTime.UtcNow.Ticks;
            }
            catch (Exception e)
            {

            }
        }

        private void ReadRssEpisodes(SyndicationFeed syndicationFeed)
        {
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
                    
                    Episode search = new Episode()
                    {
                        Uri = uri,
                    };
                    Episode episode = GetEpisodeById(search.Id);
                    
                    episode.Description = episodeSummary;
                    episode.Title = episodeTitle;
                    episode.Uri = uri;
                    //episode.ParentCollection = AllEpisodes;
                }
            }
        }

        public async Task DownloadEpisodes()
        {
            foreach (Episode episode in Episodes)
            {
                if (episode.State == EpisodeState.PendingDownload)
                {
                    await episode.Download();
                }
            }
        }

        private async Task LoadImage(Uri imageUri)
        {
            Uri validUri;
            if (!System.Uri.TryCreate(imageUri.ToString(), UriKind.Absolute, out validUri))
            {
                return;
            }
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            string imageExtension = Path.GetExtension(imageUri.ToString());
            string localImagePath = string.Format("{0}{1}", Id, imageExtension);
            ulong oldFileSize = await GetCachedFileSize(localImagePath);
            // the image we have is from the cache
            StorageFile localImageFile = await localFolder.CreateFileAsync(localImagePath, CreationCollisionOption.ReplaceExisting);
            BackgroundDownloader downloader = new BackgroundDownloader();
            try
            {
                DownloadOperation downloadOperation = downloader.CreateDownload(imageUri, localImageFile);
                await downloadOperation.StartAsync();
                Image = localImageFile.Path;

                ulong newFileSize = await GetCachedFileSize(localImagePath);
                //if (newFileSize != oldFileSize)
                {
                    NotifyPropertyChanged("Image");
                }
            }
            catch (Exception e)
            {

            }
        }

        private async Task<ulong> GetCachedFileSize(string path)
        {
            ulong fileSize = 0;
            try
            {
                StorageFile existingFile = await ApplicationData.Current.LocalFolder.GetFileAsync(path);
                if (existingFile != null)
                {
                    BasicProperties fileProperties = await existingFile.GetBasicPropertiesAsync();
                    fileSize = fileProperties.Size;
                }
            }
            catch (Exception)
            {

            }
            return fileSize;
        }

        private async Task UpdateFields (Podcast fromCache)
        {
            Title = fromCache.Title;
            Description = fromCache.Description;
            Image = fromCache.Image;
            LastRefreshTimeTicks = fromCache.LastRefreshTimeTicks;

            foreach (Episode episodeFromCache in fromCache.AllEpisodes)
            {
                Episode episode = GetEpisodeById(episodeFromCache.Id);
                await episode.UpdateFromCache(episodeFromCache);
            }
        }

        private Episode GetEpisodeById(string episodeId)
        {
            IEnumerable<Episode> found = AllEpisodes.Where((episode) => episode.Id.Equals(episodeId));

            if (found.Count() > 0)
            {
                return found.First();
            }

            // nothing in cache, but something in roaming settings
            Episode newEpisode = new Episode()
            {
                PodcastId = Id,
                Id = episodeId,
            };
            AllEpisodes.Add(newEpisode);
            return newEpisode;
        }

        private string CacheFileName
        {
            get
            {
                return string.Format("{0}.json", Id);
            }
        }

        public int DisplayNextEpisodes(int increment)
        {
            if (Episodes.Count() >= AllEpisodes.Count())
            {
                return Episodes.Count();
            }
            int target = Episodes.Count() + increment;
            while (Episodes.Count() < Math.Min(AllEpisodes.Count(), target))
            {
                Episode next = AllEpisodes[Episodes.Count()];
                if (!Episodes.Contains(next))
                {
                    Episodes.Insert(Episodes.Count(), next);
                }
            }
            return Episodes.Count();
        }

        public async Task Store()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile jsonFile = await localFolder.CreateFileAsync(CacheFileName, CreationCollisionOption.ReplaceExisting);
            DataContractJsonSerializer serialzer = new DataContractJsonSerializer(typeof(Podcast));
            using (Stream stream = await jsonFile.OpenStreamForWriteAsync())
            {
                serialzer.WriteObject(stream, this);
            }
            /*using (Stream stream = await jsonFile.OpenStreamForWriteAsync())
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
            }*/
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this,
                    new PropertyChangedEventArgs(propertyName));
            }
        }

        public override bool Equals(object obj)
        {
            Podcast other = obj as Podcast;
            if (other == null)
                return false;
            return other.Id == Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
