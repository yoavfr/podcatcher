﻿using PodCatch.DataModel;
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
            : this(uri, null)
        {
            Title = title;
            Description = description;
            PodcastImage.Update(searchImage, ImageSource.Search);
            LastUpdatedTimeTicks = 0;
            LastStoreTimeTicks = 0;
        }

        public Podcast(String uri, BaseData parent) : base (null)
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

        [DataMember]
        public ObservableCollection<Episode> Episodes {get; private set;}
        [DataMember]
        private long LastUpdatedTimeTicks { get; set; }
        [DataMember]
        private long LastStoreTimeTicks { get; set; }

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
                    PodcastImage.Update(syndicationFeed.ImageUri.AbsoluteUri, ImageSource.Rss);
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
                    PodcastImage.Update(readItem.PodcastImage.Image, readItem.PodcastImage.ImageSource);
                    
                    // load episode states
                    foreach (Episode episode in Episodes)
                    {
                        episode.Parent = this;
                        await episode.LoadStateAsync(Episodes);
                    }
                }
                catch (Exception ex)
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
            await PodcastImage.StoreToCacheAsync();
            if (PodcastImage.ImageSource == ImageSource.Rss)
            {
                NotifyPropertyChanged("Image");
            }

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile xmlFile = await localFolder.CreateFileAsync(string.Format("{0}.json", UniqueId), CreationCollisionOption.ReplaceExisting);
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Podcast));
            using (Stream stream = await xmlFile.OpenStreamForWriteAsync())
            {
                serializer.WriteObject(stream, this);
            }
        }

        public override string ToString()
        {
            return this.Title;
        }
    }
}
