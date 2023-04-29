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
    private IPerformanceService performanceService;

    private string performanceProfileText, performanceProfileImage;

    private string powerModeText, powerModeImage;
    private bool isBatterySaver;

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

        powerModeText = string.Empty;
        powerModeImage = string.Empty;

        powerManagement = hardwareService.ResolveNotNull<IPowerManagement>();
        performanceService = hardwareService.ResolveNotNull<IPerformanceService>();

        BoostCommand = commandService.ResolveNotNull<ToggleBoostCommand>();
        PerformanceCommand = commandService.ResolveNotNull<PerformanceCommand>();
        PowerModeCommand = commandService.ResolveNotNull<PowerModeCommand>();
        DisplayRefreshRateCommand = commandService.ResolveNotNull<DisplayRefreshRateCommand>();
        GpuCommand = commandService.ResolveNotNull<GpuCommand>();
        TouchPadCommand = commandService.ResolveNotNull<TouchPadCommand>();

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
        updatesDisposable?.Dispose();
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

        performanceService.ActiveProfile
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(profile =>
            {
                PerformanceProfileText = profile.Name;

                if (profile.Id == PerformanceProfile.Default.Id)
                {
                    PerformanceProfileImage = Images.Performance_Default;
                }
                else if (profile.Id == PerformanceProfile.Turbo.Id)
                {
                    PerformanceProfileImage = Images.Performance_Turbo;
                }
                else if (profile.Id == PerformanceProfile.Silent.Id)
                {
                    PerformanceProfileImage = Images.Performance_Silent;
                }
                else
                {
                    PerformanceProfileImage = Images.Performance_User;
                }
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

        return localDisposable;
    }

    public void Dispose()
    {
        Deactivate();

        disposable?.Dispose();
        disposable = null;
    }
}
