using PodCatch.DataModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace PodCatch.Search
{
    public class ITunesSearch : ISearch
    {
        public async Task<ICollection<Podcast>> FindAsync(string searchTerm, int limit)
        {
            IList<Podcast> results = new List<Podcast>();
            try
            {
                string query = string.Format(@"https://itunes.apple.com/search?term={0}&media=podcast&entity=podcast&attribute=titleTerm&limit={1}", searchTerm, limit);
                HttpClient httpClient = new HttpClient();

                HttpResponseMessage response = await httpClient.GetAsync(query);
                byte[] jsonResult = response.Content.ReadAsByteArrayAsync().Result;
                using (Stream stream = new MemoryStream(jsonResult))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ITunesSearchResults));
                    ITunesSearchResults appleSearchResults = (ITunesSearchResults)serializer.ReadObject(stream);
                    foreach (ITunesSearchResult result in appleSearchResults.results)
                    {
                        Podcast podcast = new Podcast(result.artistName, result.feedUrl, result.artworkUrl100, string.Empty);
                        results.Add(podcast);
                    }
                }
                return results;
            }
            catch (Exception e)
            {
                return results;
            }
        }
    }

    [DataContract]
    public class ITunesSearchResults
    {
        [DataMember]
        public IEnumerable<ITunesSearchResult> results { get; set; }
    }

    [DataContract]
    public class ITunesSearchResult
    {
        [DataMember]
        public string artistName { get; set; }
        [DataMember]
        public string collectionName { get; set; }
        [DataMember]
        public string feedUrl { get; set; }
        public string artworkUrl30 { get; set; }
        [DataMember]
        public string artworkUrl60 { get; set; }
        [DataMember]
        public string artworkUrl100 { get; set; }
    }

}
