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

namespace flowOSD.Services;

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;

internal class MockHardwareService : IHardwareService, IHardwareFeatures
{
    private Dictionary<Type, object> register;
    private Atk atk;
    private Keyboard keyboard;
    private TouchPad touchPad;
    private Display display;
    private PowerManagement powerManagement;
    private Microphone microphone;
    private Battery battery;

    private ITextResources textResources;
    private IConfig config;
    private INotificationService notificationService;
    private IPerformanceService performanceService;

    public MockHardwareService(
        ITextResources textResources,
        IConfig config, 
        INotificationService notificationService)
    {
        this.textResources = textResources ?? throw new ArgumentNullException(nameof(textResources));
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

        atk = new Atk();
        keyboard = new Keyboard();
        touchPad = new TouchPad();
        display = new Display();
        powerManagement = new PowerManagement();
        microphone = new Microphone();
        battery = new Battery();

        performanceService = new Hardware.PerformanceService(
            textResources,
            config,
            notificationService,
            atk,
            powerManagement);

        register = new Dictionary<Type, object>();

        register[typeof(IAtk)] = atk;
        register[typeof(IKeyboard)] = keyboard;
        register[typeof(IKeyboardBacklight)] = keyboard;
        register[typeof(IKeyboardBacklightControl)] = keyboard;
        register[typeof(ITouchPad)] = touchPad;
        register[typeof(IDisplay)] = display;
        register[typeof(IDisplayBrightness)] = display;
        register[typeof(IBattery)] = battery;
        register[typeof(IPowerManagement)] = powerManagement;
        register[typeof(IMicrophone)] = microphone;
        register[typeof(IPerformanceService)] = performanceService;
        register[typeof(IHardwareFeatures)] = this;
    }

    public bool OptimizationService => false;

    public bool CpuTemperature => true;

    public bool CpuFanSpeed => true;

    public bool GpuFanSpeed => true;

    public bool PerformanceSwitch => true;

    public bool GpuSwitch => true;

    public bool Charger => true;

    public bool ChargeLimit => true;

    public bool CpuPowerLimit => true;

    public bool AmdIntegratedGpu => true;

    public bool BootSound => true;

    public T? Resolve<T>() where T : class
    {
        return register.ContainsKey(typeof(T)) ? (T)register[typeof(T)] : null;
    }

    public T ResolveNotNull<T>() where T : class
    {
        return (T)register[typeof(T)];
    }

    private sealed class Atk : IAtk
    {
        private BehaviorSubject<PerformanceMode> performanceMode;
        private BehaviorSubject<GpuMode> gpuMode;
        private readonly BehaviorSubject<DeviceState> bootSoundSubject;

        public readonly BehaviorSubject<int> cpu, cpuFanSpeed, gpuFanSpeed;
        public readonly BehaviorSubject<TabletMode> tabletMode;
        public readonly BehaviorSubject<ChargerTypes> charger;

        public Atk()
        {
            performanceMode = new BehaviorSubject<PerformanceMode>(Core.Hardware.PerformanceMode.Performance);
            gpuMode = new BehaviorSubject<GpuMode>(Core.Hardware.GpuMode.dGpu);

            cpu = new BehaviorSubject<int>(0);
            cpuFanSpeed = new BehaviorSubject<int>(0);
            gpuFanSpeed = new BehaviorSubject<int>(0);

            tabletMode = new BehaviorSubject<TabletMode>(Core.Hardware.TabletMode.Notebook);
            charger = new BehaviorSubject<ChargerTypes>(ChargerTypes.Connected);
            bootSoundSubject = new BehaviorSubject<DeviceState>(DeviceState.Enabled);

            PerformanceMode = performanceMode.AsObservable();
            GpuMode = gpuMode.AsObservable();
            CpuTemperature = cpu.AsObservable();
            CpuFanSpeed = cpuFanSpeed.AsObservable();
            GpuFanSpeed = gpuFanSpeed.AsObservable();
            TabletMode = tabletMode.AsObservable();
            Charger = charger.AsObservable();

            BootSound = bootSoundSubject.AsObservable();
        }

        public IObservable<PerformanceMode> PerformanceMode { get; }

        public IObservable<GpuMode> GpuMode { get; }

        public IObservable<int> CpuTemperature { get; }

