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
        public PodcastGroup(String uniqueId, String title, String subtitle, String imagePath, String description)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Subtitle = subtitle;
            this.Description = description;
            this.ImagePath = imagePath;
            this.Items = new ObservableCollection<Podcast>();
        }

        [GlobalDataMember]
        public string UniqueId { get; private set; }
        [GlobalDataMember]
        public string Title { get; private set; }
        [GlobalDataMember]
        public string Subtitle { get; private set; }
        [GlobalDataMember]
        public string Description { get; private set; }
        [GlobalDataMember]
        public string ImagePath { get; private set; }
        [DataMember]
        [GlobalDataMember]
        public ObservableCollection<Podcast> Items { get; private set; }

        public override string ToString()
        {
            return this.Title;
        }
    }
}
