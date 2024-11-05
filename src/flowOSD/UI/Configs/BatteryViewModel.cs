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

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;
using flowOSD.Extensions;

public sealed class BatteryViewModel : ConfigViewModelBase
{
    private CompositeDisposable? disposable = null;

    private IBattery battery;
    private IHardwareFeatures hardwareFeatures;

    private int percentage, rate;
    private uint capacity;
    private string statusImage, powerState;
    private string? estimatedTime;

    public BatteryViewModel(
        ITextResources textResources,
        IImageResources imageResources, 
        IConfig config, 
        IHardwareService hardwareService)
        : base(
            textResources,
            imageResources,
            config, 
            "Config.Battery.Title",
            "Hardware.Battery")
    {
        if (hardwareService == null)
        {
            throw new ArgumentNullException(nameof(hardwareService));
        }

        battery = hardwareService.ResolveNotNull<IBattery>();

        hardwareFeatures = hardwareService.ResolveNotNull<IHardwareFeatures>();

        statusImage = string.Empty;
        powerState = string.Empty;
        estimatedTime = string.Empty;
    }

    public bool UseBatteryChargeLimit
    {
        get => Config.Common.UseBatteryChargeLimit;
        set => Config.Common.UseBatteryChargeLimit = value;
    }

    public bool IsBatteryChargeLimitVisible => hardwareFeatures.ChargeLimit;

    public uint BatteryChargeLimit
    {
        get => Config.Common.BatteryChargeLimit;
    }

    public string Name => battery.Name;

    public string ManufactureName => battery.ManufactureName;

    public uint DesignedCapacity => battery.DesignedCapacity;

    public uint FullChargedCapacity => battery.FullChargedCapacity;

    public uint WearPercentage => 100 - Convert.ToUInt32(Math.Round(100f * FullChargedCapacity / DesignedCapacity));

    public string StatusImage
    {
        get => statusImage;
        private set => SetProperty(ref statusImage, value);
    }

    public int Percentage
    {
        get => percentage;
        private set => SetProperty(ref percentage, value);
    }

    public uint Capacity
    {
        get => capacity;
        private set => SetProperty(ref capacity, value);
    }

    public string? EstimatedTime
    {
        get => estimatedTime;
        private set => SetProperty(ref estimatedTime, value);
    }

    public string PowerState
    {
        get => powerState;
        private set => SetProperty(ref powerState, value);
    }

    public int Rate
    {
        get => rate;
        private set => SetProperty(ref rate, value);
    }

    protected override void OnActivated()
    {
        disposable = new CompositeDisposable();

        battery.Rate
            .DistinctUntilChanged()
            .CombineLatest(
                battery.Capacity.DistinctUntilChanged(),
                battery.PowerState.DistinctUntilChanged(),
                (rate, capacity, powerState) => new { rate, capacity, powerState })
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(x => UpdateBattery(x.rate, x.capacity, x.powerState))
            .DisposeWith(disposable);

        battery.PowerState
            .DistinctUntilChanged()
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(UpdatePowerState)
            .DisposeWith(disposable);

        battery.EstimatedTime
            .DistinctUntilChanged()
            .CombineLatest(
                battery.PowerState.DistinctUntilChanged(),
                (estimatedTime, powerState) => new { estimatedTime, powerState })
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(x => UpdateEstimatedTime(x.estimatedTime, x.powerState))
            .DisposeWith(disposable);

        Config.Common.PropertyChanged
            .Where(name => name == nameof(CommonConfig.BatteryChargeLimit) || name == nameof(CommonConfig.UseBatteryChargeLimit))
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

    private void UpdateBattery(int rate, uint capacity, BatteryPowerState powerState)
    {
        StatusImage = ImageResources.GetBatteryIcon(capacity, battery.FullChargedCapacity, powerState);
        Percentage = Convert.ToInt32(Math.Round(100f * capacity / battery.FullChargedCapacity));
        Rate = Math.Abs(rate) > 100 ? rate : 0;
        Capacity = capacity;
    }

    private void UpdateEstimatedTime(uint estimatedTime, BatteryPowerState powerState)
    {
        EstimatedTime = Math.Abs(Rate) > 100 && (powerState & BatteryPowerState.Discharging) == BatteryPowerState.Discharging
            ? TimeSpan.FromSeconds(estimatedTime).ToString(@"hh\:mm")
            : "—";
    }

    private void UpdatePowerState(BatteryPowerState powerState)
    {
        var text = new StringBuilder();

        if ((powerState & BatteryPowerState.PowerOnLine) == BatteryPowerState.PowerOnLine)
        {
            text.Append(TextResources["Config.Battery.PluggedIn"]);
        }

        if ((powerState & BatteryPowerState.Critical) == BatteryPowerState.Critical)
        {
            if (text.Length > 0)
            {
                text.Append(", ");
            }

            text.Append(TextResources["Config.Battery.Critical"]);
        }

        if ((powerState & BatteryPowerState.Charging) == BatteryPowerState.Charging && Math.Abs(Rate) > 100)
        {
            if (text.Length > 0)
            {
                text.Append(", ");
            }

            text.Append(TextResources["Config.Battery.Charging"]);
        }

        if ((powerState & BatteryPowerState.Discharging) == BatteryPowerState.Discharging && Math.Abs(Rate) > 100)
        {
            if (text.Length > 0)
            {
                text.Append(", ");
            }

            text.Append(TextResources["Config.Battery.Discharging"]);
        }

        PowerState = text.ToString();
    }
}