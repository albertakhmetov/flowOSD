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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static flowOSD.Native.Atiadlxx;

sealed class AmdGpu : IDisposable
{
    private const int AMD_VENDOR_ID = 1002;

    private IntPtr context;

    public AmdGpu()
    {
        try
        {
            ADL2_Main_Control_Create(size => Marshal.AllocCoTaskMem(size), 1, out context);
            IsSupported = true;
        }
        catch(DllNotFoundException)
        {
            IsSupported = false;
        }
    }

    ~AmdGpu()
    {
        Dispose(false);
    }

    public bool IsSupported { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public bool SetVariBright(bool enabled)
    {
        if (!IsSupported || context == IntPtr.Zero)
        {
            return false;
        }

        var iGpu = FindByType(INTEGRATED);
        if (iGpu is null)
        {
            return false;
        }
        if (enabled == GetVariBright(iGpu))
        {
            return true;
        }
        else
        {
            return ADL2_Adapter_VariBrightEnable_Set(context, iGpu.Value.AdapterIndex, enabled ? 1 : 0) == SUCCESS;
        }
    }

    public bool? GetVariBright()
    {
        if (!IsSupported || context == IntPtr.Zero)
        {
            return false;
        }

        var iGpu = FindByType(INTEGRATED);

        return GetVariBright(iGpu);
    }

    private bool? GetVariBright(AdapterInfo? adapterInfo)
    {
        if (adapterInfo == null || ADL2_Adapter_VariBright_Caps(
            context,
            adapterInfo.Value.AdapterIndex,
            out int supported,
            out int enabled,
            out _) != SUCCESS)
        {
            return null;
        }

        return supported > 0 ? enabled == 3 : null;
    }

    private AdapterInfo? FindByType(int type)
    {
        ADL2_Adapter_NumberOfAdapters_Get(context, out int numberOfAdapters);
        if (numberOfAdapters <= 0)
        {
            return null;
        }

        var adapterInfoData = new AdapterInfoArray();
        var buffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(adapterInfoData));
        try
        {
            Marshal.StructureToPtr(adapterInfoData, buffer, false);
            if (ADL2_Adapter_AdapterInfo_Get(
                context,
                buffer,
                Marshal.SizeOf(adapterInfoData)) != SUCCESS)
            {
                return null;
            }

            adapterInfoData = Marshal.PtrToStructure<AdapterInfoArray>(buffer);
        }
        finally
        {
            Marshal.FreeCoTaskMem(buffer);
        }

        var adapterInfo = adapterInfoData.AdapterInfo
            .Where(x => x.Exist != 0 && x.Present != 0 && x.VendorID == AMD_VENDOR_ID)
            .FirstOrDefault(x =>
            {
                if (ADL2_Adapter_ASICFamilyType_Get(
                    context,
                    x.AdapterIndex,
                    out int asicTypes,
                    out int valids) != SUCCESS)
                {
                    return false;
                }

                return ((asicTypes & valids) & type) == type;
            });

        return adapterInfo.Exist == 0 ? null : adapterInfo;
    }

    private void Dispose(bool isDisposing)
    {
        if (IsSupported && context != IntPtr.Zero)
        {
            ADL2_Main_Control_Destroy(context);
            context = IntPtr.Zero;
        }
    }
}
