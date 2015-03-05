using Podcatch.Common.StateMachine;
using PodCatch.DataModel;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace PodCatch.Common
{
    public class EpisodeStateProgressVisiblityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            IState<Episode, EpisodeEvent> state = value as IState<Episode, EpisodeEvent>;
            if (state != null)
            {
                if (state is EpisodeStateDownloading) return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}