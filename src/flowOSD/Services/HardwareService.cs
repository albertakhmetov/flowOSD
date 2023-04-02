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

sealed class HardwareService : IDisposable, IHardwareService
{
    private CompositeDisposable? disposable = new CompositeDisposable();

    private IConfig config;
    private IMessageQueue messageQueue;
    private IKeysSender keysSender;

    private HidDevice hidDevice;

    private IAtk atk;
    private IAtkWmi atkWmi;
    private ICpu cpu;
    private IKeyboard keyboard;
    private IKeyboardBacklight keyboardBacklight;
    private ITouchPad touchPad;
    private IDisplay display;
    private IDisplayBrightness displayBrightness;
    private Battery battery;
    private IPowerManagement powerManagement;
    private IMicrophone microphone;

    private Dictionary<Type, object> devices = new Dictionary<Type, object>();

    private KeyboardBacklightService? keyboardBacklightService;
    private RefreshRateService refreshRateService;

    public HardwareService(IConfig config, IMessageQueue messageQueue, IKeysSender keysSender)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.messageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
        this.keysSender = keysSender ?? throw new ArgumentNullException(nameof(keysSender));

        hidDevice = HidDevice.Devices
            .Where(i => i.VendorId == 0xB05 && i.ReadFeatureData(out byte[] data, Keyboard.FEATURE_KBD_REPORT_ID))
            .FirstOrDefault() ?? throw new ApplicationException("Can't connect to HID");

        InitHid();

        atk = new Atk(config.Common.PerformanceModeOverrideEnabled ? config.Common.PerformanceModeOverride : null);
        atkWmi = new AtkWmi(atk);
        cpu = new Cpu();

        if (config.UseOptimizationMode)
        {
            keyboard = (atkWmi as IKeyboard)!;
            keyboardBacklight = new Hardware.Optimization.KeyboardBacklight();
            touchPad = new Hardware.Optimization.TouchPad(this.messageQueue, this.keysSender);
        }
        else
        {
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

        Register<IAtk>(atk);
        Register<IAtkWmi>(atkWmi);
        Register<ICpu>(cpu);
        Register<IKeyboard>(keyboard);
        Register<IKeyboardBacklight>(keyboardBacklight);
        Register<ITouchPad>(touchPad);
        Register<IDisplay>(display);
        Register<IDisplayBrightness>(displayBrightness);
        Register<IBattery>(battery);
        Register<IPowerManagement>(powerManagement);
        Register<IMicrophone>(microphone);

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

        atkWmi.TabletMode
            .Throttle(TimeSpan.FromMicroseconds(2000))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(UpdateTouchPad)
            .DisposeWith(disposable);

        keyboardBacklightService = config.UseOptimizationMode ? null : new KeyboardBacklightService(
            config, 
            keyboardBacklight,
            keyboard,
            powerManagement).DisposeWith(disposable);

        refreshRateService = new RefreshRateService(
            this.config,
            display,
            powerManagement).DisposeWith(disposable);
    }

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
        return Resolve<T>() ?? throw new InvalidOperationException($"Can't resolve {typeof(T).Name}");
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
        if (!config.UseOptimizationMode)
        {
            keyboardBacklight.SetState(DeviceState.Disabled, force: true);
        }
    }

    private void OnResume()
    {
        battery.Reconnect();

        if (config.Common.PerformanceModeOverrideEnabled)
        {
            atk.SetPerformanceMode(config.Common.PerformanceModeOverride);
        }

        if (!config.UseOptimizationMode)
        {
            InitHid();
            keyboardBacklightService?.ResetTimer();
            keyboardBacklight.SetState(DeviceState.Enabled, force: true);
        }

        refreshRateService.Update();
    }

    private void InitHid()
    {
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
}
