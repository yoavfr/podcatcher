using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PodCatch.DataModel.Data
{
    [DataContract]
    public class PodcastGroupData
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public IEnumerable<RoamingPodcastData> RoamingPodcastsData { get; set; }
    }
}