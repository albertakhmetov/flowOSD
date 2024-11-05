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

using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;
using flowOSD.Extensions;
using static flowOSD.Extensions.Common;

sealed class TouchPadCommand : CommandBase
{
    private ITouchPad touchPad;

    public TouchPadCommand(
        ITextResources textResources,
        IImageResources imageResources,
        ITouchPad touchPad) 
        : base(
            textResources,
            imageResources)
    {
        this.touchPad = touchPad ?? throw new ArgumentNullException(nameof(touchPad));

        this.touchPad.State
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(Update)
            .DisposeWith(Disposable!);

        Description = TextResources["Commands.TouchPad.Description"];
        Enabled = true;
    }

    public override void Execute(object? parameter = null)
    {
        try
        {
            touchPad.Toggle();
        }
        catch (Exception ex)
        {
            TraceException(ex, TextResources["Errors.TouchPadToggleUI"]);
        }
    }

    private void Update(DeviceState state)
    {
        IsChecked = state == DeviceState.Enabled;
        Text = IsChecked ? TextResources["Commands.TouchPad.Disable"] : TextResources["Commands.TouchPad.Enable"];
    }
}