        public IObservable<int> CpuFanSpeed { get; }

        public IObservable<int> GpuFanSpeed { get; }

        public IObservable<TabletMode> TabletMode { get; }

        public IObservable<ChargerTypes> Charger { get; }

        public IObservable<DeviceState> BootSound { get; }

        public uint MinBatteryChargeLimit { get; set; }

        public uint MaxBatteryChargeLimit { get; set; }

        public uint MinPowerLimit { get; set; }

        public uint MaxPowerLimit { get; set; }

        public bool Get(uint deviceId, out int value)
        {
            value = 0;
            return true;
        }

        public bool Get(uint deviceId, uint status, out byte[] outBuffer)
        {
            outBuffer = new byte[0];
            return true;
        }

        public bool Set(uint deviceId, uint status, out byte[] outBuffer)
        {
            outBuffer = new byte[0];
            return true;
        }

        public bool Set(uint deviceId, byte[] parameters, out byte[] outBuffer)
        {
            outBuffer = new byte[0];
            return true;
        }

        public IList<FanDataPoint> GetFanCurve(FanType fanType, PerformanceMode performanceMode)
        {
            return new FanDataPoint[0];
        }

        public bool SetBatteryChargeLimit(uint value)
        {
            return true;
        }

        public bool SetCpuLimit(uint value)
        {
            return true;
        }

        public bool SetFanCurve(FanType fanType, IList<FanDataPoint> dataPoints)
        {
            return true;
        }

        public bool SetGpuMode(GpuMode gpuMode)
        {
            this.gpuMode.OnNext(gpuMode);
            return true;
        }

        public bool SetPerformanceMode(PerformanceMode performanceMode)
        {
            this.performanceMode.OnNext(performanceMode);
            return true;
        }

        public bool SetBootSound(DeviceState state)
        {
            bootSoundSubject.OnNext(state);
            return true;
        }
    }

    private sealed class TouchPad : ITouchPad
    {
        private BehaviorSubject<DeviceState> state;

        public TouchPad()
        {
            state = new BehaviorSubject<DeviceState>(DeviceState.Enabled);

            State = state.AsObservable();
        }

        public IObservable<DeviceState> State { get; }

        public void Toggle()
        {
            state.OnNext(state.Value == DeviceState.Enabled ? DeviceState.Disabled : DeviceState.Enabled);
        }
    }

    private sealed class Keyboard : IKeyboard, IKeyboardBacklight, IKeyboardBacklightControl
    {
        private BehaviorSubject<KeyboardBacklightLevel> level;
        private BehaviorSubject<DeviceState> state;

        public readonly Subject<uint> activity;
        public readonly Subject<AtkKey> keyPressed;

        public Keyboard()
        {
            level = new BehaviorSubject<KeyboardBacklightLevel>(KeyboardBacklightLevel.Medium);
            state = new BehaviorSubject<DeviceState>(DeviceState.Enabled);

            activity = new Subject<uint>();
            keyPressed = new Subject<AtkKey>();

            State = state.AsObservable();
            Level = level.AsObservable();
            Activity = activity.AsObservable();
            KeyPressed = keyPressed.AsObservable();
        }

        public IObservable<uint> Activity { get; }

        public IObservable<AtkKey> KeyPressed { get; }

        public IObservable<DeviceState> State { get; }

        public IObservable<KeyboardBacklightLevel> Level { get; }

        public void LevelUp()
        {
            var level = this.level.Value;

            if (level < KeyboardBacklightLevel.High)
            {
                SetLevel((KeyboardBacklightLevel)((byte)level + 1));
            }
        }

        public void LevelDown()
        {
            var level = this.level.Value;

            if (level > KeyboardBacklightLevel.Off)
            {
                SetLevel((KeyboardBacklightLevel)((byte)level - 1));
            }
        }

        public void SetLevel(KeyboardBacklightLevel value, bool force = false)
        {
            level.OnNext(value);
        }

        public void SetState(DeviceState value, bool force = false)
        {
            state.OnNext(value);
        }
    }

    private sealed class Display : IDisplay, IDisplayBrightness
    {
        public readonly BehaviorSubject<DeviceState> state;
        public readonly BehaviorSubject<DisplayRefreshRates> refreshRates;
        public readonly BehaviorSubject<uint> refreshRate;

