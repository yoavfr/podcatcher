using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PodCatch.DataModel.Data
{
    [DataContract]
    public class PodcastData
    {
        [DataMember]
        public IEnumerable<EpisodeData> Episodes { get; set; }

        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string Image { get; set; }

        [DataMember]
        public long LastRefreshTimeTicks { get; set; }
    }
}
