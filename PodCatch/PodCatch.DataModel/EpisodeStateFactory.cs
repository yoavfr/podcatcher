using Podcatch.Common.StateMachine;

namespace PodCatch.DataModel
{
    internal class EpisodeStateFactory : AbstractStateFactory<Episode, EpisodeEvent>
    {
        private static EpisodeStateFactory s_Instance = new EpisodeStateFactory();

        public static EpisodeStateFactory Instance
        {
            get
            {
                return s_Instance;
            }
        }

        private EpisodeStateFactory()
            : base(new AbstractState<Episode, EpisodeEvent>[]
        {
            new EpisodeStateUnknown(),
            new EpisodeStatePendingDownload(),
            new EpisodeStateDownloading(),
            new EpisodeStateDownloaded(),
            new EpisodeStatePlaying(),
            new EpisodeStateScanning(),
        })
        { }
    }
}