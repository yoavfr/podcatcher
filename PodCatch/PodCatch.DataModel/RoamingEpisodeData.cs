using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PodCatch.DataModel
{
    [DataContract]
    sealed public class RoamingEpisodeData
    {
        [DataMember]
        public string Id { get; set; }
        [DataMember]
        private long m_PositionTicks;
        public TimeSpan Position 
        { 
            get
            {
                return TimeSpan.FromTicks(m_PositionTicks);
            }
            set
            {
                m_PositionTicks = value.Ticks;
            }
        }
        
        public static RoamingEpisodeData FromEpisode(Episode episode)
        {
            RoamingEpisodeData roamingEpisodeData = new RoamingEpisodeData()
            {
                Id = episode.Id,
                Position = episode.Position,
            };
            return roamingEpisodeData;
        }

        public Episode ToEpisode(string podcastId)
        {
            Episode episode = new Episode()
            {
                Id = Id,
                PodcastId = podcastId,
                Position = Position,
            };

            return episode;
        }
    }
}
