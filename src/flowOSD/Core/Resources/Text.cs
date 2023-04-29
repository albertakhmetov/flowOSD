/*  Copyright © 2021-2023, Albert Akhmetov <akhmetov@live.com>   
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
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;

namespace flowOSD.Core.Resources;

public class Text
{
    public readonly static Text Instance = new Text();

    public static string ToText(PerformanceMode performanceMode)
    {
        switch (performanceMode)
        {
            case PerformanceMode.Silent:
                return "Silent";

            case PerformanceMode.Default:
                return "Default";

            case PerformanceMode.Turbo:
                return "Turbo";

            default:
                return "";
        }
    }

    public static string ToText(PowerMode powerMode)
    {
        switch (powerMode)
        {
            case PowerMode.BestPowerEfficiency:
                return "Power Effeciency";

            case PowerMode.Balanced:
                return "Balanced";

            case PowerMode.BestPerformance:
                return "Performance";

            default:
                return "";
        }
    }

    public static string ToText(NotificationType notificationType)
    {
        switch (notificationType)
        {
            case NotificationType.PerformanceMode:
                return "Performance mode notifications";

            case NotificationType.PowerMode:
                return "Power mode notifications";

            case NotificationType.PowerSource:
                return "Power source notifications";

            case NotificationType.Boost:
                return "CPU boost mode notifications";

            case NotificationType.TouchPad:
                return "TouchPad state notifications";

            case NotificationType.DisplayRefreshRate:
                return "Display refresh rate notifications";

            case NotificationType.Mic:
                return "Microphone status notifications";

            case NotificationType.Gpu:
                return "dGPU notifications";

            default:
                return "";
        }
    }

    private Text()
    {
        Main = new MainSection();
        Config = new ConfigSection();
    }

    public MainSection Main { get; }

    public ConfigSection Config { get; }

    public sealed class MainSection
    {
        public string CpuBoost => "CPU Boost";

        public string HighRefreshRate => "High Refesh Rate";

        public string Gpu => "dGPU";

        public string TouchPad => "Touchpad";

        public string BatterySaver => "Battery Saver is on";
    }

    public sealed class ConfigSection
    {
        public string Title => "Settings";

        public string General => "General";

        public string Notifications => "Notifications";

        public string Keyboard => "Keyboard";

        public string HotKeys => "HotKeys";

        public string Monitoring => "Monitoring";

        public string Profiles => "Profiles";

        public string About => "About";

        public string RunAtStartup => "Run at logon";

        public string DisableTouchPadInTabletMode => "Disable TouchPad in tablet mode";

        public string ControlDisplayRefreshRate => "Control display refresh rate";

        public string ConfirmGpuModeChange => "Confirm GPU change";

        public string CheckForUpdates => "Check for updates at startup";

        public string KeyboardBacklightTimeout => "Backlight timeout";

        public string KeyboardBacklightWithDisplay => "Turn off backlight if laptop display is off";

        public string HotKeyAction => "Action";

        public string ShowBatteryChargeRate => "Show battery charge rate";

        public string ShowCpuTemperature => "Show CPU temperature";
    }
}
