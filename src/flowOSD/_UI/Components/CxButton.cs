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

internal sealed class CxButton : CxButtonBase, IButtonControl
{
    private const float TEXT_HOVER = 0;
    private const float TEXT_PRESSED = -.1f;
    private const float TEXT_DISABLED = -.3f;

    private Color textColor, textBrightColor;
    private bool isToggle, isChecked, isTransparent;

    private string icon;
    private Font? iconFont;

    private CornerRadius borderRadius;

    private CxContextMenu? dropDownMenu;

    public CxButton()
    {
        isToggle = false;
        isChecked = false;
        isTransparent = false;

        FocusColor = Color.White;

        textColor = Color.White;
        textBrightColor = Color.Black;

        icon = string.Empty;
        iconFont = null;

        borderRadius = CornerRadius.Round;
    }

    [DefaultValue(CornerRadius.Round)]
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

    public CxContextMenu? DropDownMenu
    {
        get => dropDownMenu;
        set
        {
            if (dropDownMenu == value)
            {
                return;
            }

            dropDownMenu = value;
            IsToggle = IsToggle && value != null;
        }
    }

    public Color TextColor
    {
        get => textColor;
        set
        {
            if (textColor == value)
            {
                return;
            }

            textColor = value;
            Invalidate();
        }
    }

    public Color TextBrightColor
    {
        get => textBrightColor;
        set
        {
            if (textBrightColor == value)
            {
                return;
            }

            textBrightColor = value;
            Invalidate();
        }
    }

    public string Icon
    {
        get => icon;
        set
        {
            if (icon == value)
            {
                return;
            }

            icon = value;
            Invalidate();
        }
    }

    public Font? IconFont
    {
        get => iconFont;
        set
        {
            if (iconFont == value)
            {
                return;
            }

            iconFont = value;
            Invalidate();
        }
    }

    [Bindable(true)]
    [DefaultValue(false)]
    public bool IsToggle
    {
        get => isToggle;
        set
        {
            if (isToggle == value)
            {
                return;
            }

            isToggle = value;

            if (!IsToggle && IsChecked)
            {
                IsChecked = false;
            }
            else
            {
                Invalidate();
            }
        }
    }

    [Bindable(true)]
    [DefaultValue(false)]
    public bool IsChecked
    {
        get => isChecked;
        set
        {
            if ((!IsToggle && value) || isChecked == value)
            {
                return;
            }

            isChecked = value;
            Invalidate();
        }
    }

    [Bindable(true)]
    [DefaultValue(false)]
    public bool IsTransparent
    {
        get => isTransparent;
        set
        {
            if (isTransparent == value)
            {
                return;
            }

            isTransparent = value;
            Invalidate();
        }
    }

    public DialogResult DialogResult { get; set; }

    private bool IsDropDownToggle => DropDownMenu != null && IsToggle;

    public void NotifyDefault(bool value)
    { }

    public void PerformClick()
    {
        OnClick(EventArgs.Empty);
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        if (IsDisposed)
        {
            return Size.Empty;
        }

        using var g = Graphics.FromHwnd(Handle);
        var symbolSize = IconFont == null
            ? new Size(0, 0)
            : g.MeasureString(Icon ?? string.Empty, IconFont);

        var textSize = g.MeasureString(Text ?? string.Empty, Font);

        var width = symbolSize.Width + textSize.Width;
        var height = Math.Max(symbolSize.Height, textSize.Height);

        if (DropDownMenu != null && IconFont != null)
        {
            var arrowSymbol = "\ue972";
            var arrowSymbolSize = g.MeasureString(arrowSymbol ?? string.Empty, IconFont);

            width += arrowSymbolSize.Width;
        }

        return new Size(
            FOCUS_SPACE * 2 + (int)(Padding.Left + Padding.Right + width),
            FOCUS_SPACE * 2 + (int)(Padding.Top + Padding.Bottom + height));
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (ClientRectangle.Contains(e.Location))
        {
            if (IsDropDownToggle && e.Location.X > Width / 2)
            {
                State |= ButtonState.DropDownHover;
            }
            else
            {
                State &= ~ButtonState.DropDownHover;
            }
        }

        base.OnMouseMove(e);
    }

