using PodCatch.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodCatch.Search
{
    public interface ISearch
    {
        Task<ICollection<Podcast>> FindAsync(string searchTerm, int limit);
    }
}
