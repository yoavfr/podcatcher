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
        public PodcastGroup(String uniqueId, String titleText, String subtitleText, String imagePath, String descriptionText)
        {
            this.UniqueId = uniqueId;
            this.TitleText = titleText;
            this.SubtitleText = subtitleText;
            this.DescriptionText = descriptionText;
            this.ImagePath = imagePath;
            this.Podcasts = new ObservableCollection<Podcast>();
        }

        [GlobalDataMember]
        public string UniqueId { get; private set; }
        [GlobalDataMember]
        public string TitleText { get; private set; }
        [GlobalDataMember]
        public string SubtitleText { get; private set; }
        [GlobalDataMember]
        public string DescriptionText { get; private set; }
        [GlobalDataMember]
        public string ImagePath { get; private set; }
        [DataMember]
        [GlobalDataMember]
        public ObservableCollection<Podcast> Podcasts { get; private set; }

        public override string ToString()
        {
            return this.TitleText;
        }
    }
}
