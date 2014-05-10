using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.FileProperties;

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

        public bool Changed { get; private set; }

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
            Changed = false;
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

            ulong oldFileSize = await GetCachedFileSize(localImagePath);

            // the image we have is from the cache
            StorageFile localImageFile = await localFolder.CreateFileAsync(localImagePath, CreationCollisionOption.ReplaceExisting);
            BackgroundDownloader downloader = new BackgroundDownloader();
            try
            {
                DownloadOperation downloadOperation = downloader.CreateDownload(new Uri(Image), localImageFile);
                await downloadOperation.StartAsync();
                Update(localImageFile.Path, ImageSource);

                ulong newFileSize = await GetCachedFileSize(localImagePath);
                if (newFileSize > oldFileSize)
                {
                    Changed = true;
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
    }
}