    protected override void OnClick(EventArgs e)
    {
        if (DropDownMenu != null)
        {
            var clientRect = ClientRectangle;
            if (IsToggle)
            {
                clientRect = new Rectangle(
                    clientRect.X + clientRect.Width / 2,
                    clientRect.Y,
                    clientRect.Width / 2,
                    clientRect.Bottom);
            }

            if (clientRect.Contains(PointToClient(MousePosition)))
            {
                DropDownMenu.Show(this.PointToScreen(new Point(FOCUS_SPACE, this.Height)));

                if (TabListener?.ShowKeyboardFocus == true && DropDownMenu.Items.Count > 0)
                {
                    DropDownMenu.Items[0].Select();
                }

                return;
            }
        }

        if (Command == null && IsToggle)
        {
            IsChecked = !IsChecked;
        }

        base.OnClick(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && DropDownMenu != null)
        {
            DropDownMenu.Dispose();
            DropDownMenu = null;
        }

        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        e.Graphics.Clear(Parent?.BackColor ?? Color.Transparent);

        var baseColor = IsChecked ? AccentColor : BackColor;
        var backgroundColor = GetBackgroundColor(baseColor, !IsDropDownToggle);

        var drawingAreaRect = new Rectangle(
            FOCUS_SPACE,
            FOCUS_SPACE,
            Width - 1 - FOCUS_SPACE * 2,
            Height - 1 - FOCUS_SPACE * 2);

        if (backgroundColor != Color.Transparent || (State & ButtonState.MouseHover) == ButtonState.MouseHover)
        {
            using var brush = new SolidBrush(backgroundColor);
            e.Graphics.FillRoundedRectangle(brush, drawingAreaRect, (int)BorderRadius);

            DrawDropDownHover(e, baseColor, drawingAreaRect);

            using var pen = new Pen(GetBorderColor(baseColor), 1);
            if (IsToggle && DropDownMenu != null)
            {
                e.Graphics.DrawLine(pen,
                    drawingAreaRect.X + drawingAreaRect.Width / 2,
                    drawingAreaRect.Y,
                    drawingAreaRect.X + drawingAreaRect.Width / 2,
                    drawingAreaRect.Bottom);
            }

            e.Graphics.DrawRoundedRectangle(pen, drawingAreaRect, (int)BorderRadius);
        }

        if (TabListener?.ShowKeyboardFocus == true && Focused)
        {
            e.Graphics.DrawRoundedRectangle(FocusPen, 1, 1, Width - 3, Height - 3, (int)BorderRadius);
        }

        using var textBrush = new SolidBrush(GetTextColor(baseColor, (State & ButtonState.DropDownHover) == 0));

        var symbolSize = IconFont == null
            ? new Size(0, 0)
            : e.Graphics.MeasureString(Icon ?? string.Empty, IconFont);

        var textSize = e.Graphics.MeasureString(Text ?? string.Empty, Font);

        drawingAreaRect = DrawDropDownArrow(e, baseColor, drawingAreaRect, symbolSize, textSize);

        var symbolPoint = new PointF(
            GetContentX(drawingAreaRect, symbolSize.Width + textSize.Width),
            (Height - symbolSize.Height) / 2 + 2);

        var textPoint = new PointF(
            symbolPoint.X + symbolSize.Width,
            (Height - textSize.Height) / 2);

        if (IconFont != null)
        {
            e.Graphics.DrawString(Icon, IconFont, textBrush, symbolPoint);
        }

        e.Graphics.DrawString(Text, Font, textBrush, textPoint);
    }

    private float GetContentX(Rectangle drawingAreaRect, float contentWidth)
    {
        switch (TextAlign)
        {
            case ContentAlignment.TopCenter:
            case ContentAlignment.MiddleCenter:
            case ContentAlignment.BottomCenter:
                return drawingAreaRect.X + drawingAreaRect.Width / 2 - contentWidth / 2;

            case ContentAlignment.TopRight:
            case ContentAlignment.MiddleRight:
            case ContentAlignment.BottomRight:
                return drawingAreaRect.X + drawingAreaRect.Width - contentWidth - Padding.Right;

            case ContentAlignment.TopLeft:
            case ContentAlignment.MiddleLeft:
            case ContentAlignment.BottomLeft:
                return drawingAreaRect.X + Padding.Left;
        }

        throw new NotSupportedException($"Content Alignment {TextAlign} isn't supported");
    }

