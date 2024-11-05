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

using System.ComponentModel;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using System.Windows.Input;

public sealed class DisplayRefreshRates
{
    public static readonly DisplayRefreshRates Empty = new DisplayRefreshRates(new HashSet<uint>());

    private const uint LoEdge = 60, HiEdge = 90;

    public DisplayRefreshRates(HashSet<uint> values)
    {
        IsEmpty = values == null || values.Count == 0;

        var min = IsEmpty ? 0 : values!.Min();
        var max = IsEmpty ? 0 : values!.Max();

        Low = min < LoEdge && min >= HiEdge ? null : min;
        High = max < HiEdge ? null : max;
    }

    public uint? Low { get; }

    public uint? High { get; }

    public bool IsLowAvailable => Low.HasValue;

    public bool IsHighAvailable => High.HasValue;

    public bool IsEmpty { get; }

    public static bool IsHigh(uint value) => value >= HiEdge;
}
