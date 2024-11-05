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

internal class TextResources : ITextResources
{
    protected readonly Dictionary<string, string> resources = new Dictionary<string, string>();

    public TextResources()
    {
        Load();
    }

    public string this[string resourceKey]
    {
        get
        {
            if (resources.TryGetValue(resourceKey, out var value))
            {
                return value;
            }
            else
            {
                return string.Empty;
            }
        }
    }

    public string For(PerformanceMode performanceMode)
    {
        switch (performanceMode)
        {
            case PerformanceMode.Performance:
                return this["PerformanceMode.Performance"];

            case PerformanceMode.Silent:
                return this["PerformanceMode.Silent"];

            case PerformanceMode.Turbo:
                return this["PerformanceMode.Turbo"];

            default:
                return string.Empty;
        }
    }

    public string For(PowerMode powerMode)
    {
        switch (powerMode)
        {
            case PowerMode.BestPowerEfficiency:
                return this["PowerMode.BestPowerEfficiency"];

            case PowerMode.Balanced:
                return this["PowerMode.Balanced"];

            case PowerMode.BestPerformance:
                return this["PowerMode.BestPerformance"];

            default:
                return string.Empty;
        }
    }

    public string For(NotificationType notificationType)
    {
        switch (notificationType)
        {
            case NotificationType.PerformanceMode:
                return this["Notifications.PerformanceMode"];

            case NotificationType.PowerMode:
                return this["Notifications.PowerMode"];

            case NotificationType.PowerSource:
                return this["Notifications.PowerSource"];

            case NotificationType.Boost:
                return this["Notifications.Boost"];

            case NotificationType.TouchPad:
                return this["Notifications.TouchPad"];

            case NotificationType.DisplayRefreshRate:
                return this["Notifications.DisplayRefreshRate"];

            case NotificationType.Mic:
                return this["Notifications.Mic"];

            case NotificationType.Gpu:
                return this["Notifications.Gpu"];

            default:
                return "";
        }
    }

    private void Load(string locale = "en")
    {
        resources["Links.HomePage"] = "https://github.com/albertakhmetov/flowOSD";
        resources["Links.License"] = "https://raw.githubusercontent.com/albertakhmetov/flowOSD/main/LICENSE";
        resources["Links.Optimization"] = "https://github.com/albertakhmetov/flowOSD/wiki/ASUS-Optimization";
        resources["Links.CustomFanCurves"] = "https://github.com/albertakhmetov/flowOSD/wiki/Custom-Fan-Curves";
        resources["Links.GitLatest"] = "https://github.com/albertakhmetov/flowOSD/releases/latest";
        resources["Links.NotebookMode"] = "https://github.com/albertakhmetov/flowOSD/wiki/Notebook-Mode";

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"flowOSD.Resources.Text.{locale}.json");

        if (stream == null)
        {
            throw new InvalidOperationException($"Can't load text resources for {locale} language");
        }

        var doc = JsonDocument.Parse(stream);
        Enumerate(string.Empty, doc.RootElement);
    }

    private void Enumerate(string key, JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var i in element.EnumerateObject())
        {
            if (i.Value.ValueKind == JsonValueKind.Object)
            {
                Enumerate(key + $"{i.Name}.", i.Value);
            }
            else if (i.Value.ValueKind == JsonValueKind.String)
            {
                resources.Add(key + $"{i.Name}", i.Value.GetString() ?? string.Empty);
            }
        }
    }
}