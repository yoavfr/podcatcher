using System;
using Windows.UI.Xaml.Data;

namespace PodCatch.Common
{
    public class TimespanToTicksConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (!(value is TimeSpan))
            {
                throw new InvalidOperationException("The value must be a TimeSpan");
            }
            if (targetType == typeof(double))
            {
                return (double)((TimeSpan)value).Ticks;
            }
            else if (targetType == typeof(long))
            {
                return ((TimeSpan)value).Ticks;
            }
            throw new InvalidOperationException("Target type must be either long or double");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return TimeSpan.FromTicks((long)((double)value));
        }
    }
}