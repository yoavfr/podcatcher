using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.UI.Xaml.Shapes;

namespace PodCatch.Data
{
    [DataContract]
    public class EpisodeDataItem
    {
        public EpisodeDataItem(string podcastUniqueId, string title, string description, DateTimeOffset publishDate, Uri uri, ObservableCollection<EpisodeDataItem> parentCollection)
        {
            PodcastUniqueId = podcastUniqueId;
            Title = title;
            Description = description;
            PublishDate = publishDate;
            Uri = uri;
            m_parentCollection = parentCollection;

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            string localPath = GetLocalPath();
            try
            {
                //var operation = localFolder.GetFileAsync(localPath);
                //operation.AsTask().Wait();
                IsDownloaded = true;
            }
            catch (FileNotFoundException e)
            {
                IsDownloaded = false;
            }

        }

        public async Task DownloadAsync()
        {

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            string localPath = GetLocalPath();
            StorageFile localFile = await localFolder.CreateFileAsync(localPath, CreationCollisionOption.ReplaceExisting);
            BackgroundDownloader downloader = new BackgroundDownloader();
            try
            {
                DownloadOperation downloadOperation = downloader.CreateDownload(Uri, localFile);
                await downloadOperation.StartAsync();
                IsDownloaded = true;
            }
            catch (Exception ex)
            {

            }
          
        }

        private string GetLocalPath()
        {
            return System.IO.Path.Combine(PodcastUniqueId, System.IO.Path.GetFileName(Uri.ToString()));
        }

        private ObservableCollection<EpisodeDataItem> m_parentCollection;
        [DataMember]
        public string Title { get; private set; }
        [DataMember]
        public DateTimeOffset PublishDate { get; private set; }
        [DataMember]
        public string PodcastUniqueId { get; private set; }
        [DataMember]
        public string Description { get; private set; }
        public string UniqueId
        {
            get
            {
                return String.Format(@"{0}\{1}", PodcastUniqueId, Title);
            }
        }
        [DataMember]
        public Uri Uri { get; private set; }

        [DataMember]
        public bool IsDownloaded
        {
            get;
            private set;
        }

        public int Index
        {
            get
            {
                return m_parentCollection.IndexOf(this);
            }
        }

        public override string ToString()
        {
            return this.Title;
        }
    }
}
