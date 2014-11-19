using System;
using System.Text;
using System.Threading;

namespace Podcatch.Common
{
    internal class InvocationContext : SynchronizationContext, IDisposable
    {
        public string Prefix { get; private set; }
        public StringBuilder PrePrefix { get; private set; }
        private readonly SynchronizationContext m_OriginalSynchronizationContext;

        public InvocationContext(string prefix, StringBuilder prePrefix)
        {
            Prefix = prefix;
            PrePrefix = prePrefix;
            m_OriginalSynchronizationContext = Current;
            SetSynchronizationContext(this);
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            if (m_OriginalSynchronizationContext != null)
            {
                m_OriginalSynchronizationContext.Post(SwitchContext(d), state);
            }
            else
            {
                base.Post(d, state);
            }
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            if (m_OriginalSynchronizationContext != null)
            {
                m_OriginalSynchronizationContext.Send(SwitchContext(d), state);
            }
            else
            {
                base.Send(d, state);
            }
        }

        private SendOrPostCallback SwitchContext(SendOrPostCallback d)
        {
            return s =>
            {
                using (new InvocationContext(Prefix, PrePrefix))
                {
                    d(s);
                }
            };
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                SetSynchronizationContext(m_OriginalSynchronizationContext);
            }
        }
    }
}
