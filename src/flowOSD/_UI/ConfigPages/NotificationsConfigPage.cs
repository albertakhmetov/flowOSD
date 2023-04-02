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
namespace flowOSD.UI.ConfigPages;

using flowOSD.Api;
using flowOSD.Api.Configs;
using flowOSD.Extensions;
using flowOSD.UI.Components;
using System.Reactive.Disposables;
using System.Reactive.Linq;

internal class NotificationsConfigPage : ConfigPageBase
{
    private CompositeDisposable? disposable = new CompositeDisposable();
    private Dictionary<NotificationType, CxToggle> toggles = new Dictionary<NotificationType, CxToggle>();

    public NotificationsConfigPage(IConfig config, CxTabListener tabListener)
        : base(config, tabListener)
    {
        Text = "Notifications";

        toggles[NotificationType.PowerSource] = AddConfig(
             UIImages.Hardware_AC,
             "Show power source notifications",
             () => config.Notifications[NotificationType.PowerSource],
             x => config.Notifications[NotificationType.PowerSource] = x);

        toggles[NotificationType.Boost] = AddConfig(
            UIImages.Hardware_Cpu,
            "Show CPU boost mode notifications",
             () => config.Notifications[NotificationType.Boost],
             x => config.Notifications[NotificationType.Boost] = x);

        toggles[NotificationType.PerformanceMode] = AddConfig(
            UIImages.Performance_Turbo,
            "Show performance mode notifications",
             () => config.Notifications[NotificationType.PerformanceMode],
             x => config.Notifications[NotificationType.PerformanceMode] = x);

        toggles[NotificationType.PowerMode] = AddConfig(
            UIImages.Power_Balanced,
            "Show power mode notifications",
             () => config.Notifications[NotificationType.PowerMode],
             x => config.Notifications[NotificationType.PowerMode] = x);

        toggles[NotificationType.TouchPad] = AddConfig(
            UIImages.Hardware_TouchPad,
            "Show TouchPad notifications",
             () => config.Notifications[NotificationType.TouchPad],
             x => config.Notifications[NotificationType.TouchPad] = x);

        toggles[NotificationType.DisplayRefreshRate] = AddConfig(
            UIImages.Hardware_Screen,
            "Show display refesh rate notifications",
             () => config.Notifications[NotificationType.DisplayRefreshRate],
             x => config.Notifications[NotificationType.DisplayRefreshRate] = x);

        toggles[NotificationType.Mic] = AddConfig(
            UIImages.Hardware_Mic,
            "Show microphone status notifications",
             () => config.Notifications[NotificationType.Mic],
             x => config.Notifications[NotificationType.Mic] = x);

        toggles[NotificationType.Gpu] = AddConfig(
            UIImages.Hardware_Gpu,
            "Show dGPU notifications",
             () => config.Notifications[NotificationType.Gpu],
             x => config.Notifications[NotificationType.Gpu] = x);

        config.Notifications.NotificationChanged
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(x =>
            {
                if (toggles.TryGetValue(x, out var toggle))
                {
                    toggle.IsChecked = config.Notifications[x];
                }
            })
            .DisposeWith(disposable);
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
}