    private Rectangle DrawDropDownArrow(PaintEventArgs e, Color baseColor, Rectangle drawingAreaRect, SizeF symbolSize, SizeF textSize)
    {
        if (IconFont != null && DropDownMenu != null)
        {
            using var arrowBrush = new SolidBrush(
                GetTextColor(baseColor, !IsDropDownToggle || (State & ButtonState.DropDownHover) == ButtonState.DropDownHover));

            var arrowSymbol = "\ue972";
            var arrowSymbolSize = e.Graphics.MeasureString(arrowSymbol, IconFont);
            var arrowSymbolPoint = new PointF(
                drawingAreaRect.Right - (Height - arrowSymbolSize.Height) / 3 - arrowSymbolSize.Width,
                (Height - arrowSymbolSize.Height) / 2 + 2);

            e.Graphics.DrawString(arrowSymbol, IconFont, arrowBrush, arrowSymbolPoint);

            drawingAreaRect.Width = IsToggle
                ? drawingAreaRect.Width / 2
                : (int)arrowSymbolPoint.X - drawingAreaRect.X;
        }

        return drawingAreaRect;
    }

    private void DrawDropDownHover(PaintEventArgs e, Color baseColor, Rectangle drawingAreaRect)
    {
        if (!IsDropDownToggle)
        {
            return;
        }

        using var hoveredBrush = new SolidBrush(GetBackgroundColor(baseColor, true));

        if ((State & ButtonState.DropDownHover) == ButtonState.DropDownHover)
        {
            e.Graphics.FillRoundedRectangle(
                hoveredBrush,
                new Rectangle(
                    drawingAreaRect.X + drawingAreaRect.Width / 2,
                    drawingAreaRect.Y,
                    drawingAreaRect.Width / 2,
                    drawingAreaRect.Height),
                (int)BorderRadius,
                Drawing.Corners.BottomRight | Drawing.Corners.TopRight);
        }
        else
        {
            e.Graphics.FillRoundedRectangle(
                hoveredBrush,
                new Rectangle(
                    drawingAreaRect.X,
                    drawingAreaRect.Y,
                    drawingAreaRect.Width / 2,
                    drawingAreaRect.Height),
                (int)BorderRadius,
                Drawing.Corners.BottomLeft | Drawing.Corners.TopLeft);
        }
    }

    private Color GetBorderColor(Color baseColor)
    {
        return baseColor.IsBright() ? baseColor.Luminance(-BORDER) : baseColor.Luminance(BORDER);
    }

    private Color GetTextColor(Color baseColor, bool isHoveredPart)
    {
        var isBright = baseColor.IsBright();

        if (!Enabled)
        {
            return isBright ? TextBrightColor.Luminance(-TEXT_DISABLED) : TextColor.Luminance(TEXT_DISABLED);
        }
        else if (isHoveredPart && (State & ButtonState.Pressed) == ButtonState.Pressed)
        {
            return isBright ? TextBrightColor.Luminance(-TEXT_DISABLED) : TextColor.Luminance(TEXT_PRESSED);
        }
        else
        {
            return isBright ? TextBrightColor : TextColor;
        }
    }

    private Color GetBackgroundColor(Color color, bool isHoveredPart)
    {
        var sign = color.IsBright() ? -1 : +1;

        if (!Enabled)
        {
            return color.Luminance(sign * BACKGROUND_DISABLED);
        }
        else if (isHoveredPart && (State & ButtonState.Pressed) == ButtonState.Pressed)
        {
            return color.Luminance(sign * BACKGROUND_PRESSED);
        }
        else if (isHoveredPart && (State & ButtonState.MouseHover) == ButtonState.MouseHover)
        {
            return color.Luminance(sign * BACKGROUND_HOVER);
        }
        else if (IsTransparent)
        {
            return Color.Transparent;
        }
        else
        {
            return color;
        }
    }
}
