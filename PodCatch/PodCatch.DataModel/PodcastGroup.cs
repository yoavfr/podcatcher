using PodCatch.Common;
using PodCatch.Common.Collections;
using PodCatch.DataModel.Data;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PodCatch.DataModel
{
    [DataContract]
    public class PodcastGroup : ServiceConsumer
    {
        public PodcastGroup(IServiceContext serviceContext)
            : base(serviceContext)
        {
            Podcasts = new ObservableConcurrentCollection<Podcast>();
        }

        public ObservableConcurrentCollection<Podcast> Podcasts { get; set; }

        public string Id { get; set; }

        public string TitleText { get; set; }

        public string SubtitleText { get; set; }

        public string DescriptionText { get; set; }

        public static PodcastGroup FromData(IServiceContext serviceContext, PodcastGroupData data)
        {
            PodcastGroup group = new PodcastGroup(serviceContext)
            {
                Id = data.Id
            };
            group.Podcasts = new ObservableConcurrentCollection<Podcast>();
            foreach (RoamingPodcastData podcastData in data.RoamingPodcastsData)
            {
                group.Podcasts.Add(Podcast.FromRoamingData(serviceContext, podcastData));
            }
            return group;
        }

        public PodcastGroupData ToData()
        {
            PodcastGroupData data = new PodcastGroupData()
            {
                Id = Id
            };
            List<RoamingPodcastData> podcasts = new List<RoamingPodcastData>();
            data.RoamingPodcastsData = podcasts;

            foreach (Podcast podcast in Podcasts)
            {
                podcasts.Add(podcast.ToRoamingData());
            }
            return data;
        }

        public override bool Equals(object obj)
        {
            PodcastGroup other = obj as PodcastGroup;
            if (other != null)
            {
                return other.Id == Id;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}