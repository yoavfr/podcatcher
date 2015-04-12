using PodCatch.Common;
using PodCatch.Common.Collections;
using PodCatch.DataModel.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Web.Http;
using Windows.Web.Syndication;

namespace PodCatch.DataModel
{
    public class Podcast : ServiceConsumer, INotifyPropertyChanged
    {
        private string m_Title;
        private string m_Description;
        private string m_Image;
        private IDownloadService m_DownloadService;

        private static Func<Episode, object> s_EpisodeOrdering = (e => -e.PublishDate.Ticks);

        public Podcast(IServiceContext serviceContext)
            : base(serviceContext)
        {
            m_DownloadService = serviceContext.GetService<IDownloadService>();
            Episodes = new ConcurrentObservableCollection<Episode>(s_EpisodeOrdering);
        }

        public static Podcast FromData(IServiceContext serviceContext, PodcastData data)
        {
            Podcast podcast = new Podcast(serviceContext);
            podcast.Title = data.Title;
            podcast.Description = data.Description;
            podcast.Image = data.Image;
            podcast.LastRefreshTimeTicks = data.LastRefreshTimeTicks;
            podcast.Episodes = new ConcurrentObservableCollection<Episode>(s_EpisodeOrdering, true);
            foreach (EpisodeData episodeData in data.Episodes)
            {
                Episode episode = Episode.FromData(serviceContext, podcast.FileName, episodeData);
                podcast.Episodes.Add(episode);
            }
            podcast.Episodes.HoldNotifications = false;
            return podcast;
        }

        public static Podcast FromRoamingData(IServiceContext serviceContext, RoamingPodcastData data)
        {
            Podcast podcast = new Podcast(serviceContext);
            podcast.Title = data.Title;
            podcast.PodcastUri = data.Uri;
            ConcurrentObservableCollection<Episode> episodes = new ConcurrentObservableCollection<Episode>(s_EpisodeOrdering, true);
            podcast.Episodes = episodes;
            foreach (RoamingEpisodeData episodeData in data.RoamingEpisodesData)
            {
                episodes.Add(Episode.FromRoamingData(serviceContext, podcast.FileName, episodeData));
            }
            podcast.Episodes.HoldNotifications = false;
            return podcast;
        }

        public PodcastData ToData()
        {
            PodcastData data = new PodcastData()
            {
                Title = Title,
                Description = Description,
                Image = Image,
                LastRefreshTimeTicks = LastRefreshTimeTicks
            };
            List<EpisodeData> episodes = new List<EpisodeData>();
            data.Episodes = episodes;
            foreach (Episode episode in Episodes)
            {
                episodes.Add(episode.ToData());
            }
            return data;
        }

        public RoamingPodcastData ToRoamingData()
        {
            RoamingPodcastData data = new RoamingPodcastData()
            {
                Uri = PodcastUri,
                Title = Title
            };
            List<RoamingEpisodeData> episodes = new List<RoamingEpisodeData>();
            data.RoamingEpisodesData = episodes;
            foreach (Episode episode in Episodes)
            {
                // Store as little as possible in roaming settings.
                // If there is no Position to record or this has not been played, we can rely on the default values upon deserialization
                if (episode.Position > TimeSpan.FromTicks(0) || episode.Played)
                {
                    episodes.Add(episode.ToRoamingData());
                }
            }
            return data;
        }

        public string PodcastUri { get; set; }

        /// <summary>
        /// All the episodes of this podcast
        /// </summary>
        public ConcurrentObservableCollection<Episode> Episodes { get; private set; }

        public void AddEpisode(Episode episode)
        {
            episode.PropertyChanged += OnEpisodePropertyChanged;
            Episodes.Add(episode);
        }

        private void OnEpisodePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Played" || e.PropertyName == "State")
            {
                NotifyPropertyChanged(() => Episodes);
            }
        }

        public string Id
        {
            get
            {
                if (PodcastUri != null)
                {
                    return String.Format("0x{0:X8}", PodcastUri.GetFixedHashCode());
                }
                return null;
            }
        }

        public string FileName
        {
            get
            {
                string fileName = Title.StripIllegalPathChars();
                return fileName.Substring(0, Math.Min(fileName.Length, 100));
            }
        }

