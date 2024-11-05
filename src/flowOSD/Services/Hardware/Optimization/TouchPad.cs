/*  Copyright © 2021-2024, Albert Akhmetov <akhmetov@live.com>   
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

namespace flowOSD.Services.Hardware.Optimization;

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using flowOSD.Core;
using flowOSD.Core.Hardware;
using flowOSD.Extensions;
using Microsoft.Win32;
using static flowOSD.Native.User32;

sealed class TouchPad : ITouchPad, IDisposable
{
    private static int WM_TOUCHPAD = RegisterWindowMessage("Touchpad status reported from ATKHotkey");

    private const string TOUCHPAD_STATE_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\PrecisionTouchPad\Status";
    private const string TOUCHPAD_STATE_VALUE = "Enabled";

    private CompositeDisposable? disposable = new CompositeDisposable();

    private BehaviorSubject<DeviceState> stateSubject;
    private IMessageQueue messageQueue;
    private IKeysSender keysSender;

    public TouchPad(IMessageQueue messageQueue, IKeysSender keysSender)
    {
        this.messageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
        this.keysSender = keysSender ?? throw new ArgumentNullException(nameof(keysSender));

        stateSubject = new BehaviorSubject<DeviceState>(GetState());
        State = stateSubject.AsObservable();

        this.messageQueue.Subscribe(WM_TOUCHPAD, ProcessMessage).DisposeWith(disposable);
    }

    public IObservable<DeviceState> State { get; }

    public void Toggle()
    {
        keysSender.SendKeys(Windows.System.VirtualKey.F24, Windows.System.VirtualKey.Control, Windows.System.VirtualKey.LeftWindows);
    }

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    private DeviceState GetState()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(TOUCHPAD_STATE_KEY, false))
        {
            return key?.GetValue(TOUCHPAD_STATE_VALUE)?.ToString() == "1"
                ? DeviceState.Enabled
                : DeviceState.Disabled;
        }
    }

    private void ProcessMessage(int messageId, IntPtr wParam, IntPtr lParam)
    {
        if (messageId == WM_TOUCHPAD)
        {
            stateSubject.OnNext((int)lParam == 1 ? DeviceState.Enabled : DeviceState.Disabled);
        }
    }
}