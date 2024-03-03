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

namespace flowOSD.Services.Hardware;

using System.ComponentModel;
using System.Management;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;
using flowOSD.Extensions;
using Microsoft.Win32.SafeHandles;
using static flowOSD.Native.Kernel32;
using static flowOSD.Native.User32;

sealed partial class Atk : IDisposable, IAtk, IKeyboard
{
    public const int FEATURE_KBD_REPORT_ID = 0x5a;

    private const int FAN_CURVE_POINTS = 8;

    private const uint IO_CONTROL_CODE = 0x0022240C;
    private const uint ASUS_WMI_METHODID_INIT = 0x54494E49;

    private const uint DSTS_PRESENCE_BIT = 0x00010000;
    private const uint DSTS = 0x53545344;
    private const uint DEVS = 0x53564544;

    private const uint DEVID_GPU_ECO_MODE = 0x00090020;
    private const uint DEVID_THROTTLE_THERMAL_POLICY = 0x00120075;

    private const uint CPU_FAN_SPEED = 0x00110013;
    private const uint GPU_FAN_SPEED = 0x00110014;

    private const uint CPU_FAN_CURVE = 0x00110024;
    private const uint GPU_FAN_CURVE = 0x00110025;

    private const uint CPU_TEMPERATURE = 0x00120094;
    private const uint GPU_TEMPERATURE = 0x00120097;

    private const uint DEVID_BOOT_SOUND = 0x00130022;

    private const uint DEVID_BATTERY_LIMIT = 0x00120057;

    private const uint DEVID_CHARGER = 0x0012006c;
    private const uint DEVID_TABLET = 0x00060077;

    private const int PPT_APU = 0x001200A3;
    private const int PPT_CPU = 0x001200A0;
    private const int PPT_CPUB0 = 0x001200B0;

    private const int AK_TABLET_STATE = 0xBD;
    private const int AK_CHARGER = 0x7B;

    private const int POWER_SOURCE_BATTERY = 0x00;
    private const int POWER_SOURCE_65 = 0x22;
    private const int POWER_SOURCE_45 = 0x2D;
    private const int POWER_SOURCE_FULL = 0x2A;

    private CompositeDisposable? disposable = new CompositeDisposable();

    private readonly ITextResources textResources;

    private readonly BehaviorSubject<PerformanceMode> performanceModeSubject;
    private readonly BehaviorSubject<GpuMode> gpuModeSubject;
    private readonly CountableSubject<int> cpuTemperatureSubject;
    private readonly CountableSubject<int> cpuFanSpeedSubject;
    private readonly CountableSubject<int> gpuFanSpeedSubject;

    private readonly BehaviorSubject<TabletMode> tabletModeSubject;
    private readonly BehaviorSubject<ChargerTypes> chargerSubject;
    private Subject<AtkKey> keyPressedSubject;
    private readonly BehaviorSubject<DeviceState> bootSoundSubject;

    private SafeFileHandle handle;
    private ManagementEventWatcher? watcher;

    private IDisposable? updateSubscription;

    private readonly object ControlLocker = new object();

