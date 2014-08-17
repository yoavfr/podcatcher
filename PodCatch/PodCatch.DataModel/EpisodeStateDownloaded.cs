using Podcatch.StateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodCatch.DataModel
{
    public class EpisodeStateDownloaded : AbstractState<Episode, EpisodeEvent>
    {
        public override async Task OnEntry(Episode owner, IState<Episode, EpisodeEvent> fromState, IEventProcessor<Episode, EpisodeEvent> stateMachine)
        {
            owner.NotifyPropertyChanged("State");
        }

        public override async Task OnExit(Episode owner, IState<Episode, EpisodeEvent> toState, IEventProcessor<Episode, EpisodeEvent> stateMachine)
        {
        }

        public override async Task<IState<Episode, EpisodeEvent>> OnEvent(Episode owner, EpisodeEvent anEvent, IEventProcessor<Episode, EpisodeEvent> stateMachine)
        {
            switch (anEvent)
            {
                case EpisodeEvent.Play:
                    {
                        return EpisodeStateFactory.Instance.GetState<EpisodeStatePlaying>();
                    }
                case EpisodeEvent.Scan:
                    {
                        return EpisodeStateFactory.Instance.GetState<EpisodeStateScanning>();
                    }
            }
            return null;
        }
    }
}
