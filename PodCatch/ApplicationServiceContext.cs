using PodCatch.Common;
using PodCatch.DataModel;

namespace PodCatch
{
    public class ApplicationServiceContext
    {
        private static IServiceContext s_Instance;

        static public IServiceContext Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    ServiceContext serviceContext = new ServiceContext(new DebugTracer());
                    serviceContext.PublishService<PodcastDataSource>();
                    serviceContext.PublishService<DownloadService>();
                    s_Instance = serviceContext;
                }
                return s_Instance;
            }
            set
            {
                s_Instance = value;
            }
        }
    }
}
