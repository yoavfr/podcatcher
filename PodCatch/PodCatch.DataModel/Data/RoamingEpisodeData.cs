using System.Runtime.Serialization;

namespace PodCatch.DataModel.Data
{
    [DataContract]
    sealed public class RoamingEpisodeData
    {
        [DataMember]
        public string Uri { get; set; }

        [DataMember]
        public bool Played { get; set; }

        [DataMember]
        public long PositionTicks { get; set; }

        [DataMember]
        public long m_PositionTicks { get; set; }
    }
}