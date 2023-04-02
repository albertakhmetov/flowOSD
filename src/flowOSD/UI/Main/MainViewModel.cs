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

namespace flowOSD.UI.Main;

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;
using flowOSD.Extensions;
using flowOSD.UI.Commands;

public class MainViewModel : ViewModelBase, IDisposable
{
    private CompositeDisposable? disposable = new CompositeDisposable();

    private IConfig config;
    private IPowerManagement powerManagement;
    private IBattery battery;
    private ICpu cpu;

    private string powerModeText, powerModeImage;
    private bool isBatterySaver;

    private bool showBatteryChargeRate, showCpuTemperature;
    private string batteryChargeRate, batteryChargeRateIcon, batteryChargeRateInfo, cpuTemperature;

    private IDisposable? updatesDisposable;

    public MainViewModel(IConfig config, ICommandService commandService, IHardwareService hardwareService)
    {
        if (commandService == null)
        {
            throw new ArgumentNullException(nameof(commandService));
        }

        if (hardwareService == null)
        {
            throw new ArgumentNullException(nameof(hardwareService));
        }

        this.config = config ?? throw new ArgumentNullException(nameof(config));

        powerManagement = hardwareService.ResolveNotNull<IPowerManagement>();
        battery = hardwareService.ResolveNotNull<IBattery>();
        cpu = hardwareService.ResolveNotNull<ICpu>();

        BoostCommand = commandService.ResolveNotNull<ToggleBoostCommand>();
        PerformanceModeCommand = commandService.ResolveNotNull<PerformanceModeCommand>();
        PowerModeCommand = commandService.ResolveNotNull<PowerModeCommand>();
        DisplayRefreshRateCommand = commandService.ResolveNotNull<DisplayRefreshRateCommand>();
        GpuCommand = commandService.ResolveNotNull<GpuCommand>();
        TouchPadCommand = commandService.ResolveNotNull<TouchPadCommand>();

        ConfigCommand = commandService.ResolveNotNull<ConfigCommand>();

        TogglePerformanceModeOverrideCommand = new RelayCommand(x =>
        {
            if (IsPerformaceModeOverrideEnabled)
            {
                PerformanceModeCommand.Execute(PerformanceMode.Default);
            }
            else
            {
                PerformanceModeCommand.Execute(config.Common.PerformanceModeOverride);
            }
        });
    }

    public CommandBase BoostCommand { get; }

    public CommandBase PerformanceModeCommand { get; }

    public RelayCommand TogglePerformanceModeOverrideCommand { get; }

    public bool IsPerformaceModeOverrideEnabled => config.Common.PerformanceModeOverrideEnabled;

    public string PerformanceModeOverrideText => Text.ToText(config.Common.PerformanceModeOverride);

    public string PerformanceModeOverrideImage => Images.ToImage(config.Common.PerformanceModeOverride);

    public CommandBase PowerModeCommand { get; }

    public bool IsBatterySaver
    {
        get => isBatterySaver;
        set => SetProperty(ref isBatterySaver, value);            
    }

    public string PowerModeText
    {
        get => powerModeText;
        set => SetProperty(ref powerModeText, value);
    }

    public string PowerModeImage
    {
        get => powerModeImage;
        set => SetProperty(ref powerModeImage, value);
    }

    public string BatteryChargeRate
    {
        get => batteryChargeRate;
        set => SetProperty(ref batteryChargeRate, value);
    }

    public string BatteryChargeRateIcon
    {
        get => batteryChargeRateIcon;
        set => SetProperty(ref batteryChargeRateIcon, value);
    }

    public string BatteryChargeRateInfo
    {
        get => batteryChargeRateInfo;
        set => SetProperty(ref batteryChargeRateInfo, value);
    }

    public string CpuTemperature
    {
        get => cpuTemperature;
        set => SetProperty(ref cpuTemperature, value);
    }

    public bool ShowBatteryChargeRate
    {
        get => config.Common.ShowBatteryChargeRate;
        set => config.Common.ShowBatteryChargeRate = value;
    }

    public bool ShowCpuTemperature
    {
        get => config.Common.ShowCpuTemperature;
        set => config.Common.ShowCpuTemperature = value;
    }

