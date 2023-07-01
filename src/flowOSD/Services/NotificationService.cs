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

using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Resources;
using flowOSD.Native;
using Microsoft.Windows.AppNotifications;

internal sealed class NotificationService : IDisposable, INotificationService
{
    private bool isRegisted;
    private IConfig config;

    public NotificationService(IConfig config)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));

        AppNotificationManager notificationManager = AppNotificationManager.Default;

        notificationManager.NotificationInvoked += OnNotificationInvoked;

        notificationManager.Register();
        isRegisted = true;
    }

    ~NotificationService()
    {
        Dispose(disposing: false);
    }

    public void ShowError(string message, Exception exception)
    {
        ShowError(message, exception?.Message);
    }

    public void ShowError(string message, string? details = null)
    {
        Comctl32.Error($"{config.ProductName}: {Text.Instance.Errors.Title}", message, details ?? string.Empty);
    }

    public void ShowWarning(WarningType warningType)
    {
       // Comctl32.Warning($"{config.ProductName}: {Text.Instance.Warnings.Title}", message, details ?? string.Empty);
    }

    public bool ShowConfirmation(string message, string? details = null)
    {
        return Comctl32.Confirm($"{config.ProductName}: {Text.Instance.Confirmations.Title}", message, details ?? string.Empty);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!isRegisted)
        {
            AppNotificationManager.Default.Unregister();
            isRegisted = false;
        }
    }

    private void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        throw new NotImplementedException();
    }
}
