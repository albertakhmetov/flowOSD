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

public interface IAtk
{
    IObservable<PerformanceMode> PerformanceMode { get; }

    IObservable<GpuMode> GpuMode { get; }

    IObservable<int> CpuTemperature { get; }

    uint MinBatteryChargeLimit { get; }

    uint MaxBatteryChargeLimit { get; }

    uint MinPowerLimit { get; }

    uint MaxPowerLimit { get; }

    int Get(uint deviceId);

    byte[] Get(uint deviceId, uint status);

    byte[] Set(uint deviceId, uint status);

    byte[] Set(uint deviceId, byte[] parameters);

    bool SetBatteryChargeLimit(uint value);

    bool SetCpuLimit(uint value);

    void SetPerformanceMode(PerformanceMode performanceMode);

    void SetGpuMode(GpuMode gpuMode);

    bool SetFanCurve(FanType fanType, IList<FanDataPoint> dataPoints);

    IList<FanDataPoint> GetFanCurve(FanType fanType, PerformanceMode performanceMode);
}