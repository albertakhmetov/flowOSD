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
using flowOSD.Core.Hardware;
using flowOSD.Extensions;
using flowOSD.Native;
using static Native.Kernel32;
using static Native.Powrprof;

sealed partial class PowerManagement : IDisposable, IPowerManagement
{
    private static Guid GUID_ACDC_POWER_SOURCE = new Guid("5D3E9A59-E9D5-4B00-A6BD-FF34FF516548");
    private static Guid GUID_SESSION_USER_PRESENCE = new Guid("3C0F4548-C03F-4c4d-B9F2-237EDE686376");
    private static Guid GUID_SESSION_DISPLAY_STATUS = new Guid("2B84C20E-AD23-4ddf-93DB-05FFBD7EFCA5");

    private static Guid PROCESSOR_SUBGROUP = new Guid("54533251-82be-4824-96c1-47b60b740d00");
    private static Guid BOOST_SETTING = new Guid("be337238-0d82-4146-a960-4f3749d470c7");

    private static Guid GUID_POWERSCHEME_PERSONALITY = new Guid("245d8541-3943-4422-b025-13A784F679B7");

    private static Guid OVERLAY_BETTER_BATTERY = new Guid("961cc777-2547-4f9d-8174-7d86181b8a7a");
    private static Guid OVERLAY_BETTER_PERFORMANCE = new Guid("3af9B8d9-7c97-431d-ad78-34a8bfea439f");
    private static Guid OVERLAY_BEST_PERFORMANCE = new Guid("ded574b5-45a0-4f42-8737-46345c09c238");


    private CompositeDisposable? disposable = new CompositeDisposable();

    private BehaviorSubject<bool> isBoostSubject, isBatterySaverSubject;
    private BehaviorSubject<PowerSource> powerSourceSubject;
    private BehaviorSubject<PowerMode> powerModeSubject;
    private Subject<PowerEvent> powerEventSubject;

    private Guid activeScheme;

    public PowerManagement()
    {
        UpdateActiveScheme();

        if (!GetSystemPowerStatus(out SYSTEM_POWER_STATUS powerStatus))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var powerSource = powerStatus.ACLineStatus == 0
            ? Core.Hardware.PowerSource.Battery
            : Core.Hardware.PowerSource.Charger;

        powerSourceSubject = new BehaviorSubject<PowerSource>(powerSource);

        var isBoostEnabled = ReadValueIndex(ref PROCESSOR_SUBGROUP, ref BOOST_SETTING) != 0;
        isBoostSubject = new BehaviorSubject<bool>(isBoostEnabled);

        isBatterySaverSubject = new BehaviorSubject<bool>(powerStatus.SystemStatusFlag == 1);

        powerModeSubject = new BehaviorSubject<PowerMode>(GetCurrentPowerMode());
        powerEventSubject = new Subject<PowerEvent>();

        IsBoost = isBoostSubject.AsObservable();
        PowerSource = powerSourceSubject.AsObservable();

        IsBatterySaver = isBatterySaverSubject.AsObservable();

        PowerMode = powerModeSubject.AsObservable();
        PowerEvent = powerEventSubject.AsObservable();

        new PowerSettingSubscription(BOOST_SETTING, HandlerCallback).DisposeWith(disposable);
        new PowerSettingSubscription(GUID_ACDC_POWER_SOURCE, HandlerCallback).DisposeWith(disposable);
        new PowerSettingSubscription(GUID_SESSION_DISPLAY_STATUS, HandlerCallback).DisposeWith(disposable);

        new PowerModeSubscription(PowerModeCallback).DisposeWith(disposable);
        new PowerSuspendResumeSubscription(HandlerCallback).DisposeWith(disposable);
    }

    public IObservable<bool> IsBoost { get; }

    public IObservable<PowerSource> PowerSource { get; }

    public IObservable<bool> IsBatterySaver { get; }

    public IObservable<PowerMode> PowerMode { get; }

    public IObservable<PowerEvent> PowerEvent { get; }

    public void ToggleBoost()
    {
        if (isBoostSubject.Value)
        {
            DisableBoost();
        }
        else
        {
            EnableBoost();
        }
    }

