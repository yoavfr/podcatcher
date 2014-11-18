using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Web.Http;

namespace PodCatch.DataModel
{
    public class Downloader
    {
        private Uri m_SourceUri;
        private StorageFolder m_DestinationStorageFolder;
        private string m_DestinationFileName;
        private uint m_bufferSize = 2048;
        private Progress<Downloader> m_Progress;

        public ulong TotalBytes { private set; get; }
        public ulong DownloadedBytes { private set; get; }

        public Downloader(Uri sourceUri, StorageFolder destinationStorageFolder, string destinationFileName)
        {
            m_SourceUri = sourceUri;
            m_DestinationStorageFolder = destinationStorageFolder;
            m_DestinationFileName = destinationFileName;
        }

        public Downloader(Uri sourceUri, StorageFolder destinationStorageFolder, string destinationFileName, Progress<Downloader> progress)
            : this(sourceUri, destinationStorageFolder, destinationFileName)
        {
            m_Progress = progress;
        }

        public async Task<StorageFile> Download()
        {
            return await Task<StorageFile>.Run(async () =>
                {
                    StorageFile tempFile = await m_DestinationStorageFolder.CreateFileAsync(m_DestinationFileName + ".tmp", CreationCollisionOption.GenerateUniqueName);
                    using (HttpClient httpClient = new HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
                        HttpResponseMessage response = await httpClient.GetAsync(m_SourceUri, HttpCompletionOption.ResponseHeadersRead);
                        if (response.StatusCode != HttpStatusCode.Ok /*||
                            Path.GetFileName(response.RequestMessage.RequestUri.AbsoluteUri) != Path.GetFileName(m_SourceUri.ToString())*/)
                        {
                            string result = await response.Content.ReadAsStringAsync();
                            throw new Exception(result);
                        }
                        
                        if (response.Content.Headers.ContentLength != null)
                        {
                            TotalBytes = response.Content.Headers.ContentLength.Value;
                        }
                        IBuffer buffer = new Windows.Storage.Streams.Buffer(m_bufferSize);
                        using (IRandomAccessStream fileStream = await tempFile.OpenAsync(FileAccessMode.ReadWrite))
                        {
                            using (IInputStream httpStream = await response.Content.ReadAsInputStreamAsync())
                            {
                                do
                                {
                                    await httpStream.ReadAsync(buffer, m_bufferSize, InputStreamOptions.ReadAhead);
                                    if (buffer.Length > 0)
                                    {
                                        await fileStream.WriteAsync(buffer);
                                        if (m_Progress != null)
                                        {
                                            DownloadedBytes += buffer.Length;
                                            ((IProgress<Downloader>)m_Progress).Report(this);
                                        }
                                    }
                                }
                                while (buffer.Length > 0);
                            }
                            await fileStream.FlushAsync();
                        }
                    }

                    await tempFile.RenameAsync(Path.GetFileName(m_DestinationFileName), NameCollisionOption.ReplaceExisting);
                    return tempFile;
                });
        }
    }
}
