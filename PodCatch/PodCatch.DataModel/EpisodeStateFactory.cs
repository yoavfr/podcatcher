using Podcatch.Common.StateMachine;
using PodCatch.Common;

namespace PodCatch.DataModel
{
    internal class EpisodeStateFactory : AbstractStateFactory<Episode, EpisodeEvent>
    {
        private static EpisodeStateFactory s_Instance = null;

        public static EpisodeStateFactory GetInstance(IServiceContext serviceContext)
        {
            if (s_Instance == null)
            {
                s_Instance = new EpisodeStateFactory(serviceContext);
            }
            return s_Instance;
        }

        private EpisodeStateFactory(IServiceContext serviceContext)
            : base(new AbstractState<Episode, EpisodeEvent>[]
        {
            new EpisodeStateUnknown(serviceContext),
            new EpisodeStatePendingDownload(serviceContext),
            new EpisodeStateDownloading(serviceContext),
            new EpisodeStateDownloaded(serviceContext),
            new EpisodeStatePlaying(serviceContext),
            new EpisodeStateScanning(serviceContext),
        })
        { }
    }
}