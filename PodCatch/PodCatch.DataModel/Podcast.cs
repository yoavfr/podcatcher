using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
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
        int m_numEpisodesToShow = 3;
        ObservableCollection<Episode> m_Episodes;

        public Podcast()
        {
            AllEpisodes = new List<Episode>();
        }

        public string Uri { get; set; }
        [DataMember]
        public List<Episode> AllEpisodes { get; set; }

        public ObservableCollection<Episode> Episodes 
        {
            get
            {
                if (m_Episodes == null)
                {
                    m_Episodes = new ObservableCollection<Episode>(AllEpisodes.Where((x,index) => index < m_numEpisodesToShow));
                }
                return m_Episodes;
            }
        }
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
            Debug.WriteLine("Podcast.Load(): from {0}",localFolder.Path);
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
                Debug.WriteLine("Podcast.Load(): error loading {0}. {1}", Id, e);
            }
            await RefreshFromRss(false);
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
                }
            }
        }

        public async Task DownloadEpisodes()
        {
            List<Task> allEpisodes = new List<Task>();
            foreach (Episode episode in Episodes)
            {
                if (episode.State == EpisodeState.PendingDownload)
                {
                    allEpisodes.Add(episode.Download());
                }
            }
            DisplayEpisodes();
            await Task.WhenAll(allEpisodes);
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
            Downloader downloader = new Downloader(imageUri, localImageFile);
            try
            {
                Debug.WriteLine("Podcast.LoadImage(): Downloading {0} -> {1}", imageUri, localImageFile.Path);
                await downloader.Download();
                Debug.WriteLine("Podcast.LoadImage(): Finished downloading {0} -> {1}", imageUri, localImageFile.Path);
                Image = localImageFile.Path;

                ulong newFileSize = await GetCachedFileSize(localImagePath);
                //if (newFileSize != oldFileSize)
                {
                    NotifyPropertyChanged("Image");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Podcast.LoadImage(): Failed downloading {0} -> {1}. {2}", imageUri, localImageFile.Path, e);
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
            if (fromCache.AllEpisodes == null)
            {
                return;
            }

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
            if (m_numEpisodesToShow >= AllEpisodes.Count())
            {
                return m_numEpisodesToShow;
            }
            int target = m_numEpisodesToShow + increment;
            m_numEpisodesToShow = Math.Min(AllEpisodes.Count(), target);
            DisplayEpisodes();
            return m_numEpisodesToShow;
        }

        public void DisplayEpisodes()
        {
            Dispatcher.Instance.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Episodes.Clear();
                Episodes.AddAll(AllEpisodes.Where((x,index) => index < m_numEpisodesToShow));
            });
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
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged == null)
            {
                return;
            }
            IAsyncAction t = Dispatcher.Instance.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                PropertyChanged(this,
                    new PropertyChangedEventArgs(propertyName));
            });
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
