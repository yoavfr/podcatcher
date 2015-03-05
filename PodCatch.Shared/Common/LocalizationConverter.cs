using PodCatch.Resources;
using System;
using Windows.UI.Xaml.Data;

namespace PodCatch.Common
{
    public class LocalizationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (targetType != typeof(string))
                throw new InvalidOperationException("The target must be a string");

            string identifier = value as string;
            if (identifier == null)
                throw new InvalidOperationException("The value must be a string");

            return LocalizedStrings.Get(identifier);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}