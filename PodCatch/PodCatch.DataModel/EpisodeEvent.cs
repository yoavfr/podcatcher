namespace PodCatch.DataModel
{
    public enum EpisodeEvent
    {
        UpdateDownloadStatus,
        Download,
        DownloadSuccess,
        DownloadFail,
        Play,
        PlayStarted,
        Ended,
        ResumePlaying,
        Pause,
        Paused,
        Scan,
        ScanDone,
        DonePlaying,
        Refresh,
    }
}