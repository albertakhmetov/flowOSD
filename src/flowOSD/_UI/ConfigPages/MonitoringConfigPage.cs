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
namespace flowOSD.UI.ConfigPages;

using flowOSD.Api;
using flowOSD.Api.Configs;
using flowOSD.Api.Hardware;
using flowOSD.UI.Components;
using System.Reactive.Disposables;

internal class MonitoringConfigPage : ConfigPageBase
{
    public MonitoringConfigPage(IConfig config, CxTabListener tabListener, ICpu? cpu)
        : base(config, tabListener)
    {
        Text = "Monitoring";

        AddConfig(
            "\uf5f2",
            "Show battery charge rate",
            nameof(CommonConfig.ShowBatteryChargeRate));

        if (cpu?.IsAvailable == true)
        {
            AddConfig(
                UIImages.Temperature,
                "Show CPU temperature",
                nameof(CommonConfig.ShowCpuTemperature));
        }
    }
}
