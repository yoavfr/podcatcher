using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Web.Http;

namespace PodCatch.DataModel
{
    public class Downloader
    {
        private Uri m_SourceUri;
        private StorageFile m_DestinationFile;
        private uint m_bufferSize = 2048;
        private Progress<Downloader> m_Progress;

        public ulong TotalBytes { private set; get; }
        public ulong DownloadedBytes { private set; get; }

        public Downloader(Uri sourceUri, StorageFile destinationFile)
        {
            m_SourceUri = sourceUri;
            m_DestinationFile = destinationFile;
        }

        public Downloader(Uri sourceUri, StorageFile destinationFile, Progress<Downloader> progress) : this (sourceUri, destinationFile)
        {
            m_Progress = progress;
        }

        public async Task Download()
        {
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
                HttpResponseMessage response = await httpClient.GetAsync(m_SourceUri, HttpCompletionOption.ResponseHeadersRead);
                TotalBytes = response.Content.Headers.ContentLength.Value;
                IBuffer buffer = new Windows.Storage.Streams.Buffer(m_bufferSize);
                using (IRandomAccessStream fileStream = await m_DestinationFile.OpenAsync(FileAccessMode.ReadWrite))
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
        }
    }
}
