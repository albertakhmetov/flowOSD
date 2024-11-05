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

sealed class PowerModeCommand : CommandBase
{
    private IPowerManagement powerManagement;

    public PowerModeCommand(
        ITextResources textResources,
        IImageResources imageResources,
        IPowerManagement powerManagement) 
        : base(
            textResources,
            imageResources)
    {
        this.powerManagement = powerManagement ?? throw new ArgumentNullException(nameof(powerManagement));

        this.powerManagement.IsBatterySaver
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(isBatterySaver => Enabled = !isBatterySaver)
            .DisposeWith(Disposable!);

        Description = TextResources["Commands.PowerMode.Description"];
        Enabled = true;
    }

    public override bool CanExecuteWithHotKey => true;

    public override async void Execute(object? parameter = null)
    {
        if (!Enabled)
        {
            return;
        }

        if (parameter is PowerMode powerMode)
        {
            powerManagement.SetPowerMode(powerMode);
        }
        else
        {
            var nextPowerMode = await GetNextPowerMode();
            if (nextPowerMode != null)
            {
                powerManagement.SetPowerMode(nextPowerMode.Value);
            }
        }
    }

    private async Task<PowerMode?> GetNextPowerMode()
    {
        var powerMode = await powerManagement.PowerMode.FirstAsync();
        switch (powerMode)
        {
            case PowerMode.BestPowerEfficiency:
                return PowerMode.Balanced;

            case PowerMode.Balanced:
                return PowerMode.BestPerformance;

            case PowerMode.BestPerformance:
                return PowerMode.BestPowerEfficiency;

            default:
                return null;
        }
    }
}
