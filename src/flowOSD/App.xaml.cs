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

namespace flowOSD;

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using flowOSD.Core;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;
using flowOSD.Extensions;
using flowOSD.Native;
using flowOSD.Services;
using flowOSD.Services.Resources;
using flowOSD.UI;
using flowOSD.UI.Commands;
using flowOSD.UI.Configs;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using WinRT.Interop;

public partial class App : Application
{
    private int WM_HELLO_FLOWOSD = User32.RegisterWindowMessage("Hello, it's me! #flowOSD");

    private ElevatedService elevatedService;
    private Mutex? instanceMutex;

    private CompositeDisposable? disposable;
    private IDisposable? helloMessageSubsciption;

    private TextResources textResources;
    private ImageResources imageResources;

    private IUpdateService updateService;

    private ConfigService configService;
    private NotificationService notificationService;
    private MessageQueue messageQueue;
    private SystemEvents systemEvents;
    private KeysSender keysSender;
    private Osd osd;

    private IHardwareService hardwareService;
    private CommandService commandService;
    private HotKeysService hotKeyService;
    private AwakeService awakeService;

    private OsdNotificationService osdNotificationService;

    private NotifyIconService notifyIconService;
    private ServiceWatcher serviceWatcher;

    public App()
    {
        elevatedService = new ElevatedService();
        if (elevatedService.IsElevatedRequest())
        {
            Exit();
            return;
        }

#if !DEBUG
        instanceMutex = new Mutex(true, "com.albertakhmetov.flowosd", out bool isMutexCreated);
        if (!isMutexCreated)
        {
            User32.SendMessage(Messages.HWND_BROADCAST, WM_HELLO_FLOWOSD, IntPtr.Zero, IntPtr.Zero);

            instanceMutex.Dispose();
            instanceMutex = null;
            Exit();

            return;
        }
#endif
        textResources = new TextResources();
        imageResources = new ImageResources();


        InitializeComponent();

        try
        {
            disposable = new CompositeDisposable();
            awakeService = new AwakeService().DisposeWith(disposable);

            serviceWatcher = new ServiceWatcher().DisposeWith(disposable);

            configService = new ConfigService(textResources).DisposeWith(disposable);
            notificationService = new NotificationService(
                textResources,
                configService,
                elevatedService).DisposeWith(disposable);
            updateService = new UpdateService(
                textResources,
                configService);
            messageQueue = new MessageQueue().DisposeWith(disposable);
            systemEvents = new SystemEvents(messageQueue).DisposeWith(disposable);

            keysSender = new KeysSender();
            osd = new Osd(
                textResources,
                imageResources,
                configService,
                systemEvents);

            if (configService.Common.UseMockMode)
            {
                hardwareService = new MockHardwareService(
                    textResources,
                    configService,
                    notificationService);
            }
            else
            {
                hardwareService = new HardwareService(
                    textResources,
                    configService,
                    notificationService,
                    messageQueue,
                    keysSender,
                    serviceWatcher,
                    elevatedService).DisposeWith(disposable);
            }

            osdNotificationService = new OsdNotificationService(
                textResources,
                imageResources,
                configService,
                osd,
                hardwareService,
                awakeService).DisposeWith(disposable);

            commandService = new CommandService(
                textResources,
                imageResources,
                configService,
                hardwareService,
                keysSender,
                systemEvents,
                updateService,
                osd,
                awakeService,
                notificationService,
                elevatedService,
                messageQueue).DisposeWith(disposable);

            hotKeyService = new HotKeysService(
                configService,
                commandService,
                hardwareService).DisposeWith(disposable);

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

            systemEvents.AppException
                .Subscribe(OnError)
                .DisposeWith(disposable);

            updateService.State
               .Where(x => x == UpdateServiceState.ReadyToDownload)
               .ObserveOn(SynchronizationContext.Current!)
               .Subscribe(x =>
               {
                   if (commandService.ResolveNotNull<ConfigCommand>().IsWindowActive)
                   {
                       return;
                   }

                   var showUpdate = notificationService.ShowConfirmation(
                       textResources["Config.About.Update"],
                       textResources["Confirmations.NewVersion"]);

                   if (showUpdate)
                   {
                       commandService.ResolveNotNull<ConfigCommand>().Execute(nameof(AboutViewModel));
                   }
               })
               .DisposeWith(disposable);

            if (configService.Common.CheckForUpdates)
            {
                updateService.CheckUpdate();
            }

            commandService.ResolveNotNull<MainUICommand>().IsWindowVisible
                .CombineLatest(commandService.ResolveNotNull<NotifyMenuCommand>().IsWindowVisible, (x, y) => x || y)
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(x => notifyIconService.Text = x ? "" : $"{configService.ProductName}")
                .DisposeWith(disposable);
        }
        catch (Exception ex)
        {
            Common.TraceException(ex, textResources["Errors.Initialization"]);
            Comctl32.Error(textResources["Errors.CriticalTitle"], textResources["Errors.Initialization"], ex.Message);

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

    private void OnError(Exception? ex)
    {
        if (ex == null)
        {
            return;
        }

        Common.TraceException(ex, textResources["Errors.Unhandled"]);

        notificationService.ShowError(textResources["Errors.Unhandled"], ex);
    }

    private void OnSuspend()
    {
    }

    private void OnResume()
    {
        osd.InitSystemOsd();
    }
}