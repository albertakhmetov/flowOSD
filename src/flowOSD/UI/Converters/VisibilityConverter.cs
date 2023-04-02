namespace flowOSD.UI.Converters;

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

internal class VisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (targetType == typeof(Visibility) && value is bool b)
        {
            return parameter as string == "!"
                ? (!b ? Visibility.Visible : Visibility.Collapsed)
                : (b ? Visibility.Visible : Visibility.Collapsed);
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value;
    }
}
