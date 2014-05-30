using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PodCatch.DataModel
{
    [DataContract]
    public class RoamingPodcastData
    {
        public RoamingPodcastData()
        {
            RoamingEpisodesData = new Collection<RoamingEpisodeData>();
        }
        [DataMember]
        public string Uri { get; set; }
        [DataMember]
        public string Title { get; set; }
        [DataMember]
        public Collection<RoamingEpisodeData> RoamingEpisodesData { get; set; }

        public static RoamingPodcastData FromPodcast(Podcast podcast)
        {
            RoamingPodcastData roamingPodcastData = new RoamingPodcastData();
            roamingPodcastData.Uri = podcast.Uri;
            roamingPodcastData.Title = podcast.Title;
            foreach (Episode episode in podcast.AllEpisodes)
            {
                if (episode.Position > TimeSpan.FromTicks(0))
                {
                    roamingPodcastData.RoamingEpisodesData.Add(RoamingEpisodeData.FromEpisode(episode));
                }
            }
            return roamingPodcastData;
        }

        public Podcast ToPodcast()
        {
            Podcast podcast = new Podcast()
            {
                Uri = Uri,
                Title = Title,
            };
            foreach (RoamingEpisodeData roamingEpisodeData in RoamingEpisodesData)
            {
                Episode episode = roamingEpisodeData.ToEpisode(podcast.Id);
                //episode.ParentCollection = podcast.AllEpisodes;
                podcast.AllEpisodes.Add(episode);
            }
            return podcast;
        }
    }
}
