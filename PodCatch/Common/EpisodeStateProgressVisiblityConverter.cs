using PodCatch.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace PodCatch.Common
{
    public class EpisodeStateProgressVisiblityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null && value is EpisodeState)
            {
                switch ((EpisodeState)value)
                {
                    case EpisodeState.PendingDownload:
                    case EpisodeState.Downloaded:
                    case EpisodeState.Playing:
                    case EpisodeState.Scanning:
                        return Visibility.Collapsed;
                    case EpisodeState.Downloading:
                        return Visibility.Visible;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
