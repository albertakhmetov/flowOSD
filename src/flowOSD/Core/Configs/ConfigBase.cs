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

namespace flowOSD.Core.Configs;

using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

public abstract class ConfigBase
{
    private Subject<string?> propertyChangedSubject;

    public ConfigBase()
    {
        propertyChangedSubject = new Subject<string?>();

        PropertyChanged = propertyChangedSubject.AsObservable();
    }

    [JsonIgnore]
    public IObservable<string?> PropertyChanged { get; }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        propertyChangedSubject.OnNext(propertyName);
    }

    protected void SetProperty<T>(ref T property, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!Equals(property, value))
        {
            property = value;
            OnPropertyChanged(propertyName);
        }
    }
}
