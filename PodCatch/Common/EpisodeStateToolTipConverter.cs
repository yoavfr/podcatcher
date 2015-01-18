using Podcatch.Common.StateMachine;
using PodCatch.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace PodCatch.Common
{
    public class EpisodeStateToolTipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            IState<Episode, EpisodeEvent> state = value as IState<Episode, EpisodeEvent>;
            if (state != null)
            {
                if (state is EpisodeStatePendingDownload) return "Download";
                else if (state is EpisodeStateDownloading) return "Downloading";
                else if (state is EpisodeStateDownloaded) return "Play";
                else if (state is EpisodeStatePlaying) return "Pause";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
