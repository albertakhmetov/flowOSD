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
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;
using flowOSD.Extensions;
using Microsoft.UI.Xaml.Controls;
using static flowOSD.Extensions.Common;

sealed class GpuCommand : CommandBase
{
    private IAtk atk;
    private IConfig config;
    private INotificationService notificationService;

    public GpuCommand(
        ITextResources textResources,
        IImageResources imageResources,
        IConfig config,
        IAtk atk,
        INotificationService notificationService) 
        : base(
            textResources,
            imageResources)
    {
        this.atk = atk ?? throw new ArgumentNullException(nameof(atk));
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

        this.atk.GpuMode
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(Update)
            .DisposeWith(Disposable!);

        Description = TextResources["Commands.Gpu.Description"];
        Enabled = true;
    }

    public async override void Execute(object? parameter = null)
    {
        var isGpuEnabled = await atk.GpuMode.FirstAsync() == GpuMode.dGpu;
        if (Confirm(isGpuEnabled) == false)
        {
            return;
        }

        try
        {
            atk.SetGpuMode(isGpuEnabled ? GpuMode.iGpu : GpuMode.dGpu);
        }
        catch (Exception ex)
        {
            TraceException(ex, TextResources["Errors.GpuToggleUI"]);
        }
    }

    private bool Confirm(bool isGpuEnabled)
    {
        // TODO: ASK if GPU is used by any app

        if (!config.Common.ConfirmGpuModeChange)
        {
            return true;
        }
        else
        {
            return notificationService.ShowConfirmation(
                isGpuEnabled ? TextResources["Commands.Gpu.TurnOffConfirmation"] : TextResources["Commands.Gpu.TurnOnConfirmation"]);
        }
    }

    private void Update(GpuMode gpuMode)
    {
        IsChecked = gpuMode == GpuMode.dGpu;
        Text = IsChecked ? TextResources["Commands.Gpu.Disable"] : TextResources["Commands.Gpu.Enable"];
    }
}
