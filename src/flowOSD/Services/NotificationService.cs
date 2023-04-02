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
namespace flowOSD;

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;
using flowOSD.Extensions;
using flowOSD.Services;
using flowOSD.UI;
using flowOSD.UI.Commands;
using static flowOSD.Extensions.Common;
using static flowOSD.Native.User32;

sealed class NotificationService : IDisposable
{
    private CompositeDisposable? disposable = new CompositeDisposable();

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

    public NotificationService(IConfig config, IOsd osd, IHardwareService hardwareService)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.osd = osd ?? throw new ArgumentNullException(nameof(osd));

        if (hardwareService == null)
        {
            throw new ArgumentNullException("hardwareService");
        }

        atk = hardwareService.ResolveNotNull<IAtk>();
        powerManagement = hardwareService.ResolveNotNull<IPowerManagement>();
        touchPad = hardwareService.ResolveNotNull<ITouchPad>();
        display = hardwareService.ResolveNotNull<IDisplay>();
        displayBrightness = hardwareService.ResolveNotNull<IDisplayBrightness>();
        keyboard = hardwareService.ResolveNotNull<IKeyboard>();
        keyboardBacklight = hardwareService.ResolveNotNull<IKeyboardBacklight>();
        microphone = hardwareService.ResolveNotNull<IMicrophone>();

        Init(disposable);
    }

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    private void Init(CompositeDisposable disposable)
    {
        atk.PerformanceMode
            .Skip(1)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ShowPerformanceModeNotification)
            .DisposeWith(disposable);

        powerManagement.PowerMode
            .Skip(1)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ShowPowerModeNotification)
            .DisposeWith(disposable);

        powerManagement.PowerSource
            .Skip(1)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromSeconds(2))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ShowPowerSourceNotification)
            .DisposeWith(disposable);

        touchPad.State
            .Skip(1)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ShowTouchPadNotification)
            .DisposeWith(disposable);

        powerManagement.IsBoost
            .Skip(1)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ShowBoostNotification)
            .DisposeWith(disposable);

        display.RefreshRate
            .CombineLatest(display.State, (refreshRate, displayState) => new { refreshRate, displayState })
            .Where(x => x.displayState == DeviceState.Enabled)
            .Select(x => x.refreshRate)
            .Skip(1)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ShowDisplayRefreshRateNotification)
            .DisposeWith(disposable);

        atk.GpuMode
            .Skip(1)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ShowGpuNotification)
            .DisposeWith(disposable);
    }

    private void ShowPerformanceModeNotification(PerformanceMode performanceMode)
    {
        if (!config.Notifications[NotificationType.PerformanceMode])
        {
            return;
        }

        switch (performanceMode)
        {
            case PerformanceMode.Default:
                osd.Show(new OsdMessage($"{Text.ToText(performanceMode)} performance mode", Images.Performance_Default));
                break;

            case PerformanceMode.Turbo:
                osd.Show(new OsdMessage($"{Text.ToText(performanceMode)} performance mode", Images.Performance_Turbo));
                break;

            case PerformanceMode.Silent:
                osd.Show(new OsdMessage($"{Text.ToText(performanceMode)} performance mode", Images.Performance_Silent));
                break;
        }
    }

    private void ShowPowerModeNotification(PowerMode powerMode)
    {
        if (!config.Notifications[NotificationType.PowerMode])
        {
            return;
        }

        switch (powerMode)
        {
            case PowerMode.BestPowerEfficiency:
                osd.Show(new OsdMessage($"{Text.ToText(powerMode)} power mode", Images.Power_BestPowerEfficiency));
                break;

            case PowerMode.Balanced:
                osd.Show(new OsdMessage($"{Text.ToText(powerMode)} power mode", Images.Power_Balanced));
                break;

            case PowerMode.BestPerformance:
                osd.Show(new OsdMessage($"{Text.ToText(powerMode)} power mode", Images.Power_BestPerformance));
                break;
        }
    }

    private void ShowPowerSourceNotification(PowerSource powerSource)
    {
        if (!config.Notifications[NotificationType.PowerSource])
        {
            return;
        }

        osd.Show(new OsdMessage(
            powerSource == PowerSource.Battery ? "On Battery" : "Plugged In",
            powerSource == PowerSource.Battery ? Images.Hardware_DC : Images.Hardware_AC));
    }

    private void ShowDisplayRefreshRateNotification(uint refreshRate)
    {
        if (!config.Notifications[NotificationType.DisplayRefreshRate])
        {
            return;
        }

        osd.Show(new OsdMessage(DisplayRefreshRates.IsHigh(refreshRate) ? "High Refresh Rate" : "Low Refresh Rate", Images.Hardware_Screen));
    }

    private void ShowBoostNotification(bool isEnabled)
    {
        if (!config.Notifications[NotificationType.Boost])
        {
            return;
        }

        osd.Show(new OsdMessage(isEnabled ? "Boost Mode is on" : "Boost Mode is off", Images.Hardware_Cpu));
    }

    private void ShowTouchPadNotification(DeviceState state)
    {
        if (!config.Notifications[NotificationType.TouchPad])
        {
            return;
        }

        osd.Show(new OsdMessage(state == DeviceState.Enabled ? "TouchPad is on" : "TouchPad is off", Images.Hardware_TouchPad));
    }

    private void ShowGpuNotification(GpuMode gpuMode)
    {
        if (!config.Notifications[NotificationType.Gpu])
        {
            return;
        }

        osd.Show(new OsdMessage(gpuMode == GpuMode.dGpu ? "dGPU is on" : "dGPU is off", Images.Hardware_Gpu));
    }
}
