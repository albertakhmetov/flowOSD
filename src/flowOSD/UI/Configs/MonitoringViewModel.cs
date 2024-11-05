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

namespace flowOSD.UI.Configs;

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Resources;
using flowOSD.Extensions;

public class MonitoringViewModel : ConfigViewModelBase, IDisposable
{
    private CompositeDisposable? disposable = null;

    private IHardwareFeatures hardwareFeatures;

    public MonitoringViewModel(
        ITextResources textResources,
        IImageResources imageResources,
        IConfig config, 
        IHardwareService hardwareService)
        : base(
            textResources,
            imageResources,
            config, 
            "Config.Monitoring.Title",
            "Common.Diagnostic")
    {
        if(hardwareService == null)
        {
            throw new ArgumentNullException(nameof(hardwareService));
        }

        hardwareFeatures = hardwareService.ResolveNotNull<IHardwareFeatures>();
    }

    public bool IsCpuTemperatureVisible => hardwareFeatures.CpuTemperature;

    public bool IsFanSpeedVisible => hardwareFeatures.CpuFanSpeed || hardwareFeatures.GpuFanSpeed;

    public bool ShowBatteryChargeRate
    {
        get => Config.Common.ShowBatteryChargeRate;
        set => Config.Common.ShowBatteryChargeRate = value;
    }

    public bool ShowCpuTemperature
    {
        get => Config.Common.ShowCpuTemperature;
        set => Config.Common.ShowCpuTemperature = value;
    }

    public bool ShowFanSpeed
    {
        get => Config.Common.ShowFanSpeed;
        set => Config.Common.ShowFanSpeed = value;
    }

    public void Dispose()
    {
        OnDeactivated();
    }

    protected override void OnActivated()
    {
        disposable = new CompositeDisposable();

        Config.Common.PropertyChanged
            .Where(propertyName => propertyName == nameof(ShowBatteryChargeRate) || propertyName == nameof(ShowCpuTemperature))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(OnPropertyChanged)
            .DisposeWith(disposable);

        OnPropertyChanged(null);
    }

    protected override void OnDeactivated()
    {
        disposable?.Dispose();
        disposable = null;
    }
}
