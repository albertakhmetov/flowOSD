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
namespace flowOSD;

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;
using flowOSD.Extensions;
using flowOSD.Native;
using flowOSD.Services;
using flowOSD.UI;
using flowOSD.UI.Commands;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using static flowOSD.Extensions.Common;
using static flowOSD.Native.User32;

sealed class OsdNotificationService : IDisposable
{
    private CompositeDisposable? disposable = new CompositeDisposable();

    private ITextResources textResources;
    private IImageResources imageResources;
    private IConfig config;
    private IOsd osd;

    private IAtk atk;
    private IPowerManagement powerManagement;
    private ITouchPad touchPad;
    private IDisplay display;
    private IDisplayBrightness displayBrightness;
    private IKeyboard keyboard;
    private IKeyboardBacklight keyboardBacklight;
    private IMicrophone microphone;
    private IBattery battery;
    private IPerformanceService performanceService;
    private INotebookModeService notebookModeService;
    private IAwakeService awakeService;

    private IHardwareFeatures hardwareFeatures;

    public OsdNotificationService(
        ITextResources textResources,
        IImageResources imageResources,
        IConfig config,
        IOsd osd,
        IHardwareService hardwareService,
        IAwakeService awakeService)
    {
        this.textResources = textResources ?? throw new ArgumentNullException(nameof(textResources));
        this.imageResources = imageResources ?? throw new ArgumentNullException(nameof(imageResources));
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.osd = osd ?? throw new ArgumentNullException(nameof(osd));
        this.awakeService = awakeService ?? throw new ArgumentNullException(nameof(awakeService));

        if (hardwareService == null)
        {
            throw new ArgumentNullException(nameof(hardwareService));
        }

        atk = hardwareService.ResolveNotNull<IAtk>();
        powerManagement = hardwareService.ResolveNotNull<IPowerManagement>();
        touchPad = hardwareService.ResolveNotNull<ITouchPad>();
        display = hardwareService.ResolveNotNull<IDisplay>();
        displayBrightness = hardwareService.ResolveNotNull<IDisplayBrightness>();
        keyboard = hardwareService.ResolveNotNull<IKeyboard>();
        keyboardBacklight = hardwareService.ResolveNotNull<IKeyboardBacklight>();
        microphone = hardwareService.ResolveNotNull<IMicrophone>();
        battery = hardwareService.ResolveNotNull<IBattery>();
        performanceService = hardwareService.ResolveNotNull<IPerformanceService>();
        notebookModeService = hardwareService.ResolveNotNull<INotebookModeService>();

        hardwareFeatures = hardwareService.ResolveNotNull<IHardwareFeatures>();

        Init(disposable);

        if (hardwareFeatures.OptimizationService)
        {
            InitForOptimizationService(disposable);
        }
    }

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    private void Init(CompositeDisposable disposable)
    {
        powerManagement.PowerMode
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .DistinctUntilChanged()
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ShowPowerModeNotification)
            .DisposeWith(disposable);

        if (hardwareFeatures.Charger)
        {
            atk.Charger
                .Skip(1)
                .Throttle(TimeSpan.FromSeconds(2))
                .DistinctUntilChanged()
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(ShowPowerSourceNotification)
                .DisposeWith(disposable);
        }
        else
        {
            powerManagement.PowerSource
                .Skip(1)
                .Throttle(TimeSpan.FromSeconds(2))
                .DistinctUntilChanged()
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(ShowPowerSourceNotification)
                .DisposeWith(disposable);
        }

