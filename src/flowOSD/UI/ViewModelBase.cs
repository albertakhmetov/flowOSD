/*  Copyright © 2021-2024, Albert Akhmetov <akhmetov@live.com>   
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

namespace flowOSD.UI;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using flowOSD.Core.Resources;
using Microsoft.UI.Xaml;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    protected ViewModelBase(
        ITextResources textResources, 
        IImageResources imageResources)
    {
        TextResources = textResources ?? throw new ArgumentNullException(nameof(textResources));
        ImageResources = imageResources ?? throw new ArgumentNullException(nameof(imageResources));
    }

    public ITextResources TextResources { get; }

    public IImageResources ImageResources { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public static Visibility BoolToVisiblity(bool value)
    {
        return value ? Visibility.Visible : Visibility.Collapsed;
    }

    public static Visibility BoolNotToVisiblity(bool value)
    {
        return !value ? Visibility.Visible : Visibility.Collapsed;
    }

    protected void SetProperty<T>(ref T property, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!Equals(property, value))
        {
            property = value;
            OnPropertyChanged(propertyName);
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