    public CommandBase DisplayRefreshRateCommand { get; }

    public CommandBase GpuCommand { get; }

    public CommandBase TouchPadCommand { get; }

    public CommandBase ConfigCommand { get; }

    public void Activate()
    {
        updatesDisposable = InitSubscriptions();
    }

    public void Deactivate()
    {
        updatesDisposable?.Dispose();
        updatesDisposable = null;
    }

    private IDisposable InitSubscriptions()
    {
        var localDisposable = new CompositeDisposable();

        config.Common.PropertyChanged
            .Where(propertyName => propertyName == nameof(CommonConfig.PerformanceModeOverrideEnabled))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(_ => OnPropertyChanged(nameof(IsPerformaceModeOverrideEnabled)))
            .DisposeWith(localDisposable);

        config.Common.PropertyChanged
            .Where(propertyName => propertyName == nameof(CommonConfig.PerformanceModeOverride))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(_ =>
            {
                OnPropertyChanged(nameof(PerformanceModeOverrideText));
                OnPropertyChanged(nameof(PerformanceModeOverrideImage));
            })
            .DisposeWith(localDisposable);

        powerManagement.PowerMode
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(powerMode =>
            {
                PowerModeText = Text.ToText(powerMode);
                PowerModeImage = Images.ToImage(powerMode);
            })
            .DisposeWith(localDisposable);

        powerManagement.IsBatterySaver
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(isBatterySaver => IsBatterySaver = isBatterySaver)
            .DisposeWith(localDisposable);

        config.Common.PropertyChanged
            .Where(propertyName => propertyName == nameof(ShowBatteryChargeRate) || propertyName == nameof(ShowCpuTemperature))
            .SubscribeOn(SynchronizationContext.Current!)
            .Subscribe(OnPropertyChanged)
            .DisposeWith(localDisposable);

        if(config.Common.ShowBatteryChargeRate)
        {
            battery.Rate
                .CombineLatest(
                    battery.Capacity,
                    battery.PowerState,
                    battery.EstimatedTime,
                    (rate, capacity, powerState, estimatedTime) => new { rate, capacity, powerState, estimatedTime })
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(x => UpdateBattery(x.rate, x.capacity, x.powerState, x.estimatedTime));
        }

        if (cpu.IsAvailable && config.Common.ShowCpuTemperature)
        {
            cpu.Temperature
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(value => CpuTemperature = value == 0 ? string.Empty : $"{value} °C");
        }

        return localDisposable;
    }

    public void Dispose()
    {
        Deactivate();

        disposable?.Dispose();
        disposable = null;
    }

    private void UpdateBattery(int rate, uint capacity, BatteryPowerState powerState, uint estimatedTime)
    {
        var isEmptyRate = Math.Abs(rate) < 100;

        BatteryChargeRateIcon = Images.GetBatteryIcon(capacity, battery.FullChargedCapacity, powerState);
        BatteryChargeRate = isEmptyRate ? "" : $"{rate / 1000f:N1} W";

        var time = TimeSpan.FromSeconds(estimatedTime);
        var builder = new StringBuilder();
        builder.Append($"{capacity * 100f / battery.FullChargedCapacity:N0}%");

        if (isEmptyRate)
        {
            builder.Append("");
        }
        else if ((powerState & BatteryPowerState.Discharging) == BatteryPowerState.Discharging)
        {
            builder.Append(" remaining");
        }
        else if ((powerState & BatteryPowerState.Charging) == BatteryPowerState.Charging)
        {
            builder.Append(" available");
        }

        if ((powerState & BatteryPowerState.PowerOnLine) == BatteryPowerState.PowerOnLine)
        {
            builder.Append(" (plugged in)");
        }

        if (!isEmptyRate && (powerState & BatteryPowerState.Discharging) == BatteryPowerState.Discharging && time.TotalMinutes > 1)
        {
            builder.AppendLine();

            if (time.Hours > 0)
            {
                builder.Append($"{time.Hours}h ");
            }

            builder.Append($"{time.Minutes.ToString().PadLeft(2, '0')}min");
        }

        BatteryChargeRateInfo = builder.ToString();
    }
}
