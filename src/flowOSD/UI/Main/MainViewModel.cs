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
    private IPerformanceService performanceService;
    private IBattery battery;
    private IAtk atk;
    private IHardwareFeatures hardwareFeatures;
    private IElevatedService elevatedService;

    private string performanceProfileText, performanceProfileImage;

    private string powerModeText, powerModeImage;
    private bool isBatterySaver;

    private bool isLowPower;
    private bool hasRate;
    private string batteryImage;
    private int rate, cpuTemperature, cpuFanSpeed, gpuFanSpeed;

    private uint capacity, fullChargedCapacity, estimatedTime;

    public MainViewModel(
        ITextResources textResources,
        IImageResources imageResources,
        IConfig config,
        ICommandService commandService,
        IHardwareService hardwareService,
        IElevatedService elevatedService) 
        : base(
            textResources,
            imageResources)
    {
        this.elevatedService = elevatedService ?? throw new ArgumentNullException(nameof(elevatedService));

        if (commandService == null)
        {
            throw new ArgumentNullException(nameof(commandService));
        }

        if (hardwareService == null)
        {
            throw new ArgumentNullException(nameof(hardwareService));
        }

        this.config = config ?? throw new ArgumentNullException(nameof(config));

        powerModeText = string.Empty;
        powerModeImage = string.Empty;

        powerManagement = hardwareService.ResolveNotNull<IPowerManagement>();
        performanceService = hardwareService.ResolveNotNull<IPerformanceService>();
        battery = hardwareService.ResolveNotNull<IBattery>();
        atk = hardwareService.ResolveNotNull<IAtk>();
        hardwareFeatures = hardwareService.ResolveNotNull<IHardwareFeatures>();

        BoostCommand = commandService.ResolveNotNull<ToggleBoostCommand>();
        PerformanceCommand = commandService.ResolveNotNull<PerformanceCommand>();
        PowerModeCommand = commandService.ResolveNotNull<PowerModeCommand>();
        DisplayRefreshRateCommand = commandService.ResolveNotNull<DisplayRefreshRateCommand>();
        GpuCommand = commandService.ResolveNotNull<GpuCommand>();
        TouchPadCommand = commandService.ResolveNotNull<TouchPadCommand>();
        NotebookModeCommand = commandService.ResolveNotNull<NotebookModeCommand>();
        AwakeCommand = commandService.ResolveNotNull<AwakeCommand>();

        ConfigCommand = commandService.ResolveNotNull<ConfigCommand>();
    }

    public CommandBase BoostCommand { get; }

    public CommandBase PerformanceCommand { get; }

    public string PerformanceProfileText
    {
        get => performanceProfileText;
        private set => SetProperty(ref performanceProfileText, value);
    }

    public string PerformanceProfileImage
    {
        get => performanceProfileImage;
        private set => SetProperty(ref performanceProfileImage, value);
    }

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

    public string NotebookModeImage
    {
        get => elevatedService.IsElevated ? ImageResources["Hardware.Notebook"] : ImageResources["Hardware.NotebookShield"];
    }

    public bool ShowBatteryChargeRate
    {
        get => config.Common.ShowBatteryChargeRate;
        set => config.Common.ShowBatteryChargeRate = value;
    }

    public bool ShowCpuTemperature
    {
        get => config.Common.ShowCpuTemperature && hardwareFeatures.CpuTemperature;
    }

    public bool ShowFanSpeed => ShowCpuFanSpeed || ShowGpuFanSpeed;

    public bool ShowCpuFanSpeed
    {
        get => config.Common.ShowFanSpeed && hardwareFeatures.CpuFanSpeed;
    }

    public bool ShowGpuFanSpeed
    {
        get => config.Common.ShowFanSpeed && hardwareFeatures.GpuFanSpeed;
    }

    public bool HasRate
    {
        get => hasRate;
        set => SetProperty(ref hasRate, value);
    }

    public string BatteryImage
    {
        get => batteryImage;
        set => SetProperty(ref batteryImage, value);
    }

    public bool IsLowPower
    {
        get => isLowPower;
        set => SetProperty(ref isLowPower, value);
    }

    public int Rate
    {
        get => rate;
        set => SetProperty(ref rate, value);
    }

    public uint Capacity
    {
        get => capacity;
        set => SetProperty(ref capacity, value);
    }

    public uint FullChargedCapacity
    {
        get => fullChargedCapacity;
        set => SetProperty(ref fullChargedCapacity, value);
    }

    public uint EstimatedTime
    {
        get => estimatedTime;
        set => SetProperty(ref estimatedTime, value);
    }

    public int CpuTemperature
    {
        get => cpuTemperature;
        set => SetProperty(ref cpuTemperature, value);
    }

    public int CpuFanSpeed
    {
        get => cpuFanSpeed;
        set => SetProperty(ref cpuFanSpeed, value);
    }

    public int GpuFanSpeed
    {
        get => gpuFanSpeed;
        set => SetProperty(ref gpuFanSpeed, value);
    }

    public bool ControlDisplayRefreshRate => config.Common.ControlDisplayRefreshRate;

    public CommandBase DisplayRefreshRateCommand { get; }

    public CommandBase GpuCommand { get; }

    public CommandBase TouchPadCommand { get; }

    public CommandBase NotebookModeCommand { get; }

    public CommandBase AwakeCommand { get; }

    public CommandBase ConfigCommand { get; }

    public void Activate()
    {
        disposable?.Dispose();
        disposable = new CompositeDisposable();

        performanceService.ActiveProfile
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(profile =>
            {
                PerformanceProfileText = profile.Name;

                if (profile.Id == PerformanceProfile.DefaultId)
                {
                    PerformanceProfileImage = ImageResources["PerformanceMode.Performance"];
                }
                else if (profile.Id == PerformanceProfile.TurboId)
                {
                    PerformanceProfileImage = ImageResources["PerformanceMode.Turbo"];
                }
                else if (profile.Id == PerformanceProfile.SilentId)
                {
                    PerformanceProfileImage = ImageResources["PerformanceMode.Silent"];
                }
                else
                {
                    PerformanceProfileImage = ImageResources["PerformanceMode.User"];
                }
            })
            .DisposeWith(disposable);

        powerManagement.PowerMode
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(powerMode =>
            {
                PowerModeText = TextResources.For(powerMode);
                PowerModeImage = ImageResources.For(powerMode);
            })
            .DisposeWith(disposable);

        powerManagement.IsBatterySaver
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(isBatterySaver => IsBatterySaver = isBatterySaver)
            .DisposeWith(disposable);

        config.Common.PropertyChanged
            .Where(propertyName => propertyName == nameof(ShowBatteryChargeRate) || propertyName == nameof(ShowCpuTemperature) || propertyName == nameof(ControlDisplayRefreshRate))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(OnPropertyChanged)
            .DisposeWith(disposable);

        config.Common.PropertyChanged
            .Where(propertyName => propertyName == nameof(config.Common.ShowFanSpeed))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(_ =>
            {
                OnPropertyChanged(nameof(ShowFanSpeed));
                OnPropertyChanged(nameof(ShowCpuFanSpeed));
                OnPropertyChanged(nameof(ShowGpuFanSpeed));
            })
            .DisposeWith(disposable);

        if (config.Common.ShowBatteryChargeRate)
        {
            battery.Rate.DistinctUntilChanged()
                .CombineLatest(
                    battery.Capacity.DistinctUntilChanged(),
                    battery.PowerState.DistinctUntilChanged(),
                    battery.EstimatedTime.DistinctUntilChanged(),
                    (rate, capacity, powerState, estimatedTime) => new { rate, capacity, powerState, estimatedTime })
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(x => UpdateBattery(x.rate, x.capacity, x.powerState, x.estimatedTime))
                .DisposeWith(disposable);

            atk.Charger
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(x => IsLowPower = (x & ChargerTypes.LowPower) == ChargerTypes.LowPower)
                .DisposeWith(disposable);
        }

        if (ShowCpuTemperature)
        {
            atk.CpuTemperature
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(value => CpuTemperature = value)
                .DisposeWith(disposable);
        }

        if (ShowCpuFanSpeed)
        {
            atk.CpuFanSpeed
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(value => CpuFanSpeed = value)
                .DisposeWith(disposable);
        }

        if (ShowGpuFanSpeed)
        {
            atk.GpuFanSpeed
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(value => GpuFanSpeed = value)
                .DisposeWith(disposable);
        }

        OnPropertyChanged(null);
    }

    public void Deactivate()
    {
        disposable?.Dispose();
        disposable = null;
    }

    public void Dispose()
    {
        Deactivate();
    }

    private void UpdateBattery(int rate, uint capacity, BatteryPowerState powerState, uint estimatedTime)
    {
        HasRate = Math.Abs(rate) > 500;
        Rate = HasRate ? Convert.ToInt32(Math.Round(rate / 1000f)) : 0;

        Capacity = capacity;
        FullChargedCapacity = battery.FullChargedCapacity;
        EstimatedTime = estimatedTime;

        BatteryImage = ImageResources.GetBatteryIcon(capacity, battery.FullChargedCapacity, powerState);
    }
}
