using System.Collections.Generic;
using System.Threading.Tasks;

namespace PodCatch.DataModel.Search
{
    public interface ISearch
    {
        Task<IEnumerable<Podcast>> FindAsync(string searchTerm, int limit);
    }
}