    public void EnableBoost()
    {
        if (isBoostSubject.Value)
        {
            return;
        }

        WriteValueIndex(ref PROCESSOR_SUBGROUP, ref BOOST_SETTING, 2);
        PowerSetActiveScheme(IntPtr.Zero, ref activeScheme);
    }

    public void DisableBoost()
    {
        if (!isBoostSubject.Value)
        {
            return;
        }

        WriteValueIndex(ref PROCESSOR_SUBGROUP, ref BOOST_SETTING, 0);
        PowerSetActiveScheme(IntPtr.Zero, ref activeScheme);
    }

    public void SetPowerMode(PowerMode powerMode)
    {
        switch (powerMode)
        {
            case Core.Hardware.PowerMode.BestPerformance:
                PowerSetActiveOverlayScheme(OVERLAY_BEST_PERFORMANCE);
                break;

            case Core.Hardware.PowerMode.Balanced:
                PowerSetActiveOverlayScheme(Guid.Empty);
                break;

            case Core.Hardware.PowerMode.BestPowerEfficiency:
                PowerSetActiveOverlayScheme(OVERLAY_BETTER_BATTERY);
                break;
        }
    }

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    private PowerMode GetCurrentPowerMode()
    {
        PowerGetActualOverlayScheme(out Guid actualOverlayGuid);
        if (actualOverlayGuid == OVERLAY_BEST_PERFORMANCE)
        {
            return Core.Hardware.PowerMode.BestPerformance;
        }
        else if (actualOverlayGuid == OVERLAY_BETTER_PERFORMANCE || actualOverlayGuid == Guid.Empty)
        {
            return Core.Hardware.PowerMode.Balanced;
        }
        else if (actualOverlayGuid == OVERLAY_BETTER_BATTERY)
        {
            return Core.Hardware.PowerMode.BestPowerEfficiency;
        }

        throw new NotSupportedException();

    }

    private void UpdateActiveScheme()
    {
        IntPtr SchemeGuid = IntPtr.Zero;
        var errorCode = PowerGetActiveScheme(IntPtr.Zero, ref SchemeGuid);

        if (errorCode != 0)
        {
            throw new Win32Exception((int)errorCode);
        }
        try
        {
            activeScheme = Marshal.PtrToStructure<Guid>(SchemeGuid);
        }
        finally
        {
            LocalFree(SchemeGuid);
        }
    }

    private uint ReadValueIndex(ref Guid subgroup, ref Guid setting)
    {
        var value = default(uint);
        var errorCode = powerSourceSubject.Value == Core.Hardware.PowerSource.Battery
            ? PowerReadDCValueIndex(IntPtr.Zero, ref activeScheme, ref subgroup, ref setting, ref value)
            : PowerReadACValueIndex(IntPtr.Zero, ref activeScheme, ref subgroup, ref setting, ref value);

        if (errorCode != 0)
        {
            throw new Win32Exception((int)errorCode);
        }

        return value;
    }

    private void WriteValueIndex(ref Guid subgroup, ref Guid setting, uint value)
    {
        var errorCode = powerSourceSubject.Value == Core.Hardware.PowerSource.Battery
            ? PowerWriteDCValueIndex(IntPtr.Zero, ref activeScheme, ref subgroup, ref setting, value)
            : PowerWriteACValueIndex(IntPtr.Zero, ref activeScheme, ref subgroup, ref setting, value);

        if (errorCode != 0)
        {
            throw new Win32Exception((int)errorCode);
        }
    }

