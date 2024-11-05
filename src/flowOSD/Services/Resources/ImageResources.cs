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

namespace flowOSD.Services.Resources;

using System.Reflection;
using System.Text.Json;
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;

internal class ImageResources : TextResources, IImageResources
{
    public ImageResources()
    {
        Load();
    }

    public string GetBatteryIcon(uint capacity, uint fullChargedCapacity, BatteryPowerState powerState)
    {
        var power = (powerState & BatteryPowerState.PowerOnLine) == BatteryPowerState.PowerOnLine
            ? 11
            : 0;
        var c = Math.Min(10, Math.Round((capacity * 10f) / fullChargedCapacity));

        return new string((char)(0xf5f2 + c + power), 1);
    }

    private void Load()
    {
        resources["Common.Shield"] = "\uEA18";
        resources["Common.Info"] = "\uE946";
        resources["Common.Diagnostic"] = "\uE9D9";
        resources["Common.Home"] = "\uE80F";
        resources["Common.KeyboardSettings"] = "\uF210";
        resources["Common.Notification"] = "\uEC42";
        resources["Common.Game"] = "\ue7fc";
        resources["Common.Paste"] = "\ue77f";
        resources["Common.Copy"] = "\ue8c8";
        resources["Common.Airplane"] = "\ue709";
        resources["Common.Suspend"] = "\ue823";
        resources["Common.Updater"] = "\ue895";
        resources["Common.Temperature"] = "\ue9ca";
        resources["Common.Settings"] = "\ue713";
        resources["Common.Locked"] = "\uF809";
        resources["Common.Awake"] = "\uEA8F";

        resources["Hardware.Battery"] = "\uF5FC";
        resources["Hardware.Tablet"] = "\uE70A";
        resources["Hardware.BrightnessDown"] = "\uec8a";
        resources["Hardware.BrightnessUp"] = "\ue706";
        resources["Hardware.AC"] = "\ue83e";
        resources["Hardware.DC"] = "\ue83f";
        resources["Hardware.Mic"] = "\ue720";
        resources["Hardware.MicMuted"] = "\uf781";
        resources["Hardware.KeyboardLightUp"] = "\ued39";
        resources["Hardware.KeyboardLightDown"] = "\ued3a";
        resources["Hardware.Cpu"] = "\ue950";
        resources["Hardware.Gpu"] = "\uf211";
        resources["Hardware.Screen"] = "\ue7f4";
        resources["Hardware.TouchPad"] = "\uefa5";
        resources["Hardware.Notebook"] = "\uE7F8";
        resources["Hardware.NotebookShield"] = "\uF552";

        resources["PowerMode.BatterySaver"] = "\uebc0";
        resources["PowerMode.BestPowerEfficiency"] = "\uec48";
        resources["PowerMode.Balanced"] = "\uec49";
        resources["PowerMode.BestPerformance"] = "\uec4a";

        resources["PerformanceMode.Performance"] = "\ue945";
        resources["PerformanceMode.Turbo"] = "\uECAD";
        resources["PerformanceMode.Silent"] = "\uec0a";
        resources["PerformanceMode.User"] = "\uEE57";

        resources["Notifications.PerformanceMode"] = this["PerformanceMode.Performance"];
        resources["Notifications.PowerMode"] = this["PowerMode.Balanced"];
        resources["Notifications.PowerSource"] = this["Hardware.AC"];
        resources["Notifications.Boost"] = this["Hardware.Cpu"];
        resources["Notifications.TouchPad"] = this["Hardware.TouchPad"];
        resources["Notifications.DisplayRefreshRate"] = this["Hardware.Screen"];
        resources["Notifications.Mic"] = this["Hardware.Mic"];
        resources["Notifications.Gpu"] = this["Hardware.Gpu"];
        resources["Notifications.NotebookMode"] = this["Hardware.Notebook"];
        resources["Notifications.AwakeMode"] = this["Common.Awake"];
    }
}
