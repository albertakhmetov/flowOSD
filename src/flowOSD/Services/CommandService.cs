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

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;
using flowOSD.UI.Commands;

sealed class CommandService : ICommandService, IDisposable
{
    private ITextResources textResources;
    private IImageResources imageResources;
    private IConfig config;
    private IHardwareService hardwareService;
    private IKeysSender keysSender;
    private IUpdateService updateService;
    private INotificationService notificationService;
    private IAwakeService awakeService;
    private IElevatedService elevatedService;
    private IMessageQueue messageQueue;

    private Dictionary<string, Lazy<CommandBase>> instances = new Dictionary<string, Lazy<CommandBase>>();

    public CommandService(
        ITextResources textResources,
        IImageResources imageResources,
        IConfig config,
        IHardwareService hardwareService,
        IKeysSender keysSender,
        ISystemEvents systemEvents,
        IUpdateService updateService,
        IOsd osd,
        IAwakeService awakeService,
        INotificationService notificationService,
        IElevatedService elevatedService,
        IMessageQueue messageQueue)
    {
        this.textResources = textResources ?? throw new ArgumentNullException(nameof(textResources));
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.hardwareService = hardwareService ?? throw new ArgumentNullException(nameof(hardwareService));
        this.keysSender = keysSender ?? throw new ArgumentNullException(nameof(keysSender));
        this.updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
        this.notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        this.awakeService = awakeService ?? throw new ArgumentNullException(nameof(awakeService));
        this.elevatedService = elevatedService ?? throw new ArgumentNullException(nameof(elevatedService));
        this.messageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));

        Register(() => new ToggleBoostCommand(
            textResources,
            imageResources,
            hardwareService.ResolveNotNull<IPowerManagement>()));
        Register(() => new PerformanceCommand(
            textResources,
            imageResources,
            config,
            hardwareService.ResolveNotNull<IAtk>(),
            hardwareService.ResolveNotNull<IPowerManagement>(),
            hardwareService.ResolveNotNull<IPerformanceService>()));
        Register(() => new PowerModeCommand(
            textResources,
            imageResources,
            hardwareService.ResolveNotNull<IPowerManagement>()));
        Register(() => new GpuCommand(
            textResources,
            imageResources,
            config,
            hardwareService.ResolveNotNull<IAtk>(),
            notificationService));
        Register(() => new TouchPadCommand(
            textResources,
            imageResources,
            hardwareService.ResolveNotNull<ITouchPad>()));
        Register(() => new MicrophoneCommand(
            textResources,
            imageResources,
            config,
            osd,
            hardwareService.ResolveNotNull<IMicrophone>()));
        Register(() => new DisplayRefreshRateCommand(
            textResources,
            imageResources,
            hardwareService.ResolveNotNull<IPowerManagement>(),
            hardwareService.ResolveNotNull<IDisplay>(),
            config.Common));

        Register(() => new ExitCommand(
            textResources,
            imageResources));
        Register(() => new RestartAppCommand(
            textResources,
            imageResources,
            notificationService));
        Register(() => new SuspendCommand(
            textResources,
            imageResources));

        Register(() => new ConfigCommand(
            textResources,
            imageResources,
            config,
            systemEvents,
            this,
            hardwareService,
            updateService));
        Register(() => new MainUICommand(
            textResources,
            imageResources,
            config,
            systemEvents,
            this,
            hardwareService,
            elevatedService));
        Register(() => new NotifyMenuCommand(
            textResources,
            imageResources,
            config,
            systemEvents,
            this));
        Register(() => new UpdateCommand(
            textResources,
            imageResources,
            updateService));

        Register(() => new DisplayBrightnessCommand(
            textResources,
            imageResources,
            config,
            osd,
            hardwareService.ResolveNotNull<IDisplayBrightness>()));
        Register(() => new KeyboardBacklightCommand(
            textResources,
            imageResources,
            config,
            osd,
            hardwareService));

        Register(() => new NotebookModeCommand(
            textResources,
            imageResources,
            config,
            hardwareService.ResolveNotNull<IAtk>(),
            hardwareService.ResolveNotNull<INotebookModeService>(),
            elevatedService));

        Register(() => new DisplayOffCommand(
            textResources,
            imageResources,
            messageQueue));

        Register(() => new AwakeCommand(
            textResources,
            imageResources,
            awakeService));
    }

    public void Dispose()
    {
        foreach (var i in instances)
        {
            if (i.Value.IsValueCreated && i.Value is IDisposable disposable)
            {
                disposable.Dispose();
            }

            instances.Clear();
        }
    }

    public void Register<T>(Func<T> commandFactory) where T : CommandBase
    {
        instances[typeof(T).Name] = new Lazy<CommandBase>(commandFactory, isThreadSafe: false);
    }

    public void Register(CommandBase command, params CommandBase[] commands)
    {
        instances[command.GetType().Name] = new Lazy<CommandBase>(command);

        foreach (var c in commands)
        {
            instances[c.GetType().Name] = new Lazy<CommandBase>(c);
        }
    }

    public CommandBase? Resolve(string? commandName)
    {
        return !string.IsNullOrEmpty(commandName) && instances.TryGetValue(commandName, out Lazy<CommandBase>? lazyCommand)
            ? lazyCommand.Value : null;
    }

    public T? Resolve<T>() where T : CommandBase
    {
        return Resolve(typeof(T).Name) as T;
    }

    public T ResolveNotNull<T>() where T : CommandBase
    {
        return Resolve<T>() ?? throw new InvalidOperationException($"Can't resolve {typeof(T).Name}");
    }

    public bool TryResolve<T>(out T? command) where T : CommandBase
    {
        command = Resolve<T>();
        return command != null;
    }

    public IList<CommandBase> Commands => instances.Values.Select(i => i.Value).Where(i => i.CanExecuteWithHotKey).ToArray();
}