    private int HandlerCallback(IntPtr context, int eventType, IntPtr setting)
    {
        switch (eventType)
        {
            case PBT_APMSUSPEND:
                powerEventSubject.OnNext(Core.Hardware.PowerEvent.Suspend);
                break;

            case PBT_APMRESUMEAUTOMATIC:
                powerEventSubject.OnNext(Core.Hardware.PowerEvent.Resume);
                break;

            case PBT_POWERSETTINGCHANGE:
                var pbs = Marshal.PtrToStructure<POWERBROADCAST_SETTING>(setting);

                if (pbs.PowerSetting == BOOST_SETTING)
                {
                    isBoostSubject.OnNext(pbs.Data != 0);
                }

                if (pbs.PowerSetting == GUID_ACDC_POWER_SOURCE)
                {
                    var powerSource = pbs.Data == 1
                        ? Core.Hardware.PowerSource.Battery
                        : Core.Hardware.PowerSource.Charger;

                    powerSourceSubject.OnNext(powerSource);
                }

                if (pbs.PowerSetting == GUID_SESSION_DISPLAY_STATUS)
                {
                    switch (pbs.Data)
                    {
                        case 0:
                            powerEventSubject.OnNext(Core.Hardware.PowerEvent.DisplayOff);
                            break;

                        case 1:
                            powerEventSubject.OnNext(Core.Hardware.PowerEvent.DisplayOn);
                            break;

                        case 2:
                            powerEventSubject.OnNext(Core.Hardware.PowerEvent.DisplayDimmed);
                            break;
                    }
                }

                break;
        }

        return 0;
    }

    private void PowerModeCallback(EFFECTIVE_POWER_MODE Mode, IntPtr Context)
    {
        PowerMode powerMode;
        switch (Mode)
        {
            case EFFECTIVE_POWER_MODE.EffectivePowerModeBatterySaver:
                isBatterySaverSubject.OnNext(true);
                return;

            case EFFECTIVE_POWER_MODE.EffectivePowerModeBetterBattery:
                powerMode = Core.Hardware.PowerMode.BestPowerEfficiency;
                break;

            case EFFECTIVE_POWER_MODE.EffectivePowerModeBalanced:
                powerMode = Core.Hardware.PowerMode.Balanced;
                break;

            case EFFECTIVE_POWER_MODE.EffectivePowerModeMaxPerformance:
            case EFFECTIVE_POWER_MODE.EffectivePowerModeHighPerformance:
                powerMode = Core.Hardware.PowerMode.BestPerformance;
                break;

            default:
                return;
        }

        if (isBatterySaverSubject.Value)
        {
            isBatterySaverSubject.OnNext(false);
        }

        powerModeSubject.OnNext(powerMode);
    }

    private sealed class PowerSettingSubscription : IDisposable
    {
        private DEVICENOTIFYPROC callback;
        private IntPtr handle;

        public PowerSettingSubscription(Guid setting, DEVICENOTIFYPROC callback)
        {
            this.callback = callback;

            var errorCode = PowerSettingRegisterNotification(
                ref setting,
                DEVICE_NOTIFY_CALLBACK,
                ref this.callback,
                ref handle);

            if (errorCode != 0)
            {
                throw new Win32Exception((int)errorCode);
            }
        }

        ~PowerSettingSubscription()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (handle != IntPtr.Zero)
            {
                PowerSettingUnregisterNotification(handle);
                handle = IntPtr.Zero;
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    private sealed class PowerModeSubscription : IDisposable
    {
        private EFFECTIVE_POWER_MODE_CALLBACK callback;
        private IntPtr handle;

        public PowerModeSubscription(EFFECTIVE_POWER_MODE_CALLBACK callback)
        {
            this.callback = callback;

            var errorCode = PowerRegisterForEffectivePowerModeNotifications(
                1, // EFFECTIVE_POWER_MODE_V1
                this.callback,
                IntPtr.Zero,
                out handle);

            if (errorCode != 0)
            {
                throw new Win32Exception(errorCode);
            }
        }

        void IDisposable.Dispose()
        {
            if (handle != IntPtr.Zero)
            {
                PowerUnregisterFromEffectivePowerModeNotifications(handle);
                handle = IntPtr.Zero;
            }
        }
    }

    private sealed class PowerSuspendResumeSubscription : IDisposable
    {
        private DEVICENOTIFYPROC callback;
        private IntPtr handle;

        public PowerSuspendResumeSubscription(DEVICENOTIFYPROC callback)
        {
            this.callback = callback;

            var errorCode = PowerRegisterSuspendResumeNotification(
                DEVICE_NOTIFY_CALLBACK,
                ref this.callback,
                ref handle);

            if (errorCode != 0)
            {
                throw new Win32Exception((int)errorCode);
            }
        }

        ~PowerSuspendResumeSubscription()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (handle != IntPtr.Zero)
            {
                PowerUnregisterSuspendResumeNotification(handle);
                handle = IntPtr.Zero;
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}