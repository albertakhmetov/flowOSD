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

using System.Diagnostics;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Resources;
using flowOSD.Native;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

internal sealed class NotificationService : IDisposable, INotificationService
{
    private const string ACTION = nameof(ACTION);

    private AppNotificationManager notificationManager;
    private bool isRegisted;

    private ITextResources textResources;
    private IConfig config;
    private IElevatedService elevatedService;

    public NotificationService(
        ITextResources textResources,
        IConfig config, 
        IElevatedService elevatedService)
    {
        this.textResources = textResources ?? throw new ArgumentNullException(nameof(textResources));
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.elevatedService = elevatedService ?? throw new ArgumentNullException(nameof(elevatedService));

        notificationManager = AppNotificationManager.Default;

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
        Comctl32.Error($"{config.ProductName}: {textResources["Errors.Title"]}", message, details ?? string.Empty);
    }

    public void ShowWarning(WarningType warningType)
    {
        AppNotification notification;

        switch (warningType)
        {
            case WarningType.SlateMode:
                notification = new AppNotificationBuilder()
                    .AddText(textResources["Warnings.NotebookMode.Title"])
                    .AddText(textResources["Warnings.NotebookMode.Text"])
                    .AddArgument(ACTION, nameof(MoreDetailsAboutSlateMode))
                    .AddButton(
                        new AppNotificationButton(textResources["Warnings.NotebookMode.Disable"])
                            .AddArgument(ACTION, nameof(DisableNotebookMode)))
                    .AddButton(
                        new AppNotificationButton(textResources["Warnings.NotebookMode.DisableSlate"])
                            .AddArgument(ACTION, nameof(DisableSlateMode)))
                    .BuildNotification();
                break;

            default:
                return;
        }

        notificationManager.Show(notification);
    }

    public bool ShowConfirmation(string message, string? details = null)
    {
        return Comctl32.Confirm($"{config.ProductName}: {textResources["Confirmations.Title"]}", message, details ?? string.Empty);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (isRegisted)
        {
            AppNotificationManager.Default.RemoveAllAsync().AsTask().Wait();

            AppNotificationManager.Default.Unregister();
            isRegisted = false;
        }
    }

    private void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        if (args.Arguments.ContainsKey(ACTION))
        {
            switch(args.Arguments[ACTION])
            {
                case nameof(MoreDetailsAboutSlateMode):
                    MoreDetailsAboutSlateMode();
                    break;

                case nameof(DisableNotebookMode):
                    DisableNotebookMode();
                    break;

                case nameof(DisableSlateMode):
                    DisableSlateMode();
                    break;
            }
        }
    }

    private void MoreDetailsAboutSlateMode()
    {
        OpenUrl(textResources["Links.NotebookMode"]);
    }

    private void DisableNotebookMode()
    {
        elevatedService.DisableNotebookMode();
    }

    private void DisableSlateMode()
    {
        elevatedService.DisableSlateMode();
    }

    private void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
