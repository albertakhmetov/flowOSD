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

namespace flowOSD.Services.Hardware.Hid;

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using flowOSD.Extensions;
using Microsoft.Win32;
using static flowOSD.Native.User32;
using static flowOSD.Native.Messages;
using System.Management;
using System.Security.Principal;
using flowOSD.Services.Hardware;
using flowOSD.Core.Hardware;

sealed class TouchPad : IDisposable, ITouchPad
{
    public const int FEATURE_KBD_REPORT_ID = 0x5a;

    private const string TOUCHPAD_STATE_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\PrecisionTouchPad\Status";
    private const string TOUCHPAD_STATE_VALUE = "Enabled";

    private CompositeDisposable? disposable = new CompositeDisposable();

    private HidDevice device;

    private ManagementEventWatcher watcher;
    private BehaviorSubject<DeviceState> stateSubject;

    public TouchPad(HidDevice device)
    {
        this.device = device ?? throw new ArgumentNullException(nameof(device));

        stateSubject = new BehaviorSubject<DeviceState>(GetState());
        State = stateSubject.AsObservable();

        var sid = WindowsIdentity.GetCurrent().User;
        var query = "SELECT * FROM RegistryValueChangeEvent WHERE Hive='HKEY_USERS' " +
            $"AND KeyPath='{sid}\\\\{TOUCHPAD_STATE_KEY.Replace("\\", "\\\\")}' AND ValueName='{TOUCHPAD_STATE_VALUE}'";

        watcher = new ManagementEventWatcher(query).DisposeWith(disposable);
        watcher.EventArrived += OnWmiEvent;
        watcher.Start();
    }

    public IObservable<DeviceState> State { get; }

    public void Toggle()
    {
        var isOk = WriteToggle();
    }

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    private bool WriteToggle()
    {
        return device.WriteFeatureData(
            FEATURE_KBD_REPORT_ID,
            0xf4,
            0x6b);
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

    private void OnWmiEvent(object sender, EventArrivedEventArgs e)
    {
        stateSubject.OnNext(GetState());
    }
}
