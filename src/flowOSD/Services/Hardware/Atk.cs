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

namespace flowOSD.Services.Hardware;

using System.ComponentModel;
using System.Management;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using flowOSD.Core.Hardware;
using flowOSD.Extensions;
using Microsoft.Win32.SafeHandles;
using static flowOSD.Native.Kernel32;

sealed partial class Atk : IDisposable, IAtk
{
    public const int FEATURE_KBD_REPORT_ID = 0x5a;

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

    private const uint DEVID_BATTERY_LIMIT = 0x00120057;

    const int PPT_APU = 0x001200A3;
    const int PPT_CPU = 0x001200A0;
    const int PPT_CPUB0 = 0x001200B0;

    private CompositeDisposable? disposable = new CompositeDisposable();

    private readonly BehaviorSubject<PerformanceMode> performanceModeSubject;
    private readonly BehaviorSubject<GpuMode> gpuModeSubject;
    private readonly CountableSubject<int> cpuTemperatureSubject;

    private SafeFileHandle handle;

    private IDisposable? updateSubscription;

    private readonly object ControlLocker = new object();

    public Atk(PerformanceMode? performanceMode)
    {
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
            throw new ApplicationException("Can't connect to ACPI.");
        }

        Get(DEVID_GPU_ECO_MODE, out var gpuMode);
        Get(CPU_TEMPERATURE, out var cpuTemperature);

        performanceModeSubject = new BehaviorSubject<PerformanceMode>(performanceMode ?? Core.Hardware.PerformanceMode.Default);
        gpuModeSubject = new BehaviorSubject<GpuMode>((GpuMode)gpuMode);
        cpuTemperatureSubject = new CountableSubject<int>(cpuTemperature);

        PerformanceMode = performanceModeSubject.AsObservable();
        GpuMode = gpuModeSubject.AsObservable();
        CpuTemperature = cpuTemperatureSubject.AsObservable();

        SetPerformanceMode(performanceMode ?? Core.Hardware.PerformanceMode.Default);

        cpuTemperatureSubject.Count
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
                        .Subscribe(_ =>
                            {
                                if (Get(CPU_TEMPERATURE, out var temperature))
                                {
                                    cpuTemperatureSubject?.OnNext(temperature);
                                }
                            });
                }
            })
            .DisposeWith(disposable);

        Invoke(ASUS_WMI_METHODID_INIT, new byte[8], out var initBuffer);
    }

    public IObservable<PerformanceMode> PerformanceMode { get; }

    public IObservable<GpuMode> GpuMode { get; }

    public IObservable<int> CpuTemperature { get; }

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
        return Set(PPT_CPU, limit, out buffer) && IsOk(buffer)
            && Set(PPT_APU, limit, out buffer) && IsOk(buffer);
    }

    public bool SetFanCurve(FanType fanType, IList<FanDataPoint> dataPoints)
    {
        if (dataPoints == null || dataPoints.Count != 8)
        {
            throw new ArgumentNullException(nameof(dataPoints));
        }

        var data = new byte[16];
        for (var i = 0; i < dataPoints.Count; i++)
        {
            data[i] = dataPoints[i].Temperature;
            data[8 + i] = Math.Min((byte)99, dataPoints[i].Value);
        }

        byte[] buffer;
        switch (fanType)
        {
            case FanType.Cpu:
                return Set(CPU_FAN_CURVE, data, out buffer) && IsOk(buffer);

            case FanType.Gpu:
                return Set(GPU_FAN_CURVE, data, out buffer) && IsOk(buffer);
        }

        return false;
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

    public void Dispose()
    {
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

    private bool Invoke(uint MethodId, byte[] args, out byte[] outBuffer, bool throwException = false)
    {
        lock (ControlLocker)
        {
            var acpiBuffer = new byte[8 + args.Length];
            outBuffer = new byte[20];

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
}