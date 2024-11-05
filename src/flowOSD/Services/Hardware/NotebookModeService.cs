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

namespace flowOSD.Services.Hardware;

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using flowOSD.Core;
using flowOSD.Core.Hardware;
using flowOSD.Extensions;
using Microsoft.Win32;

sealed class NotebookModeService : IDisposable, INotebookModeService
{
    private CompositeDisposable? disposable = new CompositeDisposable();

    public const string SENSOR_MONITORING_SERVICE = "SensrSvc", SENSOR_SERVICE = "SensorService";
    public const string SLATE_KEY = "SYSTEM\\CurrentControlSet\\Control\\PriorityControl";
    public const string SLATE_PROPERTY = "ConvertibleSlateMode";

    private IElevatedService elevatedService;
    private INotificationService notificationService;

    private bool isMonitoringServiceStarted, isSensorServiceStarted;

    private BehaviorSubject<DeviceState> state;

    public NotebookModeService(IServiceWatcher serviceWatcher, IElevatedService elevatedService, INotificationService notificationService)
    {
        this.elevatedService = elevatedService ?? throw new ArgumentNullException(nameof(elevatedService));
        this.notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

        if (serviceWatcher == null)
        {
            throw new ArgumentNullException(nameof(serviceWatcher));
        }

        isMonitoringServiceStarted = serviceWatcher.IsStarted(SENSOR_MONITORING_SERVICE);
        isSensorServiceStarted = serviceWatcher.IsStarted(SENSOR_SERVICE);

        var deviceState = GetDeviceState();
        state = new BehaviorSubject<DeviceState>(deviceState);

        State = state.AsObservable();
        State
           .Distinct()
           .Throttle(TimeSpan.FromSeconds(2))
           .ObserveOn(SynchronizationContext.Current!)
           .Subscribe(UpdateSlateState)
           .DisposeWith(disposable);

        serviceWatcher.Subscribe(SENSOR_MONITORING_SERVICE, OnServiceStateChanged).DisposeWith(disposable);
        serviceWatcher.Subscribe(SENSOR_SERVICE, OnServiceStateChanged).DisposeWith(disposable);
    }

    public IObservable<DeviceState> State { get; }

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    private void OnServiceStateChanged(string serviceName, bool isStarted)
    {
        if (serviceName == SENSOR_MONITORING_SERVICE)
        {
            isMonitoringServiceStarted = isStarted;
        }

        if (serviceName == SENSOR_SERVICE)
        {
            isSensorServiceStarted = isStarted;
        }

        UpdateServiceState();
    }

    private void UpdateServiceState()
    {
        var deviceState = GetDeviceState();

        if (state.Value != deviceState)
        {
            state.OnNext(deviceState);
        }
    }

    private DeviceState GetDeviceState()
    {
        return isMonitoringServiceStarted == false && isSensorServiceStarted == false ? DeviceState.Enabled : DeviceState.Disabled;
    }

    private void UpdateSlateState(DeviceState notebookModeState)
    {
        if (notebookModeState == DeviceState.Disabled)
        {
            return;
        }

        using var key = Registry.LocalMachine.OpenSubKey(SLATE_KEY, false);
        if (key?.GetValue(SLATE_PROPERTY) is int slateMode == false || slateMode == 1)
        {
            return;
        }

        if (elevatedService.IsElevated)
        {
            elevatedService.DisableSlateMode();
        }
        else
        {
            notificationService.ShowWarning(Core.Configs.WarningType.SlateMode);
        }
    }
}
