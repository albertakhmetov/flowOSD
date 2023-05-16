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

namespace flowOSD.Core.Resources;

using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;

public class Text
{
    public readonly static Text Instance = new Text();

    private Text()
    {
        PerformanceMode = new PerformanceModeSection();
        PowerMode = new PowerModeSection();
        Charger = new ChargerSection();
        
        Main = new MainSection();
        Config = new ConfigSection();

        Common = new CommonSection();
        Battery = new BatterySection();
        Notification = new NotificationSection();
        Performance = new PerformanceSection();
        Tablet = new TabletSection();
        About = new AboutSection();
    }

    public PerformanceModeSection PerformanceMode { get; }

    public PowerModeSection PowerMode { get; }

    public ChargerSection Charger { get; }

    public MainSection Main { get; }

    public ConfigSection Config { get; }

    public CommonSection Common { get; }

    public NotificationSection Notification { get; }

    public PerformanceSection Performance { get; }

    public TabletSection Tablet { get; }

    public BatterySection Battery { get; }

    public AboutSection About { get; }

    public sealed class PerformanceModeSection
    {
        public string Performance => "Performance";

        public string Silent => "Silent";

        public string Turbo => "Turbo";

        public string From(PerformanceMode performanceMode)
        {
            switch (performanceMode)
            {
                case Hardware.PerformanceMode.Default:
                    return Performance;

                case Hardware.PerformanceMode.Silent:
                    return Silent;

                case Hardware.PerformanceMode.Turbo:
                    return Turbo;

                default:
                    return string.Empty;
            }
        }
    }

    public sealed class PowerModeSection
    {
        public string BatterySaver => "Battery Saver is on";

        public string BestPowerEfficiency => "Power Effeciency";

        public string Balanced => "Balanced";

        public string BestPerformance => "Performance";

        public string From(PowerMode powerMode)
        {
            switch (powerMode)
            {
                case Hardware.PowerMode.BestPowerEfficiency:
                    return BestPowerEfficiency;

                case Hardware.PowerMode.Balanced:
                    return Balanced;

                case Hardware.PowerMode.BestPerformance:
                    return BestPerformance;

                default:
                    return string.Empty;
            }
        }
    }

    public sealed class ChargerSection
    {
        public string Battery => "Battery";

        public string Connected => "Plugged In";

        public string LowPower => "Plugged In (Low Power Charger)";
    }

    public sealed class MainSection
    {
        public string MicOn => "On air";

        public string MicOff => "Muted";

        public string CpuBoost => "CPU Boost";

        public string HighRefreshRate => "High Refesh Rate";

        public string Gpu => "dGPU";

        public string TouchPad => "Touchpad";
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

        public string From(NotificationType notificationType)
        {
            switch (notificationType)
            {
                case NotificationType.PerformanceMode:
                    return PerformanceMode;

                case NotificationType.PowerMode:
                    return PowerMode;

                case NotificationType.PowerSource:
                    return PowerSource;

                case NotificationType.Boost:
                    return Boost;

                case NotificationType.TouchPad:
                    return TouchPad;

                case NotificationType.DisplayRefreshRate:
                    return DisplayRefreshRate;

                case NotificationType.Mic:
                    return Mic;

                case NotificationType.Gpu:
                    return Gpu;

                default:
                    return "";
            }
        }
    }

    public sealed class PerformanceSection
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
