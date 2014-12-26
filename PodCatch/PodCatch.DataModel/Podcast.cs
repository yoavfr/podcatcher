﻿using PodCatch.Common;
using PodCatch.DataModel.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Core;
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
        int m_numEpisodesToShow = 3;
        int m_numUnplayedEpisodes = 0;
        ObservableCollection<Episode> m_Episodes;

        public Podcast(IServiceContext serviceContext) : base(serviceContext)
        {
            m_DownloadService = serviceContext.GetService<IDownloadService>();
            AllEpisodes = new List<Episode>();
        }

        public static Podcast FromData (IServiceContext serviceContext, PodcastData data)
        {
            Podcast podcast = new Podcast(serviceContext);
            podcast.Title = data.Title;
            podcast.Description = data.Description;
            podcast.Image = data.Image;
            podcast.LastRefreshTimeTicks = data.LastRefreshTimeTicks;
            podcast.AllEpisodes = new List<Episode>();
            foreach(EpisodeData episodeData in data.Episodes)
            {
                Episode episode = Episode.FromData(serviceContext, podcast.FileName, episodeData);
                podcast.AllEpisodes.Add(episode);
            }
            return podcast;
        }

        public static Podcast FromRoamingData (IServiceContext serviceContext, RoamingPodcastData data)
        {
            Podcast podcast = new Podcast(serviceContext);
            podcast.Title = data.Title;
            podcast.PodcastUri = data.Uri;
            List<Episode> episodes = new List<Episode>();
            podcast.AllEpisodes = episodes;
            foreach (RoamingEpisodeData episodeData in data.RoamingEpisodesData)
            {
                episodes.Add(Episode.FromRoamingData(serviceContext, podcast.FileName, episodeData));
            }
            return podcast;
        }

        public PodcastData ToData ()
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
            foreach (Episode episode in AllEpisodes)
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
            foreach (Episode episode in AllEpisodes)
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
        public List<Episode> AllEpisodes { get; set; }

        public void AddEpisode(Episode episode)
        {
            episode.PropertyChanged += OnEpisodePropertyChanged; 
            AllEpisodes.Add(episode);
        }

        private void OnEpisodePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Played" || e.PropertyName == "State")
            {
                UpdateUnplayedEpisodes();
            }
        }

        /// <summary>
        /// Episodes that have Episode.Visible == true. Ideally we would do the filtering in a binding converter, but this is difficult in WinRT's ICollectionView
        /// </summary>
        public ObservableCollection<Episode> Episodes 
        {
            get
            {
                if (m_Episodes == null)
                {
                    m_Episodes = new ObservableCollection<Episode>(AllEpisodes.Where((episode) => episode.Visible));
                    int i = 0;
                    foreach (Episode episode in m_Episodes)
                    {
                        episode.Index = i++;
                    }
                }
                return m_Episodes;
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
                    NotifyPropertyChanged("Title"); 
                }
            }
        }
        public string Description
        {
            get { return m_Description; }
            private set 
            {
                if (m_Description != value)
                {
                    m_Description = value; 
                    NotifyPropertyChanged("Description"); 
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
                    NotifyPropertyChanged("Image");
                }
            }
        }

        public long LastRefreshTimeTicks { get; set; }

        public void UpdateUnplayedEpisodes()
        {
            NumUnplayedEpisodes = AllEpisodes.Where((episode) => episode.Visible && !episode.Played && episode.IsDownloaded()).Count();
        }

        public int NumUnplayedEpisodes
        {
            get
            {
                return m_numUnplayedEpisodes;
            }
            private set
            {
                if (m_numUnplayedEpisodes != value)
                {
                    m_numUnplayedEpisodes = value;
                    NotifyPropertyChanged("NumUnplayedEpisodes");
                }
            }
        }

        public async Task Load()
        {
            await Task.Run(async () =>
                {
                    StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                    Debug.WriteLine("Podcast.Load(): {0} from {1}", Title, localFolder.Path);
                    try
                    {
                        StorageFile file = await localFolder.GetFileAsync(CacheFileName);
                        if (file != null)
                        {
                            TouchedFiles.Instance.Add(file.Path);
                            using (Stream stream = await file.OpenStreamForReadAsync())
                            {
                                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(PodcastData));
                                Podcast readPodcast = Podcast.FromData(ServiceContext, (PodcastData)serializer.ReadObject(stream));

                                await UpdateFields(readPodcast);
                                await RefreshDisplay();
                            }
                        }
                        await RefreshFromRss(false);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Podcast.Load(): error loading {0}. {1}", CacheFileName, e);
                    }
                });
        }

        public async Task RefreshFromRss(bool force)
        {
            await Task.Run(async () =>
                {
                    DateTime lastRefreshTime = new DateTime(LastRefreshTimeTicks);
                    if (DateTime.UtcNow - lastRefreshTime < TimeSpan.FromHours(2) && !force && AllEpisodes.Count > 0)
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

                        ReadRssEpisodes(syndicationFeed);
                    }

                    // keep record of last update time
                    LastRefreshTimeTicks = DateTime.UtcNow.Ticks;
                    await RefreshDisplay();
                });
        }

        private async Task RefreshDisplay()
        {
            AllEpisodes.Sort((a, b) => { return a.PublishDate > b.PublishDate ? -1 : 1; });
            MarkVisibleEpisodes();
            await DisplayEpisodes();
            UpdateUnplayedEpisodes();
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

        public async Task DownloadEpisodes()
        {
            List<Task> downloadTasks = new List<Task>();
            MarkVisibleEpisodes();
            foreach (Episode episode in AllEpisodes)
            {
                if (episode.Visible)
                {
                    downloadTasks.Add(episode.PostEvent(EpisodeEvent.Download));
                }
            }
            await Task.WhenAll(downloadTasks);
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
                    Debug.WriteLine("Podcast.LoadImage(): Downloading {0} -> {1}", validUri, localImagePath);
                    StorageFile localImageFile = await downloader.Download();
                    Debug.WriteLine("Podcast.LoadImage(): Finished downloading {0} -> {1}", validUri, localImageFile.Path);
                    Image = localImageFile.Path;
                    
                    ulong newFileSize = await GetCachedFileSize(localImagePath);
                    if (newFileSize != oldFileSize)
                    {
                        NotifyPropertyChanged("Image");
                    }
                }

                TouchedFiles.Instance.Add(Image);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Podcast.LoadImage(): Failed downloading {0} -> {1}. {2}", validUri, localImagePath, e);
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

        private Task UpdateFields (Podcast fromCache)
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
            if (fromCache.AllEpisodes == null)
            {
                return Task.FromResult<object>(null);
            }

            foreach (Episode episodeFromCache in fromCache.AllEpisodes)
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
            IEnumerable<Episode> found = AllEpisodes.Where((episode) => episode.Uri.Equals(uri));

            if (found.Count() > 0)
            {
                return found.First();
            }


            // new episode from RSS
            Episode newEpisode = new Episode(ServiceContext, FileName, uri);
            AddEpisode(newEpisode);

            return newEpisode;
        }

        private string CacheFileName
        {
            get
            {
                return string.Format("{0}.json", FileName);
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
            MarkVisibleEpisodes();
            Task t = DisplayEpisodes();
            return m_numEpisodesToShow;
        }

        public void MarkVisibleEpisodes()
        {
            AllEpisodes.ForEach((episode, index) =>
            {
                if (index < m_numEpisodesToShow)
                {
                    episode.Visible = true;
                }
                else
                {
                    episode.Visible = false;
                }
            });
        }

        private async Task DisplayEpisodes()
        {
            // when not running in UI
            if (CoreApplication.Views.Count == 0)
            {
                return;
            }
            CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
            await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Episodes.Clear();
                Episodes.AddAll(AllEpisodes.Where((episode) => episode.Visible));
                int i=0;
                foreach(Episode episode in Episodes)
                {
                    episode.Index = i++;
                }
            });
        }

        public async Task Store()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile jsonFile = await localFolder.CreateFileAsync(CacheFileName+".tmp", CreationCollisionOption.ReplaceExisting);
            DataContractJsonSerializer serialzer = new DataContractJsonSerializer(typeof(PodcastData));
            using (Stream stream = await jsonFile.OpenStreamForWriteAsync())
            {
                serialzer.WriteObject(stream, this.ToData());
            }
            await jsonFile.RenameAsync(CacheFileName, NameCollisionOption.ReplaceExisting);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler == null || CoreApplication.Views.Count == 0)
            {
                return;
            }
            CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;

            if (dispatcher.HasThreadAccess)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
            else
            {
                IAsyncAction t = dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    handler(this, new PropertyChangedEventArgs(propertyName));
                });
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
