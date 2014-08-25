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
        public string Uri { get; set; }
        [DataMember]
        private long m_PositionTicks;
        [DataMember]
        public bool Played { get; set; }
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
                Uri = episode.Uri.ToString(),
                Position = episode.Position,
                Played = episode.Played,
            };
            return roamingEpisodeData;
        }

        public Episode ToEpisode(string podcastId)
        {
            Episode episode = new Episode(new Uri(Uri))
            {
                PodcastId = podcastId,
                Position = Position,
                Played = Played,
            };

            return episode;
        }
    }
}
