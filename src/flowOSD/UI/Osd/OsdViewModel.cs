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

namespace flowOSD.UI.Osd;

using System;
using flowOSD.Core;
using flowOSD.Core.Resources;

public sealed class OsdViewModel : ViewModelBase
{
    private string? text, icon;
    private double value;
    private bool showCaution;

    public OsdViewModel(
        ITextResources textResources,
        IImageResources imageResources)
        : base(
            textResources,
            imageResources)
    {
    }

    public string? Icon
    {
        get => icon;
        private set => SetProperty(ref icon, value);
    }

    public string? Text
    {
        get => text;
        private set => SetProperty(ref text, value);
    }

    public double Value
    {
        get => value;
        private set => SetProperty(ref this.value, value);
    }

    public bool ShowCaution
    {
        get => showCaution;
        private set => SetProperty(ref showCaution, value);
    }

    public bool IsValue => value >= 0;

    public bool IsMessage => value < 0;

    public void Update(object data)
    {
        if (data is OsdValue v)
        {
            value = v.Value;
            icon = v.Icon;
            text = null;
        }

        if (data is OsdMessage m)
        {
            value = -1;
            icon = m.Icon;
            text = m.Text;
            showCaution = m.ShowCaution;
        }

        OnPropertyChanged(null);
    }
}
