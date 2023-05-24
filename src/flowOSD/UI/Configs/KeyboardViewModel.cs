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

namespace flowOSD.UI.Configs;

using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;
using flowOSD.Extensions;
using flowOSD.UI.Commands;

public class KeyboardViewModel : ConfigViewModelBase, IDisposable
{
    private CompositeDisposable? disposable = null;

    public KeyboardViewModel(IConfig config, ICommandService commandService, IHardwareFeatures hardwareFeatures)
            : base(config, Text.Instance.Config.Keyboard.Title, Images.Instance.Common.KeyboardSettings)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (commandService == null)
        {
            throw new ArgumentNullException(nameof(commandService));
        }

        if (hardwareFeatures == null)
        {
            throw new ArgumentNullException(nameof(hardwareFeatures));
        }

        BacklightControl = !hardwareFeatures.OptimizationService;
        Timeouts = new ReadOnlyCollection<int>(new int[] { 5, 15, 30, 60, 600, 0 });

        var commands = new ReadOnlyCollection<CommandBase>(
            new CommandBase[] { CommandBase.Empty }.Union(commandService.Commands.Where(i => i.CanExecuteWithHotKey)).ToArray());

        var hotkeys = new List<KeyboardHotKeyViewModel>()
        {
            new KeyboardHotKeyViewModel(config.HotKeys, commandService, AtkKey.Copy, commands),
            new KeyboardHotKeyViewModel(config.HotKeys, commandService, AtkKey.Paste, commands),
            new KeyboardHotKeyViewModel(config.HotKeys, commandService, AtkKey.Rog, commands),
        };

        if (!hardwareFeatures.OptimizationService)
        {
            hotkeys.Add(new KeyboardHotKeyViewModel(config.HotKeys, commandService, AtkKey.Mic, commands));
            hotkeys.Add(new KeyboardHotKeyViewModel(config.HotKeys, commandService, AtkKey.BacklightDown, commands));
            hotkeys.Add(new KeyboardHotKeyViewModel(config.HotKeys, commandService, AtkKey.BacklightUp, commands));
        }

        hotkeys.Add(new KeyboardHotKeyViewModel(config.HotKeys, commandService, AtkKey.Aura, commands));
        hotkeys.Add(new KeyboardHotKeyViewModel(config.HotKeys, commandService, AtkKey.Fan, commands));

        if (!hardwareFeatures.OptimizationService)
        {
            hotkeys.Add(new KeyboardHotKeyViewModel(config.HotKeys, commandService, AtkKey.BrightnessDown, commands));
            hotkeys.Add(new KeyboardHotKeyViewModel(config.HotKeys, commandService, AtkKey.BrightnessUp, commands));
            hotkeys.Add(new KeyboardHotKeyViewModel(config.HotKeys, commandService, AtkKey.TouchPad, commands));
            hotkeys.Add(new KeyboardHotKeyViewModel(config.HotKeys, commandService, AtkKey.Sleep, commands));
            hotkeys.Add(new KeyboardHotKeyViewModel(config.HotKeys, commandService, AtkKey.Wireless, commands));
        }

        HotKeys = new ReadOnlyCollection<KeyboardHotKeyViewModel>(hotkeys);
    }

    public Text TextResources => Text.Instance;

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

    protected override void OnActivated()
    {
        disposable = new CompositeDisposable();

        Config.Common.PropertyChanged
            .SubscribeOn(SynchronizationContext.Current!)
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
