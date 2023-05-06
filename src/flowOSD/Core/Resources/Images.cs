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

public static class Images
{
    public static string GetBatteryIcon(uint capacity, uint fullChargedCapacity, BatteryPowerState powerState)
    {
        var power = (powerState & BatteryPowerState.PowerOnLine) == BatteryPowerState.PowerOnLine
            ? 11
            : 0;
        var c = Math.Round((capacity * 10f) / fullChargedCapacity);

        return new string((char)(0xf5f2 + c + power), 1);
    }

    public static string ToImage(PerformanceMode performanceMode)
    {
        switch (performanceMode)
        {
            case PerformanceMode.Default:
                return Performance_Default;

            case PerformanceMode.Silent:
                return Performance_Silent;

            case PerformanceMode.Turbo:
                return Performance_Turbo;

            default:
                return "";
        }
    }

    public static string ToImage(PowerMode powerMode)
    {
        switch (powerMode)
        {
            case PowerMode.BestPowerEfficiency:
                return Power_BestPowerEfficiency;

            case PowerMode.Balanced:
                return Power_Balanced;

            case PowerMode.BestPerformance:
                return Power_BestPerformance;

            default:
                return "";
        }
    }

    public static string ToImage(NotificationType notificationType)
    {
        switch (notificationType)
        {
            case NotificationType.PerformanceMode:
                return Performance_Turbo;

            case NotificationType.PowerMode:
                return Power_Balanced;

            case NotificationType.PowerSource:
                return Hardware_AC;

            case NotificationType.Boost:
                return Hardware_Cpu;

            case NotificationType.TouchPad:
                return Hardware_TouchPad;

            case NotificationType.DisplayRefreshRate:
                return Hardware_Screen;

            case NotificationType.Mic:
                return Hardware_Mic;

            case NotificationType.Gpu:
                return Hardware_Gpu;

            default:
                return "";
        }
    }

    public static string Info = "\uE946";

    public static string Diagnostic = "\uE9D9";

    public static string Home = "\uE80F";

    public static string KeyboardSettings = "\uF210";

    public static string Notification = "\uEC42";

    public static string Game = "\ue7fc";

    public static string Paste = "\ue77f";

    public static string Copy = "\ue8c8";

    public static string Airplane = "\ue709";

    public static string Suspend = "\ue823";

    public static string Updater = "\ue895";

    public static string Temperature = "\ue9ca";

    public static string Settings = "\ue713";

    public static string Hardware_Battery = "\uF5FC";

    public static string Hardware_Tablet = "\uE70A";

    public static string Hardware_BrightnessDown = "\uec8a";

    public static string Hardware_BrightnessUp = "\ue706";

    public static string Hardware_AC = "\ue83e";

    public static string Hardware_DC = "\ue83f";

    public static string Hardware_Mic = "\ue720";

    public static string Hardware_MicMuted = "\uf781";

    public static string Hardware_KeyboardLightUp = "\ued39";

    public static string Hardware_KeyboardLightDown = "\ued3a";

    public static string Hardware_Cpu => "\ue950";

    public static string Hardware_Gpu => "\uf211";

    public static string Hardware_Screen => "\ue7f4";

    public static string Hardware_TouchPad => "\uefa5";

    public static string Performance_Default => "\ue945";

    public static string Performance_Turbo => "\uECAD";

    public static string Performance_Silent => "\uec0a";

    public static string Performance_User => "\uEE57";

    public static string Power_BatterySaver => "\uebc0";

    public static string Power_BestPowerEfficiency => "\uec48";

    public static string Power_Balanced => "\uec49";

    public static string Power_BestPerformance => "\uec4a";

}
