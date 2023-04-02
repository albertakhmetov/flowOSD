using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace flowOSD.UI;

public abstract class ViewModelBase : INotifyPropertyChanged
{
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
