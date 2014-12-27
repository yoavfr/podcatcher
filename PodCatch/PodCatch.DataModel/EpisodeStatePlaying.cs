using Podcatch.StateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodCatch.DataModel
{
    public class EpisodeStatePlaying : AbstractState<Episode, EpisodeEvent>
    {
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
                case EpisodeEvent.Pause:
                case EpisodeEvent.DonePlaying:
                    {
                        return Task.FromResult<IState<Episode, EpisodeEvent>>(EpisodeStateFactory.Instance.GetState<EpisodeStateDownloaded>());
                    }
                case EpisodeEvent.Scan:
                    {
                        return Task.FromResult<IState<Episode, EpisodeEvent>>(EpisodeStateFactory.Instance.GetState<EpisodeStateScanning>());
                    }
            }
            return Task.FromResult<IState<Episode, EpisodeEvent>>(null);
        }
    }
}
