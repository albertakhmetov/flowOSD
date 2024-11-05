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

namespace flowOSD.Native;

static class Hid
{
    [StructLayout(LayoutKind.Sequential)]
    public struct HIDD_ATTRIBUTES
    {
        public int Size;
        public ushort VendorID;
        public ushort ProductID;
        public ushort VersionNumber;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HIDP_CAPS
    {
        public short Usage;
        public short UsagePage;
        public ushort InputReportByteLength;
        public ushort OutputReportByteLength;
        public ushort FeatureReportByteLength;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        public ushort[] Reserved;

        public ushort NumberLinkCollectionNodes;
        public ushort NumberInputButtonCaps;
        public ushort NumberInputValueCaps;
        public ushort NumberInputDataIndices;
        public ushort NumberOutputButtonCaps;
        public ushort NumberOutputValueCaps;
        public ushort NumberOutputDataIndices;
        public ushort NumberFeatureButtonCaps;
        public ushort NumberFeatureValueCaps;
        public ushort NumberFeatureDataIndices;
    }

    [DllImport(nameof(Hid), SetLastError = true)]
    public static extern void HidD_GetHidGuid(
        out Guid hudGuid);

    [DllImport(nameof(Hid), SetLastError = true)]
    public static extern bool HidD_GetAttributes(
        SafeHandle HidDeviceObject,
        ref HIDD_ATTRIBUTES Attributes);

    [DllImport(nameof(Hid), SetLastError = true)]
    public static extern bool HidD_GetPreparsedData(
        SafeHandle HidDeviceObject, 
        ref IntPtr PreparsedData);

    [DllImport(nameof(Hid), SetLastError = true)]
    public static extern bool HidD_FreePreparsedData(
        IntPtr PreparsedData);

    [DllImport(nameof(Hid), SetLastError = true)]
    public static extern bool HidP_GetCaps(
        IntPtr PreparsedData, 
        ref HIDP_CAPS Capabilities);

    [DllImport(nameof(Hid), SetLastError = true)]
    public static extern bool HidD_GetFeature(
        SafeHandle HidDeviceObject, 
        byte[] ReportBuffer, 
        int ReportBufferLength);

    [DllImport(nameof(Hid), SetLastError = true)]
    public static extern bool HidD_SetFeature(
        SafeHandle HidDeviceObject, 
        byte[] ReportBuffer, 
        int ReportBufferLength);
}
