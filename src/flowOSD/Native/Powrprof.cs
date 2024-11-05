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

static class Powrprof
{
    public const int DEVICE_NOTIFY_CALLBACK = 0x2;

    public const int PBT_POWERSETTINGCHANGE = 0x8013;
    public const int PBT_APMRESUMEAUTOMATIC = 0x0012;
    public const int PBT_APMPOWERSTATUSCHANGE = 0x000A;
    public const int PBT_APMRESUMESUSPEND = 0x0007;
    public const int PBT_APMSUSPEND = 0x0004;

    public delegate int DEVICENOTIFYPROC(IntPtr context, int type, IntPtr setting);

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct POWERBROADCAST_SETTING
    {
        public Guid PowerSetting;
        public uint DataLength;
        public byte Data;
    }

    public enum EFFECTIVE_POWER_MODE
    {
        EffectivePowerModeBatterySaver,
        EffectivePowerModeBetterBattery,
        EffectivePowerModeBalanced,
        EffectivePowerModeHighPerformance,
        EffectivePowerModeMaxPerformance,
        EffectivePowerModeGameMode,
        EffectivePowerModeMixedReality
    };

    public delegate void EFFECTIVE_POWER_MODE_CALLBACK(EFFECTIVE_POWER_MODE Mode, IntPtr Context);

    [DllImport(nameof(Powrprof), SetLastError = true)]
    public static extern uint PowerSettingRegisterNotification(
        ref Guid settingGuid,
        uint flags,
        ref DEVICENOTIFYPROC recipient,
        ref IntPtr registrationHandle);

    [DllImport(nameof(Powrprof), SetLastError = true)]
    public static extern uint PowerSettingUnregisterNotification(
        IntPtr registrationHandle);

    [DllImport(nameof(Powrprof), SetLastError = true)]
    public static extern uint PowerRegisterSuspendResumeNotification(
        uint flags,
        ref DEVICENOTIFYPROC recipient,
        ref IntPtr registrationHandle    );

    [DllImport(nameof(Powrprof), SetLastError = true)]
    public static extern uint PowerUnregisterSuspendResumeNotification(
        IntPtr registrationHandle);

    [DllImport(nameof(Powrprof), SetLastError = true)]
    public static extern uint PowerGetActiveScheme(
        IntPtr RootPowerKey, 
        ref IntPtr SchemeGuid);

    [DllImport(nameof(Powrprof), SetLastError = true)]
    public static extern uint PowerSetActiveScheme(
        IntPtr RootPowerKey, 
        ref Guid SchemeGuid);

    [DllImport(nameof(Powrprof), SetLastError = true)]
    public static extern uint PowerReadACValueIndex(
        IntPtr RootPowerKey,
        ref Guid SchemeGuid,
        ref Guid SubGroupOfPowerSettingGuid,
        ref Guid PowerSettingGuid,
        ref uint AcValueIndex);

    [DllImport(nameof(Powrprof), SetLastError = true)]
    public static extern uint PowerReadDCValueIndex(
        IntPtr RootPowerKey,
        ref Guid SchemeGuid,
        ref Guid SubGroupOfPowerSettingGuid,
        ref Guid PowerSettingGuid,
        ref uint AcValueIndex);

    [DllImport(nameof(Powrprof), SetLastError = true)]
    public static extern uint PowerWriteACValueIndex(
        IntPtr RootPowerKey,
        ref Guid SchemeGuid,
        ref Guid SubGroupOfPowerSettingGuid,
        ref Guid PowerSettingGuid,
        uint AcValueIndex);

    [DllImport(nameof(Powrprof), SetLastError = true)]
    public static extern uint PowerWriteDCValueIndex(
        IntPtr RootPowerKey,
        ref Guid SchemeGuid,
        ref Guid SubGroupOfPowerSettingGuid,
        ref Guid PowerSettingGuid,
        uint AcValueIndex);

    [DllImport(nameof(Powrprof), SetLastError = true)]
    public static extern int PowerRegisterForEffectivePowerModeNotifications(
        uint Version,
        EFFECTIVE_POWER_MODE_CALLBACK Callback,
        IntPtr Context,
        out IntPtr RegistrationHandle);

    [DllImport(nameof(Powrprof), SetLastError = true)]
    public static extern int PowerUnregisterFromEffectivePowerModeNotifications(
        IntPtr registrationHandle);

    [DllImport(nameof(Powrprof), SetLastError = true)]
    public static extern uint PowerSetActiveOverlayScheme(
        Guid OverlaySchemeGuid);

    [DllImport(nameof(Powrprof), SetLastError = true)]
    public static extern uint PowerGetActualOverlayScheme(
        out Guid actualOverlayGuid);

    [DllImport(nameof(Powrprof), SetLastError = true)]
    public static extern uint PowerGetEffectiveOverlayScheme(
        out Guid effectiveOverlayGuid);

    [DllImport(nameof(Powrprof), SetLastError = true)]
    public static extern bool SetSuspendState(
        bool hiberate, 
        bool forceCritical,
        bool disableWakeEvent);

}
