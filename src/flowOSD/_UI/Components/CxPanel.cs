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
using System.Reactive.Disposables;
using System.Reactive.Linq;
using flowOSD.Extensions;
using static flowOSD.Extensions.Common;

namespace flowOSD.UI.Components;

internal sealed class CxPanel : Panel
{
    private const int SCROLLER_WIDTH = 12;
    private const int SCROLLER_HOVER_DELTA = 10;
    private const int SCROLLER_PADDING = 2;

    private Scroller scroller;
    private Control? content;
    private Color scrollerColor;

    public CxPanel()
    {
        AutoScroll = false;
        scroller = new Scroller(this);
    }

    public Color ScrollerColor
    {
        get => scroller.BackColor;
        set => scroller.BackColor = value;
    }

    public Control? Content
    {
        get => content;
        set
        {
            if (content != null)
            {
                Controls.Remove(content);
                UpdateFocusEvent(content, false);
            }

            content = value;

            if (content != null)
            {
                content.Dock = DockStyle.None;
                content.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;

                content.Left = 0;
                content.Top = 0;

                Controls.Add(content);
                UpdateFocusEvent(content, true);

                scroller.BringToFront();

                UpdateScroll(0);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !scroller.IsDisposed)
        {
            scroller.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);

        if (Content == null)
        {
            scroller.Hide();
            return;
        }

        Content.Width = ClientSize.Width;

        UpdateScroll(0);

        scroller.Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);

        if (Content == null)
        {
            return;
        }

        UpdateScroll(e.Delta);
    }

    private void UpdateFocusEvent(Control? control, bool bind)
    {
        if (control == null)
        {
            return;
        }

        if (bind)
        {
            control.GotFocus += OnControlGotFocus;
        }
        else
        {
            control.GotFocus -= OnControlGotFocus;

        }

        foreach (var c in control.Controls)
        {
            UpdateFocusEvent(c as Control, bind);
        }
    }

    private void OnControlGotFocus(object? sender, EventArgs e)
    {
        ScrollTo((sender as Control)?.Parent);
    }

    private void ScrollTo(Control? control)
    {
        if (control == null || Content == null)
        {
            return;
        }

        var p = ScrollToControl(control);
        UpdateScroll(p.Y);
    }

    private void UpdateScroll(int dY)
    {
        if (Content == null)
        {
            return;
        }

        Content.Top = Math.Min(0, Math.Max(Height - Content.Height, Content.Top + dY));

        var scale = -1f * Content.Height / Height;
        if (Math.Abs(scale) <= 1)
        {
            Content.Width = ClientSize.Width;

            scroller.Hide();
        }
        else
        {
            Content.Width = ClientSize.Width - SCROLLER_PADDING * 2 - SCROLLER_WIDTH;

            scroller.Left = Width - SCROLLER_WIDTH - SCROLLER_PADDING * 2;
            scroller.Width = SCROLLER_WIDTH;
            scroller.Top = (int)Math.Round(Content.Top / scale);

            scroller.Height = (int)Math.Round(Height / Math.Abs(scale));

            scroller.Show();
        }
    }

    private sealed class Scroller : Label
    {
        private CompositeDisposable? disposable = new CompositeDisposable();
        private CxPanel owner;

        public Scroller(CxPanel owner)
        {
            SetStyle(ControlStyles.Selectable, false);

            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));

            var mouseDown = Observable.FromEventPattern<MouseEventArgs>(this, "MouseDown");
            var mouseUp = Observable.FromEventPattern<MouseEventArgs>(this, "MouseUp");
            var mouseMove = Observable.FromEventPattern<MouseEventArgs>(this, "MouseMove");

            mouseDown.Select(x => x.EventArgs.Location)
                .CombineLatest(
                    mouseMove.Select(x => x.EventArgs.Location),
                    (down, move) => new Point(down.X - move.X, down.Y - move.Y))
                .TakeUntil(mouseUp)
                .Repeat()
                .Throttle(TimeSpan.FromMilliseconds(2))
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(x => owner.UpdateScroll(x.Y))
                .DisposeWith(disposable);

            owner.Controls.Add(this);

            BringToFront();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;

            using var pen = new Pen(BackColor.Luminance(BackColor.IsBright() ? -.3f : +.3f), 1);
            using var brush = new SolidBrush(BackColor.Luminance(BackColor.IsBright() ? -.2f : +.2f));

            var rect = new Rectangle(1, 1, Width - 3, Height - 3);
            var r = (int)(IsWindows11 ? CornerRadius.Small : CornerRadius.Off);
            e.Graphics.FillRoundedRectangle(brush, rect, r);
            e.Graphics.DrawRoundedRectangle(pen, rect, r);
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                disposable?.Dispose();
                disposable = null;
            }

            base.Dispose(disposing);
        }
    }
}
