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
    [DataContract]
    public class Podcast : INotifyPropertyChanged
    {
        private string m_Title;
        private string m_Description;
        private string m_Image;
        int m_numEpisodesToShow = 3;
        ObservableCollection<Episode> m_Episodes;

        public Podcast()
        {
            AllEpisodes = new List<Episode>();
        }

        public string PodcastUri { get; set; }
        
        /// <summary>
        /// All the episodes of this podcast
        /// </summary>
        [DataMember]
        public List<Episode> AllEpisodes { get; set; }

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
                return Title.StripIllegalPathChars();
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

        public int NumUnplayedEpisodes
        {
            get
            {
                return AllEpisodes.Where((episode) => episode.Visible && !episode.Played && episode.IsDownloaded()).Count();
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
                                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Podcast));
                                Podcast readPodcast = (Podcast)serializer.ReadObject(stream);

                                await UpdateFields(readPodcast);
                                await RefreshDisplay();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Podcast.Load(): error loading {0}. {1}", CacheFileName, e);
                    }
                    await RefreshFromRss(false);
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
            NotifyPropertyChanged("NumUnplayedEpisodes");
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
            List<Task> allEpisodes = new List<Task>();
            MarkVisibleEpisodes();
            foreach (Episode episode in AllEpisodes)
            {
                if (episode.Visible)
                {
                    allEpisodes.Add(episode.PostEvent(EpisodeEvent.Download));
                }
            }
            await Task.WhenAll(allEpisodes);
        }

        public Podcast Self
        {
            get
            {
                return this;
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
            Downloader downloader = new Downloader(validUri, ApplicationData.Current.LocalFolder, localImagePath);
            try
            {
                Debug.WriteLine("Podcast.LoadImage(): Downloading {0} -> {1}", validUri, localImagePath);
                StorageFile localImageFile = await downloader.Download();
                Debug.WriteLine("Podcast.LoadImage(): Finished downloading {0} -> {1}", validUri, localImageFile.Path);
                Image = localImageFile.Path;

                TouchedFiles.Instance.Add(Image);
                ulong newFileSize = await GetCachedFileSize(localImagePath);
                //if (newFileSize != oldFileSize)
                {
                    NotifyPropertyChanged("Image");
                }
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
            Episode newEpisode = new Episode(uri)
            {
                PodcastFileName = FileName,
            };
            AllEpisodes.Add(newEpisode);

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
            DataContractJsonSerializer serialzer = new DataContractJsonSerializer(typeof(Podcast));
            using (Stream stream = await jsonFile.OpenStreamForWriteAsync())
            {
                serialzer.WriteObject(stream, this);
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
