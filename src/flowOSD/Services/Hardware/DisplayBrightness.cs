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

namespace flowOSD.Services.Hardware;

using System.Management;
using flowOSD.Core.Hardware;

sealed class DisplayBrightness : IDisplayBrightness
{
    private const uint D3DKMDT_VOT_INTERNAL = 0x80000000;

    public double GetLevel()
    {
        if (GetBrightness(GetInternalDisplayDeviceName(), out _, out _, out var value))
        {
            return value!.Value;
        }

        return 0;
    }

    public void SetLevel(double value)
    {
        var deviceName = GetInternalDisplayDeviceName();
        if (string.IsNullOrEmpty(deviceName) || !GetBrightness(deviceName, out var level, out var levels, out var oldValue))
        {
            return;
        }

        var newIndex = GetNewIndex(value, levels!);
        SetBrightness(deviceName, levels![newIndex]);
    }

    public void LevelUp()
    {
        var deviceName = GetInternalDisplayDeviceName();
        if (string.IsNullOrEmpty(deviceName) || !GetBrightness(deviceName, out _, out var levels, out var oldValue))
        {
            return;
        }

        var newIndex = GetNewIndex(oldValue!.Value + 0.1, levels!);
        SetBrightness(deviceName, levels![newIndex]);
    }

    public void LevelDown()
    {
        var deviceName = GetInternalDisplayDeviceName();
        if (string.IsNullOrEmpty(deviceName) || !GetBrightness(deviceName, out _, out var levels, out var oldValue))
        {
            return;
        }

        var newIndex = GetNewIndex(oldValue!.Value - 0.1, levels!);
        SetBrightness(deviceName, levels![newIndex]);
    }

    private static int GetNewIndex(double value, byte[] levels)
    {
        var newValue = Math.Max(0, Math.Min(1, Math.Round(value * 10) / 10));
        var newIndex = (int)Math.Round((levels!.Length - 1) * newValue);
        return newIndex;
    }

    private bool GetBrightness(string? deviceName, out byte? level, out byte[]? levels, out double? value)
    {
        if (!string.IsNullOrEmpty(deviceName))
        {
            var searcher = new ManagementObjectSearcher("root\\wmi", "SELECT * FROM WmiMonitorBrightness");
            foreach (var i in searcher.Get())
            {
                if (i.Properties["InstanceName"].Value as string != deviceName)
                {
                    continue;
                }

                if (i.Properties["CurrentBrightness"].Value is byte && i.Properties["Level"].Value is byte[])
                {
                    level = (byte)i.Properties["CurrentBrightness"].Value;
                    levels = (byte[])i.Properties["Level"].Value;

                    value = 1d * Array.IndexOf(levels!, level) / (levels!.Length - 1);

                    return true;
                }

            }
        }

        level = null;
        levels = null;
        value = null;

        return false;
    }

    private void SetBrightness(string deviceName, byte level)
    {
        if (!string.IsNullOrEmpty(deviceName))
        {
            using var searcher = new ManagementObjectSearcher("root\\wmi", "SELECT * FROM WmiMonitorBrightnessMethods");
            foreach (ManagementObject i in searcher.Get())
            {
                if (i.Properties["InstanceName"].Value as string != deviceName)
                {
                    continue;
                }

                i.InvokeMethod("WmiSetBrightness", new object[] { uint.MaxValue, level });
                return;
            }
        }
    }

    private string? GetInternalDisplayDeviceName()
    {
        var searcher = new ManagementObjectSearcher("root\\wmi", "SELECT * FROM WmiMonitorConnectionParams");
        foreach (var i in searcher.Get())
        {
            if (i.Properties["VideoOutputTechnology"].Value is uint videoOutputTechnology
                && (videoOutputTechnology & D3DKMDT_VOT_INTERNAL) == D3DKMDT_VOT_INTERNAL)
            {
                return i.Properties["InstanceName"].Value as string;
            }
        }

        return null;
    }
}

