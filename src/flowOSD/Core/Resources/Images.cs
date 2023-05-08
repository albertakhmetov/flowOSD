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

public class Images
{
    public static readonly Images Instance = new Images();

    private Images()
    {
        Common = new CommonSection();
        Hardware = new HardwareSection();
        Power = new PowerSection();
        Performance = new PerformanceSection();
        Notification = new NotificationSection(this);
    }

    public CommonSection Common { get;  }

    public HardwareSection Hardware { get; }

    public PowerSection Power { get; }

    public PerformanceSection Performance { get; }

    public NotificationSection Notification { get; }

    public sealed class CommonSection
    {
        public string Info => "\uE946";

        public string Diagnostic => "\uE9D9";

        public string Home => "\uE80F";

        public string KeyboardSettings => "\uF210";

        public string Notification => "\uEC42";

        public string Game => "\ue7fc";

        public string Paste => "\ue77f";

        public string Copy => "\ue8c8";

        public string Airplane => "\ue709";

        public string Suspend => "\ue823";

        public string Updater => "\ue895";

        public string Temperature => "\ue9ca";

        public string Settings => "\ue713";
    }

    public sealed class HardwareSection
    {
        public string Battery => "\uF5FC";

        public string Tablet => "\uE70A";

        public string BrightnessDown => "\uec8a";

        public string BrightnessUp => "\ue706";

        public string AC => "\ue83e";

        public string DC => "\ue83f";

        public string Mic => "\ue720";

        public string MicMuted => "\uf781";

        public string KeyboardLightUp => "\ued39";

        public string KeyboardLightDown => "\ued3a";

        public string Cpu => "\ue950";

        public string Gpu => "\uf211";

        public string Screen => "\ue7f4";

        public string TouchPad => "\uefa5";

    }

    public sealed class PowerSection
    {
        public string BatterySaver => "\uebc0";

        public string BestPowerEfficiency => "\uec48";

        public string Balanced => "\uec49";

        public string BestPerformance => "\uec4a";
    }

    public sealed class PerformanceSection
    {
        public string Performance => "\ue945";

        public string Turbo => "\uECAD";

        public string Silent => "\uec0a";

        public string User => "\uEE57";
    }

    public sealed class NotificationSection
    {
        private Images root;

        public NotificationSection(Images root)
        {
            this.root = root ?? throw new ArgumentNullException(nameof(root));
        }

        public string PerformanceMode => root.Performance.Turbo;

        public string PowerMode => root.Power.Balanced;

        public string PowerSource => root.Hardware.AC;

        public string Boost => root.Hardware.Cpu;

        public string TouchPad => root.Hardware.TouchPad;

        public string DisplayRefreshRate => root.Hardware.Screen;

        public string Mic => root.Hardware.Mic;

        public string Gpu => root.Hardware.Gpu;
    }

    public string GetBatteryIcon(uint capacity, uint fullChargedCapacity, BatteryPowerState powerState)
    {
        var power = (powerState & BatteryPowerState.PowerOnLine) == BatteryPowerState.PowerOnLine
            ? 11
            : 0;
        var c = Math.Round((capacity * 10f) / fullChargedCapacity);

        return new string((char)(0xf5f2 + c + power), 1);
    }

    public string ToImage(PerformanceMode performanceMode)
    {
        switch (performanceMode)
        {
            case PerformanceMode.Default:
                return Performance.Performance;

            case PerformanceMode.Silent:
                return Performance.Silent;

            case PerformanceMode.Turbo:
                return Performance.Turbo;

            default:
                return "";
        }
    }

    public string ToImage(PowerMode powerMode)
    {
        switch (powerMode)
        {
            case PowerMode.BestPowerEfficiency:
                return Power.BestPowerEfficiency;

            case PowerMode.Balanced:
                return Power.Balanced;

            case PowerMode.BestPerformance:
                return Power.BestPerformance;

            default:
                return "";
        }
    }

    public string ToImage(NotificationType notificationType)
    {
        switch (notificationType)
        {
            case NotificationType.PerformanceMode:
                return Performance.Turbo;

            case NotificationType.PowerMode:
                return Power.Balanced;

            case NotificationType.PowerSource:
                return Hardware.AC;

            case NotificationType.Boost:
                return Hardware.Cpu;

            case NotificationType.TouchPad:
                return Hardware.TouchPad;

            case NotificationType.DisplayRefreshRate:
                return Hardware.Screen;

            case NotificationType.Mic:
                return Hardware.Mic;

            case NotificationType.Gpu:
                return Hardware.Gpu;

            default:
                return "";
        }
    }
}
