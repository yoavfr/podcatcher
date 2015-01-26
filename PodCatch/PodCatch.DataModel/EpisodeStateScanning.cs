using Podcatch.Common.StateMachine;
using PodCatch.Common;
using System.Threading.Tasks;

namespace PodCatch.DataModel
{
    public class EpisodeStateScanning : AbstractState<Episode, EpisodeEvent>
    {
        public EpisodeStateScanning(IServiceContext serviceContext)
            : base(serviceContext)
        {
        }

        public override Task OnEntry(Episode owner, IState<Episode, EpisodeEvent> fromState, IEventProcessor<Episode, EpisodeEvent> stateMachine)
        {
            owner.NotifyPropertyChanged(() => owner.State);
            return Task.FromResult<object>(null);
        }

        public override Task OnExit(Episode owner, IState<Episode, EpisodeEvent> toState, IEventProcessor<Episode, EpisodeEvent> stateMachine)
        {
            return Task.FromResult<object>(null);
        }

        public override Task<IState<Episode, EpisodeEvent>> OnEvent(Episode owner, EpisodeEvent anEvent, IEventProcessor<Episode, EpisodeEvent> stateMachine)
        {
            switch (anEvent)
            {
                case EpisodeEvent.Play:
                    {
                        return Task.FromResult<IState<Episode, EpisodeEvent>>(GetState<EpisodeStatePlaying>());
                    }
            }
            return Task.FromResult<IState<Episode, EpisodeEvent>>(null);
        }
    }
}