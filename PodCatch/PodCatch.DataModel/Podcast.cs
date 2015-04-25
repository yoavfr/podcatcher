using PodCatch.Common;
using PodCatch.Common.Collections;
using PodCatch.DataModel.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
        private string m_SearchImage;
        private string m_RssImage;

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
            get
            {
                if (!string.IsNullOrEmpty(m_RssImage))
                {
                    Tracer.TraceInformation("Returning rss image location {0} for podcast {1}", m_RssImage, Title);
                    return m_RssImage;
                }
                if (!string.IsNullOrEmpty(m_SearchImage))
                {
                    Tracer.TraceInformation("Returning search image location {0} for podcast {1}", m_SearchImage, Title);
                    return m_SearchImage;
            }
                // Cached image if exists will always have this path
                Tracer.TraceInformation("Returning default cached image location for podcast {0}", Title);
                return string.Format("{0}\\{1}.jpg", ApplicationData.Current.LocalFolder.Path, Id);
        }
        }

        public string SearchImage
        {
            set
            {
                m_SearchImage = value;
            }
        }

        public long LastRefreshTimeTicks { get; set; }

        public async Task Load(bool force)
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            Stopwatch stopwatch = new Stopwatch();
            Tracer.TraceInformation("Podcast.Load(): start loading {0} from {1}", Title, localFolder.Path);
            try
            {
                var file = await localFolder.TryGetFileAsync(CacheFileName);
                if (file != null)
                {
                    TouchedFiles.Instance.Add(file.Path);
                    using (Stream stream = await file.OpenStreamForReadAsync())
                    {
                        stopwatch.Start();
                        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(PodcastData));
                        Podcast readPodcast = Podcast.FromData(ServiceContext, (PodcastData)serializer.ReadObject(stream));
                        stopwatch.Stop();
                        Tracer.TraceInformation("Podcast.Load() deserializing cached for {0} took {1}", Title, stopwatch.Elapsed);
                        stopwatch.Reset();
                        stopwatch.Start();
                        await UpdateFields(readPodcast);
                        stopwatch.Stop();
                        Tracer.TraceInformation("Podcast.Load() update from local cache of {0} took {1}", Title, stopwatch.Elapsed);
                    }
                }
                Tracer.TraceInformation("Podcast.Load(): done reading local cache for {0}", Title);
                await RefreshFromRss(false, true);
                Tracer.TraceInformation("Podcast.Load(): done refreshing {0}", Title);

                PruneEmptyEpisodes();
            }
            catch (Exception e)
            {
                Tracer.TraceInformation("Podcast.Load(): error loading {0}. {1}", CacheFileName, e);
            }
        }

        public async Task RefreshFromRss(bool force, bool cacheImage)
        {
            DateTime lastRefreshTime = new DateTime(LastRefreshTimeTicks);
            if (DateTime.UtcNow - lastRefreshTime < TimeSpan.FromHours(24) && !force && Episodes.Count > 0)
            {
                Tracer.TraceInformation("Not refreshing Podcast {0} from Rss", Title);
                return;
            }

            Tracer.TraceInformation("Refreshing {0} from Rss", Title);
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

                // If we have a source for the image in the syndication feed use it
                if (syndicationFeed.ImageUri != null)
                {
                    if (cacheImage)
                    {
                    await LoadImage(syndicationFeed.ImageUri.ToString());
                        m_RssImage = null;
                    }
                    else
                    {
                        m_RssImage = syndicationFeed.ImageUri.ToString();
                        NotifyPropertyChanged(() => Image);
                    }
                }
                else if (m_SearchImage != null && cacheImage)
                {
                    await LoadImage(m_SearchImage);
                    m_SearchImage = null;
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

        public Task CacheImage()
        {
            return LoadImage(Image);
        }

        private async Task LoadImage(string imageUri)
        {
            Uri validUri;
            if (!System.Uri.TryCreate(imageUri, UriKind.Absolute, out validUri))
            {
                return;
            }

            // Path.GetExtension(validUri.AbsolutePath);
            string localImagePath = string.Format("{0}.jpg", Id);
            ulong oldFileSize = await GetCachedFileSize(localImagePath);
            // the image we have is from the cache
            IDownloader downloader = m_DownloadService.CreateDownloader(validUri, ApplicationData.Current.LocalFolder, localImagePath, null);
            try
            {
                // Don't try to download if all we have is the local file
                if (!validUri.IsFile)
                {
                    Tracer.TraceInformation("Podcast.LoadImage(): Downloading {0} -> {1}", validUri, localImagePath);
                    var localImageFile = await downloader.Download();
                    Tracer.TraceInformation("Podcast.LoadImage(): Finished downloading {0} -> {1}", validUri, localImageFile.Path);

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
            TouchedFiles.Instance.Add(Image);
            LastRefreshTimeTicks = fromCache.LastRefreshTimeTicks;
            if (fromCache.Episodes == null)
            {
                return Task.FromResult<object>(null);
            }

            Episodes.HoldNotifications = true;
            foreach (Episode episodeFromCache in fromCache.Episodes)
            {
                Episode episode = GetEpisodeByUri(episodeFromCache.Uri);
                if (episode != null)
                {
                    episode.UpdateFromCache(episodeFromCache);
                }
            }
            Episodes.HoldNotifications = false;
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
            return uri.Host + uri.AbsolutePath;
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