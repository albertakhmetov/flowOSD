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
namespace flowOSD.Services;

using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using flowOSD.Core;
using flowOSD.Extensions;
using flowOSD.Native;
using static flowOSD.Native.Dwmapi;
using static Native.User32;
using static flowOSD.Extensions.Common;

sealed partial class Osd : IOsd, IDisposable
{
    // 260;72 (100%)
    // center!
    // bottom - 90

    private CompositeDisposable? disposable = new CompositeDisposable();

    private UI.Osd.OsdWindow? window;
    private SystemOsd systemOsd;

    private Subject<OsdMessage> messageSubject;
    private Subject<OsdValue> valueSubject;

    public Osd(UI.Osd.OsdWindow window)
    {
        this.window = window ?? throw new ArgumentNullException(nameof(window));
        systemOsd = new SystemOsd();
        systemOsd.IsVisible
            .Where(i => i)
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(_ => this.window.AppWindow.Hide())
            .DisposeWith(disposable);

        messageSubject = new Subject<OsdMessage>();
        valueSubject = new Subject<OsdValue>();

        messageSubject
            .Throttle(TimeSpan.FromMilliseconds(500))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ShowWindow)
            .DisposeWith(disposable);

        valueSubject
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ShowWindow)
            .DisposeWith(disposable);
    }

    public void Dispose()
    {
        if(window != null)
        {
            window.Close();
            window = null;
        }

        disposable?.Dispose();
        disposable = null;
    }

    public void Show(OsdMessage message)
    {
        messageSubject.OnNext(message);
    }

    public void Show(OsdValue value)
    {
        valueSubject.OnNext(value);
    }

    private void ShowWindow(object data)
    {
        systemOsd?.Hide();
        window?.Show(data);
    }

    /* private sealed class OsdForm : Form
     {
         private CompositeDisposable disposable = new CompositeDisposable();
         private IDisposable hideTimer;
         private OsdData data;

         private bool isDarkTheme;

         private Brush textBrush;
         private Pen accentPen, indicatorBackgroundPen, lightIndicatorBackgroundPen, darkIndicatorBackgroundPen;

         public OsdForm(ISystemEvents systemEvents)
         {
             isDarkTheme = false;

             FormBorderStyle = FormBorderStyle.None;

             ShowInTaskbar = false;
             DoubleBuffered = true;
             TopMost = true;

             Font = new Font("Segoe UI Light", this.DpiScale(Parameters.TextValueHeight), FontStyle.Bold, GraphicsUnit.Pixel);

             darkIndicatorBackgroundPen = CreateIndicatorBackgroundPen(
                 Parameters.IndicatorDarkBackgroundColor,
                 this.DpiScale(Parameters.IndicatorValueHeight)).DisposeWith(disposable);
             lightIndicatorBackgroundPen = CreateIndicatorBackgroundPen(
                 Parameters.IndicatorLightBackgroundColor,
                 this.DpiScale(Parameters.IndicatorValueHeight)).DisposeWith(disposable);

             UpdateTheme();

             systemEvents.AccentColor
                 .Subscribe(color => InvalidateAccentColor(color))
                 .DisposeWith(disposable);

             systemEvents.SystemDarkMode
                 .Subscribe(isDarkMode => IsDarkTheme = isDarkMode)
                 .DisposeWith(disposable);
         }

         public bool IsDarkTheme
         {
             get { return isDarkTheme; }
             private set
             {
                 isDarkTheme = value;

                 UpdateTheme();
             }
         }

         protected override bool ShowWithoutActivation => false;

         public void Show(OsdData data)
         {
             const int SW_SHOWNOACTIVATE = 4;

             this.data = data;

             hideTimer?.Dispose();
             hideTimer = null;

             UpdatePositionAndSize();

             Opacity = 1;
             Invalidate();
             ShowWindow(Handle, SW_SHOWNOACTIVATE);
             BringWindowToTop(Handle);

             hideTimer = Observable
                 .Timer(DateTimeOffset.Now.AddMilliseconds(Parameters.Timeout), TimeSpan.FromMilliseconds(500 / 16))
                 .ObserveOn(SynchronizationContext.Current!)
                 .Subscribe(t =>
                 {
                     Opacity -= .1;
                     Location = new Point(Location.X, Location.Y - 5);

                     if (Opacity <= 0)
                     {
                         Visible = false;
                     }
                 });
         }

         protected override CreateParams CreateParams
         {
             get
             {
                 const int WS_EX_NOACTIVATE = 0x08000000;
                 const int WS_EX_TOOLWINDOW = 0x00000080;

                 var p = base.CreateParams;
                 p.ExStyle |= (WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);

                 return p;
             }
         }
     }*/

    private sealed class SystemOsd : IDisposable
    {
        const uint EVENT_OBJECT_CREATE = 0x8000;
        const uint EVENT_OBJECT_SHOW = 0x8002;
        const uint EVENT_OBJECT_HIDE = 0x8003;
        const uint EVENT_OBJECT_STATECHANGE = 0x800A;

        private Subject<bool>? isVisibleSubject;

        private WINEVENTPROC proc;
        private IntPtr hookId;
        private IntPtr handle;

        public SystemOsd()
        {
            const uint WINEVENT_OUTOFCONTEXT = 0x0000;

            handle = GetSystemOsdHandle();

            isVisibleSubject = new Subject<bool>();
            proc = new WINEVENTPROC(WinEventProc);

            hookId = SetWinEventHook(
               EVENT_OBJECT_CREATE,
               EVENT_OBJECT_STATECHANGE,
               IntPtr.Zero,
               proc,
               (uint)GetShellProcessId(),
               0,
               WINEVENT_OUTOFCONTEXT);

            IsVisible = isVisibleSubject.AsObservable();
        }

        ~SystemOsd()
        {
            Dispose(false);
        }

        public IObservable<bool> IsVisible { get; }

        public void Hide()
        {
            User32.ShowWindow(handle, 0);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                isVisibleSubject?.Dispose();
                isVisibleSubject = null;
            }

            UnhookWinEvent(hookId);
        }

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (idObject != 0 || idChild != 0 || isVisibleSubject == null)
            {
                return;
            }

            if (handle == IntPtr.Zero && (eventType == EVENT_OBJECT_CREATE || eventType == EVENT_OBJECT_SHOW))
            {
                if (GetWindowClassName(hWnd) == "NativeHWNDHost")
                {
                    handle = GetSystemOsdHandle();
                }
            }

            if (handle == hWnd && eventType == EVENT_OBJECT_SHOW)
            {
                isVisibleSubject.OnNext(true);
            }

            if (handle == hWnd && eventType == EVENT_OBJECT_HIDE)
            {
                isVisibleSubject.OnNext(false);
            }
        }

        private static IntPtr GetSystemOsdHandle()
        {
            var outerClass = IsWindows11 ? "XamlExplorerHostIslandWindow" : "NativeHWNDHost";
            var innerClass = IsWindows11 ? "Windows.UI.Composition.DesktopWindowContentBridge" : "DirectUIHWND";
            var innerName = IsWindows11 ? "DesktopWindowXamlSource" : "";

            IntPtr hWndHost;
            while ((hWndHost = FindWindowEx(IntPtr.Zero, IntPtr.Zero, outerClass, "")) != IntPtr.Zero)
            {
                if (FindWindowEx(hWndHost, IntPtr.Zero, innerClass, innerName) != IntPtr.Zero)
                {
                    GetWindowThreadProcessId(hWndHost, out var pid);
                    if (Process.GetProcessById(pid).ProcessName.ToLower() == "explorer")
                    {
                        return hWndHost;
                    }
                }
            }

            return IntPtr.Zero;
        }
    }
}