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

using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using static Native.Hid;
using static Native.SetupAPI;
using static Native.Kernel32;

sealed class HidDevice
{
    private readonly int featureReportByteLength, inputReportByteLength;

    private HidDevice(string devicePath)
    {
        DevicePath = devicePath ?? throw new ArgumentNullException(nameof(devicePath));

        using var device = OpenDeviceIO();

        var deviceAttributes = new HIDD_ATTRIBUTES();
        deviceAttributes.Size = Marshal.SizeOf(deviceAttributes);
        HidD_GetAttributes(device, ref deviceAttributes);

        VendorId = deviceAttributes.VendorID;
        ProductId = deviceAttributes.ProductID;

        var capabilities = GetCapabilities(device);
        featureReportByteLength = capabilities.FeatureReportByteLength;
        inputReportByteLength = capabilities.InputReportByteLength;
    }

    public string DevicePath { get; }

    public int VendorId { get; }

    public int ProductId { get; }

    public static IEnumerable<HidDevice> Devices { get; } = new HidDevices();

    public bool ReadFeatureData(out byte[] data, byte reportId = 0)
    {
        if (featureReportByteLength <= 0)
        {
            data = new byte[0];
            return false;
        }

        data = new byte[featureReportByteLength];

        var buffer = new byte[featureReportByteLength];
        buffer[0] = reportId;

        using var device = OpenDeviceIO();
        if (HidD_GetFeature(device, buffer, buffer.Length))
        {
            Array.Copy(buffer, 0, data, 0, Math.Min(data.Length, featureReportByteLength));
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool WriteFeatureData(params byte[] data)
    {
        if (featureReportByteLength <= 0)
        {
            return false;
        }

        var buffer = new byte[featureReportByteLength];
        Array.Copy(data, 0, buffer, 0, Math.Min(data.Length, featureReportByteLength));

        using var device = OpenDeviceIO();

        return HidD_SetFeature(device, buffer, buffer.Length);
    }

    public async Task<byte[]> ReadDataAsync()
    {
        return await ReadDataAsync(CancellationToken.None);
    }

    public async Task<byte[]> ReadDataAsync(CancellationToken token)
    {
        if (inputReportByteLength < 0)
        {
            return new byte[0];
        }

        using var device = OpenDeviceIO();

        using var fs = new FileStream(device, FileAccess.Read);

        var buffer = new byte[inputReportByteLength];
        await fs.ReadAsync(buffer, 0, buffer.Length, token);

        return buffer;
    }

    public byte[] ReadData()
    {
        if (inputReportByteLength < 0)
        {
            return new byte[0];
        }

        using var device = OpenDeviceIO();

        using var fs = new FileStream(device, FileAccess.Read);

        var buffer = new byte[inputReportByteLength];
        fs.Read(buffer, 0, buffer.Length);

        return buffer;
    }

    private SafeFileHandle OpenDeviceIO(
        FileAccess deviceAccess = FileAccess.ReadWrite,
        FileShare shareMode = FileShare.ReadWrite,
        bool isOverlapped = false)
    {
        var security = new SECURITY_ATTRIBUTES();
        security.lpSecurityDescriptor = IntPtr.Zero;
        security.bInheritHandle = true;
        security.nLength = Marshal.SizeOf(security);

        return CreateFile(
            DevicePath,
            deviceAccess,
            shareMode,
            ref security,
            FileMode.Open,
            isOverlapped ? FILE_ATTRIBUTE_OVERLAPPED : 0,
            IntPtr.Zero);
    }

    private static HIDP_CAPS GetCapabilities(SafeHandle handle)
    {
        var capabilities = default(HIDP_CAPS);
        var preparsedDataPointer = default(IntPtr);

        if (HidD_GetPreparsedData(handle, ref preparsedDataPointer))
        {
            HidP_GetCaps(preparsedDataPointer, ref capabilities);
            HidD_FreePreparsedData(preparsedDataPointer);
        }

        return capabilities;
    }

    private sealed class HidDevices : IEnumerable<HidDevice>
    {
        public IEnumerator<HidDevice> GetEnumerator()
        {
            return new HidDeviceEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new HidDeviceEnumerator();
        }
    }

    private sealed class HidDeviceEnumerator : IEnumerator<HidDevice>
    {
        private Guid hidClassGuid;

        private HidDevice? current;
        private uint deviceIndex, deviceInterfaceIndex;

        private IntPtr deviceInfoSet;

        public HidDeviceEnumerator()
        {
            HidD_GetHidGuid(out hidClassGuid);

            deviceInfoSet = SetupDiGetClassDevs(
                ref hidClassGuid,
                null,
                hwndParent: IntPtr.Zero,
                DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

            Reset();
        }

        ~HidDeviceEnumerator()
        {
            Dispose(disposing: false);
        }

        void IDisposable.Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        object System.Collections.IEnumerator.Current => Current;

        public HidDevice Current => current ?? throw new InvalidOperationException();

        public bool MoveNext()
        {
            var deviceInfoData = new SP_DEVINFO_DATA();
            deviceInfoData.cbSize = Marshal.SizeOf(deviceInfoData);
            deviceInfoData.DevInst = 0;
            deviceInfoData.ClassGuid = Guid.Empty;
            deviceInfoData.Reserved = IntPtr.Zero;

            if (!SetupDiEnumDeviceInfo(deviceInfoSet, deviceIndex, ref deviceInfoData))
            {
                return false;
            }
            else
            {
                var deviceInterfaceData = new SP_DEVICE_INTERFACE_DATA();
                deviceInterfaceData.cbSize = Marshal.SizeOf(deviceInterfaceData);

                var isOk = SetupDiEnumDeviceInterfaces(
                    deviceInfoSet,
                    ref deviceInfoData,
                    ref hidClassGuid,
                    deviceInterfaceIndex,
                    ref deviceInterfaceData);

                if (!isOk)
                {
                    deviceInterfaceIndex = 0;
                    deviceIndex++;

                    return MoveNext();
                }
                else
                {
                    deviceInterfaceIndex++;

                    var bufferSize = 0;
                    var interfaceDetail = new SP_DEVICE_INTERFACE_DETAIL_DATA
                    {
                        cbSize = IntPtr.Size == 4 ? 4 + Marshal.SystemDefaultCharSize : 8
                    };

                    SetupDiGetDeviceInterfaceDetail(
                        deviceInfoSet,
                        ref deviceInterfaceData,
                        IntPtr.Zero,
                        0,
                        ref bufferSize,
                        IntPtr.Zero);

                    var isDetailOk = SetupDiGetDeviceInterfaceDetail(
                        deviceInfoSet,
                        ref deviceInterfaceData,
                        ref interfaceDetail,
                        bufferSize,
                        ref bufferSize,
                        IntPtr.Zero);

                    if (isDetailOk)
                    {
                        current = new HidDevice(interfaceDetail.DevicePath);
                        return true;
                    }
                    else
                    {
                        return MoveNext();
                    }
                }
            }
        }

        public void Reset()
        {
            current = null;
            deviceIndex = 0;
            deviceInterfaceIndex = 0;
        }

        private void Dispose(bool disposing)
        {
            if (deviceInfoSet != IntPtr.Zero)
            {
                SetupDiDestroyDeviceInfoList(deviceInfoSet);
                deviceInfoSet = IntPtr.Zero;
            }
        }
    }
}
