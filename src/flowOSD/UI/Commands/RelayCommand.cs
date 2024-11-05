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

namespace flowOSD.UI.Commands;

using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using System.Windows.Input;

public sealed class RelayCommand : ICommand
{
    private Action<object?> action;
    private Func<object?, bool>? canExecute;

    public RelayCommand(Action<object?> action, Func<object?, bool>? canExecute = null)
    {
        this.action = action ?? throw new ArgumentNullException(nameof(action));
        this.canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return canExecute == null ? true : canExecute(parameter);
    }

    public void Execute(object? parameter)
    {
        action(parameter);
    }

    public void Update()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
