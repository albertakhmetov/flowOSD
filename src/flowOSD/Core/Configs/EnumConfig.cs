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

using System.Reactive.Linq;
using System.Reactive.Subjects;

public sealed class EnumConfig<T> : ConfigBase where T : struct, Enum
{
    private HashSet<T> values;
    private Subject<T> valueChangedSubject;

    public EnumConfig()
    {
        values = new HashSet<T>();
        valueChangedSubject = new Subject<T>();

        ValueChanged = valueChangedSubject.AsObservable();
    }

    public bool this[T enumType]
    {
        get => !values.Contains(enumType);
        set
        {
            if (value)
            {
                values.Remove(enumType);
            }
            else
            {
                values.Add(enumType);
            }

            valueChangedSubject.OnNext(enumType);
            OnPropertyChanged();
        }
    }

    public IObservable<T> ValueChanged { get; }

    public void Reset()
    {
        var store = values.ToArray();
        values.Clear();

        foreach (var s in store)
        {
            valueChangedSubject.OnNext(s);
        }

        OnPropertyChanged(null);
    }
}
