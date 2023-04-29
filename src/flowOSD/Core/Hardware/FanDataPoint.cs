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

public sealed class FanDataPoint
{
    public static IList<FanDataPoint> MakeSafe(IEnumerable<FanDataPoint> points, out bool isCorrected)
    {
        isCorrected = false;
        var result = new List<FanDataPoint>(points.Take(points.Count() - 3));

        var last = points.Skip(result.Count).ToArray();

        if (last[0].Temperature < 70 || last[0].Temperature > 79 || last[0].Value < 35)
        {
            result.Add(new FanDataPoint(
                Math.Min((byte)70, Math.Max((byte)79, last[0].Temperature)),
                Math.Max((byte)35, last[0].Value)));

            isCorrected = true;
        }
        else
        {
            result.Add(last[0]);
        }

        if (last[1].Temperature < 80 || last[1].Temperature > 89 || last[1].Value < 50)
        {
            result.Add(new FanDataPoint(
                Math.Min((byte)80, Math.Max((byte)89, last[1].Temperature)),
                Math.Max((byte)50, last[1].Value)));

            isCorrected = true;
        }
        else
        {
            result.Add(last[1]);
        }

        if (last[2].Temperature < 90 || last[2].Value < 60)
        {
            result.Add(new FanDataPoint(
                Math.Max((byte)90, last[2].Temperature),
                Math.Max((byte)60, last[2].Value)));

            isCorrected = true;
        }
        else
        {
            result.Add(last[2]);
        }

        return result;
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
