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
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using flowOSD;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;
using flowOSD.Extensions;
using Microsoft.Win32.SafeHandles;
using static flowOSD.Native.Kernel32;
using static Native.SetupAPI;

sealed partial class Battery : IDisposable, IBattery
{
    private const uint IOCTL_BATTERY_QUERY_TAG = 0x294040;
    private const uint IOCTL_BATTERY_QUERY_INFORMATION = 0x294044;
    private const uint IOCTL_BATTERY_QUERY_STATUS = 0x29404C;

    private const uint BATTERY_SYSTEM_BATTERY = 0x80000000;

    private const int ERROR_INSUFFICIENT_BUFFER = 0x7A;
    private const int ERROR_NO_MORE_ITEMS = 0x103;

    private static Guid GUID_DEVICE_BATTERY = new(0x72631e54, 0x78A4, 0x11d0, 0xbc, 0xf7, 0x00, 0xaa, 0x00, 0xb7, 0xb3, 0x2a);

    private CompositeDisposable? disposable = new CompositeDisposable();

    private readonly ITextResources textResources;

    private SafeFileHandle? batteryHandle;
    private uint batteryTag;

    private CountableSubject<int> rateSubject;
    private CountableSubject<uint> capacitySubject;
    private CountableSubject<uint> estimatedTimeSubject;
    private CountableSubject<BatteryPowerState> powerStateSubject;

    private IDisposable? updateSubscription;

