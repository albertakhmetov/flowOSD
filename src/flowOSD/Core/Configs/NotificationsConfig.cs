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

namespace flowOSD.Core.Configs;

using System.Reactive.Linq;
using System.Reactive.Subjects;

public sealed class NotificationsConfig : ConfigBase
{
    private HashSet<NotificationType> notifications;
    private Subject<NotificationType> notificationChangedSubject;

    public NotificationsConfig()
    {
        notifications = new HashSet<NotificationType>();
        notificationChangedSubject = new Subject<NotificationType>();

        NotificationChanged = notificationChangedSubject.AsObservable();
    }

    public bool this[NotificationType notificationType]
    {
        get => notifications.Contains(notificationType);
        set
        {
            if (value)
            {
                notifications.Add(notificationType);
            }
            else
            {
                notifications.Remove(notificationType);
            }

            notificationChangedSubject.OnNext(notificationType);
            OnPropertyChanged();
        }
    }

    public IObservable<NotificationType> NotificationChanged { get; }

    public void Clear()
    {
        var store = notifications.ToArray();
        notifications.Clear();

        foreach (var s in store)
        {
            notificationChangedSubject.OnNext(s);
        }

        OnPropertyChanged(null);
    }
}