        private double level;

        public Display()
        {
            state = new BehaviorSubject<DeviceState>(DeviceState.Enabled);
            refreshRates = new BehaviorSubject<DisplayRefreshRates>(new DisplayRefreshRates(new HashSet<uint>(new uint[] { 60, 120 })));
            refreshRate = new BehaviorSubject<uint>(refreshRates.Value.Low!.Value);

            State = state.AsObservable();
            RefreshRates = refreshRates.AsObservable();
            RefreshRate = refreshRate.AsObservable();
        }

        public IObservable<DeviceState> State { get; }

        public IObservable<DisplayRefreshRates> RefreshRates { get; }

        public IObservable<uint> RefreshRate { get; }

        public double GetLevel()
        {
            return level;
        }

        public void LevelDown()
        {
            level = Math.Max(0, level - 10);
        }

        public void LevelUp()
        {
            level = Math.Min(100, level + 10);
        }

        public void SetLevel(double value)
        {
            level = value;
        }

        public bool SetRefreshRate(uint? value)
        {
            if (value != null)
            {
                refreshRate.OnNext(value.Value);
                return true;
            }

            return false;
        }
    }

    private sealed class PowerManagement : IPowerManagement
    {
        public readonly BehaviorSubject<bool> boost, batterySaver;
        public readonly BehaviorSubject<PowerSource> powerSource;
        public readonly BehaviorSubject<PowerMode> powerMode;
        public readonly Subject<PowerEvent> powerEvent;

        public PowerManagement()
        {
            boost = new BehaviorSubject<bool>(true);
            powerSource = new BehaviorSubject<PowerSource>(Core.Hardware.PowerSource.Charger);
            powerMode = new BehaviorSubject<PowerMode>(Core.Hardware.PowerMode.Balanced);
            batterySaver = new BehaviorSubject<bool>(false);
            powerEvent = new Subject<PowerEvent>();

            IsBoost = boost.AsObservable();
            PowerMode = powerMode.AsObservable();
            PowerSource = powerSource.AsObservable();
            IsBatterySaver = batterySaver.AsObservable();
            PowerEvent = powerEvent.AsObservable();
        }

        public IObservable<bool> IsBoost { get; }

        public IObservable<PowerSource> PowerSource { get; }

        public IObservable<bool> IsBatterySaver { get; }

        public IObservable<PowerMode> PowerMode { get; }

        public IObservable<PowerEvent> PowerEvent { get; }

        public void DisableBoost()
        {
            boost.OnNext(false);
        }

        public void EnableBoost()
        {
            boost.OnNext(true);
        }

        public void SetPowerMode(PowerMode powerMode)
        {
            this.powerMode.OnNext(powerMode);
        }

        public void ToggleBoost()
        {
            boost.OnNext(!boost.Value);
        }
    }

    private sealed class Microphone : IMicrophone
    {
        private bool isMuted = false;

        public bool IsMicMuted()
        {
            return isMuted;
        }

        public void Toggle()
        {
            isMuted = !isMuted;
        }
    }

    private sealed class Battery : IBattery
    {
        public readonly BehaviorSubject<int> rate;
        public readonly BehaviorSubject<uint> capacity, estimatedTime;
        public readonly BehaviorSubject<BatteryPowerState> powerState;

        public Battery()
        {
            rate = new BehaviorSubject<int>(0);
            capacity = new BehaviorSubject<uint>(DesignedCapacity);
            estimatedTime = new BehaviorSubject<uint>(0);
            powerState = new BehaviorSubject<BatteryPowerState>(BatteryPowerState.PowerOnLine);

            Rate = rate.AsObservable();
            Capacity = capacity.AsObservable();
            EstimatedTime = estimatedTime.AsObservable();
            PowerState = powerState.AsObservable();
        }

        public string Name { get; set; } = "ASUS Battery";

        public string ManufactureName { get; set; } = "ASUSTeK";

        public uint DesignedCapacity { get; set; } = 62000;

        public uint FullChargedCapacity { get; set; } = 49000;

        public IObservable<int> Rate { get; }

        public IObservable<uint> Capacity { get; }

        public IObservable<uint> EstimatedTime { get; }

        public IObservable<BatteryPowerState> PowerState { get; }

        public void Update()
        {
            ;
        }
    }
}
