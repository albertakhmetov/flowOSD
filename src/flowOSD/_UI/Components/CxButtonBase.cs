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

abstract class CxButtonBase : ButtonBase
{
    protected const float BACKGROUND_HOVER = +.1f;
    protected const float BACKGROUND_PRESSED = +.15f;
    protected const float BACKGROUND_DISABLED = .4f;
    protected const float BORDER = +.2f;
    protected const int FOCUS_SPACE = 8;

    private ButtonState state;
    private CxTabListener? tabListener;
    private Pen? focusPen;
    private Color accentColor;

    protected CxButtonBase()
    {
        state = 0;
        tabListener = null;

        accentColor = Color.FromArgb(255, 25, 110, 191).Luminance(0.2f);
    }

    public Color AccentColor
    {
        get => accentColor;
        set
        {
            if (accentColor == value)
            {
                return;
            }

            accentColor = value;
            Invalidate();
        }
    }

    public Color FocusColor
    {
        get => (focusPen?.Color) ?? Color.Empty;
        set
        {
            if (focusPen?.Color == value)
            {
                return;
            }

            if (focusPen != null)
            {
                disposable?.Remove(focusPen);
                focusPen.Dispose();
            }

            focusPen = new Pen(value, 3);
            Invalidate();
        }
    }

    public CxTabListener? TabListener
    {
        get => tabListener;
        set
        {
            if (tabListener == value)
            {
                return;
            }

            if (tabListener != null)
            {
                tabListener.ShowKeyboardFocusChanged -= OnShowKeyboardFocusChanged;
            }

            tabListener = value;

            if (tabListener != null)
            {
                tabListener.ShowKeyboardFocusChanged += OnShowKeyboardFocusChanged;
            }

            Invalidate();
        }
    }

    protected ButtonState State
    {
        get => state;
        set
        {
            if (state == value)
            {
                return;
            }

            state = value;
            Invalidate();
        }
    }

    protected Pen FocusPen => focusPen ?? (ForeColor.IsBright() ? Pens.Black : Pens.White);

    protected CompositeDisposable? disposable { get; private set; } = new CompositeDisposable();

    protected override void OnMouseEnter(EventArgs e)
    {
        State |= ButtonState.MouseHover;

        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        State &= ~ButtonState.MouseHover;

        base.OnMouseLeave(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (ClientRectangle.Contains(e.Location))
        {
            State |= ButtonState.MouseHover;
        }
        else
        {
            State &= ~ButtonState.MouseHover;
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            State |= ButtonState.Pressed;
        }

        if (TabListener != null)
        {
            TabListener.ShowKeyboardFocus = false;
        };

        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            State &= ~ButtonState.Pressed;
        }

        base.OnMouseUp(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space || (Command != null && e.KeyCode == Keys.Enter))
        {
            State |= ButtonState.Pressed;
        }

        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space || (Command != null && e.KeyCode == Keys.Enter))
        {
            State &= ~ButtonState.Pressed;
        }

        if (e.KeyCode == Keys.Enter)
        {
            Command?.Execute(CommandParameter);

            return;
        }

        base.OnKeyUp(e);
    }

    protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
    {
        if ((e.KeyCode == Keys.Tab || e.KeyCode == Keys.Space) && TabListener != null)
        {
            TabListener.ShowKeyboardFocus = true;
        }

        base.OnPreviewKeyDown(e);
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

    private void OnShowKeyboardFocusChanged(object? sender, EventArgs e)
    {
        Invalidate();
    }

    [Flags]
    protected enum ButtonState
    {
        Default = 1,
        MouseHover = 2,
        Pressed = 4,
        DropDownHover = 8
    }
}
