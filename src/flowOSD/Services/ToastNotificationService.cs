using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Windows.AppNotifications;

namespace flowOSD.Services;

internal sealed class ToastNotificationService : IDisposable
{
    private bool isRegisted;

    public ToastNotificationService()
    {
        AppNotificationManager notificationManager = AppNotificationManager.Default;

        notificationManager.NotificationInvoked += OnNotificationInvoked;

        notificationManager.Register();
        isRegisted = true;
    }

    ~ToastNotificationService()
    {
        Dispose(disposing: false);
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
