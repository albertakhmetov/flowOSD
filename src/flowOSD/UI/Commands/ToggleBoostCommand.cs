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

sealed class ToggleBoostCommand : CommandBase
{
    private IPowerManagement powerManagement;

    public ToggleBoostCommand(
        ITextResources textResources,
        IImageResources imageResources,
        IPowerManagement powerManagement) 
        : base(
            textResources,
            imageResources)
    {
        this.powerManagement = powerManagement;

        this.powerManagement.IsBoost
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(Update)
            .DisposeWith(Disposable!);

        Description = TextResources["Commands.Boost.Description"];
        Enabled = true;
    }

    public override void Execute(object? parameter = null)
    {
        try
        {
            powerManagement.ToggleBoost();
        }
        catch (Exception ex)
        {
            TraceException(ex, TextResources["Errors.BoostToggleUI"]);
        }
    }

    private void Update(bool isEnabled)
    {
        IsChecked = isEnabled;
        Text = IsChecked ? TextResources["Commands.Boost.Disable"] : TextResources["Commands.Boost.Enable"];
    }
}
