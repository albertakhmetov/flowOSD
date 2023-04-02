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

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;

using static Native.UxTheme;
using static Native.User32;
using static Native.Messages;
using flowOSD.Extensions;
using Microsoft.Win32;
using Windows.UI;
using flowOSD.Core;
using Windows.Foundation;
using flowOSD.Native;

sealed partial class SystemEvents : ISystemEvents, IDisposable
{
    private const string PERSONALIZE_KEY = "Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";
    private const string PERSONALIZE_APP_VALUE = "AppsUseLightTheme";
    private const string PERSONALIZE_SYSTEM_VALUE = "SystemUsesLightTheme";

    private CompositeDisposable? disposable = new CompositeDisposable();

    private BehaviorSubject<bool> systemDarkModeSubject;
    private BehaviorSubject<bool> appsDarkModeSubject;
    private BehaviorSubject<Color> accentColorSubject;
    private BehaviorSubject<int> dpiSubject;

    public SystemEvents(IMessageQueue messageQueue)
    { 
        systemDarkModeSubject = new BehaviorSubject<bool>(IsSystemUseDarkMode());
        appsDarkModeSubject = new BehaviorSubject<bool>(IsAppUseDarkMode());
        accentColorSubject = new BehaviorSubject<Color>(GetAccentColor());

        dpiSubject = new BehaviorSubject<int>(GetDpiForWindow(messageQueue.Handle));

        SystemDarkMode = systemDarkModeSubject.AsObservable();
        AppsDarkMode = appsDarkModeSubject.AsObservable();
        AccentColor = accentColorSubject.AsObservable();
        Dpi = dpiSubject.AsObservable();

    
        //AppShutdown = Observable
        //    .FromEventPattern<EventHandler, EventArgs>(h => Application.ApplicationExit += h, h => Application.ApplicationExit -= h)
        //    .Select(_ => true)
        //    .AsObservable();
        //AppException = Observable
        //    .FromEventPattern<ThreadExceptionEventHandler, ThreadExceptionEventArgs>(h => Application.ThreadException += h, h => Application.ThreadException -= h)
        //    .Select(x => x.EventArgs.Exception)
        //    .AsObservable();

        messageQueue.Subscribe(WM_WININICHANGE, ProcessMessage).DisposeWith(disposable);
        messageQueue.Subscribe(WM_DISPLAYCHANGE, ProcessMessage).DisposeWith(disposable);
        messageQueue.Subscribe(WM_DPICHANGED, ProcessMessage).DisposeWith(disposable);
    }

    public IObservable<bool> SystemDarkMode { get; }

    public IObservable<bool> AppsDarkMode { get; }

    public IObservable<Color> AccentColor { get; }

    public IObservable<int> Dpi { get; }

    public IObservable<bool> AppShutdown { get; }

    public IObservable<Exception> AppException { get; }

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    private void ProcessMessage(int messageId, IntPtr wParam, IntPtr lParam)
    {
        if (messageId == WM_WININICHANGE && Marshal.PtrToStringAnsi(lParam) == "ImmersiveColorSet")
        {
            systemDarkModeSubject.OnNext(IsSystemUseDarkMode());
            appsDarkModeSubject.OnNext(IsAppUseDarkMode());
            accentColorSubject.OnNext(GetAccentColor());
        }

        //if (messageId == WM_DISPLAYCHANGE)
        //{
        //    primaryWorkAreaSubject.OnNext(GetPrimaryWorkArea());
        //}

        if (messageId == WM_DPICHANGED)
        {
            dpiSubject.OnNext(HiWord(wParam));
        }
    }

    private bool IsAppUseDarkMode()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(PERSONALIZE_KEY, false))
        {
            return key?.GetValue(PERSONALIZE_APP_VALUE)?.ToString() != "1";
        }
    }

    private bool IsSystemUseDarkMode()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(PERSONALIZE_KEY, false))
        {
            return key?.GetValue(PERSONALIZE_SYSTEM_VALUE)?.ToString() != "1";
        }
    }
}