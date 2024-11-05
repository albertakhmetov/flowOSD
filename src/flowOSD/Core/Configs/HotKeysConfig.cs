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
using flowOSD.Core.Hardware;

public sealed class HotKeysConfig : ConfigBase
{
    private Dictionary<AtkKey, Command> keyToCommand;
    private Subject<AtkKey> keyChangedSubject;

    public HotKeysConfig()
    {
        keyToCommand = new Dictionary<AtkKey, Command>();
        keyChangedSubject = new Subject<AtkKey>();

        KeyChanged = keyChangedSubject.AsObservable();
    }

    public Command? this[AtkKey key]
    {
        get => keyToCommand.ContainsKey(key) ? keyToCommand[key] : null;
        set
        {
            if (value == null)
            {
                keyToCommand.Remove(key);
            }
            else
            {
                keyToCommand[key] = value;
            }

            keyChangedSubject.OnNext(key);
            OnPropertyChanged($"Item[{key}]");
        }
    }

    public IObservable<AtkKey> KeyChanged { get; }

    public void Clear()
    {
        var store = keyToCommand.Keys.ToArray();
        keyToCommand.Clear();

        foreach (var s in store)
        {
            keyChangedSubject.OnNext(s);
        }

        OnPropertyChanged(null);
    }

    public sealed class Command
    {
        public Command(string name, string? parameter = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Parameter = parameter;
        }

        public string Name { get; }

        public string? Parameter { get; }
    }
}
