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

namespace flowOSD.Services;

using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;
using flowOSD.Extensions;
using flowOSD.Services.Hardware;
using flowOSD.Native;
using flowOSD.Services.Hardware.Hid;
using Microsoft.Win32;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;

sealed class HardwareService : IDisposable, IHardwareService, IHardwareFeatures
{
    private CompositeDisposable? disposable = new CompositeDisposable();

    private IConfig config;
    private IMessageQueue messageQueue;
    private IKeysSender keysSender;

    private HidDevice? hidDevice;

    private Atk atk;
    private IKeyboard keyboard;
    private IKeyboardBacklight keyboardBacklight;
    private ITouchPad touchPad;
    private Display display;
    private DisplayBrightness displayBrightness;
    private Battery battery;
    private PowerManagement powerManagement;
    private Microphone microphone;
    private PerformanceService performanceService;

    private AmdGpu amd;

    private Dictionary<Type, object> devices = new Dictionary<Type, object>();

    private KeyboardBacklightService? keyboardBacklightService;
    private RefreshRateService refreshRateService;
    private BatteryChargeService? batteryChargeService;
    private NotebookModeService notebookModeService;

    public HardwareService(
        IConfig config,
        INotificationService notificationService,
        IMessageQueue messageQueue,
        IKeysSender keysSender,
        IServiceWatcher serviceWatcher)
    {
        if (serviceWatcher == null)
        {
            throw new ArgumentNullException(nameof(serviceWatcher));
        }

        try
        {
            OptimizationService = serviceWatcher.IsStarted("ASUSOptimization");
        }
        catch
        {
            OptimizationService = false;
        }

        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.messageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
        this.keysSender = keysSender ?? throw new ArgumentNullException(nameof(keysSender));

        atk = new Atk(PerformanceMode.Performance);

        if (OptimizationService)
        {
            hidDevice = null;

            keyboard = (atk as IKeyboard)!;
            keyboardBacklight = new Hardware.Optimization.KeyboardBacklight();
            touchPad = new Hardware.Optimization.TouchPad(this.messageQueue, this.keysSender);
        }
        else
        {
            hidDevice = HidDevice.Devices
                .Where(i => i.VendorId == 0xB05 && i.ReadFeatureData(out byte[] data, Keyboard.FEATURE_KBD_REPORT_ID))
                .FirstOrDefault() ?? throw new AppException(Text.Instance.Errors.CanNotConnectToHid);

            InitHid();

            keyboard = new Hardware.Hid.Keyboard(hidDevice);
            keyboardBacklight = new Hardware.Hid.KeyboardBacklight(hidDevice, config.Common.KeyboardBacklightLevel);
            keyboardBacklight.Level
                .Subscribe(x => config.Common.KeyboardBacklightLevel = x)
                .DisposeWith(disposable);
            touchPad = new Hardware.Hid.TouchPad(hidDevice);
        }

        display = new Display(this.messageQueue);
        displayBrightness = new DisplayBrightness();

        battery = new Battery();
        powerManagement = new PowerManagement();

        microphone = new Microphone();

        performanceService = new PerformanceService(
            config,
            notificationService,
            atk,
            powerManagement);

        amd = new AmdGpu();

        notebookModeService = new NotebookModeService(serviceWatcher);

        Register<IAtk>(atk);
        Register<IKeyboard>(keyboard);
        Register<IKeyboardBacklight>(keyboardBacklight);

        if (keyboardBacklight is IKeyboardBacklightControl keyboardBacklightControl)
        {
            Register<IKeyboardBacklightControl>(keyboardBacklightControl);
        }

        Register<ITouchPad>(touchPad);
        Register<IDisplay>(display);
        Register<IDisplayBrightness>(displayBrightness);
        Register<IBattery>(battery);
        Register<IPowerManagement>(powerManagement);
        Register<IMicrophone>(microphone);
        Register<IPerformanceService>(performanceService);
        Register<IHardwareFeatures>(this);
        Register<INotebookModeService>(notebookModeService);

        powerManagement.PowerEvent
           .Where(x => x == PowerEvent.Suspend)
           .Throttle(TimeSpan.FromMicroseconds(50))
           .ObserveOn(SynchronizationContext.Current!)
           .Subscribe(_ => OnSuspend())
           .DisposeWith(disposable);

        powerManagement.PowerEvent
           .Where(x => x == PowerEvent.Resume)
           .Throttle(TimeSpan.FromSeconds(5))
           .ObserveOn(SynchronizationContext.Current!)
           .Subscribe(_ => OnResume())
           .DisposeWith(disposable);

        atk.TabletMode
            .Throttle(TimeSpan.FromMicroseconds(2000))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(UpdateTouchPad)
            .DisposeWith(disposable);

        keyboardBacklightService = OptimizationService ? null : new KeyboardBacklightService(config, this).DisposeWith(disposable);

        refreshRateService = new RefreshRateService(
            this.config,
            atk,
            display,
            powerManagement).DisposeWith(disposable);

        if (!OptimizationService && ChargeLimit)
        {
            batteryChargeService = new BatteryChargeService(
                this.config,
                atk).DisposeWith(disposable);
        }

        config.Common.PropertyChanged
            .Where(name => name == nameof(CommonConfig.DisableVariBright))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(_ => UpdateVariBrightState())
            .DisposeWith(disposable);

        UpdateVariBrightState();
    }

