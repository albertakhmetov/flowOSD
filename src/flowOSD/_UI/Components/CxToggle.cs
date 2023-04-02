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
using static flowOSD.Extensions.Common;

[DefaultBindingProperty(nameof(IsChecked))]
internal sealed class CxToggle : CxButtonBase
{
    private static readonly object EVENT_ISCHECKEDCHANGED = new object();

    private bool isChecked;

    public CxToggle()
    {
    }

    public event EventHandler? IsCheckedChanged
    {
        add => Events.AddHandler(EVENT_ISCHECKEDCHANGED, value);
        remove => Events.RemoveHandler(EVENT_ISCHECKEDCHANGED, value);
    }

    [Bindable(true, BindingDirection.TwoWay)]
    [DefaultValue(false)]
    public bool IsChecked
    {
        get => isChecked;
        set
        {
            if (isChecked == value)
            {
                return;
            }

            isChecked = value;
            Invalidate();

            ((EventHandler?)Events[EVENT_ISCHECKEDCHANGED])?.Invoke(this, EventArgs.Empty);
        }
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        return DefaultSize;
    }

    protected override void OnClick(EventArgs e)
    {
        IsChecked = !IsChecked;
        base.OnClick(e);
    }

    protected override Size DefaultSize => new Size(60 + FOCUS_SPACE * 2, 30 + FOCUS_SPACE * 2);

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        e.Graphics.Clear(Parent?.BackColor ?? BackColor);

        var drawingAreaRect = new Rectangle(
            FOCUS_SPACE,
            FOCUS_SPACE,
            Width - 1 - FOCUS_SPACE * 2,
            Height - 1 - FOCUS_SPACE * 2);

        using var brush = new SolidBrush(GetBackgroundColor());
        e.Graphics.FillRoundedRectangle(brush, drawingAreaRect, drawingAreaRect.Height / 2);

        if (!IsChecked)
        {
            using var pen = new Pen(ForeColor, 1);
            e.Graphics.DrawRoundedRectangle(pen, drawingAreaRect, drawingAreaRect.Height / 2);
        }

        if (TabListener?.ShowKeyboardFocus == true && Focused)
        {
            e.Graphics.DrawRoundedRectangle(
                FocusPen,
                1,
                1,
                Width - 3,
                Height - 3,
                (int)(IsWindows11 ? CornerRadius.Round : CornerRadius.Off));
        }

        using var toggleBrush = new SolidBrush(GetToggleColor());

        var offsetX = 4;
        var offsetY = 4;
        var d = drawingAreaRect.Height - 8;

        var toggleRect = new Rectangle(
            IsChecked ? (drawingAreaRect.Right - offsetX - d) : (drawingAreaRect.X + offsetX),
            drawingAreaRect.Y + offsetY,
            d,
            d);

        if ((State & ButtonState.MouseHover) == ButtonState.MouseHover && (State & ButtonState.Pressed) == ButtonState.Pressed)
        {
            if (IsChecked)
            {
                e.Graphics.TranslateTransform(-toggleRect.Width / 2, 0);
            }

            toggleRect.Width = (int)(toggleRect.Width * 1.5);
        }

        e.Graphics.FillRoundedRectangle(toggleBrush, toggleRect, toggleRect.Height / 2);
    }

    private Color GetBackgroundColor()
    {
        var color = IsChecked ? AccentColor : BackColor;
        var sign = color.IsBright() ? -1 : +1;

        if ((State & ButtonState.MouseHover) == ButtonState.MouseHover && (State & ButtonState.Pressed) == ButtonState.Pressed)
        {
            return color.Luminance(sign * BACKGROUND_PRESSED);
        }
        else if ((State & ButtonState.MouseHover) == ButtonState.MouseHover)
        {
            return color.Luminance(sign * BACKGROUND_HOVER);
        }
        else
        {
            return color;
        }
    }

    private Color GetToggleColor()
    {
        var color = IsChecked ? (Parent?.BackColor ?? BackColor) : ForeColor;
        var sign = color.IsBright() ? -1 : +1;

        if ((State & ButtonState.MouseHover) == ButtonState.MouseHover && (State & ButtonState.Pressed) == ButtonState.Pressed)
        {
            return color.Luminance(sign * BACKGROUND_PRESSED);
        }
        else if ((State & ButtonState.MouseHover) == ButtonState.MouseHover)
        {
            return color.Luminance(sign * BACKGROUND_HOVER);
        }
        else
        {
            return color;
        }
    }
}
