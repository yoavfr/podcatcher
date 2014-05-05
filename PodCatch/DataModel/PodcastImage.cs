using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;

namespace PodCatch.DataModel
{
    public enum ImageSource
    {
        NotSet,
        Search,
        Rss
    }

    [DataContract]
    public class PodcastImage : BaseData
    {
        [DataMember]
        public string Image { get; private set; }

        [DataMember]
        public ImageSource ImageSource { get; private set;}

        private string UniqueId { get; set; }

        public PodcastImage(string imagePath, ImageSource imageSource, string uniqueId) : base (null)
        {
            Image = imagePath;
            ImageSource = imageSource;
            UniqueId = uniqueId;
        }

        public void Update(string imagePath, ImageSource imageSource)
        {
            if (ImageSource == ImageSource.Rss && imageSource == ImageSource.Search 
                || imageSource == ImageSource.NotSet)
            {
                return;
            }
            Image = imagePath;
            ImageSource = imageSource;
        }

        public override async Task StoreToCacheAsync()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            if (string.IsNullOrEmpty(Image))
            {
                return;
            }

            Uri validUri;
            if (!Uri.TryCreate(Image, UriKind.Absolute, out validUri))
            {
                // should never happen
                return;
            }

            if (validUri.Scheme == "file")
            {
                // we are already in the cache
                return;
            }
                
            string imageExtension = Path.GetExtension(Image);
            string localImagePath = string.Format("{0}{1}", UniqueId, imageExtension);

            // the image we have is from the cache
            StorageFile localImageFile = await localFolder.CreateFileAsync(localImagePath, CreationCollisionOption.ReplaceExisting);
            BackgroundDownloader downloader = new BackgroundDownloader();
            try
            {
                DownloadOperation downloadOperation = downloader.CreateDownload(new Uri(Image), localImageFile);
                await downloadOperation.StartAsync();
                Update(localImageFile.Path, ImageSource);
            }
            catch (Exception e)
            {

            }
        }


    }
}
