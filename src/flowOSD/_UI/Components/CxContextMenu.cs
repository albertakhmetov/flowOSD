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
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reactive.Disposables;
using flowOSD.Api;
using System.Windows.Input;
using static flowOSD.Native.Dwmapi;
using static flowOSD.Extensions.Common;
using flowOSD.Native;
using flowOSD.Extensions;

namespace flowOSD.UI.Components;

sealed class CxContextMenu : ContextMenuStrip
{
    private CompositeDisposable? disposable = new CompositeDisposable();

    private CornerRadius borderRadius;

    public CxContextMenu()
    {
        base.Renderer = new MenuRenderer().DisposeWith(disposable);

        borderRadius = CornerRadius.Round;

        BackgroundHoverColor = Color.FromArgb(255, 25, 110, 191);
        BackgroundCheckedColor = Color.FromArgb(255, 25, 25, 25);
        AccentColor = Color.FromArgb(255, 25, 110, 191).Luminance(0.2f);

        SeparatorColor = Color.FromArgb(255, 96, 96, 96);
        TextColor = Color.White;
        TextBrightColor = Color.Black;
        TextDisabledColor = Color.LightGray;
    }

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

            if (IsHandleCreated)
            {
                SetCornerRadius();
            }
        }
    }

    public Color BackgroundColor
    {
        get => Renderer.BackgroundColor;
        set
        {
            if (Renderer.BackgroundColor == value)
            {
                return;
            }

            Renderer.BackgroundColor = value;
            Acrylic.EnableAcrylic(this, Renderer.BackgroundColor.SetAlpha(210));
            Invalidate();
        }
    }

    public Color AccentColor
    {
        get => Renderer.AccentColor;
        set
        {
            if (Renderer.AccentColor == value)
            {
                return;
            }

            Renderer.AccentColor = value;
        }
    }

    public Color BackgroundCheckedColor
    {
        get => Renderer.BackgroundCheckedColor;
        set
        {
            if (Renderer.BackgroundCheckedColor == value)
            {
                return;
            }

            Renderer.BackgroundCheckedColor = value;
        }
    }

    public Color BackgroundHoverColor
    {
        get => Renderer.BackgroundHoverColor;
        set
        {
            if (Renderer.BackgroundHoverColor == value)
            {
                return;
            }

            Renderer.BackgroundHoverColor = value;
            Invalidate();
        }
    }

    public Color SeparatorColor
    {
        get => Renderer.SeparatorColor;
        set
        {
            if (Renderer.SeparatorColor == value)
            {
                return;
            }

            Renderer.SeparatorColor = value;
            Invalidate();
        }
    }

    public Color TextColor
    {
        get => Renderer.TextColor;
        set
        {
            if (Renderer.TextColor == value)
            {
                return;
            }

            Renderer.TextColor = value;
            Invalidate();
        }
    }

    public Color TextBrightColor
    {
        get => Renderer.TextBrightColor;
        set
        {
            if (Renderer.TextBrightColor == value)
            {
                return;
            }

            Renderer.TextBrightColor = value;
            Invalidate();
        }
    }

    public Color TextDisabledColor
    {
        get => Renderer.TextDisabledColor;
        set
        {
            if (Renderer.TextDisabledColor == value)
            {
                return;
            }

            Renderer.TextDisabledColor = value;
            Invalidate();
        }
    }

    private new MenuRenderer Renderer => (base.Renderer as MenuRenderer)!;

    public static ToolStripMenuItem CreateMenuItem(string text, ICommand command, object? commandParameter = null)
    {
        var item = new ToolStripMenuItem();
        item.Margin = new Padding(0, 8, 0, 8);
        item.Text = text;
        item.Command = command;
        item.CommandParameter = commandParameter;

        return item;
    }

    public static ToolStripMenuItem CreateMenuItem(CommandBase command, object? commandParameter = null)
    {
        var item = new ToolStripMenuItem();
        item.Margin = new Padding(0, 8, 0, 8);
        item.Command = command;
        item.CommandParameter = commandParameter;

        item.DataBindings.Add("Text", command, "Text");

        return item;
    }

    public static ToolStripSeparator CreateSeparator(ToolStripItem? dependsOn = null)
    {
        var item = new ToolStripSeparator();

        if (dependsOn != null)
        {
            dependsOn.VisibleChanged += (sender, e) => item.Visible = dependsOn.Visible;
        }

        return item;
    }

    public ToolStripMenuItem AddMenuItem(CommandBase command, object? commandParameter = null)
    {
        var item = CreateMenuItem(command, commandParameter);

        Items.Add(item);

        return item;
    }

    public ToolStripMenuItem AddMenuItem(string text, ICommand command, object? commandParameter = null)
    {
        var item = CreateMenuItem(text, command, commandParameter);

        Items.Add(item);

        return item;
    }

    public ToolStripSeparator AddSeparator(ToolStripItem? dependsOn = null)
    {
        var item = CreateSeparator(dependsOn);

        Items.Add(item);

        return item;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            disposable?.Dispose();
            disposable = null;
        }

        base.Dispose(disposing);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Color.Transparent);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (!IsWindows11)
        {
            using var pen = new Pen(BackColor.IsBright() ? BackColor.Luminance(-.3f) : BackColor.Luminance(+.2f), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        base.OnPaint(e);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        SetCornerRadius();

        base.OnHandleCreated(e);
    }

    private void SetCornerRadius()
    {
        if (!IsWindows11)
        {
            return;
        }

        DWM_WINDOW_CORNER_PREFERENCE corner;
        switch (BorderRadius)
        {
            case CornerRadius.Off:
                corner = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_DONOTROUND;
                break;

            case CornerRadius.Small:
                corner = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUNDSMALL;
                break;

            case CornerRadius.Round:
                corner = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
                break;

            default:
                corner = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_DEFAULT;
                break;
        }

        SetCornerPreference(Handle, corner);
    }

    private class MenuRenderer : ToolStripRenderer, IDisposable
    {
        private SolidBrush? textBrush, textBrightBrush, textDisabledBrush, backgroundHoverBrush, accentBrush, backgroundCheckedBrush;
        private Pen? separatorPen;

        private CompositeDisposable? disposable = new CompositeDisposable();

        public MenuRenderer()
        {
        }

        void IDisposable.Dispose()
        {
            disposable?.Dispose();
            disposable = null;
        }

        public Color BackgroundColor
        {
            get; set;
        }

        public Color AccentColor
        {
            get => (accentBrush?.Color) ?? Color.Empty;
            set
            {
                CheckDisposed();

                if (accentBrush?.Color == value)
                {
                    return;
                }

                if (accentBrush != null)
                {
                    disposable!.Remove(accentBrush);
                    accentBrush.Dispose();
                }

                accentBrush = new SolidBrush(value).DisposeWith(disposable!);
            }
        }

        public Color BackgroundCheckedColor
        {
            get => (backgroundCheckedBrush?.Color) ?? Color.Empty;
            set
            {
                CheckDisposed();

                if (backgroundCheckedBrush?.Color == value)
                {
                    return;
                }

                if (backgroundCheckedBrush != null)
                {
                    disposable!.Remove(backgroundCheckedBrush);
                    backgroundCheckedBrush.Dispose();
                }

                backgroundCheckedBrush = new SolidBrush(value).DisposeWith(disposable!);
            }
        }

        public Color BackgroundHoverColor
        {
            get => (backgroundHoverBrush?.Color) ?? Color.Empty;
            set
            {
                CheckDisposed();

                if (backgroundHoverBrush?.Color == value)
                {
                    return;
                }

                if (backgroundHoverBrush != null)
                {
                    disposable!.Remove(backgroundHoverBrush);
                    backgroundHoverBrush.Dispose();
                }

                backgroundHoverBrush = new SolidBrush(value).DisposeWith(disposable!);
            }
        }

        public Color SeparatorColor
        {
            get => (separatorPen?.Color) ?? Color.Empty;
            set
            {
                CheckDisposed();

                if (separatorPen?.Color == value)
                {
                    return;
                }

                if (separatorPen != null)
                {
                    disposable!.Remove(separatorPen);
                    separatorPen.Dispose();
                }

                separatorPen = new Pen(value, 1).DisposeWith(disposable!);
            }
        }

        public Color TextColor
        {
            get => (textBrush?.Color) ?? Color.Empty;
            set
            {
                CheckDisposed();

                if (textBrush?.Color == value)
                {
                    return;
                }

                if (textBrush != null)
                {
                    disposable!.Remove(textBrush);
                    textBrush.Dispose();
                }

                textBrush = new SolidBrush(value).DisposeWith(disposable!);
            }
        }

        public Color TextBrightColor
        {
            get => (textBrightBrush?.Color) ?? Color.Empty;
            set
            {
                CheckDisposed();

                if (textBrightBrush?.Color == value)
                {
                    return;
                }

                if (textBrightBrush != null)
                {
                    disposable!.Remove(textBrightBrush);
                    textBrightBrush.Dispose();
                }

                textBrightBrush = new SolidBrush(value).DisposeWith(disposable!);
            }
        }

        public Color TextDisabledColor
        {
            get => (textDisabledBrush?.Color) ?? Color.Empty;
            set
            {
                CheckDisposed();

                if (textDisabledBrush?.Color == value)
                {
                    return;
                }

                if (textDisabledBrush != null)
                {
                    disposable!.Remove(textDisabledBrush);
                    textDisabledBrush.Dispose();
                }

                textDisabledBrush = new SolidBrush(value).DisposeWith(disposable!);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            if (e.Item is ToolStripMenuItem menuItem)
            {
                if (menuItem.Checked)
                {
                    e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                }

                var textHeight = e.TextFont.GetHeight(e.Graphics);
                var point = new PointF(
                    e.TextRectangle.X,
                    e.TextRectangle.Y + (e.TextRectangle.Height - textHeight) / 2);

                var backgroundColor = e.Item.Selected ? BackgroundHoverColor : BackgroundColor;

                e.Graphics.DrawString(
                    e.Text,
                    e.TextFont,
                    e.Item.Enabled ? (backgroundColor.IsBright() ? textBrightBrush : textBrush) : textDisabledBrush,
                    point);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;

            var y = e.Item.ContentRectangle.Y + e.Item.ContentRectangle.Height / 2;

            e.Graphics.DrawLine(
                separatorPen,
                e.Item.ContentRectangle.X,
                y,
                e.Item.ContentRectangle.Right,
                y);
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            //base.OnRenderItemCheck(e);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Enabled)
            {
                return;
            }

            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;

            const int offset = 8;

            var x = e.Item.ContentRectangle.X + offset;
            var y = e.Item.ContentRectangle.Y + 1;
            var width = e.Item.ContentRectangle.Width - offset * 2;
            var height = e.Item.ContentRectangle.Height - 2;

            if (!e.Item.Selected && e.Item is ToolStripMenuItem menuItem && menuItem.Checked)
            {
                e.Graphics.FillRoundedRectangle(
                    backgroundCheckedBrush,
                    x,
                    y,
                    width,
                    height,
                    (int)(IsWindows11 ? CornerRadius.Small : CornerRadius.Off));

                e.Graphics.FillRoundedRectangle(accentBrush,
                    x,
                    y + height / 8,
                    8,
                    height - height / 4,
                    (int)(IsWindows11 ? CornerRadius.Small : CornerRadius.Off));
            }

            if (e.Item.Selected)
            {
                e.Graphics.FillRoundedRectangle(
                    backgroundHoverBrush,
                    x,
                    y,
                    width,
                    height,
                    (int)(IsWindows11 ? CornerRadius.Small : CornerRadius.Off));
            }
        }

        private void CheckDisposed()
        {
            if (disposable == null)
            {
                throw new ObjectDisposedException(nameof(MenuRenderer));
            }
        }
    }
}
