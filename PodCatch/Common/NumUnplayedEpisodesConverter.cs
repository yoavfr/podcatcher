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
    public class NumUnplayedEpisodesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int numEpisodes = (int)value;
            if (numEpisodes > 9)
            {
                return "9+";
            }
            return numEpisodes;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
