using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Podcatch.Common.StateMachine
{
    class EventWrapper<O, E>
    {
        private E m_Event;
        private TaskCompletionSource<IState<O, E>> m_TaskCompletionSource;

        public EventWrapper(E anEvent, TaskCompletionSource<IState<O, E>> taskCompletionSource)
        {
            m_Event = anEvent;
            m_TaskCompletionSource = taskCompletionSource;
        }

        public E Event
        {
            get{ return m_Event;}
        }

        public TaskCompletionSource<IState<O, E>> TaskCompletionSource
        {
            get { return m_TaskCompletionSource; }
        }
    }
}
