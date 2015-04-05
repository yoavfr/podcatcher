using System;
using Windows.UI.Xaml.Data;

namespace PodCatch.Common
{
    public class TimeSpanConverter : IValueConverter
    {
        private const string c_TimeFormat = @"hh\:mm\:ss";
        private const string c_NegativeTimeFormat = @"\-hh\:mm\:ss";

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null)
            {
                TimeSpan timeSpan = TimeSpan.FromMilliseconds(0);
                if (value is TimeSpan)
                {
                    timeSpan = (TimeSpan)value;
                }
                else if (value is double)
                {
                    timeSpan = TimeSpan.FromTicks((long)(double)value);
                }
                if (timeSpan < TimeSpan.FromMilliseconds(0))
                {
                    return timeSpan.ToString(c_NegativeTimeFormat);
                }
                return timeSpan.ToString(c_TimeFormat);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}