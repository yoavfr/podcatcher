using System;
using System.Runtime.Serialization;

namespace PodCatch.DataModel.Data
{
    [DataContract]
    public class EpisodeData
    {
        [DataMember]
        public Uri Uri { get; set; }

        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public long PublishDateTicks { get; set; }
    }
}