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

namespace flowOSD.UI.Controls;

using System;
using System.Collections;
using System.ComponentModel;
using flowOSD.Core.Hardware;

public class FanCurveDataSource : IEnumerable<FanDataPoint>, INotifyPropertyChanged
{
    private FanDataPoint[] items;
    private bool isUserData;

    public FanCurveDataSource()
    {
        items = FanDataPoint.CreateDefaultCurve();
    }

    public int Count => items.Length;

    public byte GridSize => 5;

    public bool IsUserData
    {
        get => isUserData;
        set
        {
            isUserData = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsUserData)));
        }
    }

    public FanDataPoint this[int index]
    {
        get => items[index];
        set
        {
            if (!IsUserData)
            {
                return;
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (items[index].Equals(value))
            {
                return;
            }

            items[index] = AdjustToGrid(ref value);

            ItemChanged?.Invoke(this, new ItemChangedEventArgs(index));
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler<ItemChangedEventArgs>? ItemChanged;

    public event EventHandler? Changed;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Set(IList<FanDataPoint> dataPoints, bool isUserData)
    {
        if (dataPoints == null || dataPoints.Count != items.Length)
        {
            throw new ArgumentException();
        }

        items = dataPoints.OrderBy(i => i.Temperature).Select(i => isUserData ? AdjustToGrid(ref i) : i).ToArray();

        IsUserData = isUserData;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Reset()
    {
        items = FanDataPoint.CreateDefaultCurve();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public IEnumerator<FanDataPoint> GetEnumerator()
    {
        return ((IEnumerable<FanDataPoint>)items).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return items.GetEnumerator();
    }

    private FanDataPoint AdjustToGrid(ref FanDataPoint i)
    {
        var temperature = Convert.ToByte(Math.Round(1f * i.Temperature / GridSize) * GridSize);
        var value = Convert.ToByte(Math.Round(1f * i.Value / GridSize) * GridSize);

        if (temperature != i.Temperature || value != i.Value)
        {
            return new FanDataPoint(temperature, value);
        }
        else
        {
            return i;
        }
    }

    public sealed class ItemChangedEventArgs : EventArgs
    {
        public ItemChangedEventArgs(int index)
        {
            Index = index;
        }

        public int Index { get; private set; }
    }
}
