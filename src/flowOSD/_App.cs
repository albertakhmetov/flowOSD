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

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using flowOSD.Api;
using flowOSD.Api.Configs;
using flowOSD.Api.Hardware;
using flowOSD.Extensions;
using flowOSD.Hardware;
using flowOSD.Services;
using flowOSD.UI;
using flowOSD.UI.Commands;
using flowOSD.UI.Components;
using static flowOSD.Extensions.Common;

sealed partial class _App : IDisposable
{
    private CompositeDisposable? disposable = new CompositeDisposable();

    private IConfig config;
    private IUpdater updater;

    private MessageQueue messageQueue;
    private SystemEvents systemEvents;
    private KeysSender keysSender;

    private Osd osd;
    private MainUI mainUI;
    private UpdaterUI updaterUI;

    private HardwareService hardwareService;
    private CommandService commandService;

    public _App(IConfig config)
    {
        this.config = config;

        updater = new Updater(config);

        ApplicationContext = new ApplicationContext().DisposeWith(disposable);

        messageQueue = new MessageQueue().DisposeWith(disposable);
        keysSender = new KeysSender();
        hardwareService = new HardwareService(config, messageQueue, keysSender).DisposeWith(disposable);

        systemEvents = new SystemEvents(messageQueue).DisposeWith(disposable);
        systemEvents.AppException
            .Subscribe(ex =>
            {
                TraceException(ex, "Unhandled application exception");
                MessageBox.Show(ex.Message, "Unhandled application exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            })
            .DisposeWith(disposable);

        // Notifications

        osd = new Osd(systemEvents);
        new NotificationService(config, osd, hardwareService).DisposeWith(disposable);

        // Commands

        commandService = new CommandService(
            config,
            hardwareService,
            keysSender,
            systemEvents,
            updater,
            osd);

        mainUI = new MainUI(
            config,
            systemEvents,
            commandService,
            hardwareService).DisposeWith(disposable);

        updaterUI = new UpdaterUI(config, systemEvents, updater).DisposeWith(disposable!);

        commandService.Register(new MainUICommand(mainUI));
        commandService.Register(new UpdateCommand(updater, updaterUI));

        new NotifyIconUI(
            config,
            messageQueue,
            systemEvents,
            commandService,
            hardwareService.ResolveNotNull<IAtkWmi>()).DisposeWith(disposable);

        new HotKeysService(
            config,
            commandService,
            hardwareService.ResolveNotNull<IKeyboard>()).DisposeWith(disposable);

        if (config.Common.CheckForUpdates)
        {
            commandService.Resolve<UpdateCommand>()?.Execute(false);
        }
    }

    void IDisposable.Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    public ApplicationContext ApplicationContext { get; }
}