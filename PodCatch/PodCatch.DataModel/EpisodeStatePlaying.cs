﻿using Podcatch.Common.StateMachine;
using PodCatch.Common;
using System.Threading.Tasks;

namespace PodCatch.DataModel
{
    public class EpisodeStatePlaying : AbstractState<Episode, EpisodeEvent>
    {
        public EpisodeStatePlaying(IServiceContext serviceContext)
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
                case EpisodeEvent.Pause:
                    owner.MediaPlayer.Pause();
                    break;

                case EpisodeEvent.Paused:
                    return Task.FromResult<IState<Episode, EpisodeEvent>>(GetState<EpisodeStateDownloaded>());

                case EpisodeEvent.Ended:
                    owner.Played = true;
                    return Task.FromResult<IState<Episode, EpisodeEvent>>(GetState<EpisodeStateDownloaded>());

                case EpisodeEvent.DonePlaying:
                    owner.Played = true;
                    return Task.FromResult<IState<Episode, EpisodeEvent>>(GetState<EpisodeStateDownloaded>());

                case EpisodeEvent.Scan:
                    return Task.FromResult<IState<Episode, EpisodeEvent>>(GetState<EpisodeStateScanning>());
            }
            return Task.FromResult<IState<Episode, EpisodeEvent>>(null);
        }
    }
}