    public Atk(
        ITextResources textResources,
        PerformanceMode? performanceMode)
    {
        this.textResources = textResources ?? throw new ArgumentNullException(nameof(textResources));

        handle = CreateFile(
            @"\\.\\ATKACPI",
            FileAccess.ReadWrite,
            FileShare.ReadWrite,
            IntPtr.Zero,
            FileMode.Open,
            FILE_ATTRIBUTE_NORMAL,
            IntPtr.Zero
        ).DisposeWith(disposable);

        if (handle.IsInvalid)
        {
            throw new AppException(textResources["Errors.CanNotConnectToAcpi"]);
        }

        GpuSwitchSupported = Get(DEVID_GPU_ECO_MODE, out var gpuMode);
        CpuTemperatureSupported = Get(CPU_TEMPERATURE, out var cpuTemperature);

        CpuFanSpeedSupported = Get(CPU_FAN_SPEED, out var cpuFanSpeed);
        GpuFanSpeedSupported = Get(GPU_FAN_SPEED, out var gpuFanSpeed);

        PerformanceSwitchSupported = Get(DEVID_THROTTLE_THERMAL_POLICY, out _);
        TabletModeSupported = Get(DEVID_TABLET, out var tabletMode);
        ChargerSupported = Get(DEVID_CHARGER, out var charger);
        ChargeLimitSupported = Get(DEVID_BATTERY_LIMIT, out _);
        CpuPowerLimitSupported = (Get(PPT_APU, out _) && Get(PPT_CPU, out _)) || Get(PPT_CPUB0, out _);
        BootSoundSupported = Get(DEVID_BOOT_SOUND, out var bootSoundState);

        performanceModeSubject = new BehaviorSubject<PerformanceMode>(performanceMode ?? Core.Hardware.PerformanceMode.Performance);
        gpuModeSubject = new BehaviorSubject<GpuMode>((GpuMode)gpuMode);
        cpuTemperatureSubject = new CountableSubject<int>(cpuTemperature);
        cpuFanSpeedSubject = new CountableSubject<int>(cpuFanSpeed);
        gpuFanSpeedSubject = new CountableSubject<int>(gpuFanSpeed);
        tabletModeSubject = new BehaviorSubject<TabletMode>((TabletMode)tabletMode);
        chargerSubject = new BehaviorSubject<ChargerTypes>(GetChargerTypes(charger));
        keyPressedSubject = new Subject<AtkKey>();
        bootSoundSubject = new BehaviorSubject<DeviceState>(bootSoundState == 1 ? DeviceState.Enabled : DeviceState.Disabled);

        PerformanceMode = performanceModeSubject.AsObservable();
        GpuMode = gpuModeSubject.AsObservable();
        CpuTemperature = cpuTemperatureSubject.AsObservable();
        CpuFanSpeed = cpuFanSpeedSubject.AsObservable();
        GpuFanSpeed = gpuFanSpeedSubject.AsObservable();
        TabletMode = tabletModeSubject.AsObservable();
        Charger = chargerSubject.AsObservable();
        KeyPressed = keyPressedSubject.AsObservable();
        BootSound = bootSoundSubject.AsObservable();

        SetPerformanceMode(performanceMode ?? Core.Hardware.PerformanceMode.Performance);

        cpuTemperatureSubject.Count
            .CombineLatest(cpuFanSpeedSubject, gpuFanSpeedSubject, (cpuT, cpuF, gpuF) => cpuT + cpuF + gpuF)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(sum =>
            {
                if (sum == 0 && updateSubscription != null)
                {
                    updateSubscription.Dispose();
                    updateSubscription = null;
                }

                if (sum > 0 && updateSubscription == null)
                {
                    updateSubscription = Observable.Interval(TimeSpan.FromSeconds(1))
                        .Subscribe(_ => UpdateMonitoring());
                }
            })
            .DisposeWith(disposable);

        Invoke(ASUS_WMI_METHODID_INIT, new byte[8], out var initBuffer);

        watcher = new ManagementEventWatcher("root\\wmi", "SELECT * FROM AsusAtkWmiEvent");
        watcher.EventArrived += OnWmiEvent;
        watcher.Start();

        if (!TabletModeSupported)
        {
            NotifySystemTabletState();
        }
    }

    public bool CpuTemperatureSupported { get; }

    public bool CpuFanSpeedSupported { get; }

    public bool GpuFanSpeedSupported { get; }

    public bool PerformanceSwitchSupported { get; }

    public bool GpuSwitchSupported { get; }

    public bool TabletModeSupported { get; }

    public bool ChargerSupported { get; }

    public bool ChargeLimitSupported { get; }

    public bool CpuPowerLimitSupported { get; }

    public bool BootSoundSupported { get; }

    public IObservable<PerformanceMode> PerformanceMode { get; }

    public IObservable<GpuMode> GpuMode { get; }

    public IObservable<int> CpuTemperature { get; }

    public IObservable<int> CpuFanSpeed { get; }

    public IObservable<int> GpuFanSpeed { get; }

    public IObservable<TabletMode> TabletMode { get; }

    public IObservable<ChargerTypes> Charger { get; }

    public IObservable<AtkKey> KeyPressed { get; }

    public IObservable<DeviceState> BootSound { get; }

    IObservable<uint> IKeyboard.Activity { get; } = Observable.Empty<uint>();

    public uint MinBatteryChargeLimit => 40;

    public uint MaxBatteryChargeLimit => 100;

    public uint MinPowerLimit => 5;

    public uint MaxPowerLimit => 45;

