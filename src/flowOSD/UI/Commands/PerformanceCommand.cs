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
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;
using flowOSD.Extensions;

public class PerformanceCommand : CommandBase
{
    private IConfig config;
    private IAtk atk;
    private IPowerManagement powerManagement;
    private IPerformanceService performanceService;

    public PerformanceCommand(
        ITextResources textResources,
        IImageResources imageResources,
        IConfig config,
        IAtk atk,
        IPowerManagement powerManagement,
        IPerformanceService performanceService) 
        : base(
            textResources,
            imageResources)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.atk = atk ?? throw new ArgumentNullException(nameof(atk));
        this.powerManagement = powerManagement ?? throw new ArgumentNullException(nameof(powerManagement));
        this.performanceService = performanceService ?? throw new ArgumentNullException(nameof(performanceService));

        this.performanceService.ActiveProfile
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(profile => IsChecked = profile.Id != PerformanceProfile.DefaultId)
            .DisposeWith(Disposable!);

        Description = TextResources["Commands.Performance.Description"];
        Enabled = true;
    }

    public override bool CanExecuteWithHotKey => true;

    public override async void Execute(object? parameter = null)
    {
        if (!Enabled)
        {
            return;
        }

        if (parameter is Guid profileId)
        {
            performanceService.SetActiveProfile(profileId);
            await SaveActiveProfile(profileId);
        }
        else
        {
            var nextProfileId = await GetNextProfileId();
            performanceService.SetActiveProfile(nextProfileId);
            await SaveActiveProfile(nextProfileId);
        }
    }

    private async Task SaveActiveProfile(Guid profileId)
    {
        if (await atk.TabletMode.FirstOrDefaultAsync() == TabletMode.Tablet)
        {
            config.Performance.TabletProfile = profileId;
        }
        else if (await powerManagement.PowerSource.FirstOrDefaultAsync() == PowerSource.Battery)
        {
            config.Performance.ChargerProfile = profileId;
        }
        else
        {
            config.Performance.BatteryProfile = profileId;
        }
    }

    private async Task<Guid> GetNextProfileId()
    {
        var profile = await performanceService.ActiveProfile.FirstAsync();
        if (profile.Id == PerformanceProfile.DefaultId)
        {
            return PerformanceProfile.TurboId;
        }
        else if (profile.Id == PerformanceProfile.TurboId)
        {
            return PerformanceProfile.SilentId;
        }
        else
        {
            return PerformanceProfile.DefaultId;
        }
    }
}
