using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PodCatch.DataModel.Data
{
    [DataContract]
    public class RoamingPodcastData
    {
        [DataMember]
        public string Uri { get; set; }

        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public IEnumerable<RoamingEpisodeData> RoamingEpisodesData { get; set; }
    }
}