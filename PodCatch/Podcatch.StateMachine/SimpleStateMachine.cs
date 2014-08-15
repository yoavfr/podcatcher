using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;

namespace Podcatch.StateMachine
{
    /// <summary>
    /// An implementation of IStateMachine
    /// </summary>
    public class SimpleStateMachine<T> : IStateMachine<T>
    {
        private readonly T m_Owner;
        private readonly byte m_MaxPriority;
        private readonly Queue<EventWrapper<T>>[] m_EventQueues;
        private IState<T> m_CurrentState;

        private int m_NumPendingEvents;
        private bool m_PumpOn;
        private readonly Object m_PumpLock = new Object();
        private readonly IBasicLogger m_Logger;
        private const byte MIN_PRIORITY=0;
        private const byte MAX_PRIORITY=10; 

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="owner">the owner of this state machine</param>
        /// <param name="maxPriority">the maximum number (lowest priority) that can be provided for an event. Setting this to 0 means there will only be
        ///                         one priority. This can save memory when dealing with many objects that contain a state machine</param>
        public SimpleStateMachine(IBasicLogger logger, T owner, byte maxPriority)
        {
            if (maxPriority<MIN_PRIORITY || maxPriority>MAX_PRIORITY)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture,"maxPriority {0} is not between {1} and {2}",maxPriority,MIN_PRIORITY,MAX_PRIORITY));
            }
            // create a queue for every possible priority value
            m_EventQueues = new Queue<EventWrapper<T>>[maxPriority + 1];
            for (byte i = 0; i <= maxPriority; i++)
            {
                m_EventQueues[i] = new Queue<EventWrapper<T>>();
            }
            m_MaxPriority = maxPriority;
            m_Owner = owner;
            m_Logger = logger;
            Logger.LogInfo("New StateMachine owned by {0}", owner);
        }

        /// <summary>
        /// Initiate the state machine
        /// </summary>
        /// <param name="initialState"></param>
        /// <param name="enter"></param>
        public void InitState(IState<T> initialState, bool enter)
        {
            Task t = Task.FromResult<object>(null);
            lock (m_PumpLock)
            {
                m_CurrentState = initialState;
                // call OnEntry?
                if (enter)
                {
                    m_CurrentState.OnEntry(m_Owner, null, this).GetAwaiter().GetResult();
                }
            }
        }

        /// <summary>
        /// Start handling events
        /// </summary>
        public void StartPumpEvents()
        {
            lock (m_PumpLock)
            {
                if (m_PumpOn)
                {
                    return;
                }
                if (m_CurrentState == null)
                {
                    throw new InvalidOperationException("Must call InitState before calling StartPumpEvents");
                }

                m_PumpOn = true;
                if (m_NumPendingEvents >0)
                {
                    ScheduleHandleNextEvent();
                }
            }
        }


        /// <summary>
        /// Stop handling events
        /// The contract is that if an event is currently being processed, this event will complete. No guarantee is made that this method exits after 
        /// the last event is handled
        /// </summary>
        public void StopPumpEvents()
        {
            lock (m_PumpLock)
            {
                if (!m_PumpOn)
                {
                    return;
                }
                m_PumpOn = false;
            }
        }

        /// <summary>
        /// Schedules the thread pool to handle the next event.
        /// Might want to replace the use of the Standard ThreadPool for a custom thread pool to ensure that we always have a thread available
        /// </summary>
        private void ScheduleHandleNextEvent()
        {
            Task.Factory.StartNew(() => { HandleNextEvent(); });
        }

        /// <summary>
        /// Handle an incoming event from the queue
        /// </summary>
        /// <param name="state"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private async Task HandleNextEvent()
        {
            //Logger.LogVerbose("Dequeue next event for {0}",m_Owner);
            // get the most eligible wrapped event
            EventWrapper<T> eventWrapper = Dequeue();
            Debug.Assert(eventWrapper != null);
            IState<T> currentState = m_CurrentState;

            // retrieve the corresponding taskCompletionSource
            TaskCompletionSource<IState<T>> taskCompletionSource = eventWrapper.TaskCompletionSource;

            try
            {
                // call OnEvent of current state
                IState<T> nextState = await currentState.OnEvent(m_Owner, eventWrapper.Event, this);

                // if returned not null
                if (nextState != null)
                {
                    // leave this state and enter the next
                    await currentState.OnExit(m_Owner, nextState, this);
                    m_CurrentState = nextState;
                    await nextState.OnEntry(m_Owner, currentState, this);
                }
                try
                {
                    taskCompletionSource.SetResult(m_CurrentState);
                }
                catch (InvalidOperationException)
                {
                    // This is ok, there is a race condition between timeout and actual completion
                }
            }
            catch (Exception e)
            {
                // deliver the exception to the result
                try
                {
                    taskCompletionSource.SetException(e);
                }
                catch (InvalidOperationException)
                {
                    // This is ok, there is a race condition between timeout and actual completion
                }
            }
            finally
            {
                lock (m_PumpLock)
                {
                    if (m_NumPendingEvents > 0)
                    {
                        m_NumPendingEvents--;
                        if (m_NumPendingEvents > 0 && m_PumpOn)
                        {
                            ScheduleHandleNextEvent();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get the most eligible waiting event. Most eligible==highest priority queue FIFO
        /// </summary>
        /// <returns></returns>
        private EventWrapper<T> Dequeue()
        {
            // iterate over the queues
            foreach (Queue<EventWrapper<T>> eventQueue in m_EventQueues)
            {
                // lock it
                lock (eventQueue)
                {
                    // check if anything waiting
                    if (eventQueue.Count > 0)
                    {
                        // return the first
                        return eventQueue.Dequeue();
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Enqueue an event
        /// </summary>
        /// <param name="eventWrapper">an event wrapper</param>
        /// <param name="priority">the queue to enqueue to</param>
        private void Enqueue(EventWrapper<T> eventWrapper, byte priority)
        {
            if (priority > m_MaxPriority)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture,"Max priority is {0} and {1} was requested",m_MaxPriority, priority));
            }
            if (priority < MIN_PRIORITY)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "Min priority is {0} and {1} was requested", MIN_PRIORITY, priority));
            }
            // get the queue that corresponds to this priority
            Queue<EventWrapper<T>> eventQueue = m_EventQueues[priority];
            // lock it
            lock (eventQueue)
            {
                // enqueue the event on it
                eventQueue.Enqueue(eventWrapper);
            }
            lock (m_PumpLock)
            {
                if (m_NumPendingEvents == 0 && m_PumpOn)
                {
                    m_NumPendingEvents++;
                    ScheduleHandleNextEvent();
                    return;
                }
                m_NumPendingEvents++;
            }
        }

        /// <summary>
        /// Post and event
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="anEvent"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public Task<IState<T>> PostEvent(Object anEvent, byte priority)
        {
            // create an AsyncResult for this event
            TaskCompletionSource<IState<T>> taskCompletionSource = new TaskCompletionSource<IState<T>>(anEvent);
            // wrap the event and the AsycResult together
            EventWrapper<T> eventWrapper = new EventWrapper<T>(anEvent, taskCompletionSource);
            // post the wrapper on the appropriate queue
            Enqueue(eventWrapper,priority);
            // return the AsyncResult
            return taskCompletionSource.Task;
        }

        public bool ValidateStateType(Type stateType)
        {
            return m_CurrentState.GetType().Equals(stateType);
        }

        private IBasicLogger Logger
        {
            get { return m_Logger; }
        }
        public IState<T> State
        {
            get
            {
                return m_CurrentState;
            }
        }

    }
}
