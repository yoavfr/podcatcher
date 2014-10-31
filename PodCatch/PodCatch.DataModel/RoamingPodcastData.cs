﻿using System;
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
            roamingPodcastData.Uri = podcast.PodcastUri;
            roamingPodcastData.Title = podcast.Title;
            foreach (Episode episode in podcast.AllEpisodes)
            {
                // Store as little as possible in roaming settings. 
                // If there is no Position to record or this has not been played, we can rely on the default values upon deserialization
                if (episode.Position > TimeSpan.FromTicks(0) || episode.Played)
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
                PodcastUri = Uri,
                Title = Title,
            };
            foreach (RoamingEpisodeData roamingEpisodeData in RoamingEpisodesData)
            {
                Episode episode = roamingEpisodeData.ToEpisode(podcast.FileName);
                //episode.ParentCollection = podcast.AllEpisodes;
                podcast.AllEpisodes.Add(episode);
            }
            return podcast;
        }
    }
}