    public bool SetBatteryChargeLimit(uint value)
    {
        var limit = Math.Max(MinBatteryChargeLimit, Math.Min(MaxBatteryChargeLimit, value));

        byte[] buffer;
        return Set(DEVID_BATTERY_LIMIT, limit, out buffer) && IsOk(buffer);
    }

    public bool SetCpuLimit(uint value)
    {
        var limit = Math.Max(MinPowerLimit, Math.Min(MaxPowerLimit, value));

        byte[] buffer;

        if (Get(PPT_CPUB0, out _))
        {
            return Set(PPT_CPUB0, limit, out buffer) && IsOk(buffer);
        }
        else
        {
            return Set(PPT_CPU, limit, out buffer) && IsOk(buffer)
                && Set(PPT_APU, limit, out buffer) && IsOk(buffer);
        }
    }

    public bool SetFanCurve(FanType fanType, IList<FanDataPoint> dataPoints)
    {
        if (dataPoints == null || dataPoints.Count != 8 || !dataPoints.Any(i => i.Value > 0))
        {
            throw new ArgumentException("Incorrect fan curve", nameof(dataPoints));
        }

        uint fanDeviceId = fanType switch
        {
            FanType.Cpu => CPU_FAN_CURVE,
            FanType.Gpu => GPU_FAN_CURVE,
            _ => 0,
        };

        if (fanDeviceId == 0)
        {
            return false;
        }

        var data = new byte[FAN_CURVE_POINTS * 2];
        for (var i = 0; i < dataPoints.Count; i++)
        {
            data[i] = dataPoints[i].Temperature;
            data[8 + i] = Math.Min((byte)100, dataPoints[i].Value);
        }

        return Set(fanDeviceId, data, out var buffer) && IsOk(buffer);
    }

    public IList<FanDataPoint> GetFanCurve(FanType fanType, PerformanceMode performanceMode)
    {
        var device = fanType == FanType.Cpu ? CPU_FAN_CURVE : GPU_FAN_CURVE;
        uint mode;
        if (performanceMode == Core.Hardware.PerformanceMode.Turbo)
        {
            mode = 2;
        }
        else if (performanceMode == Core.Hardware.PerformanceMode.Silent)
        {
            mode = 1;
        }
        else
        {
            mode = 0;
        }

        Get(device, mode, out var r);

        var points = new FanDataPoint[8];
        for (var i = 0; i < points.Length; i++)
        {
            points[i] = new FanDataPoint(r[i], r[i + 8]);
        }

        return points;
    }

