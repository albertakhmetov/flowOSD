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
namespace flowOSD.Extensions;

using System.Diagnostics;
using System.Drawing.Drawing2D;
using static Native.Shlwapi;

static class Drawing
{
    [Flags]
    public enum Corners
    {
        TopLeft = 1,
        TopRight = 2,
        BottomRight = 4,
        BottomLeft = 8,
        All = TopLeft | TopRight | BottomRight | BottomLeft
    }

    public static void DrawRoundedRectangle(
        this Graphics g,
        Pen pen,
        Rectangle rect,
        float r,
        Corners corners = Corners.All)
    {
        DrawRoundedRectangle(g, pen, rect.X, rect.Y, rect.Width, rect.Height, r, corners);
    }

    public static void DrawRoundedRectangle(
        this Graphics g,
        Pen pen,
        RectangleF rect,
        float r,
        Corners corners = Corners.All)
    {
        DrawRoundedRectangle(g, pen, rect.X, rect.Y, rect.Width, rect.Height, r, corners);
    }

    public static void DrawRoundedRectangle(
        this Graphics g,
        Pen pen,
        float x,
        float y,
        float width,
        float height,
        float r,
        Corners corners = Corners.All)
    {
        using var path = GetRoundedRectPath(x, y, width, height, r, corners);

        g.DrawPath(pen, path);
    }

    public static void FillRoundedRectangle(
        this Graphics g,
        Brush brush,
        Rectangle rect,
        float r,
        Corners corners = Corners.All)
    {
        FillRoundedRectangle(g, brush, rect.X, rect.Y, rect.Width, rect.Height, r, corners);
    }

    public static void FillRoundedRectangle(
        this Graphics g,
        Brush brush,
        RectangleF rect,
        float r,
        Corners corners = Corners.All)
    {
        FillRoundedRectangle(g, brush, rect.X, rect.Y, rect.Width, rect.Height, r, corners);
    }

    public static void FillRoundedRectangle(
        this Graphics g,
        Brush brush,
        float x,
        float y,
        float width,
        float height,
        float r,
        Corners corners = Corners.All)
    {
        using var path = GetRoundedRectPath(x, y, width, height, r, corners);

        g.FillPath(brush, path);
    }

    public static GraphicsPath GetRoundedRectPath(float x, float y, float width, float height, float r, Corners corners = Corners.All)
    {
        var arc = new RectangleF(x, y, r * 2, r * 2);
        var path = new GraphicsPath();

        if (r == 0)
        {
            path.AddRectangle(new RectangleF(x, y, width, height));
            return path;
        }

        if ((corners & Corners.TopLeft) == Corners.TopLeft)
        {
            path.AddArc(arc, 180, 90);
        }
        else
        {
            path.AddLine(arc.X, arc.Y, arc.X, arc.Y);
        }

        arc.X = x + width - r * 2;

        if ((corners & Corners.TopRight) == Corners.TopRight)
        {
            path.AddArc(arc, 270, 90);
        }
        else
        {
            path.AddLine(arc.Right, arc.Y, arc.Right, arc.Y);
        }

        arc.Y = y + height - r * 2;

        if ((corners & Corners.BottomRight) == Corners.BottomRight)
        {
            path.AddArc(arc, 0, 90);
        }
        else
        {
            path.AddLine(arc.Right, arc.Bottom, arc.Right, arc.Bottom);
        }

        arc.X = x;

        if ((corners & Corners.BottomLeft) == Corners.BottomLeft)
        {
            path.AddArc(arc, 90, 90);
        }
        else
        {
            path.AddLine(arc.X, arc.Bottom, arc.X, arc.Bottom);
        }

        path.CloseFigure();

        return path;
    }

    public static bool IsBright(this Color color)
    {
        return color.GetBrightness() > 0.6;
    }

    public static Color SetAlpha(this Color color, byte alpha)
    {
        return Color.FromArgb(alpha, color);
    }

    public static Color Luminance(this Color color, float factor)
    {
        return Color.FromArgb(
            255,
            (int)Math.Max(byte.MinValue, Math.Min(byte.MaxValue, color.R + (255 * factor))),
            (int)Math.Max(byte.MinValue, Math.Min(byte.MaxValue, color.G + (255 * factor))),
            (int)Math.Max(byte.MinValue, Math.Min(byte.MaxValue, color.B + (255 * factor))));
    }
}
