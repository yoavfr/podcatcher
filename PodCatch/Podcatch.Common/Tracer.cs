using System.Diagnostics;
using System.Text;
using System.Threading;

namespace PodCatch.Common
{
    public enum TracingLevel
    {
        Error,
        Warning,
        Information,
        Verbose
    }

    // This is an abstract class and not an interface to allow setting methods as Conditional for performance reasons.
    public abstract class ITracer
    {
        public abstract TracingLevel TracingLevel { get; }

        public abstract void TraceError(string format, params object[] args);

        public abstract void TraceWarning(string format, params object[] args);

        public abstract void TraceInformation(string format, params object[] args);

        [Conditional("DEBUG")]
        public abstract void TraceVerbose(string format, params object[] args);
    }

    public interface ITracingConfiguration
    {
        TracingLevel TracingLevel { get; }
    }

    internal class PrefixTracer : ITracer
    {
        private ITracer m_Tracer;

        public StringBuilder PrePrefix { get; private set; }

        /// <summary>
        /// The prefix of the traces for this tracer.
        /// </summary>
        public string Prefix { get; private set; }

        public PrefixTracer(string prefixAddition, ITracer tracer, StringBuilder prePrefix = null)
        {
            m_Tracer = tracer;
            Prefix = prefixAddition;
            PrePrefix = prePrefix;
            if (string.IsNullOrEmpty(prefixAddition))
            {
                return;
            }
            var parent = tracer as PrefixTracer;
            if (parent == null)
            {
                // No parent prefix tracer. The prefix and the underlying tracer is set.
                return;
            }
            m_Tracer = parent.m_Tracer;
            if (PrePrefix == null)
            {
                PrePrefix = parent.PrePrefix;
            }
            Prefix = string.Concat(parent.Prefix, prefixAddition);
        }

        private static bool IsOverlap(string str1, string str2)
        {
            if (str1.Length > 0 && str2.Length > 0)
            {
                return (str1[0] == str2[0]);
            }
            return false;
        }

        private string CreateFormat(string format)
        {
            var prefixFixed = Prefix;
            string prefixDynamic = null;
            string prefix = null;
            StringBuilder prePrefix = null;
            InvocationContext invocationContext = SynchronizationContext.Current as InvocationContext;
            if (invocationContext != null && !string.IsNullOrEmpty(invocationContext.Prefix))
            {
                prefixDynamic = invocationContext.Prefix;
                prePrefix = invocationContext.PrePrefix;
            }
            if (!string.IsNullOrEmpty(prefixFixed) && prefixDynamic != null)
            {
                // Prefer dynamic context as it is more specific. But if there is no commonality in two
                // prefixes, may concatenate.
                if (IsOverlap(prefixFixed, prefixDynamic))
                {
                    prefix = prefixDynamic;
                }
                else
                {
                    prefix = string.Concat(prefixDynamic, prefixFixed);
                }
            }
            if (string.IsNullOrEmpty(prefixFixed))
            {
                prefix = prefixDynamic;
            }
            if (prefixDynamic == null)
            {
                prefix = prefixFixed;
            }
            if (prefix == null)
            {
                return format;
            }
            if (PrePrefix != null)
            {
                prePrefix = PrePrefix;
            }
            return string.Format("{0}{1} {2}", prePrefix == null ? string.Empty : prePrefix.ToString(), prefix, format);
        }

        public override TracingLevel TracingLevel
        {
            get
            {
                return m_Tracer.TracingLevel;
            }
        }

        public override void TraceError(string format, params object[] args)
        {
            m_Tracer.TraceError(CreateFormat(format), args);
        }

        public override void TraceWarning(string format, params object[] args)
        {
            if (TracingLevel >= TracingLevel.Warning)
            {
                m_Tracer.TraceWarning(CreateFormat(format), args);
            }
        }

        public override void TraceInformation(string format, params object[] args)
        {
            if (TracingLevel >= TracingLevel.Information)
            {
                m_Tracer.TraceInformation(CreateFormat(format), args);
            }
        }

        public override void TraceVerbose(string format, params object[] args)
        {
            if (TracingLevel >= TracingLevel.Verbose)
            {
                m_Tracer.TraceVerbose(CreateFormat(format), args);
            }
        }
    }
}