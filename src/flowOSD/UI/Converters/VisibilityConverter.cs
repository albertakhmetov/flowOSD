/*  Copyright © 2021-2023, Albert Akhmetov <akhmetov@live.com>   
 *
 *  This file is part of flowOSD.
 *
 *  flowOSD is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  flowOSD is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with flowOSD. If not, see <https://www.gnu.org/licenses/>.   
 *
 */

namespace flowOSD.UI.Converters;

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

public sealed class VisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (targetType == typeof(Visibility) && value is bool b)
        {
            return parameter as string == "!"
                ? (!b ? Visibility.Visible : Visibility.Collapsed)
                : (b ? Visibility.Visible : Visibility.Collapsed);
        }

        if (targetType == typeof(Visibility) && value is object)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        return value;
    }
}
