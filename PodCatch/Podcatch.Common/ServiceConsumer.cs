using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodCatch.Common
{
    public abstract class ServiceConsumer
    {
        protected ServiceConsumer(IServiceContext serviceContext)
        {
            ServiceContext = serviceContext;
            Tracer = serviceContext.GetService<ITracer>();
        }

        public IServiceContext ServiceContext { get; private set; }

        protected ITracer Tracer { get; private set; }
    }
}
