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

namespace flowOSD.Services;

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using flowOSD.Core;
using flowOSD.Core.Hardware;
using flowOSD.Extensions;
using static flowOSD.Native.Kernel32;

internal class AwakeService : IAwakeService, IDisposable
{
    private static readonly TimeSpan TimerPeriod = TimeSpan.FromSeconds(4);
    private BehaviorSubject<DeviceState> stateSubject;

    private CompositeDisposable? disposable;
    private IDisposable? monitor;

    public AwakeService()
    {
        disposable = new CompositeDisposable();

        stateSubject = new BehaviorSubject<DeviceState>(DeviceState.Disabled);

        stateSubject
            .Throttle(TimeSpan.FromMilliseconds(250))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(UpdateMonitorState)
            .DisposeWith(disposable);

        State = stateSubject.AsObservable();
    }

    public IObservable<DeviceState> State { get; }

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;

        monitor?.Dispose();
        monitor = null;
    }

    public async Task Toggle()
    {
        stateSubject.OnNext(stateSubject.Value == DeviceState.Disabled
            ? DeviceState.Enabled
            : DeviceState.Disabled);

        await Task.Delay(TimeSpan.FromMilliseconds(50));
    }

    private void UpdateMonitorState(DeviceState state)
    {
        if (state == DeviceState.Disabled)
        {
            monitor?.Dispose();
            monitor = null;

            SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);

            return;
        }

        if (state == DeviceState.Enabled && monitor == null)
        {
            monitor = Observable
                .Timer(TimerPeriod)
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(_ =>
                {
                    if (stateSubject.Value == DeviceState.Enabled)
                    {
                        SetThreadExecutionState(
                            EXECUTION_STATE.ES_DISPLAY_REQUIRED |
                            EXECUTION_STATE.ES_SYSTEM_REQUIRED |
                            EXECUTION_STATE.ES_CONTINUOUS);
                    }
                });
        }
    }
}