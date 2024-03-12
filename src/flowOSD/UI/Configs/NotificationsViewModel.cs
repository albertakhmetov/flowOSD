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

namespace flowOSD.UI.Configs;

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using flowOSD.Core.Configs;
using flowOSD.Core.Resources;
using flowOSD.Extensions;

public class NotificationsViewModel : ConfigViewModelBase, IDisposable
{
    private CompositeDisposable? disposable = null;

    public NotificationsViewModel(
        ITextResources textResources,
        IImageResources imageResources,
        IConfig config)
        : base(
            textResources,
            imageResources,
            config, 
            "Config.Notifications.Title",
            "Common.Notification")
    {
    }

    public bool PerformanceMode
    {
        get => Config.Notifications[NotificationType.PerformanceMode];
        set => Config.Notifications[NotificationType.PerformanceMode] = value;
    }

    public bool PowerMode
    {
        get => Config.Notifications[NotificationType.PowerMode]; set => Config.Notifications[NotificationType.PowerMode] = value;
    }

    public bool PowerSource
    {
        get => Config.Notifications[NotificationType.PowerSource];
        set => Config.Notifications[NotificationType.PowerSource] = value;
    }

    public bool Boost
    {
        get => Config.Notifications[NotificationType.Boost];
        set => Config.Notifications[NotificationType.Boost] = value;
    }

    public bool TouchPad
    {
        get => Config.Notifications[NotificationType.TouchPad];
        set => Config.Notifications[NotificationType.TouchPad] = value;
    }

    public bool DisplayRefreshRate
    {
        get => Config.Notifications[NotificationType.DisplayRefreshRate];
        set => Config.Notifications[NotificationType.DisplayRefreshRate] = value;
    }

    public bool Mic
    {
        get => Config.Notifications[NotificationType.Mic];
        set => Config.Notifications[NotificationType.Mic] = value;
    }

    public bool Gpu
    {
        get => Config.Notifications[NotificationType.Gpu];
        set => Config.Notifications[NotificationType.Gpu] = value;
    }

    public bool NotebookMode
    {
        get => Config.Notifications[NotificationType.NotebookMode];
        set => Config.Notifications[NotificationType.NotebookMode] = value;
    }

    public bool AwakeMode
    {
        get => Config.Notifications[NotificationType.AwakeMode];
        set => Config.Notifications[NotificationType.AwakeMode] = value;
    }

    public void Dispose()
    {
        OnDeactivated();
    }

    protected override void OnActivated()
    {
        disposable = new CompositeDisposable();

        Config.Notifications.ValueChanged
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(notificationType => OnPropertyChanged(Enum.GetName(notificationType)))
            .DisposeWith(disposable);

        OnPropertyChanged(null);
    }

    protected override void OnDeactivated()
    {
        disposable?.Dispose();
        disposable = null;
    }
}
