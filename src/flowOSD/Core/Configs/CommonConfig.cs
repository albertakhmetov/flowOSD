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

namespace flowOSD.Core.Configs;

using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using flowOSD.Core.Hardware;

public sealed class CommonConfig : ConfigBase
{
    private bool runAtStartup;
    private bool disableTouchPadInTabletMode;
    private bool controlDisplayRefreshRate;
    private bool tabletWithHighDisplayRefreshRate;
    private bool confirmGpuModeChange;
    private bool disableVariBright;

    private bool checkForUpdates;

    private KeyboardBacklightLevel keyboardBacklightLevel;
    private int keyboardBacklightTimeout;
    private bool keyboardBacklightWithDisplay;

    private bool showBatteryChargeRate;
    private bool showCpuTemperature;
    private bool showFanSpeed;

    private bool useBatteryChargeLimit;
    private uint batteryChargeLimit;
    private bool forceCustomFanCurves;

    private bool useMockMode;

    public CommonConfig()
    {
        // Default values

        controlDisplayRefreshRate = true;
        tabletWithHighDisplayRefreshRate = true;
        disableTouchPadInTabletMode = true;
        confirmGpuModeChange = true;
        checkForUpdates = true;

        keyboardBacklightLevel = KeyboardBacklightLevel.Low;
        keyboardBacklightTimeout = 60;
        keyboardBacklightWithDisplay = true;

        showBatteryChargeRate = true;
        showCpuTemperature = true;
        showFanSpeed = true;

        useBatteryChargeLimit = false;
        batteryChargeLimit = 80;
        forceCustomFanCurves = false;

        useMockMode = false;
        disableVariBright = true;
    }

    [JsonIgnore]
    public bool RunAtStartup
    {
        get => runAtStartup;
        set => SetProperty(ref runAtStartup, value);
    }

    public bool CheckForUpdates
    {
        get => checkForUpdates;
        set => SetProperty(ref checkForUpdates, value);
    }

    public bool DisableTouchPadInTabletMode
    {
        get => disableTouchPadInTabletMode;
        set => SetProperty(ref disableTouchPadInTabletMode, value);
    }

    public bool ControlDisplayRefreshRate
    {
        get => controlDisplayRefreshRate;
        set => SetProperty(ref controlDisplayRefreshRate, value);
    }

    public bool TabletWithHighDisplayRefreshRate
    {
        get => tabletWithHighDisplayRefreshRate;
        set => SetProperty(ref tabletWithHighDisplayRefreshRate, value);
    }    

    public bool ConfirmGpuModeChange
    {
        get => confirmGpuModeChange;
        set => SetProperty(ref confirmGpuModeChange, value);
    }

    public bool DisableVariBright
    {
        get => disableVariBright;
        set => SetProperty(ref disableVariBright, value);
    }

    public KeyboardBacklightLevel KeyboardBacklightLevel
    {
        get => keyboardBacklightLevel;
        set => SetProperty(ref keyboardBacklightLevel, value);
    }

    public int KeyboardBacklightTimeout
    {
        get => keyboardBacklightTimeout;
        set => SetProperty(ref keyboardBacklightTimeout, value);
    }

    public bool KeyboardBacklightWithDisplay
    {
        get => keyboardBacklightWithDisplay;
        set => SetProperty(ref keyboardBacklightWithDisplay, value);
    }

    public bool ShowBatteryChargeRate
    {
        get => showBatteryChargeRate;
        set => SetProperty(ref showBatteryChargeRate, value);
    }

    public bool ShowCpuTemperature
    {
        get => showCpuTemperature;
        set => SetProperty(ref showCpuTemperature, value);
    }

    public bool ShowFanSpeed
    {
        get => showFanSpeed;
        set => SetProperty(ref showFanSpeed, value);
    }

    public bool UseBatteryChargeLimit
    {
        get => useBatteryChargeLimit;
        set => SetProperty(ref useBatteryChargeLimit, value);
    }

    public uint BatteryChargeLimit
    {
        get => batteryChargeLimit;
        set => SetProperty(ref batteryChargeLimit, value);
    }

    public bool ForceCustomFanCurves
    {
        get => forceCustomFanCurves;
        set => SetProperty(ref forceCustomFanCurves, value);
    }

    public bool UseMockMode
    {
        get => useMockMode;
        set => SetProperty(ref useMockMode, value);
    }
}