using Podcatch.StateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodCatch.DataModel
{
    class EpisodeStateFactory : AbstractStateFactory<Episode, EpisodeEvent>
    {
        private static EpisodeStateFactory s_Instance = new EpisodeStateFactory();
        public static EpisodeStateFactory Instance
        {
            get
            {
                return s_Instance;
            }
        }
        private  EpisodeStateFactory() : base (new AbstractState<Episode, EpisodeEvent>[] 
        {
            new EpisodeStatePendingDownload(),
            new EpisodeStateDownloading(),
            new EpisodeStateDownloaded(),
            new EpisodeStatePlaying(),
            new EpisodeStateScanning(),
        })
        { }
    }
}
