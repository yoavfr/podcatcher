using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace Podcatch.Common
{
    public interface IServiceContext
    {
        T TryGetService<T>() where T : class;

        /// <summary>
        /// Returns an instance of an object that provides the requested service.
        /// </summary>
        /// <typeparam name="T">The type of service requested. This is normally an interface.</typeparam>
        /// <returns>An instance of an object providing the requested service.</returns>
        T GetService<T>() where T : class;

        /// <summary>
        /// Create child context with possible addition to a prefix.
        /// </summary>
        /// <param name="prefixAddition">The string to be appended to prefix of all traces from this context.</param>
        /// <param name="mutationBudget">Maximal number of allowed changes in the prefix</param>
        /// <returns></returns>
        ServiceContext CreateChild(string prefixAddition = null, StringBuilder prePrefix = null);
    }

    public class ServiceContext : IServiceContext
    {
        private ITracer m_Tracer;
        private PrefixTracer m_PrefixTracer;
        private IServiceContext m_Parent;
        private List<object> m_Services = new List<object>();
        private List<object> m_Factories = new List<object>();
        private object m_Lock = new object();

        private void SetTracer(ITracer tracer)
        {
            m_Tracer = tracer;
            PublishService(tracer);
        }

        public ServiceContext(ITracer tracer)
        {
            m_PrefixTracer = new PrefixTracer(null, tracer);
            SetTracer(m_PrefixTracer);
        }

        public ServiceContext(IServiceContext parent, string prefixAddition = null, StringBuilder prePrefix = null)
        {
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }
            m_Parent = parent;
            var tracer = parent.GetService<ITracer>();
            if (prefixAddition != null)
            {
                tracer = m_PrefixTracer = new PrefixTracer(prefixAddition, tracer, prePrefix);
            }
            SetTracer(tracer);
        }

        /// <summary>
        /// Publish an instance of an object as a service. Any request for a service that this object implements will match this instance and 
        /// have it returned. The object is tested with the "is" operator for matches.
        /// Common use would be registering an object that implements at least one interface, and using GetService with the interface.
        /// </summary>
        /// <param name="service"></param>
        public void PublishService(object service)
        {
            lock (m_Lock)
            {
                m_Services.Insert(0, service);
            }
        }

        /// <summary>
        /// Publishes a type as a service. This registers the type into the Service Context in a way that allows it to be created later.
        /// This is the preferred registration method, since it does not rely on registration order (instances are created lazily, so dependencies
        /// only need to be registered when creating an instance.
        /// Common use would be registering an object that implements at least one interface, and using GetService&lt;T&gt; with the interface.
        /// </summary>
        /// <typeparam name="T">The type of the service to register. This type must have a constructor receiving only IServiceContext or a parameterless constructor.</typeparam>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "Intended")]
        public void PublishService<T>(string prefix = null)
        {
            lock (m_Lock)
            {
                Func<T> factory = CreateFactoryMethod<T>(prefix);
                m_Factories.Add(factory);
                m_Tracer.TraceVerbose("Published new service for type {0}", typeof(T).Name);
            }
        }

        private Func<T> CreateFactoryMethod<T>(string prefix)
        {
            // Look for a constructor receiving an IServiceContext. This is our first option.
            ConstructorInfo constructor = typeof(T).GetConstructor(new Type[] { typeof(IServiceContext) });

            if (constructor != null)
            {
                // If we have an appropriate constructor, create a factory method that calls the constructor
                // We pass in "this" as the IServiceContext.
                return () =>
                {
                    m_Tracer.TraceVerbose("Creating new instance of type {0} with IServiceContext parameter", typeof(T).Name);
                    var serviceContext = this;
                    if (prefix != null)
                    {
                        serviceContext = serviceContext.CreateChild(prefix);
                    }
                    return (T)constructor.Invoke(new object[] { (IServiceContext)serviceContext });
                };
            }

            // Next, try looking for a constructor with no parameters.
            constructor = typeof(T).GetConstructor(new Type[] { });
            if (constructor != null)
            {
                // If we have an appropriate constructor, create a factory method that calls the constructor.
                return () =>
                {
                    m_Tracer.TraceVerbose("Creating new instance of type {0} with no parameters", typeof(T).Name);
                    return (T)constructor.Invoke(new object[] { });
                };
            }

            m_Tracer.TraceError("Attempt to publish service for type {0} which has no appropriate constructors.", typeof(T).Name);
            // No appropriate constructor for this type. Throw.
            throw new InvalidOperationException("Couldn't find constructor that receives IServiceContext or one that receives no parameters. Cannot publish this type.");
        }

        /// <summary>
        /// Returns an instance of an object that provides the requested service.
        /// </summary>
        /// <typeparam name="T">The type of service requested. This is normally an interface.</typeparam>
        /// <returns>An instance of an object providing the requested service.</returns>
        public T TryGetService<T>() where T : class
        {
            lock (m_Lock)
            {
                // Start off by looking at already instantiated objects for the service we're trying to find.
                foreach (object service in m_Services)
                {
                    if (service is T)
                    {
                        return (T)service;
                    }
                }

                // If we didn't find an already instantiated instance, see if we know how to create one.
                foreach (Func<object> factory in m_Factories)
                {
                    // This test works counter-intuitively. A Func<A> is also a Func<B> if A derives from B. This relies on covariance.
                    if (factory is Func<T>)
                    {
                        m_Tracer.TraceVerbose("Found factory for service of type {0} in method {1}. Creating instance", typeof(T).Name, factory);

                        // Note this call is recursive - since it passes this into the constructed object, which in turn might call GetService<T> again.
                        T instance = (T)factory();

                        m_Tracer.TraceVerbose("Successfully created instance {0}, registering it into the instance list to reuse it later and returning.", instance);
                        m_Services.Add(instance);
                        return instance;
                    }
                }

                // If we couldn't find an instance or create one, try asking our parent.
                if (m_Parent != null)
                {
                    return m_Parent.TryGetService<T>();
                }

                return null;
            }
        }

        public T GetService<T>() where T : class
        {
            T service = TryGetService<T>();
            if (service == null)
            {
                m_Tracer.TraceError("We're out of options. We don't know how to obtain an instance of type {0}. Throwing.", typeof(T).Name);
                throw new InvalidOperationException(string.Format("Service not found: {0}", typeof(T).Name));
            }
            return service;
        }

        public ServiceContext CreateChild(string prefixAddition = null, StringBuilder prePrefix = null)
        {
            return new ServiceContext(this, prefixAddition, prePrefix);
        }
    }
}
