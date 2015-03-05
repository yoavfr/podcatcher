using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace PodCatch.Common
{
    public class NumUnplayedEpisodesMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int numEpisodes = (int)value;
            if (numEpisodes > 9)
            {
                return new Thickness(6, 5, 0, 0);
            }
            return new Thickness(11, 5, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}