    public Battery(ITextResources textResources)
    {
        this.textResources = textResources ?? throw new ArgumentNullException(nameof(textResources));

        batteryHandle = Init();

        var batteryStatus = GetBatteryStatus(batteryHandle!, batteryTag);

        rateSubject = new CountableSubject<int>(batteryStatus.Rate).DisposeWith(disposable);
        capacitySubject = new CountableSubject<uint>(batteryStatus.Capacity).DisposeWith(disposable);
        estimatedTimeSubject = new CountableSubject<uint>(GetEstimatedTime(batteryHandle, batteryTag)).DisposeWith(disposable);
        powerStateSubject = new CountableSubject<BatteryPowerState>((BatteryPowerState)batteryStatus.PowerState).DisposeWith(disposable);

        Rate = rateSubject.AsObservable();
        Capacity = capacitySubject.AsObservable();
        EstimatedTime = estimatedTimeSubject.AsObservable();
        PowerState = powerStateSubject.AsObservable();

        rateSubject.Count
            .CombineLatest(
                capacitySubject.Count,
                estimatedTimeSubject.Count,
                powerStateSubject.Count,
                (x1, x2, x3, x4) => x1 + x2 + x3 + x4)
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
                    updateSubscription = Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(_ => Update());
                }
            })
            .DisposeWith(disposable);
    }

    public string Name { get; private set; }

    public string ManufactureName { get; private set; }

    public uint DesignedCapacity { get; private set; }

    public uint FullChargedCapacity { get; private set; }

    public IObservable<int> Rate { get; }

    public IObservable<uint> Capacity { get; }

    public IObservable<uint> EstimatedTime { get; }

    public IObservable<BatteryPowerState> PowerState { get; }

    public void Dispose()
    {
        updateSubscription?.Dispose();
        updateSubscription = null;

        disposable?.Dispose();
        disposable = null;

        if (batteryHandle != null)
        {
            batteryHandle.Dispose();
            batteryHandle = null;
        }
    }

    public void Update()
    {
        CheckHandler();

        var batteryStatus = default(BATTERY_STATUS);
        try
        {
            batteryStatus = GetBatteryStatus(batteryHandle!, batteryTag);
        }
        catch (Win32Exception)
        {
            // try to reconnect
            Reconnect();

            batteryStatus = GetBatteryStatus(batteryHandle!, batteryTag);
        }

        rateSubject.OnNext(batteryStatus.Rate);
        capacitySubject.OnNext(batteryStatus.Capacity);
        powerStateSubject.OnNext((BatteryPowerState)batteryStatus.PowerState);

        var estimatedTime = GetEstimatedTime(batteryHandle!, batteryTag);
        estimatedTimeSubject.OnNext(estimatedTime);
    }

    public void Reconnect()
    {
        batteryHandle?.Dispose();

        batteryHandle = Init();
        CheckHandler();
    }

    private void CheckHandler()
    {
        if (batteryHandle == null || batteryHandle.IsInvalid || batteryHandle.IsClosed)
        {
            batteryHandle?.Dispose();
            batteryHandle = Init();
        }

        if (batteryHandle.IsInvalid)
        {
            throw new AppException(textResources["Errors.CanNotConnectToBattery"]);
        }
    }

    private SafeFileHandle Init()
    {
        var disHandle = SetupDiGetClassDevs(ref GUID_DEVICE_BATTERY, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
        if (disHandle == -1)
        {
            throw new AppException(textResources["Errors.CanNotConnectToBattery"]);
        }

        try
        {
            uint i = 0;
            SP_DEVICE_INTERFACE_DATA? deviceInterfaceData;

            while ((deviceInterfaceData = GetDeviceInterfaceData(disHandle, i++)) != null)
            {
                var devicePath = GetDevicePath(disHandle, deviceInterfaceData.Value);
                if (string.IsNullOrEmpty(devicePath))
                {
                    continue;
                }

                var battery = CreateFile(
                    devicePath,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite,
                    IntPtr.Zero,
                    FileMode.Open,
                    FILE_ATTRIBUTE_NORMAL,
                    IntPtr.Zero);

                if (!battery.IsInvalid)
                {
                    var batteryTag = GetBatteryTag(battery);
                    var batteryInformation = GetBatteryInformation(battery, batteryTag);

                    if (batteryInformation.Capabilities == BATTERY_SYSTEM_BATTERY)
                    {
                        Name = GetDeviceName(battery, batteryTag, BATTERY_QUERY_INFORMATION_LEVEL.BatteryDeviceName);
                        ManufactureName = GetDeviceName(battery, batteryTag, BATTERY_QUERY_INFORMATION_LEVEL.BatteryManufactureName);

                        DesignedCapacity = batteryInformation.DesignedCapacity;
                        FullChargedCapacity = batteryInformation.FullChargedCapacity;

                        if (Name == "ASUS Battery" && ManufactureName == "ASUSTeK")
                        {
                            this.batteryTag = batteryTag;

                            return battery;
                        }
                    }

                    battery.Dispose();
                }
            }

        }
        finally
        {
            SetupDiDestroyDeviceInfoList(disHandle);
        }

        throw new AppException(textResources["Errors.CanNotConnectToBattery"]);
    }

    private SP_DEVICE_INTERFACE_DATA? GetDeviceInterfaceData(IntPtr hdev, uint index)
    {
        SP_DEVICE_INTERFACE_DATA did = default;
        did.cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA));

        if (SetupDiEnumDeviceInterfaces(hdev, IntPtr.Zero, ref GUID_DEVICE_BATTERY, index, ref did))
        {
            return did;
        }

        if (Marshal.GetLastWin32Error() == ERROR_NO_MORE_ITEMS)
        {
            return null;
        }
        else
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    private string? GetDevicePath(IntPtr hdev, SP_DEVICE_INTERFACE_DATA did)
    {
        SetupDiGetDeviceInterfaceDetail(hdev, did, IntPtr.Zero, 0, out uint cbRequired, IntPtr.Zero);

        if (Marshal.GetLastWin32Error() == ERROR_INSUFFICIENT_BUFFER)
        {
            var ptr = Marshal.AllocHGlobal((int)cbRequired);
            try
            {
                Marshal.WriteInt32(ptr, Environment.Is64BitOperatingSystem ? 8 : 4); // cbSize.

                if (SetupDiGetDeviceInterfaceDetail(hdev, did, ptr, cbRequired, out _, IntPtr.Zero))
                {
                    return Marshal.PtrToStringUni(ptr + 4);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        return null;
    }

    private static void DeviceIoControl(
        SafeFileHandle batteryHandle,
        uint controlCode,
        IntPtr inBuffer,
        int inBufferSize,
        IntPtr outBuffer,
        int outBufferSize)
    {
        var result = Native.Kernel32.DeviceIoControl(
            batteryHandle,
            controlCode,
            inBuffer,
            inBufferSize,
            outBuffer,
            outBufferSize,
            out _,
            IntPtr.Zero);

        if (!result)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    private static uint GetBatteryTag(SafeFileHandle batteryHandle)
    {
        var inBuffer = Marshal.AllocHGlobal(sizeof(uint));
        try
        {
            Marshal.WriteInt32(inBuffer, 0);

            var outBuffer = Marshal.AllocHGlobal(sizeof(uint));
            try
            {
                DeviceIoControl(
                    batteryHandle,
                    IOCTL_BATTERY_QUERY_TAG,
                    inBuffer,
                    sizeof(uint),
                    outBuffer,
                    sizeof(uint));

                return Marshal.PtrToStructure<uint>(outBuffer);
            }
            finally
            {
                Marshal.FreeHGlobal(outBuffer);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(inBuffer);
        }
    }

    private static BATTERY_INFORMATION GetBatteryInformation(SafeFileHandle batteryHandle, uint batteryTag)
    {
        BATTERY_QUERY_INFORMATION query = default;
        query.BatteryTag = batteryTag;
        query.InformationLevel = BATTERY_QUERY_INFORMATION_LEVEL.BatteryInformation;

        var inBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(query));
        try
        {
            var outBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(BATTERY_INFORMATION)));
            try
            {
                Marshal.StructureToPtr(query, inBuffer, false);

                DeviceIoControl(
                    batteryHandle,
                    IOCTL_BATTERY_QUERY_INFORMATION,
                    inBuffer,
                    Marshal.SizeOf(query),
                    outBuffer,
                    Marshal.SizeOf(typeof(BATTERY_INFORMATION)));

                return Marshal.PtrToStructure<BATTERY_INFORMATION>(outBuffer);
            }
            finally
            {
                Marshal.FreeHGlobal(outBuffer);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(inBuffer);
        }
    }

    private static uint GetEstimatedTime(SafeFileHandle batteryHandle, uint batteryTag)
    {
        BATTERY_QUERY_INFORMATION query = default;
        query.BatteryTag = batteryTag;
        query.InformationLevel = BATTERY_QUERY_INFORMATION_LEVEL.BatteryEstimatedTime;

        var inBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(query));
        try
        {
            var outBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(BATTERY_INFORMATION)));
            try
            {
                Marshal.StructureToPtr(query, inBuffer, false);

                DeviceIoControl(
                    batteryHandle,
                    IOCTL_BATTERY_QUERY_INFORMATION,
                    inBuffer,
                    Marshal.SizeOf(query),
                    outBuffer,
                    Marshal.SizeOf(typeof(uint)));

                return Marshal.PtrToStructure<uint>(outBuffer);
            }
            finally
            {
                Marshal.FreeHGlobal(outBuffer);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(inBuffer);
        }
    }

    private static string GetDeviceName(SafeFileHandle batteryHandle, uint batteryTag, BATTERY_QUERY_INFORMATION_LEVEL level)
    {
        const int maxLoadString = 100;

        BATTERY_QUERY_INFORMATION query = default;
        query.BatteryTag = batteryTag;
        query.InformationLevel = level;

        var inBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(query));
        try
        {
            Marshal.StructureToPtr(query, inBuffer, false);

            var outBuffer = Marshal.AllocHGlobal(maxLoadString);
            try
            {

                DeviceIoControl(
                    batteryHandle,
                    IOCTL_BATTERY_QUERY_INFORMATION,
                    inBuffer,
                    Marshal.SizeOf(query),
                    outBuffer,
                    maxLoadString);

                return Marshal.PtrToStringUni(outBuffer) ?? string.Empty;
            }
            finally
            {
                Marshal.FreeHGlobal(outBuffer);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(inBuffer);
        }
    }

    private static BATTERY_STATUS GetBatteryStatus(SafeFileHandle batteryHandle, uint batteryTag)
    {
        BATTERY_WAIT_STATUS query = default;
        query.BatteryTag = batteryTag;

        var inBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(query));
        try
        {
            Marshal.StructureToPtr(query, inBuffer, false);

            var outBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(BATTERY_STATUS)));
            try
            {
                DeviceIoControl(
                    batteryHandle,
                    IOCTL_BATTERY_QUERY_STATUS,
                    inBuffer,
                    Marshal.SizeOf(query),
                    outBuffer,
                    Marshal.SizeOf(typeof(BATTERY_STATUS)));

                return Marshal.PtrToStructure<BATTERY_STATUS>(outBuffer);
            }
            finally
            {
                Marshal.FreeHGlobal(outBuffer);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(inBuffer);
        }
    }
}
