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

namespace flowOSD;

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using flowOSD.Core;
using flowOSD.Core.Hardware;
using flowOSD.Extensions;
using flowOSD.Native;
using flowOSD.Services;
using flowOSD.UI;
using flowOSD.UI.Commands;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

public partial class App : Application
{
    private int WM_HELLO_FLOWOSD = User32.RegisterWindowMessage("Hello, it's me! #flowOSD");

    private Mutex? instanceMutex;

    private CompositeDisposable? disposable;
    private IDisposable? helloMessageSubsciption;

    private IUpdater updater;

    private ConfigService configService;
    private MessageQueue messageQueue;
    private SystemEvents systemEvents;
    private KeysSender keysSender;
    private Osd osd;

    private HardwareService hardwareService;
    private CommandService commandService;
    private HotKeysService hotKeyService;

    private NotificationService notificationService;

    private NotifyIconService notifyIconService;

    public App()
    {
#if !DEBUG
        instanceMutex = new Mutex(true, "com.albertakhmetov.flowosd", out bool isMutexCreated);
        if (!isMutexCreated)
        {
            User32.SendMessage(Messages.HWND_BROADCAST, WM_HELLO_FLOWOSD, IntPtr.Zero, IntPtr.Zero);

            instanceMutex.Dispose();
            instanceMutex = null;
            Exit();
        }
#endif

        InitializeComponent();

        try
        {
            disposable = new CompositeDisposable();

            configService = new ConfigService().DisposeWith(disposable);
            updater = new Updater(configService);
            messageQueue = new MessageQueue().DisposeWith(disposable);
            systemEvents = new SystemEvents(messageQueue).DisposeWith(disposable);

            systemEvents.AppException
                .Subscribe(ex =>
                {
                    if (ex != null)
                    {
                        Common.TraceException(ex, "Unhandled application exception");
                        Comctl32.Error("ERROR", "Unhandled application exception", ex.Message);
                    }
                })
                .DisposeWith(disposable);

            keysSender = new KeysSender();
            osd = new Osd(configService, systemEvents);

            hardwareService = new HardwareService(configService, messageQueue, keysSender).DisposeWith(disposable);
            commandService = new CommandService(configService, hardwareService, keysSender, systemEvents, updater, osd).DisposeWith(disposable);
            hotKeyService = new HotKeysService(configService, commandService, hardwareService).DisposeWith(disposable);

            notificationService = new NotificationService(configService, osd, hardwareService).DisposeWith(disposable);
            notifyIconService = new NotifyIconService(
                configService,
                messageQueue,
                systemEvents,
                commandService,
                hardwareService.ResolveNotNull<IAtk>()).DisposeWith(disposable);
            notifyIconService.Show();

            var powerManagement = hardwareService.ResolveNotNull<IPowerManagement>();

            powerManagement.PowerEvent
               .Where(x => x == PowerEvent.Suspend)
               .Throttle(TimeSpan.FromMicroseconds(50))
               .ObserveOn(SynchronizationContext.Current!)
               .Subscribe(_ => OnSuspend())
               .DisposeWith(disposable);

            powerManagement.PowerEvent
               .Where(x => x == PowerEvent.Resume)
               .Throttle(TimeSpan.FromSeconds(5))
               .ObserveOn(SynchronizationContext.Current!)
               .Subscribe(_ => OnResume())
               .DisposeWith(disposable);

            messageQueue.Subscribe(WM_HELLO_FLOWOSD, ProcessMessage).DisposeWith(disposable);
            messageQueue.Subscribe(Messages.WM_TASKBARCREATED, ProcessMessage).DisposeWith(disposable);
        }
        catch (Exception ex)
        {
            Common.TraceException(ex, "An exception has occured during initialization");
            Comctl32.Error("ERORR", "Unhandled application exception", ex.Message);

            Exit();
        }
    }

    public void ShutDown()
    {
        instanceMutex?.Dispose();
        instanceMutex = null;

        helloMessageSubsciption?.Dispose();
        helloMessageSubsciption = null;

        osd?.Dispose();

        commandService?.Resolve<ConfigCommand>()?.Dispose();
        commandService?.Resolve<NotifyMenuCommand>()?.Dispose();
        commandService?.Resolve<MainUICommand>()?.Dispose();

        notifyIconService.Hide();

        disposable?.Dispose();
        disposable = null;

        Exit();
    }

    private void ProcessMessage(int messageId, IntPtr wParam, IntPtr lParam)
    {
        if (messageId == WM_HELLO_FLOWOSD)
        {
            commandService.ResolveNotNull<MainUICommand>().Execute();
        }
        else if (messageId == Messages.WM_TASKBARCREATED)
        {
            osd.InitSystemOsd();
        }
    }

    private void OnSuspend()
    {
    }

    private void OnResume()
    {
        osd.InitSystemOsd();
    }
}