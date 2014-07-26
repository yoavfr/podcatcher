using Newtonsoft.Json;
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
    public class PodcastGroup
    {
        public PodcastGroup()
        {
            Podcasts = new ObservableCollection<Podcast>();
        }
        public ObservableCollection<Podcast> Podcasts { get; private set; }
        [DataMember]
        public string Id { get; set; }
        public string TitleText { get; set; }
        public string SubtitleText { get; set; }
        public string DescriptionText { get; set; }
        [DataMember]
        public Collection<RoamingPodcastData> RoamingPodcastsData
        {
            get
            {
                Collection<RoamingPodcastData> roamingData = new Collection<RoamingPodcastData>();
                foreach(Podcast podcast in Podcasts)
                {
                    roamingData.Add(RoamingPodcastData.FromPodcast(podcast));
                }
                return roamingData;
            }
            set
            {
                // this will be null when deserializing
                if (Podcasts == null)
                {
                    Podcasts = new ObservableCollection<Podcast>();
                }
                foreach (RoamingPodcastData roamingData in value)
                {
                    Podcasts.Add(roamingData.ToPodcast());
                }
            }
        }
    }
}
