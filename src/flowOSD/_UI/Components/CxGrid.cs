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
namespace flowOSD.UI.Components;

using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reactive.Disposables;
using System.ComponentModel;
using static flowOSD.Extensions.Drawing;
using flowOSD.Extensions;
using System.Windows.Forms;
using System.Drawing;
using System;

class CxGrid : TableLayoutPanel
{
    private CornerRadius borderRadius;

    public CxGrid()
    {
        borderRadius = CornerRadius.Small;
    }

    public CxTabListener? TabListener
    {
        get; set;
    }

    [DefaultValue(CornerRadius.Small)]
    public CornerRadius BorderRadius
    {
        get => borderRadius;
        set
        {
            if (borderRadius == value)
            {
                return;
            }

            borderRadius = value;
            Invalidate();
        }
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        if (TabListener != null)
        {
            TabListener.ShowKeyboardFocus = false;
        }

        base.OnMouseClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.HighQuality;

        e.Graphics.Clear(Parent?.BackColor ?? BackColor);

        var drawingRect = new Rectangle(1, 1, Width - 3, Height - 3);

        using var brush = new SolidBrush(BackColor);
        e.Graphics.FillRoundedRectangle(brush, drawingRect, (int)BorderRadius);

        using var pen = new Pen(BackColor.IsBright() ? BackColor.Luminance(-.2f) : BackColor.Luminance(+.2f), 1);
        e.Graphics.DrawRoundedRectangle(pen, drawingRect, (int)BorderRadius);
    }
}
