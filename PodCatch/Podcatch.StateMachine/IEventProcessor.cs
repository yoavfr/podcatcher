using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Podcatch.StateMachine
{
    /// <summary>
    /// An Event Processor is a consumer of events
    /// (This would have been part of IStateMachine but for the circular dependency with IState)
    /// </summary>
    public interface IEventProcessor<O, E>
    {
        /// <summary>
        /// Post an event to the state machine.
        /// </summary>
        /// <param name="anEvent">the event that is posted to the state machine</param>
        /// <param name="priority">priority of the event. Events of high priority will be processed before events of lower priority.
        ///                         Priority 0 is highest. </param>
        /// <returns>IAsyncResult - can be used to wait on the event to be processed, and to collect and errors that occured with EndPostEvent </returns>
        Task<IState<O, E>> PostEvent(E anEvent, byte priority);


    }
}
