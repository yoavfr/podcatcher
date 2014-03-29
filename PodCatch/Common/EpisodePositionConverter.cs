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
    public class EpisodePositionConverter : IValueConverter
    {
        private const string c_TimeFormat = @"hh\:mm\:ss";
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null)
            {
                if (value is TimeSpan)
                {
                    return ((TimeSpan)value).ToString(c_TimeFormat);
                }
                else if (value is double)
                {
                    return (TimeSpan.FromTicks((long)(double)value).ToString(c_TimeFormat));
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
