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

namespace flowOSD.Core.Hardware;

public sealed class FanDataPoint
{
    public static byte GetMinValue(int temperature)
    {
        if (temperature >= 90)
        {
            return 60;
        }
        else if (temperature >= 80)
        {
            return 50;
        }
        else if (temperature >= 70)
        {
            return 35;
        }
        else
        {
            return 0;
        }
    }

    public static FanDataPoint[] CreateDefaultCurve()
    {
        return new FanDataPoint[]
        {
            new FanDataPoint(20, 0),
            new FanDataPoint(30, 0),
            new FanDataPoint(40, 0),
            new FanDataPoint(50, 0),
            new FanDataPoint(60, 10),
            new FanDataPoint(70, 35),
            new FanDataPoint(80, 50),
            new FanDataPoint(90, 60),
        };
    }

    public FanDataPoint(byte temperature, byte value)
    {
        if (temperature > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(temperature));
        }

        if (value > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        Temperature = temperature;
        Value = value;
    }

    public byte Temperature { get; }

    public byte Value { get; }

    public override bool Equals(object? obj)
    {
        return obj is FanDataPoint point
            && point.Temperature == Temperature && point.Value == Value;
    }

    public override int GetHashCode()
    {
        int v = Temperature;

        return (v << 8) | Value;
    }

    public override string ToString()
    {
        return $"{Temperature}° {Value} %";
    }
}
