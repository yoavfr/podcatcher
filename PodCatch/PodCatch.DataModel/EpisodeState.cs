﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodCatch.DataModel
{
    public enum EpisodeState
    {
        PendingDownload,
        Downloading,
        Downloaded,
        Playing,
        Scanning,
        Played
    }
}
