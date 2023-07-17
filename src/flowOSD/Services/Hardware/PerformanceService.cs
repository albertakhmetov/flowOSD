/*  Copyright © 2021-2023, Albert Akhmetov <akhmetov@live.com>   
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

namespace flowOSD.Services.Hardware;

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;
using flowOSD.Extensions;

sealed class PerformanceService : IDisposable, IPerformanceService
{
    private CompositeDisposable? disposable = new CompositeDisposable();

    private IConfig config;
    private INotificationService notificationService;
    private IAtk atk;
    private IPowerManagement powerManagement;

    private BehaviorSubject<PerformanceProfile> activeProfileSubject;

    public PerformanceService(
        IConfig config,
        INotificationService notificationService,
        IAtk atk, 
        IPowerManagement powerManagement)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.atk = atk ?? throw new ArgumentNullException(nameof(atk));
        this.powerManagement = powerManagement ?? throw new ArgumentNullException(nameof(powerManagement));

        activeProfileSubject = new BehaviorSubject<PerformanceProfile>(PerformanceProfile.Performance);
        activeProfileSubject
            .Skip(1)
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ApplyProfile)
            .DisposeWith(disposable);

        ActiveProfile = activeProfileSubject.AsObservable();

        this.powerManagement.PowerSource.Throttle(TimeSpan.FromSeconds(2))
            .CombineLatest(this.atk.TabletMode.Throttle(TimeSpan.FromSeconds(2)), (powerSource, tabletMode) => new { powerSource, tabletMode })
            .Throttle(TimeSpan.FromMicroseconds(2000))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(x => ChangeActiveProfile(x.powerSource, x.tabletMode))
            .DisposeWith(disposable);

        this.config.Performance.PropertyChanged
            .Where(IsActiveProfileProperty)
            .Throttle(TimeSpan.FromMicroseconds(1000))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(_ => ChangeActiveProfile())
            .DisposeWith(disposable);

        this.config.Performance.ProfileChanged
            .Throttle(TimeSpan.FromMicroseconds(1000))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(UpdateActiveProfile)
            .DisposeWith(disposable);

        this.atk.GpuMode
            .Skip(1)
            .Throttle(TimeSpan.FromMicroseconds(1000))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(_ => Update())
            .DisposeWith(disposable);
    }

    public IObservable<PerformanceProfile> ActiveProfile { get; }

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    public void Update()
    {
        ApplyProfile(activeProfileSubject.Value);
    }

    public void SetActiveProfile(Guid id)
    {
        activeProfileSubject.OnNext(GetProfile(id));
    }

    public PerformanceProfile GetProfile(Guid id)
    {
        if (id == PerformanceProfile.Performance.Id)
        {
            return PerformanceProfile.Performance;
        }
        else if (id == PerformanceProfile.Turbo.Id)
        {
            return PerformanceProfile.Turbo;
        }
        else if (id == PerformanceProfile.Silent.Id)
        {
            return PerformanceProfile.Silent;
        }
        else
        {
            return config.Performance[id] ?? PerformanceProfile.Performance;
        }
    }

    private async void ChangeActiveProfile()
    {
        var powerSource = await powerManagement.PowerSource.FirstAsync();
        var tabletMode = await atk.TabletMode.FirstAsync();

        ChangeActiveProfile(powerSource, tabletMode);
    }

    private void ChangeActiveProfile(PowerSource powerSource, TabletMode tabletMode)
    {
        Guid id;

        if (tabletMode == TabletMode.Tablet)
        {
            id = config.Performance.TabletProfile;
        }
        else if (powerSource == PowerSource.Battery)
        {
            id = config.Performance.ChargerProfile;
        }
        else
        {
            id = config.Performance.BatteryProfile;
        }

        if (activeProfileSubject.Value.Id != id)
        {
            SetActiveProfile(id);
        }
    }

    private void UpdateActiveProfile(Guid changedProfileId)
    {
        if (activeProfileSubject.Value.Id != changedProfileId)
        {
            return;
        }

        SetActiveProfile(changedProfileId);
    }

    private bool IsActiveProfileProperty(string? propertyName)
    {
        return
            propertyName == nameof(PerformanceConfig.ChargerProfile) ||
            propertyName == nameof(PerformanceConfig.BatteryProfile) ||
            propertyName == nameof(PerformanceConfig.TabletProfile);
    }

    private async void ApplyProfile(PerformanceProfile profile)
    {
        atk.SetPerformanceMode(profile.PerformanceMode);

        if (profile.IsUserProfile && !await SetCustomProfile(profile))
        {
            Common.TraceWarning("Can't set custom profile");

            activeProfileSubject.OnNext(PerformanceProfile.Performance);
        }
    }

    private async Task<bool> SetCustomProfile(PerformanceProfile profile)
    {
        var gpuMode = await atk.GpuMode.FirstAsync();
        if (gpuMode == GpuMode.dGpu)
        {
            if (!atk.SetFanCurve(FanType.Cpu, profile.CpuFanCurve))
            {
                Common.TraceWarning("Can't set CPU Fan Curve");
                return false;
            }

            if (!atk.SetFanCurve(FanType.Gpu, profile.GpuFanCurve))
            {
                Common.TraceWarning("Can't set GPU Fan Curve");
                return false;
            }
        }

        if (!atk.SetCpuLimit(profile.CpuLimit))
        {
            Common.TraceWarning("Can't set CPU Power Limit");
            return false;
        }

        return true;
    }
}