    public bool SetPerformanceMode(PerformanceMode performanceMode)
    {
        if (Set(DEVID_THROTTLE_THERMAL_POLICY, (uint)performanceMode, out var buffer) && IsOk(buffer))
        {
            performanceModeSubject.OnNext(performanceMode);
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool SetGpuMode(GpuMode gpuMode)
    {
        if (Get(DEVID_GPU_ECO_MODE, out var value))
        {
            var currentGpuMode = (GpuMode)value;

            if (currentGpuMode != gpuMode && Set(DEVID_GPU_ECO_MODE, (uint)gpuMode, out var buffer) && IsOk(buffer))
            {
                gpuModeSubject.OnNext(gpuMode);
                return true;
            }
        }

        return false;
    }

    public bool SetBootSound(DeviceState state)
    {
        if (Set(DEVID_BOOT_SOUND, (uint)(state == DeviceState.Disabled ? 0 : 1), out var buffer) && IsOk(buffer))
        {
            bootSoundSubject.OnNext(state);

            return true;
        }

        return false;
    }

    public void Dispose()
    {
        watcher?.Dispose();
        watcher = null;

        disposable?.Dispose();
        disposable = null;
    }

    public bool Get(uint deviceId, out int value)
    {
        var args = new byte[8];
        BitConverter.GetBytes(deviceId).CopyTo(args, 0);

        if (Get(deviceId, 0, out var buffer))
        {
            var raw = BitConverter.ToInt64(buffer, 0);
            value = Convert.ToInt32(raw & ~DSTS_PRESENCE_BIT);

            return (raw & DSTS_PRESENCE_BIT) == DSTS_PRESENCE_BIT;
        }
        else
        {
            value = 0;
            return false;
        }
    }

    public bool Get(uint deviceId, uint status, out byte[] outBuffer)
    {
        var args = new byte[8];
        BitConverter.GetBytes(deviceId).CopyTo(args, 0);
        BitConverter.GetBytes(status).CopyTo(args, 4);

        return Invoke(DSTS, args, out outBuffer);
    }

    public bool Set(uint deviceId, uint status, out byte[] outBuffer)
    {
        var args = new byte[8];
        BitConverter.GetBytes(deviceId).CopyTo(args, 0);
        BitConverter.GetBytes(status).CopyTo(args, 4);

        return Invoke(DEVS, args, out outBuffer);
    }

    public bool Set(uint deviceId, byte[] parameters, out byte[] outBuffer)
    {
        var args = new byte[4 + parameters.Length];
        BitConverter.GetBytes(deviceId).CopyTo(args, 0);
        parameters.CopyTo(args, 4);

        return Invoke(DEVS, args, out outBuffer);
    }

    private static bool IsOk(byte[] buffer)
    {
        return BitConverter.ToInt32(buffer, 0) == 1;
    }

    private static ChargerTypes GetChargerTypes(int code)
    {
        switch (code)
        {
            case POWER_SOURCE_BATTERY:
                return ChargerTypes.None;

            case POWER_SOURCE_45:
            case POWER_SOURCE_65:
                return ChargerTypes.Connected | ChargerTypes.LowPower;

            default:
                return ChargerTypes.Connected;
        }
    }

    private void UpdateMonitoring()
    {
        if (Get(CPU_TEMPERATURE, out var temperature))
        {
            cpuTemperatureSubject.OnNext(temperature);
        }

        int fanSpeed;
        const float maxCpuFanSpeed = 0.92f, maxGpuFanSpeed = 0.75f;

        if (Get(CPU_FAN_SPEED, out fanSpeed))
        {
            cpuFanSpeedSubject.OnNext(Convert.ToInt32(Math.Round(fanSpeed / maxCpuFanSpeed)));
        }

        if (Get(GPU_FAN_SPEED, out fanSpeed))
        {
            gpuFanSpeedSubject.OnNext(Convert.ToInt32(Math.Round(fanSpeed / maxGpuFanSpeed)));
        }
    }

    private async void NotifySystemTabletState()
    {
        await Task.Delay(500);

        var isTablet = GetSystemMetrics(SM_CONVERTIBLESLATEMODE) == 0;

        tabletModeSubject.OnNext(isTablet ? Core.Hardware.TabletMode.Tablet : Core.Hardware.TabletMode.Notebook);
    }

    private bool Invoke(uint MethodId, byte[] args, out byte[] outBuffer, bool throwException = false)
    {
        lock (ControlLocker)
        {
            var acpiBuffer = new byte[8 + args.Length];
            outBuffer = new byte[20];

            if (handle.IsClosed || handle.IsInvalid)
            {
                return false;
            }

            BitConverter.GetBytes(MethodId).CopyTo(acpiBuffer, 0);
            BitConverter.GetBytes(args.Length).CopyTo(acpiBuffer, 4);
            Array.Copy(args, 0, acpiBuffer, 8, args.Length);

            uint lpBytesReturned = 0;
            var result = DeviceIoControl(
                handle,
                IO_CONTROL_CODE,
                acpiBuffer,
                (uint)acpiBuffer.Length,
                outBuffer,
                (uint)outBuffer.Length,
                ref lpBytesReturned,
                IntPtr.Zero);

            if (!result && throwException)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return result;
        }
    }

    private void OnWmiEvent(object sender, EventArrivedEventArgs e)
    {
        var v = e.NewEvent.Properties.FirstOrDefault<PropertyData>(x => x.Name == "EventID")?.Value;
        if (v is not uint code)
        {
            return;
        }

        if (code >= byte.MinValue && code <= byte.MaxValue && Enum.IsDefined(typeof(AtkKey), (byte)code))
        {
            keyPressedSubject.OnNext((AtkKey)code);
            return;
        }

        switch (code)
        {
            case AK_TABLET_STATE:
                if (Get(DEVID_TABLET, out var tabletMode))
                {
                    tabletModeSubject.OnNext((TabletMode)tabletMode);
                }
                else
                {
                    NotifySystemTabletState();
                }

                break;

            case AK_CHARGER:
                if (Get(DEVID_CHARGER, out var charger))
                {
                    chargerSubject.OnNext(GetChargerTypes(charger));
                }

                break;
        }
    }
}