        touchPad.State.Throttle(TimeSpan.FromSeconds(1))
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .DistinctUntilChanged()
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ShowTouchPadNotification)
            .DisposeWith(disposable);

        powerManagement.IsBoost
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .DistinctUntilChanged()
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ShowBoostNotification)
            .DisposeWith(disposable);

        display.RefreshRate
            .CombineLatest(display.State, (refreshRate, displayState) => new { refreshRate, displayState })
            .Where(x => x.displayState == DeviceState.Enabled)
            .Select(x => x.refreshRate)
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .DistinctUntilChanged()
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ShowDisplayRefreshRateNotification)
            .DisposeWith(disposable);

        atk.GpuMode
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .DistinctUntilChanged()
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ShowGpuNotification)
            .DisposeWith(disposable);

        performanceService.ActiveProfile
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .DistinctUntilChanged(x => x.Id)
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ShowPerformanceProfileNotification)
            .DisposeWith(disposable);

        notebookModeService.State
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .DistinctUntilChanged()
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ShowNotebookModeNotification)
            .DisposeWith(disposable);

        awakeService.State
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .DistinctUntilChanged()
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ShowAwakeModeNotification)
            .DisposeWith(disposable);
    }

    private void InitForOptimizationService(CompositeDisposable disposable)
    {
        keyboard.KeyPressed
            .Where(key => key == AtkKey.BacklightDown || key == AtkKey.BacklightUp)
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ShowKeyboardBacklightNotification)
            .DisposeWith(disposable);

        keyboard.KeyPressed
            .Where(key => key == AtkKey.Mic)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(_ => ShowMicNotification())
            .DisposeWith(disposable);
    }

    private void ShowMicNotification()
    {
        var isMuted = microphone.IsMicMuted();
        osd.Show(new OsdMessage(
            isMuted ? textResources["Notifications.MicOff"] : textResources["Notifications.MicOn"],
            isMuted ? imageResources["Hardware.MicMuted"] : imageResources["Hardware.Mic"]));
    }

    private async void ShowKeyboardBacklightNotification(AtkKey key)
    {
        var backlightLevel = await keyboardBacklight.Level.FirstOrDefaultAsync();

        var icon = key == AtkKey.BacklightUp
            ? imageResources["Hardware.KeyboardLightUp"]
            : imageResources["Hardware.KeyboardLightDown"];

        osd.Show(new OsdValue((float)backlightLevel / (float)KeyboardBacklightLevel.High, icon));
    }

    private void ShowPerformanceProfileNotification(PerformanceProfile performanceProfile)
    {
        if (!config.Notifications[NotificationType.PerformanceMode])
        {
            return;
        }

        osd.Show(new OsdMessage(
            performanceProfile.Name,
            imageResources.For(performanceProfile.PerformanceMode)));
    }

    private void ShowPowerModeNotification(PowerMode powerMode)
    {
        if (!config.Notifications[NotificationType.PowerMode])
        {
            return;
        }

        osd.Show(new OsdMessage(
            $"{textResources.For(powerMode)} power mode",
            imageResources.For(powerMode)));
    }

    private void ShowPowerSourceNotification(PowerSource powerSource)
    {
        if (!config.Notifications[NotificationType.PowerSource])
        {
            return;
        }

        osd.Show(new OsdMessage(
            powerSource == PowerSource.Battery ? textResources["Charger.Battery"] : textResources["Charger.Connected"],
            powerSource == PowerSource.Battery ? imageResources["Hardware.DC"] : imageResources["Hardware.AC"]));
    }

    private async void ShowPowerSourceNotification(ChargerTypes chargerTypes)
    {
        if (!config.Notifications[NotificationType.PowerSource])
        {
            return;
        }

        var capacity = await battery.Capacity.FirstAsync();
        var fullChargedCapacity = battery.FullChargedCapacity;

        string text;
        string image;

        if ((chargerTypes & ChargerTypes.LowPower) == ChargerTypes.LowPower)
        {
            text = textResources["Charger.LowPower"];
            image = imageResources.GetBatteryIcon(capacity, fullChargedCapacity, BatteryPowerState.PowerOnLine);
        }
        else if ((chargerTypes & ChargerTypes.Connected) == ChargerTypes.Connected)
        {
            text = textResources["Charger.Connected"];
            image = imageResources.GetBatteryIcon(capacity, fullChargedCapacity, BatteryPowerState.PowerOnLine);
        }
        else
        {
            text = textResources["Charger.Battery"];
            image = imageResources.GetBatteryIcon(capacity, fullChargedCapacity, BatteryPowerState.Discharging);
        }

        osd.Show(new OsdMessage(
            text,
            image,
            (chargerTypes & ChargerTypes.LowPower) == ChargerTypes.LowPower));
    }

    private void ShowDisplayRefreshRateNotification(uint refreshRate)
    {
        if (!config.Notifications[NotificationType.DisplayRefreshRate])
        {
            return;
        }

        osd.Show(new OsdMessage(
            DisplayRefreshRates.IsHigh(refreshRate) ? "High Refresh Rate" : "Low Refresh Rate",
            imageResources["Hardware.Screen"]));
    }

    private void ShowBoostNotification(bool isEnabled)
    {
        if (!config.Notifications[NotificationType.Boost])
        {
            return;
        }

        osd.Show(new OsdMessage(
            isEnabled ? "Boost Mode is on" : "Boost Mode is off",
            imageResources["Hardware.Cpu"]));
    }

    private void ShowTouchPadNotification(DeviceState state)
    {
        if (!config.Notifications[NotificationType.TouchPad])
        {
            return;
        }

        osd.Show(new OsdMessage(
            state == DeviceState.Enabled ? "TouchPad is on" : "TouchPad is off",
            imageResources["Hardware.TouchPad"]));
    }

    private void ShowGpuNotification(GpuMode gpuMode)
    {
        if (!config.Notifications[NotificationType.Gpu])
        {
            return;
        }

        osd.Show(new OsdMessage(
            gpuMode == GpuMode.dGpu ? "dGPU is on" : "dGPU is off",
            imageResources["Hardware.Gpu"]));
    }

    private void ShowNotebookModeNotification(DeviceState state)
    {
        if (!config.Notifications[NotificationType.NotebookMode])
        {
            return;
        }

        osd.Show(new OsdMessage(
            state == DeviceState.Enabled ? textResources["Notifications.NotebookModeOn"] : textResources["Notifications.NotebookModeOff"],
            imageResources["Notifications.NotebookMode"]));
    }

    private void ShowAwakeModeNotification(DeviceState state)
    {
        if (!config.Notifications[NotificationType.AwakeMode])
        {
            return;
        }

        osd.Show(new OsdMessage(
            state == DeviceState.Enabled ? textResources["Notifications.AwakeModeOn"] : textResources["Notifications.AwakeModeOff"],
            imageResources["Notifications.AwakeMode"]));
    }
}
