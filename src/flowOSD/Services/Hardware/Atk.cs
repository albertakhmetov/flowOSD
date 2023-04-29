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

    private const int CPU_TEMPERATURE = 0x00120094;
    private const int Temp_GPU = 0x00120097;

    const int PPT_APUA3 = 0x001200A3;
    const int PPT_TotalA0 = 0x001200A0;
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

        performanceModeSubject = new BehaviorSubject<PerformanceMode>(performanceMode ?? Core.Hardware.PerformanceMode.Default);
        gpuModeSubject = new BehaviorSubject<GpuMode>((GpuMode)Get(DEVID_GPU_ECO_MODE));
        cpuTemperatureSubject = new CountableSubject<int>(Get(CPU_TEMPERATURE));

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
                        .Subscribe(_ => cpuTemperatureSubject?.OnNext(Get(CPU_TEMPERATURE)));
                }
            })
            .DisposeWith(disposable);

        Invoke(ASUS_WMI_METHODID_INIT, new byte[8]);
    }

    public IObservable<PerformanceMode> PerformanceMode { get; }

    public IObservable<GpuMode> GpuMode { get; }

    public IObservable<int> CpuTemperature { get; }

    public bool SetCpuLimit(uint value)
    {
        Set(PPT_TotalA0, value);
        Set(PPT_APUA3, value);

        return true;
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
            data[8 + i] = dataPoints[i].Value;
        }

        switch (fanType)
        {
            case FanType.Cpu:
                return BitConverter.ToInt32(Set(CPU_FAN_CURVE, data), 0) == 1;

            case FanType.Gpu:
                return BitConverter.ToInt32(Set(GPU_FAN_CURVE, data), 0) == 1;
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

        var r = Get(device, mode);

        var points = new FanDataPoint[8];
        for (var i = 0; i < 15; i += 2)
        {
            points[i / 2] = new FanDataPoint(r[i], r[i + 1]);
        }

        return points;
    }

    public void SetPerformanceMode(PerformanceMode performanceMode)
    {
        Set(DEVID_THROTTLE_THERMAL_POLICY, (uint)performanceMode);

        performanceModeSubject.OnNext(performanceMode);
    }

    public void SetGpuMode(GpuMode gpuMode)
    {
        var currentGpuMode = (GpuMode)Get(DEVID_GPU_ECO_MODE);

        if (currentGpuMode != gpuMode)
        {
            Set(DEVID_GPU_ECO_MODE, (uint)gpuMode);
            gpuModeSubject.OnNext(gpuMode);
        }
    }

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    public int Get(uint deviceId)
    {
        var args = new byte[8];
        BitConverter.GetBytes(deviceId).CopyTo(args, 0);

        return Convert.ToInt32(BitConverter.ToInt64(Get(deviceId, 0), 0) & ~DSTS_PRESENCE_BIT);
    }

    public byte[] Get(uint deviceId, uint status)
    {
        var args = new byte[8];
        BitConverter.GetBytes(deviceId).CopyTo(args, 0);
        BitConverter.GetBytes(status).CopyTo(args, 4);

        return Invoke(DSTS, args);
    }

    public byte[] Set(uint deviceId, uint status)
    {
        var args = new byte[8];
        BitConverter.GetBytes(deviceId).CopyTo(args, 0);
        BitConverter.GetBytes(status).CopyTo(args, 4);

        return Invoke(DEVS, args);
    }

    public byte[] Set(uint deviceId, byte[] parameters)
    {
        var args = new byte[4 + parameters.Length];
        BitConverter.GetBytes(deviceId).CopyTo(args, 0);
        parameters.CopyTo(args, 4);

        return Invoke(DEVS, args);
    }

    private byte[] Invoke(uint MethodId, byte[] args)
    {
        lock (ControlLocker)
        {
            var acpiBuffer = new byte[8 + args.Length];
            var outBuffer = new byte[20];

            BitConverter.GetBytes(MethodId).CopyTo(acpiBuffer, 0);
            BitConverter.GetBytes(args.Length).CopyTo(acpiBuffer, 4);
            Array.Copy(args, 0, acpiBuffer, 8, args.Length);

            uint lpBytesReturned = 0;
            if (!DeviceIoControl(
                handle,
                IO_CONTROL_CODE,
                acpiBuffer,
                (uint)acpiBuffer.Length,
                outBuffer,
                (uint)outBuffer.Length,
                ref lpBytesReturned,
                IntPtr.Zero))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return outBuffer;
        }
    }
}