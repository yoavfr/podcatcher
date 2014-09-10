using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodCatch.DataModel
{
    public enum EpisodeEvent
    {
        UpdateDownloadStatus,
        Download,
        DownloadSuccess,
        DownloadFail,
        Play,
        Pause,
        Scan,
        DonePlaying,
        Refresh,
    }
}
