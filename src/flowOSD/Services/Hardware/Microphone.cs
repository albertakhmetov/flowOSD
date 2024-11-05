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
using flowOSD.Core.Hardware;

namespace flowOSD.Services.Hardware;

sealed class Microphone : IMicrophone
{
    public bool IsMicMuted()
    {
        var masterVol = default(IAudioEndpointVolume);
        try
        {
            masterVol = GetMasterVolumeObject(EDataFlow.eCapture);
            if (masterVol == null)
            {
                return false;
            }

            masterVol.GetMute(out bool isMuted);
            return isMuted;
        }
        finally
        {
            if (masterVol != null)
            {
                Marshal.ReleaseComObject(masterVol);
            }
        }
    }

    public void Toggle()
    {
        var masterVol = default(IAudioEndpointVolume);
        try
        {
            masterVol = GetMasterVolumeObject(EDataFlow.eCapture);
            if (masterVol == null)
            {
                return;
            }

            masterVol.GetMute(out bool isMuted);
            masterVol.SetMute(!isMuted, Guid.Empty);
        }
        finally
        {
            if (masterVol != null)
            {
                Marshal.ReleaseComObject(masterVol);
            }
        }
    }

    private IAudioEndpointVolume GetMasterVolumeObject(EDataFlow dataFlow)
    {
        var deviceEnumerator = default(IMMDeviceEnumerator);
        var mic = default(IMMDevice);
        try
        {
            deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
            deviceEnumerator.GetDefaultAudioEndpoint(dataFlow, ERole.eMultimedia, out mic);

            Guid IID_IAudioEndpointVolume = typeof(IAudioEndpointVolume).GUID;
            mic.Activate(ref IID_IAudioEndpointVolume, 0, IntPtr.Zero, out object o);
            IAudioEndpointVolume masterVol = (IAudioEndpointVolume)o;

            return masterVol;
        }
        finally
        {
            if (mic != null) Marshal.ReleaseComObject(mic);
            if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
        }
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumerator
    {
    }

    private enum EDataFlow
    {
        eRender,
        eCapture,
        eAll,
        EDataFlow_enum_count
    }

    private enum ERole
    {
        eConsole,
        eMultimedia,
        eCommunications,
        ERole_enum_count
    }

    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        int NotImpl1();

        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppDevice);
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
    }

    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        int NotImpl1();

        int NotImpl2();

        int GetChannelCount(out uint channelCount);

        int SetMasterVolumeLevel(float level, Guid eventContext);

        int SetMasterVolumeLevelScalar(float level, Guid eventContext);

        int GetMasterVolumeLevel(out float level);

        int GetMasterVolumeLevelScalar(out float level);

        int SetChannelVolumeLevel(uint channelNumber, float level, Guid eventContext);

        int SetChannelVolumeLevelScalar(uint channelNumber, float level, Guid eventContext);

        int GetChannelVolumeLevel(uint channelNumber, out float level);

        int GetChannelVolumeLevelScalar(uint channelNumber, out float level);

        int SetMute(bool isMuted, Guid eventContext);

        int GetMute(out bool isMuted);

        int GetVolumeStepInfo(out uint step, out uint stepCount);
    }
}
