using PodCatch.Common;
using PodCatch.DataModel;
using PodCatch.DataModel.Search;

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
                    if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
                    {
                        serviceContext.PublishService<DesignTimePodcastDataSource>();
                    }
                    else
                    {
                        serviceContext.PublishService<PodcastDataSource>();
                    }
                    serviceContext.PublishService<DownloadService>();
                    serviceContext.PublishService<ITunesSearch>();
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