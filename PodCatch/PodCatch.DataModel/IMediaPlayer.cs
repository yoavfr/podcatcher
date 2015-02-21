using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodCatch.DataModel
{
    public interface IMediaPlayer
    {
        Episode NowPlaying { get; }
        TimeSpan Duration { get; }
        TimeSpan Position { get; set; }
        Task Play(Episode episode);
        void Pause(Episode episode);
        bool IsEpisodePlaying(Episode episode);
        void SkipForward(Episode episode);
        void SkipBackward(Episode episode);
    }
}
