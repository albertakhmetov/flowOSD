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
using System.Reactive.Linq;
using flowOSD.Core;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;
using static flowOSD.Extensions.Common;

sealed class AwakeCommand : CommandBase
{
    private IAwakeService awakeService;

    public AwakeCommand(
        ITextResources textResources,
        IImageResources imageResources,
        IAwakeService awakeService)
        : base(
            textResources,
            imageResources)
    {
        this.awakeService = awakeService ?? throw new ArgumentNullException(nameof(awakeService));

        this.awakeService.State
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(Update)
            .DisposeWith(Disposable!);

        Description = TextResources["Commands.Awake.Description"];
        Enabled = true;
    }

    public async override void Execute(object? parameter = null)
    {
        try
        {
            Enabled = false;
            await awakeService.Toggle();
            Enabled = true;
        }
        catch (Exception ex)
        {
            TraceException(ex, TextResources["Errors.AwakeToggleUI"]);
        }
    }

    private void Update(DeviceState state)
    {
        IsChecked = state == DeviceState.Enabled;
    }
}
