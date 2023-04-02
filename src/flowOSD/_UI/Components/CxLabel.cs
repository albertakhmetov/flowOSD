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
using System.Text.RegularExpressions;
using flowOSD.Extensions;
using static flowOSD.Extensions.Common;

internal sealed class CxLabel : Label
{
    private string? icon;
    private Font? iconFont;
    private bool useClearType, showKeys, blackAndWhite;

    public CxLabel()
    {
        icon = null;
        iconFont = null;
        useClearType = false;
        showKeys = false;
        blackAndWhite = false;
    }

    public string? Icon
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

    public bool UseClearType
    {
        get => useClearType;
        set
        {
            if (useClearType == value)
            {
                return;
            }

            useClearType = value;
            Invalidate();
        }
    }

    public bool ShowKeys
    {
        get => showKeys;
        set
        {
            if (showKeys == value)
            {
                return;
            }

            showKeys = value;
            Invalidate();
        }
    }

    public bool BlackAndWhite
    {
        get => blackAndWhite;
        set
        {
            if (blackAndWhite == value)
            {
                return;
            }

            blackAndWhite = value;
            Invalidate();
        }
    }

    public CxTabListener? TabListener
    {
        get; set;
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        if (IsDisposed)
        {
            return Size.Empty;
        }

        using var g = Graphics.FromHwnd(Handle);

        var symbolSize = IconFont == null
            ? new SizeF(0, 0)
            : g.MeasureString(Icon ?? string.Empty, IconFont);

        var textSize = g.MeasureString(Text, Font);

        var totalSize = new Size(
            (int)(symbolSize.Width + textSize.Width),
            (int)Math.Max(symbolSize.Height, textSize.Height));

        return new Size(
           Math.Max(MinimumSize.Width, Padding.Left + Padding.Right + totalSize.Width),
           Math.Max(MinimumSize.Height, Padding.Top + Padding.Bottom + totalSize.Height));
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
        var parts = GetParts(e.Graphics);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
        e.Graphics.TextRenderingHint = UseClearType
            ? TextRenderingHint.ClearTypeGridFit
            : TextRenderingHint.AntiAliasGridFit;

        var symbolSize = IconFont == null
            ? new SizeF(0, 0)
            : e.Graphics.MeasureString(Icon ?? string.Empty, IconFont);
        var textSize = parts.Count == 0
            ? SizeF.Empty
            : new SizeF(parts.Sum(i => i.Size.Width), parts.Max(i => i.Size.Height));
        var totalSize = new SizeF(
            symbolSize.Width + textSize.Width,
            Math.Max(symbolSize.Height, textSize.Height));

        var dY = (symbolSize.Height - textSize.Height) / 2;

        using var textBrush = new SolidBrush(ForeColor);

        var x = GetX(totalSize);
        var y = GetY(totalSize);

        if (IconFont != null)
        {
            e.Graphics.DrawString(Icon, IconFont, textBrush, x, y + Math.Max(0, -dY));
        }

        using var pen = new Pen(BlackAndWhite ? ForeColor : ForeColor.Luminance(ForeColor.IsBright() ? -.5f : +.5f), BlackAndWhite ? 2 : 1);
        using var brush = new SolidBrush(BlackAndWhite ? BackColor : ForeColor.Luminance(ForeColor.IsBright() ? -.7f : +.8f));

        var dX = x + symbolSize.Width;
        foreach (var p in parts)
        {
            if (p.IsKey)
            {
                var rect = new RectangleF(dX - 4, y - 4, p.Size.Width + 8, p.Size.Height + 8);

                var r = IsWindows11 ? 4 : 0;
                e.Graphics.FillRoundedRectangle(brush, rect, r);
                e.Graphics.DrawRoundedRectangle(pen, rect, r);
            }

            e.Graphics.DrawString(p.Text, Font, textBrush, dX, y + Math.Max(0, dY));
            dX += p.Size.Width;
        }
    }

    private IList<TextPart> GetParts(Graphics g)
    {
        var format = new StringFormat(StringFormat.GenericTypographic);
        format.FormatFlags = StringFormatFlags.MeasureTrailingSpaces;

        var r = new List<TextPart>();

        var spaceWidth = g.MeasureString(" ", Font, 0, format).Width;

        var m = Regex.Matches(Text, "`[A-Za-z0-9]+`");

        var pos = 0;

        if (ShowKeys)
        {
            for (var i = 0; i < m.Count; i++)
            {
                if (m[i].Index - pos > 0)
                {
                    var preText = Text.Substring(pos, m[i].Index - pos);
                    var preSize = g.MeasureString(preText, Font, 0, format);
                    preSize.Width += spaceWidth;

                    r.Add(new TextPart(preText, preSize, false));
                }

                var keyText = Text.Substring(m[i].Index + 1, m[i].Length - 2);
                var keySize = g.MeasureString(keyText, Font, 0, format);
                keySize.Width += spaceWidth;

                var part = new TextPart(keyText, keySize, true);
                r.Add(part);

                pos = m[i].Index + m[i].Length;
            }
        }

        if (pos < Text.Length)
        {
            var postText = Text.Substring(pos);
            var postSize = g.MeasureString(postText, Font, 0, format);
            r.Add(new TextPart(postText, postSize, false));
        }

        return r;
    }

    private float GetX(SizeF textSize)
    {
        switch (TextAlign)
        {
            case ContentAlignment.BottomLeft:
            case ContentAlignment.MiddleLeft:
            case ContentAlignment.TopLeft:
                return Padding.Left;

            case ContentAlignment.BottomCenter:
            case ContentAlignment.MiddleCenter:
            case ContentAlignment.TopCenter:
                return (Width - textSize.Width) / 2;

            case ContentAlignment.BottomRight:
            case ContentAlignment.MiddleRight:
            case ContentAlignment.TopRight:
                return Width - textSize.Width - Padding.Right;

            default:
                return 0;
        }
    }

    private float GetY(SizeF textSize)
    {
        switch (TextAlign)
        {
            case ContentAlignment.TopRight:
            case ContentAlignment.TopCenter:
            case ContentAlignment.TopLeft:
                return Padding.Top;

            case ContentAlignment.MiddleCenter:
            case ContentAlignment.MiddleRight:
            case ContentAlignment.MiddleLeft:
                return (Height - textSize.Height) / 2;

            case ContentAlignment.BottomRight:
            case ContentAlignment.BottomCenter:
            case ContentAlignment.BottomLeft:
                return Height - textSize.Height - Padding.Bottom;

            default:
                return 0;
        }
    }

    private readonly record struct TextPart(string Text, SizeF Size, bool IsKey);
}
