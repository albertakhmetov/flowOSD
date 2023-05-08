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

        Common = new CommonSection();
        Battery = new BatterySection();
        Notification = new NotificationSection();
        Tablet = new TabletSection();
        About = new AboutSection();
    }

    public MainSection Main { get; }

    public ConfigSection Config { get; }

    public CommonSection Common { get; }

    public NotificationSection Notification { get; }

    public TabletSection Tablet { get; }

    public BatterySection Battery { get; }

    public AboutSection About { get; }

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

        public string Performance => "Performance";

        public string Tablet = "Tablet";

        public string Battery = "Battery";

        public string About => "About";







        public string KeyboardBacklightTimeout => "Backlight timeout";

        public string KeyboardBacklightWithDisplay => "Turn off backlight if laptop display is off";

        public string HotKeyAction => "Action";

        public string ShowBatteryChargeRate => "Show battery charge rate";

        public string ShowCpuTemperature => "Show CPU temperature";

        public string UserProfileName => "User Profile";
    }

    public sealed class CommonSection
    {
        public string RunAtStartup => "Run at logon";

        public string ControlDisplayRefreshRate => "Control display refresh rate";

        public string ConfirmGpuModeChange => "Confirm GPU change";

        public string CheckForUpdates => "Check for updates at startup";
    }

    public sealed class NotificationSection
    {
        public string PerformanceMode => "Performance mode notifications";

        public string PowerMode => "Power mode notifications";

        public string PowerSource => "Power source notifications";

        public string Boost => "CPU boost mode notifications";

        public string TouchPad => "TouchPad state notifications";

        public string DisplayRefreshRate => "Display refresh rate notifications";

        public string Mic => "Microphone status notifications";

        public string Gpu => "dGPU notifications";
    }

    public sealed class PeformanceSection
    {
        public string NewProfileToolTip => "Create a new profile";

        public string RenameProfileToolTip => "Rename profile";

        public string RemoveProfileToolTip => "Remove profile";

        public string NewProfileNamePlaceholder => "Enter a new profile name";

        public string RemoveProfileConfirmation => "Remove the current profile?";

        public string CreateProfile => "Create Profile";

        public string RenameProfile => "Rename Profile";

        public string RemoveProfile => "Remove Profile";

        public string Cpu => "Cpu";

        public string Apu => "Apu";

        public string CpuFanCurve => "CPU Fan Curve";

        public string GpuFanCurve => "GPU Fan Curve";
    }

    public sealed class TabletSection
    {
        public string DisableTouchPadInTabletMode => "Disable TouchPad in tablet mode";

        public string Profile => "Performance profile in tablet mode";
    }

    public sealed class BatterySection
    {
        public string PluggedIn => "Plugged-in";

        public string Critical => "Critical";

        public string Charging => "Charging";

        public string Discharging => "Discharging";

        public string Name => "Name";

        public string ManufactureName => "Manufacture Name";

        public string Capacity => "Capacity";

        public string DesignedCapacity => "Designed Capacity";

        public string FullChargedCapacity => "Full Charged Capacity";

        public string WearPercentage => "Wear Percentage";

        public string EstimatedTime => "Estimated Time";

        public string BatteryChargeLimit => "Use Battery Charge Limit";

        public string BatteryChargeLimitDescription => "The battery charge will be limited by";
    }

    public sealed class AboutSection
    {
        public string HomePage => "Home page";

        public string Version => "Version";

        public string Runtime => "Runtime";

        public string Device => "Device";
    }
}
