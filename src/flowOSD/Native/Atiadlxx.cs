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

namespace flowOSD.Native;

using System.Runtime.InteropServices;


static class Atiadlxx
{
    private const int MAX_PATH = 256;
    private const int MAX_ADAPTERS = 40;

    public const int SUCCESS = 0x0;

    public const int DISCRETE = 1 << 0;
    public const int INTEGRATED = 1 << 1;

    [StructLayout(LayoutKind.Sequential)]
    public struct AdapterInfo
    {
        int Size;

        public int AdapterIndex;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
        public string UDID;
     
        public int BusNumber;
        
        public int DriverNumber;
        
        public int FunctionNumber;
        
        public int VendorID;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
        public string AdapterName;
    
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
        public string DisplayName;
        
        public int Present;
        
        public int Exist;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
        
        public string DriverPath;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
        public string DriverPathExt;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
        public string PNPString;
     
        public int OSDisplayIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AdapterInfoArray
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_ADAPTERS)]
        public AdapterInfo[] AdapterInfo;
    }

    public delegate nint ADL_Main_Memory_Alloc(int size);

    [DllImport(nameof(Atiadlxx))]
    public static extern int ADL2_Main_Control_Create(
        ADL_Main_Memory_Alloc callback, 
        int enumConnectedAdapters,
        out nint adlContextHandle);

    [DllImport(nameof(Atiadlxx))]
    public static extern int ADL2_Main_Control_Destroy(
        nint adlContextHandle);

    [DllImport(nameof(Atiadlxx))]
    public static extern int ADL2_Adapter_NumberOfAdapters_Get(
        IntPtr context,
        out int numAdapters);

    [DllImport(nameof(Atiadlxx))]
    public static extern int ADL2_Adapter_AdapterInfo_Get(
        IntPtr adlContextHandle, 
        IntPtr info,
        int inputSize);

    [DllImport(nameof(Atiadlxx))]
    public static extern int ADL2_Adapter_ASICFamilyType_Get(
        IntPtr adlContextHandle, int adapterIndex, 
        out int asicFamilyType, 
        out int asicFamilyTypeValids);

    [DllImport(nameof(Atiadlxx))]
    public static extern int ADL2_Adapter_VariBright_Caps(
        IntPtr context,
        int iAdapterIndex,
        out int iSupported,
        out int iEnabled,
        out int iVersion);

    [DllImport(nameof(Atiadlxx))]
    public static extern int ADL2_Adapter_VariBrightEnable_Set(
        IntPtr context,
        int iAdapterIndex,
        int iEnabled);

}
