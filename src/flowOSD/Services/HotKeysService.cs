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

using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Windows.Input;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;
using flowOSD.Extensions;

sealed class HotKeysService : IDisposable
{
    private CompositeDisposable? disposable = new CompositeDisposable();

    private IConfig config;
    private ICommandService commandService;
    private IKeyboard keyboard;
    private IHardwareFeatures hardwareFeatures;

    private Dictionary<AtkKey, Binding> keys = new Dictionary<AtkKey, Binding>();

    public HotKeysService(IConfig config, ICommandService commandService, IHardwareService hardwareService)
    {
        if(hardwareService== null)
        {
            throw new ArgumentNullException(nameof(hardwareService));
        }

        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

        keyboard = hardwareService.ResolveNotNull<IKeyboard>();
        hardwareFeatures = hardwareService.ResolveNotNull<IHardwareFeatures>();

        config.HotKeys.KeyChanged
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(UpdateBindings)
            .DisposeWith(disposable);

        keyboard.KeyPressed
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ExecuteCommand)
            .DisposeWith(disposable);

        RegisterHotKeys();
    }

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    private void Register(AtkKey key, HotKeysConfig.Command? commandInfo)
    {
        var command = commandService.Resolve(commandInfo?.Name);

        if (command != null)
        {
            keys[key] = new Binding(command, commandInfo?.Parameter);
        }
        else
        {
            keys.Remove(key);
        }
    }

    private void RegisterHotKeys()
    {
        foreach (var key in Enum.GetValues<AtkKey>())
        {
            UpdateBindings(key);
        }
    }

    private void UpdateBindings(AtkKey key)
    {
        Register(key, config.HotKeys[key]);
    }

    private void ExecuteCommand(AtkKey key)
    {
        if (keys.TryGetValue(key, out Binding? binding) && IsNotForOptimizationService(key))
        {
            binding.Execute();
        }
    }

    private bool IsNotForOptimizationService(AtkKey key)
    {
        if (!hardwareFeatures.OptimizationService)
        {
            return true;
        }
        else
        {
            return key == AtkKey.Aura
                || key == AtkKey.Fan
                || key == AtkKey.Rog
                || key == AtkKey.Copy
                || key == AtkKey.Paste;
        }
    }

    private sealed class Binding
    {
        public Binding(ICommand command, object? commandParameter = null)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
            CommandParameter = commandParameter;
        }

        public ICommand Command { get; }

        public object? CommandParameter { get; }

        public void Execute()
        {
            Command.Execute(CommandParameter);
        }
    }
}