        public string Title
        {
            get { return m_Title; }
            set
            {
                if (m_Title != value)
                {
                    m_Title = value;
                    NotifyPropertyChanged(() => Title);
                }
            }
        }

        public string Description
        {
            get { return m_Description; }
            set
            {
                if (m_Description != value)
                {
                    m_Description = value;
                    NotifyPropertyChanged(() => Description);
                }
            }
        }

        public string Image
        {
            get { return m_Image; }
            set
            {
                if (m_Image != value)
                {
                    m_Image = value;
                    NotifyPropertyChanged(() => Image);
                }
            }
        }

        public long LastRefreshTimeTicks { get; set; }

        public async Task Load(bool force)
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            Tracer.TraceInformation("Podcast.Load(): {0} from {1}", Title, localFolder.Path);
            try
            {
                StorageFile file = null;
                try
                {
                    file = await localFolder.GetFileAsync(CacheFileName);
                }
                catch (FileNotFoundException)
                {
                    Tracer.TraceInformation("Can't find cache file {0} for podcast {1}", CacheFileName, Id);
                }
                if (file != null)
                {
                    TouchedFiles.Instance.Add(file.Path);
                    using (Stream stream = await file.OpenStreamForReadAsync())
                    {
                        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(PodcastData));
                        Podcast readPodcast = Podcast.FromData(ServiceContext, (PodcastData)serializer.ReadObject(stream));

                        await UpdateFields(readPodcast);
                    }
                }
                await RefreshFromRss(force);
                PruneEmptyEpisodes();
            }
            catch (Exception e)
            {
                Tracer.TraceInformation("Podcast.Load(): error loading {0}. {1}", CacheFileName, e);
            }
        }

        public async Task RefreshFromRss(bool force)
        {
            DateTime lastRefreshTime = new DateTime(LastRefreshTimeTicks);
            if (DateTime.UtcNow - lastRefreshTime < TimeSpan.FromHours(2) && !force && Episodes.Count > 0)
            {
                return;
            }

            SyndicationFeed syndicationFeed = new SyndicationFeed();

            HttpClient httpClient = new HttpClient();
            string xmlString = await httpClient.GetStringAsync(new Uri(PodcastUri));
            XmlDocument feedXml = new XmlDocument();
            feedXml.LoadXml(xmlString);

            syndicationFeed.LoadFromXml(feedXml);

            // don't refresh if feed has not been updated since
            if ((syndicationFeed.LastUpdatedTime != null && syndicationFeed.LastUpdatedTime.DateTime > lastRefreshTime) ||
                force)
            {
                Title = syndicationFeed.Title.Text;

                if (syndicationFeed.Subtitle != null)
                {
                    Description = syndicationFeed.Subtitle.Text;
                }

                if (syndicationFeed.ImageUri != null)
                {
                    await LoadImage(syndicationFeed.ImageUri.ToString());
                }
                else if (Image != null)
                {
                    await LoadImage(Image);
                }

                Episodes.HoldNotifications = true;
                ReadRssEpisodes(syndicationFeed);
                Episodes.HoldNotifications = false;
            }

            // keep record of last update time
            LastRefreshTimeTicks = DateTime.UtcNow.Ticks;
        }

        private void PruneEmptyEpisodes()
        {
            List<Episode> toRemove = new List<Episode>();
            foreach (Episode episode in Episodes)
            {
                if (episode.Title == null)
                {
                    toRemove.Add(episode);
                }
            }
            
            foreach (Episode episode in toRemove)
            {
                Episodes.Remove(episode);
            }
        }

        private void ReadRssEpisodes(SyndicationFeed syndicationFeed)
        {
            foreach (SyndicationItem item in syndicationFeed.Items)
            {
                Uri uri = null;
                foreach (SyndicationLink link in item.Links)
                {
                    if (link.Relationship == "enclosure" /*&& link.MediaType == "audio/mpeg"*/)
                    {
                        uri = link.Uri;
                        break;
                    }
                }
                if (uri != null)
                {
                    string episodeTitle = item.Title != null ? item.Title.Text : "<No Title>";
                    string episodeSummary = item.Summary != null ? item.Summary.Text : "<No Summary>";
                    DateTimeOffset publishDate = item.PublishedDate;

                    Episode episode = GetEpisodeByUri(uri);

                    episode.Description = episodeSummary;
                    episode.Title = episodeTitle;
                    episode.PublishDate = publishDate;
                }
            }
        }

        private async Task LoadImage(string imageUri)
        {
            Uri validUri;
            if (!System.Uri.TryCreate(imageUri, UriKind.Absolute, out validUri))
            {
                return;
            }

            string imageExtension = Path.GetExtension(validUri.AbsolutePath);
            string localImagePath = string.Format("{0}{1}", FileName, imageExtension);
            ulong oldFileSize = await GetCachedFileSize(localImagePath);
            // the image we have is from the cache
            IDownloader downloader = m_DownloadService.CreateDownloader(validUri, ApplicationData.Current.LocalFolder, localImagePath, null);
            try
            {
                // Don't try to download if all we have is the local file
                if (!validUri.IsFile)
                {
                    Tracer.TraceInformation("Podcast.LoadImage(): Downloading {0} -> {1}", validUri, localImagePath);
                    StorageFile localImageFile = await downloader.Download();
                    Tracer.TraceInformation("Podcast.LoadImage(): Finished downloading {0} -> {1}", validUri, localImageFile.Path);
                    Image = localImageFile.Path;

                    ulong newFileSize = await GetCachedFileSize(localImagePath);
                    if (newFileSize != oldFileSize)
                    {
                        NotifyPropertyChanged(() => Image);
                    }
                }

                TouchedFiles.Instance.Add(Image);
            }
            catch (Exception e)
            {
                Tracer.TraceInformation("Podcast.LoadImage(): Failed downloading {0} -> {1}. {2}", validUri, localImagePath, e);
            }
        }

        private async Task<ulong> GetCachedFileSize(string path)
        {
            ulong fileSize = 0;
            IStorageFile existingFile = await ApplicationData.Current.LocalFolder.TryGetFileAsync(path);
            if (existingFile != null)
            {
                BasicProperties fileProperties = await existingFile.GetBasicPropertiesAsync();
                fileSize = fileProperties.Size;
            }
            return fileSize;
        }

        private Task UpdateFields(Podcast fromCache)
        {
            Title = fromCache.Title;
            if (fromCache.Description != null)
            {
                Description = fromCache.Description;
            }
            if (fromCache.Image != null)
            {
                Image = fromCache.Image;
            }
            TouchedFiles.Instance.Add(Image);
            LastRefreshTimeTicks = fromCache.LastRefreshTimeTicks;
            if (fromCache.Episodes == null)
            {
                return Task.FromResult<object>(null);
            }

            foreach (Episode episodeFromCache in fromCache.Episodes)
            {
                Episode episode = GetEpisodeByUri(episodeFromCache.Uri);
                if (episode != null)
                {
                    episode.UpdateFromCache(episodeFromCache);
                }
            }
            return Task.FromResult<object>(null);
        }

        private Episode GetEpisodeByUri(Uri uri)
        {
            IEnumerable<Episode> found = Episodes.Where((episode) => GetNormalizedEpisodeUri(episode.Uri).Equals(GetNormalizedEpisodeUri(uri)));

            if (found.Count() > 0)
            {
                return found.First();
            }

            // new episode from RSS
            Episode newEpisode = new Episode(ServiceContext, FileName, uri);
            AddEpisode(newEpisode);

            return newEpisode;
        }

        private string GetNormalizedEpisodeUri(Uri uri)
        {
            return  uri.Host + uri.AbsolutePath;
        }
        private string CacheFileName
        {
            get
            {
                return string.Format("{0}.json", FileName);
            }
        }

        public async Task Store()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile jsonFile = await localFolder.CreateFileAsync(CacheFileName + ".tmp", CreationCollisionOption.ReplaceExisting);
            DataContractJsonSerializer serialzer = new DataContractJsonSerializer(typeof(PodcastData));
            using (Stream stream = await jsonFile.OpenStreamForWriteAsync())
            {
                serialzer.WriteObject(stream, this.ToData());
            }
            await jsonFile.RenameAsync(CacheFileName, NameCollisionOption.ReplaceExisting);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged<TValue>(Expression<Func<TValue>> propertyId)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(((MemberExpression)propertyId.Body).Member.Name));
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