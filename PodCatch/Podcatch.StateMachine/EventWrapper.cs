using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Podcatch.StateMachine
{
    class EventWrapper<T>
    {
        private Object m_Event;
        private TaskCompletionSource<IState<T>> m_TaskCompletionSource;

        public EventWrapper(Object anEvent, TaskCompletionSource<IState<T>> taskCompletionSource)
        {
            m_Event = anEvent;
            m_TaskCompletionSource = taskCompletionSource;
        }

        public Object Event
        {
            get{ return m_Event;}
        }

        public TaskCompletionSource<IState<T>> TaskCompletionSource
        {
            get { return m_TaskCompletionSource; }
        }
    }
}
