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
            ParentCollection = parentCollection;

        }

        public async Task LoadStateAsync(ObservableCollection<EpisodeDataItem> parentCollection)
        {
            ParentCollection = parentCollection;
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            try
            {
                await localFolder.GetFileAsync(FileName);
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
            StorageFile localFile = await localFolder.CreateFileAsync(FileName, CreationCollisionOption.ReplaceExisting);
            BackgroundDownloader downloader = new BackgroundDownloader();
            try
            {
                DownloadOperation downloadOperation = downloader.CreateDownload(Uri, localFile);
                IsDownloaded = true;
                await downloadOperation.StartAsync();
            }
            catch (Exception ex)
            {
                IsDownloaded = false;
            }
          
        }

        public string FullFileName
        {
            get
            {
                string s = System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, FileName);
                return s;
            }
        }

        private string FileName
        {
            get
            {
                return System.IO.Path.Combine(PodcastUniqueId, System.IO.Path.GetFileName(Uri.ToString()));
            }
        }

        private ObservableCollection<EpisodeDataItem> ParentCollection { get; set; }
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
                return ParentCollection.IndexOf(this);
            }
        }

        public override string ToString()
        {
            return this.Title;
        }
    }
}
