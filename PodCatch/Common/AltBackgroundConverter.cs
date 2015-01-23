using System;
using Windows.UI;
using Windows.UI.Xaml.Data;

namespace PodCatch.Common
{
    public class AltBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (!(value is int)) return null;
            int index = (int)value;

            if (index++ % 2 == 1)
                return Colors.Black;
            else
                return Colors.DarkGray;
        }

        // No need to implement converting back on a one-way binding
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}