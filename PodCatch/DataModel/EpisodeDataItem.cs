using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PodCatch.Data
{
    [DataContract]
    public class EpisodeDataItem
    {
        public EpisodeDataItem(string podcastUniqueId, string title, string description, DateTimeOffset publishDate, Uri uri, ObservableCollection<EpisodeDataItem> parentCollection)
        {
            PodcastUniqueId = podcastUniqueId;
            Title = title;
            Description = description;
            PublishDate = publishDate;
            Uri = uri;
            m_parentCollection = parentCollection;
        }

        private ObservableCollection<EpisodeDataItem> m_parentCollection;
        [DataMember]
        public string Title { get; private set; }
        [DataMember]
        public DateTimeOffset PublishDate { get; private set; }
        public string PodcastUniqueId { get; private set; }
        [DataMember]
        public string Description { get; private set; }
        public string UniqueId
        {
            get
            {
                return String.Format(@"{0}\{1}", PodcastUniqueId, Title);
            }
        }
        [DataMember]
        public Uri Uri { get; private set; }

        public int Index
        {
            get
            {
                return m_parentCollection.IndexOf(this);
            }
        }

        public override string ToString()
        {
            return this.Title;
        }
    }
}
