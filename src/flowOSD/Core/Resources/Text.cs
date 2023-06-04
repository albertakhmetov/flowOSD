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

public sealed class Text
{
    public readonly static Text Instance = new Text();

    private Text()
    {
    }

    public MainSection Main { get; } = new MainSection();

    public ConfigSection Config { get; } = new ConfigSection();

    public CommandsSection Commands { get; } = new CommandsSection();

    public ErrorsSection Errors { get; } = new ErrorsSection();

    public WarningsSection Warnings { get; } = new WarningsSection();

    public ConfirmationsSection Confirmations { get; } = new ConfirmationsSection();

    public NotificationsSection Notifications { get; } = new NotificationsSection();

    public PerformanceModeSection PerformanceMode { get; } = new PerformanceModeSection();

    public PowerModeSection PowerMode { get; } = new PowerModeSection();

    public ChargerSection Charger { get; } = new ChargerSection();

    public UpdateSection Update { get; } = new UpdateSection();

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
        public string CpuBoost => "CPU Boost";

        public string HighRefreshRate => "High Refesh Rate";

        public string Gpu => "dGPU";

        public string TouchPad => "Touchpad";

        public string ShowApp => "Show {0}";

        public string CpuFanSpeed => "Cpu";

        public string GpuFanSpeed => "Gpu";
    }

    public sealed class ConfigSection
    {
        public string Title => "Settings";

        public GeneralSection General { get; } = new GeneralSection();

        public NotificationsSection Notifications { get; } = new NotificationsSection();

        public KeyboardSection Keyboard { get; } = new KeyboardSection();

        public MonitoringSection Monitoring { get; } = new MonitoringSection();

        public PerformanceSection Performance { get; } = new PerformanceSection();

        public TabletSection Tablet { get; } = new TabletSection();

        public BatterySection Battery { get; } = new BatterySection();

        public AboutSection About { get; } = new AboutSection();

        public sealed class GeneralSection
        {
            public string Title => "General";

            public string OptimizationTitle => "Limited functionality";

            public string OptimizationMessage => "ASUS Optimization service is running. Some functionality is disabled. Turn off ASUS Optimization service to enable it.";

            public string OptimizationMore => "View details";

            public string RunAtStartup => "Run at logon";

            public string ControlDisplayRefreshRate => "Control display refresh rate";

            public string ConfirmGpuModeChange => "Confirm GPU change";
        }

        public sealed class NotificationsSection
        {
            public string Title => "Notifications";

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

        public sealed class KeyboardSection
        {
            public string Title => "Keyboard";

            public string BacklightTimeout => "Backlight timeout";

            public string BacklightWithDisplay => "Turn off backlight if laptop display is off";

            public string HotKeys => "HotKeys";

            public string HotKeyAction => "Action";
        }

        public sealed class MonitoringSection
        {
            public string Title => "Monitoring";

            public string ShowBatteryChargeRate => "Show battery charge rate";

            public string ShowCpuTemperature => "Show CPU temperature";

            public string ShowFanSpeed => "Show Fan Speeds";
        }

        public sealed class PerformanceSection
        {
            public string Title => "Performance";

            public string PowerLimits => "Power Limits";

            public string NoProfileTitle => "Performance profiles";

            public string NoProfileMessage => "Create a custom profile to configure CPU power limit and fan curves.";

            public string UserProfileName => "User Profile";

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
            public string Title = "Tablet";

            public string DisableTouchPadInTabletMode => "Disable TouchPad in tablet mode";

            public string Profile => "Performance profile in tablet mode";
        }

        public sealed class BatterySection
        {
            public string Title = "Battery";

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
            public string Title => "About";

            public string HomePage => "Home page";

            public string Version => "Version";

            public string Runtime => "Runtime";

            public string Device => "Device";

            public string Update => "Update";

            public string CheckForUpdates => "Check for updates at startup";
        }
    }

    public sealed class ErrorsSection
    {
        public string Title = "Error";

        public string CriticalTitle => "Critical Error";

        public string Unhandled => "Unhandled application exception";

        public string Initialization => "An exception has occured during initialization";

        public string GpuToggleUI => "Error is occurred while toggling GPU (UI).";

        public string TouchPadToggleUI => "Error is occurred while toggling TouchPad state (UI).";

        public string BoostToggleUI => "Error is occurred while toggling CPU boost mode (UI).";

        public string DisplayRefreshRateToggleUI => "Error is occurred while toggling display refresh rate (UI).";

        public string MicToggle => "Error is occurred while toggling microphone state.";

        public string DisplayRefreshRateIsNotSupported = "Selected refresh rate ({0}) isn't supported.";

        public string RestartIsRequired = "Restart is required.";

        public string CanNotChangeDisplayRefreshRate = "Can't change display refresh rate. Error code: {0:X}.";

        public string CanNotConnectToBattery = "Can't connect to battery device.";

        public string CanNotConnectToAcpi = "Can't connect to ACPI.";

        public string CanNotFoundAsusOptimizationKey = "Registry Key for ASUS Optimization was not found.";

        public string CanNotReadAsusOptimizationBacklight = "Can't read the keyboard backlight value from Registry.";

        public string ConfigIsCorrupted = "Config file is corrupted";

        public string CanNotWriteStartupKey = "Can't write to Windows registry";

        public string CanNotRetriveAppPath = "Can't retrive app exe path";

        public string ProductNameIsNotSet = "Product Name isn't set";

        public string ProductVersionIsNotSet = "Product Version isn't set";

        public string CanNotConnectToHid = "Can't connect to HID";

        public string CanNotResolve = "Can't resolve {0}";
    }

    public sealed class WarningsSection
    {
        public string Title = "Warning";
    }

    public sealed class ConfirmationsSection
    {
        public string Title = "Confirmation";

        public string NewVersion => "New version is available. Show details?";
    }

    public sealed class NotificationsSection
    {
        public string MicOn => "On air";

        public string MicOff => "Muted";

        public string CpuBoost => "CPU Boost";

        public string HighRefreshRate => "High Refesh Rate";

        public string Gpu => "dGPU";

        public string TouchPad => "Touchpad";
    }

    public sealed class UpdateSection
    {
        public string CheckForUpdate => "Check for update by clicking 'Check for update'";

        public string CheckingForUpdate => "Checking for update...";

        public string Updated => "The latest version is installed";

        public string NewVersion => "New version is available";

        public string Downloading => "Update is downloading...";

        public string ReadyToInstall => "Update is downloaded and ready to install";

        public string ViewReleaseNotes => "View Release Notes";
    }

    public sealed class CommandsSection
    {
        public GpuSection Gpu { get; } = new GpuSection();

        public TouchPadSection TouchPad { get; } = new TouchPadSection();

        public BoostSection Boost { get; } = new BoostSection();

        public SuspendSection Suspend { get; } = new SuspendSection();

        public PowerModeSection PowerMode { get; } = new PowerModeSection();

        public PerformanceModeSection Performance { get; } = new PerformanceModeSection();

        public ConfigSection Config { get; } = new ConfigSection();

        public DisplayBrightnessSection DisplayBrightness { get; } = new DisplayBrightnessSection();

        public DisplayRefreshRateSection DisplayRefreshRate { get; } = new DisplayRefreshRateSection();

        public ExitSection Exit { get; } = new ExitSection();

        public KeyboardBacklightSection KeyboardBacklight { get; } = new KeyboardBacklightSection();

        public MicrophoneSection Microphone { get; } = new MicrophoneSection();

        public UpdateSection Update { get; } = new UpdateSection();

        public sealed class GpuSection
        {
            public string TurnOffConfirmation => "Do you want to turn off dGPU?";

            public string TurnOnConfirmation => "Do you want to turn on dGPU?";

            public string Disable => "Disable dGPU";

            public string Enable => "Enable dGPU";

            public string Description => "Toggle dGPU";
        }

        public sealed class TouchPadSection
        {
            public string Disable => "Disable TouchPad";

            public string Enable => "Enable TouchPad";

            public string Description => "Toggle TouchPad";
        }

        public sealed class BoostSection
        {
            public string Disable => "Disable Boost";

            public string Enable => "Enable Boost";

            public string Description => "Toggle CPU Boost Mode";
        }

        public sealed class SuspendSection
        {
            public string Hibernate => "Hibernate";

            public string Sleep => "Sleep";

            public string Description => "Suspend";
        }

        public sealed class PowerModeSection
        {
            public string Description => "Toggle Power Mode";
        }

        public sealed class PerformanceModeSection
        {
            public string Description => "Toggle Performance Mode";
        }

        public sealed class ConfigSection
        {
            public string Description => "Settings...";
        }

        public sealed class DisplayBrightnessSection
        {
            public string Description => "Display Brightness";

            public string Up => "Up";

            public string Down => "Down";
        }

        public sealed class DisplayRefreshRateSection
        {
            public string Description => "Toggle High Refresh Rate";

            public string Disable => "Disable High Refresh Rate";

            public string Enable => "Enable High Refresh Rate";
        }

        public sealed class ExitSection
        {
            public string Description => "Exit";
        }

        public sealed class KeyboardBacklightSection
        {
            public string Description => "Keyboard Backlight";

            public string Up => "Up";

            public string Down => "Down";
        }

        public sealed class MicrophoneSection
        {
            public string Description => "Toggle Microphone";
        }

        public sealed class UpdateSection
        {
            public string CheckForUpdate => "Check for update";

            public string DownloadUpdate => "Download update";

            public string CancelDownload => "Cancel";

            public string Install => "Install update";
        }
    }
}
