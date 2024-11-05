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

namespace flowOSD.UI.Configs;

using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;
using flowOSD.Extensions;
using flowOSD.UI.Commands;

public class KeyboardViewModel : ConfigViewModelBase, IDisposable
{
    private CompositeDisposable? disposable = null;

    private IHardwareFeatures hardwareFeatures;
    private ICommandService commandService;

    public KeyboardViewModel(
        ITextResources textResources,
        IImageResources imageResources,
        IConfig config,
        ICommandService commandService, 
        IHardwareService hardwareService)
        : base(
            textResources,
            imageResources,
            config, 
            "Config.Keyboard.Title",
            "Common.KeyboardSettings")
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (hardwareService == null)
        {
            throw new ArgumentNullException(nameof(hardwareService));
        }

        this.commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
        hardwareFeatures = hardwareService.ResolveNotNull<IHardwareFeatures>();

        BacklightControl = !hardwareFeatures.OptimizationService;
        Timeouts = new ReadOnlyCollection<int>(new int[] { 5, 15, 30, 60, 600, 0 });

        var commands = new ReadOnlyCollection<CommandBase>(
            new CommandBase[] { CommandBase.Empty }.Union(commandService.Commands.Where(i => i.CanExecuteWithHotKey)).ToArray());

        var hotkeys = new List<KeyboardHotKeyViewModel>()
        {
            new KeyboardHotKeyViewModel(TextResources,ImageResources, config.HotKeys, commandService, AtkKey.Copy, commands),
            new KeyboardHotKeyViewModel(TextResources,ImageResources, config.HotKeys, commandService, AtkKey.Paste, commands),
            new KeyboardHotKeyViewModel(TextResources,ImageResources, config.HotKeys, commandService, AtkKey.Rog, commands),
        };

        if (!hardwareFeatures.OptimizationService)
        {
            hotkeys.Add(new KeyboardHotKeyViewModel(TextResources, ImageResources, config.HotKeys, commandService, AtkKey.Mic, commands));
            hotkeys.Add(new KeyboardHotKeyViewModel(TextResources, ImageResources, config.HotKeys, commandService, AtkKey.BacklightDown, commands));
            hotkeys.Add(new KeyboardHotKeyViewModel(TextResources, ImageResources, config.HotKeys, commandService, AtkKey.BacklightUp, commands));
        }

        hotkeys.Add(new KeyboardHotKeyViewModel(TextResources, ImageResources, config.HotKeys, commandService, AtkKey.Aura, commands));
        hotkeys.Add(new KeyboardHotKeyViewModel(TextResources, ImageResources, config.HotKeys, commandService, AtkKey.Fan, commands));

        if (!hardwareFeatures.OptimizationService)
        {
            hotkeys.Add(new KeyboardHotKeyViewModel(TextResources, ImageResources, config.HotKeys, commandService, AtkKey.BrightnessDown, commands));
            hotkeys.Add(new KeyboardHotKeyViewModel(TextResources, ImageResources, config.HotKeys, commandService, AtkKey.BrightnessUp, commands));
            hotkeys.Add(new KeyboardHotKeyViewModel(TextResources, ImageResources, config.HotKeys, commandService, AtkKey.TouchPad, commands));
            hotkeys.Add(new KeyboardHotKeyViewModel(TextResources, ImageResources, config.HotKeys, commandService, AtkKey.Sleep, commands));
            hotkeys.Add(new KeyboardHotKeyViewModel(TextResources, ImageResources, config.HotKeys, commandService, AtkKey.Wireless, commands));
        }

        HotKeys = new ReadOnlyCollection<KeyboardHotKeyViewModel>(hotkeys);
    }

    public bool BacklightControl { get; }

    public int KeyboardBacklightTimeout
    {
        get => Config.Common.KeyboardBacklightTimeout;
        set => Config.Common.KeyboardBacklightTimeout = value;
    }

    public bool KeyboardBacklightWithDisplay
    {
        get => Config.Common.KeyboardBacklightWithDisplay;
        set => Config.Common.KeyboardBacklightWithDisplay = value;
    }

    public IReadOnlyCollection<int> Timeouts { get; }

    public IReadOnlyCollection<KeyboardHotKeyViewModel> HotKeys { get; }

    public void Dispose()
    {
        OnDeactivated();
    }

    public void ResetHotkeys()
    {
        foreach (var h in HotKeys)
        {
            switch (h.Key)
            {
                case AtkKey.Mic:
                    h.Command = commandService.ResolveNotNull<MicrophoneCommand>();
                    break;

                case AtkKey.Rog:
                    h.Command = commandService.ResolveNotNull<MainUICommand>();
                    break;

                case AtkKey.BacklightDown:
                    h.Command = commandService.ResolveNotNull<KeyboardBacklightCommand>();
                    h.ParameterInfo = h.Command.Parameters.First();
                    break;

                case AtkKey.BacklightUp:
                    h.Command = commandService.ResolveNotNull<KeyboardBacklightCommand>();
                    h.ParameterInfo = h.Command.Parameters.Last();
                    break;

                case AtkKey.Aura:
                    h.Command = commandService.ResolveNotNull<DisplayRefreshRateCommand>();
                    break;

                case AtkKey.Fan:
                    h.Command = commandService.ResolveNotNull<PerformanceCommand>();
                    break;

                case AtkKey.BrightnessDown:
                    h.Command = commandService.ResolveNotNull<DisplayBrightnessCommand>();
                    h.ParameterInfo = h.Command.Parameters.First();
                    break;

                case AtkKey.BrightnessUp:
                    h.Command = commandService.ResolveNotNull<DisplayBrightnessCommand>();
                    h.ParameterInfo = h.Command.Parameters.Last();
                    break;

                case AtkKey.TouchPad:
                    h.Command = commandService.ResolveNotNull<TouchPadCommand>();
                    break;

                case AtkKey.Sleep:
                    h.Command = commandService.ResolveNotNull<SuspendCommand>();
                    break;

                default:
                    h.Reset();
                    break;
            }
        }
    }

    protected override void OnActivated()
    {
        disposable = new CompositeDisposable();

        Config.Common.PropertyChanged
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(OnPropertyChanged)
            .DisposeWith(disposable);

        OnPropertyChanged(null);

        foreach (var i in HotKeys)
        {
            i.Activate();
        }
    }

    protected override void OnDeactivated()
    {
        disposable?.Dispose();
        disposable = null;

        foreach (var i in HotKeys)
        {
            i.Deactivate();
        }
    }
}
