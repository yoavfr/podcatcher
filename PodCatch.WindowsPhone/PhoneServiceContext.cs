using PodCatch.Common;
using PodCatch.DataModel;
using PodCatch.DataModel.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodCatch
{
    public class PhoneServiceContext
    {
        private static IServiceContext s_Instance;

        public static IServiceContext Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    var serviceContext = new ServiceContext(new ThreadAwareDebugTracer());
                    serviceContext.PublishService<PodcastDataSource>();
                    serviceContext.PublishService<DownloadService>();
                    serviceContext.PublishService<ITunesSearch>();
                    serviceContext.PublishService<MediaElementWrapper>();
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