    public bool OptimizationService { get; }

    public bool CpuTemperature => atk.CpuTemperatureSupported;

    public bool CpuFanSpeed => atk.CpuFanSpeedSupported;

    public bool GpuFanSpeed => atk.GpuFanSpeedSupported;

    public bool PerformanceSwitch => atk.PerformanceSwitchSupported;

    public bool GpuSwitch => atk.GpuSwitchSupported;

    public bool Charger => atk.ChargerSupported;

    public bool ChargeLimit => !OptimizationService && atk.ChargeLimitSupported;

    public bool CpuPowerLimit => atk.CpuPowerLimitSupported;

    public bool AmdIntegratedGpu => amd.IsSupported;

    public void Dispose()
    {
        if (disposable != null)
        {
            disposable.Dispose();
            disposable = null;

            foreach (var i in devices.Values)
            {
                (i as IDisposable)?.Dispose();
            }
        }
    }

    public T? Resolve<T>() where T : class
    {
        var isOk = devices.TryGetValue(typeof(T), out object? value);

        return isOk && value is T device ? device : null;
    }

    public T ResolveNotNull<T>() where T : class
    {
        return Resolve<T>() ?? throw new InvalidOperationException(string.Format(Text.Instance.Errors.CanNotResolve, typeof(T).Name));
    }

    private void Register<T>(T instance) where T : class
    {
        if (instance == null)
        {
            devices.Remove(typeof(T));
        }
        else
        {
            devices[typeof(T)] = instance;
        }
    }

    private void OnSuspend()
    {
        if (!OptimizationService)
        {
            keyboardBacklightService?.Disable();
        }
    }

    private async void OnResume()
    {
        battery.Reconnect();

        if (!OptimizationService)
        {
            InitHid();
            keyboardBacklightService?.ResetTimer();
            keyboardBacklightService?.Enable();
        }

        refreshRateService.Update();
        performanceService.Update();
        batteryChargeService?.Update();

        UpdateTouchPad(await atk.TabletMode.FirstOrDefaultAsync());
        UpdateVariBrightState();
    }

    private void InitHid()
    {
        if (hidDevice == null)
        {
            return;
        }

#if !DEBUG
        //  hidDevice.WriteFeatureData(0x5a, 0x89);
        hidDevice.WriteFeatureData(0x5a, 0x41, 0x53, 0x55, 0x53, 0x20, 0x54, 0x65, 0x63, 0x68, 0x2e, 0x49, 0x6e, 0x63, 0x2e);
        // hidDevice.WriteFeatureData(0x5a, 0x05, 0x20, 0x31, 0x00, 0x08);
#endif
    }

    private async void UpdateTouchPad(TabletMode tabletMode)
    {
        if (!config.Common.DisableTouchPadInTabletMode)
        {
            return;
        }

        var touchPadState = await touchPad.State.FirstOrDefaultAsync();

        if (tabletMode == TabletMode.Notebook && touchPadState == DeviceState.Disabled)
        {
            touchPad.Toggle();
            return;
        }

        if (tabletMode != TabletMode.Notebook && touchPadState != DeviceState.Disabled)
        {
            touchPad.Toggle();
            return;
        }
    }

    private void UpdateVariBrightState()
    {
        amd.SetVariBright(!config.Common.DisableVariBright);
    }
}
