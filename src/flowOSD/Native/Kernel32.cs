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

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace flowOSD.Native;

static class Kernel32
{
    public const uint FILE_ATTRIBUTE_OVERLAPPED = 0x40000000;
    public const uint FILE_ATTRIBUTE_NORMAL = 0x80;

    public const Int32 INFINITE = -1;
    public const Int32 WAIT_ABANDONED = 0x80;
    public const Int32 WAIT_OBJECT_0 = 0x00;
    public const Int32 WAIT_TIMEOUT = 0x102;
    public const Int32 WAIT_FAILED = -1;

    [StructLayout(LayoutKind.Sequential)]
    public struct SECURITY_ATTRIBUTES
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        public bool bInheritHandle;
    }

    public enum BATTERY_QUERY_INFORMATION_LEVEL
    {
        BatteryInformation,
        BatteryGranularityInformation,
        BatteryTemperature,
        BatteryEstimatedTime,
        BatteryDeviceName,
        BatteryManufactureDate,
        BatteryManufactureName,
        BatteryUniqueID,
        BatterySerialNumber
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BATTERY_QUERY_INFORMATION
    {
        public uint BatteryTag;
        public BATTERY_QUERY_INFORMATION_LEVEL InformationLevel;
        public uint AtRate;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BATTERY_INFORMATION
    {
        public uint Capabilities;
        public byte Technology;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] Reserved;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] Chemistry;

        public uint DesignedCapacity;
        public uint FullChargedCapacity;
        public uint DefaultAlert1;
        public uint DefaultAlert2;
        public uint CriticalBias;
        public uint CycleCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BATTERY_WAIT_STATUS
    {
        public uint BatteryTag;
        public uint Timeout;
        public uint PowerState;
        public uint LowCapacity;
        public uint HighCapacity;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BATTERY_STATUS
    {
        public uint PowerState;
        public uint Capacity;
        public uint Voltage;
        public int Rate;
    }

    public enum EXECUTION_STATE : uint
    {
        ES_SYSTEM_REQUIRED = 0x00000001,
        ES_DISPLAY_REQUIRED = 0x00000002,
        ES_USER_PRESENT = 0x00000004,
        ES_AWAYMODE_REQUIRED = 0x00000040,
        ES_CONTINUOUS = 0x80000000
    }

    [DllImport(nameof(Kernel32), SetLastError = true)]
    public static extern uint GetTickCount();

    [DllImport("kernel32.dll")]
    public static extern IntPtr LocalFree(
        IntPtr hMem);

    [DllImport("kernel32.dll")]
    public static extern bool GetSystemPowerStatus(
        out SYSTEM_POWER_STATUS lpSystemPowerStatus);

    [DllImport(nameof(Kernel32), SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern SafeFileHandle CreateFile(
        string lpFileName,
        FileAccess dwDesiredAccess,
        FileShare dwShareMode,
        IntPtr lpSecurityAttributes,
        FileMode dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile
    );

    [DllImport(nameof(Kernel32), SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern SafeFileHandle CreateFile(
        string lpFileName,
        FileAccess dwDesiredAccess,
        FileShare dwShareMode,
        ref SECURITY_ATTRIBUTES lpSecurityAttributes,
        FileMode dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile
    );

    [DllImport(nameof(Kernel32), SetLastError = true)]
    public static extern bool CancelIoEx(IntPtr handle, IntPtr lpOverlapped);

    [DllImport(nameof(Kernel32), SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport(nameof(Kernel32), SetLastError = true)]
    public static extern bool DeviceIoControl(
        SafeHandle hDevice,
        uint dwIoControlCode,
        byte[] lpInBuffer,
        uint nInBufferSize,
        byte[] lpOutBuffer,
        uint nOutBufferSize,
        ref uint lpBytesReturned,
        IntPtr lpOverlapped
    );

    [DllImport(nameof(Kernel32), SetLastError = true)]
    public static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer,
        int nInBufferSize,
        IntPtr lpOutBuffer,
        int nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport(nameof(Kernel32), SetLastError = true)]
    public static extern bool SetSystemPowerState(
        bool fSuspend,
        bool fForce);

    [DllImport(nameof(Kernel32), SetLastError = true)]
    public static extern Int32 WaitForSingleObject(
        IntPtr Handle,
        Int32 Wait);

    [DllImport(nameof(Kernel32), SetLastError = true)]
    public static extern uint SetThreadExecutionState(EXECUTION_STATE esFlags);

}
