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

namespace flowOSD.Core.Hardware;

using System;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using flowOSD.Core.Resources;

public class PerformanceProfile
{
    public readonly static PerformanceProfile Default = new PerformanceProfile(
        Guid.Parse("{8ACA6E25-592B-49B7-8A9F-6612AD5B52C4}"),
        Text.Instance.PerformanceMode.Performance);

    public readonly static PerformanceProfile Turbo = new PerformanceProfile(
        Guid.Parse("{B0D2F613-FE12-4B77-9A51-1AB4CC9CE676}"),
        Text.Instance.PerformanceMode.Turbo);

    public readonly static PerformanceProfile Silent = new PerformanceProfile(
        Guid.Parse("{908F1186-ECCD-42A1-B581-D5E7F02DC385}"),
        Text.Instance.PerformanceMode.Silent);

    public PerformanceProfile(
        Guid id,
        string name,
        uint cpuLimit,
        uint apuLimit,
        IList<FanDataPoint> cpuFanCurve,
        IList<FanDataPoint> gpuFanCurve)
    {
        IsUserProfile = true;
        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        CpuLimit = cpuLimit;
        ApuLimit = apuLimit;
        CpuFanCurve = new ReadOnlyCollection<FanDataPoint>(cpuFanCurve);
        GpuFanCurve = new ReadOnlyCollection<FanDataPoint>(gpuFanCurve);
    }

    private PerformanceProfile(
        Guid id,
        string name)
    {
        IsUserProfile = false;
        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        CpuLimit = 0;
        ApuLimit = 0;
        CpuFanCurve = new FanDataPoint[0];
        GpuFanCurve = new FanDataPoint[0];
    }

    [JsonIgnore]
    public bool IsUserProfile { get; }

    public Guid Id { get; }

    public string Name { get; }

    public uint CpuLimit { get; }

    public uint ApuLimit { get; }

    public IList<FanDataPoint> CpuFanCurve { get; }

    public IList<FanDataPoint> GpuFanCurve { get; }

    public PerformanceProfile Rename(string profileName)
    {
        if (profileName == Name)
        {
            return this;
        }

        return new PerformanceProfile(
            Id,
            profileName,
            CpuLimit,
            ApuLimit,
            CpuFanCurve.ToArray(),
            GpuFanCurve.ToArray());
    }
}
