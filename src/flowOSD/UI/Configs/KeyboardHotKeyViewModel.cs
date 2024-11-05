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
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;
using flowOSD.Extensions;
using flowOSD.UI.Commands;
using Microsoft.UI.Xaml;

public class KeyboardHotKeyViewModel : ViewModelBase, IDisposable
{
    private CompositeDisposable? disposable = null;

    private HotKeysConfig hotKeysConfig;
    private ICommandService commandService;

    private CommandBase command;
    private CommandParameterInfo? parameterInfo;

    public KeyboardHotKeyViewModel(
        ITextResources textResources,
        IImageResources imageResources,
        HotKeysConfig hotKeysConfig,
        ICommandService commandService,
        AtkKey key,
        IReadOnlyCollection<CommandBase> commands) 
        : base(
            textResources,
            imageResources)
    {
        this.hotKeysConfig = hotKeysConfig ?? throw new ArgumentNullException(nameof(hotKeysConfig));
        this.commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

        Key = key;
        Commands = commands;

        command = CommandBase.Empty;

        UpdateFromConfig();
    }

    public IReadOnlyCollection<CommandBase> Commands { get; }

    public AtkKey Key { get; }

    public string Text
    {
        get
        {
            switch (Key)
            {
                case AtkKey.Mic:
                    return "MIC";

                case AtkKey.Rog:
                    return "ROG";

                case AtkKey.Copy:
                    return "Fn + C";

                case AtkKey.Paste:
                    return "Fn + V";

                case AtkKey.BacklightDown:
                    return "F2";

                case AtkKey.BacklightUp:
                    return "F3";

                case AtkKey.Aura:
                    return "F4";

                case AtkKey.Fan:
                    return "F5";

                case AtkKey.BrightnessDown:
                    return "F7";

                case AtkKey.BrightnessUp:
                    return "F8";

                case AtkKey.TouchPad:
                    return "F10";

                case AtkKey.Sleep:
                    return "F11";

                case AtkKey.Wireless:
                    return "F12";

                default:
                    return "";
            }
        }
    }

    public CommandBase Command
    {
        get => command;
        set
        {
            if (command == value)
            {
                return;
            }

            command = value;
            OnPropertyChanged();

            ParameterInfo = Command?.Parameters.FirstOrDefault();
            OnPropertyChanged(null);
        }
    }

    public CommandParameterInfo? ParameterInfo
    {
        get => parameterInfo;
        set
        {
            if (parameterInfo == value && value != null)
            {
                return;
            }

            parameterInfo = value;

            hotKeysConfig[Key] = Command.IsEmptyCommand ? null : new HotKeysConfig.Command(Command.Name, ParameterInfo?.Value);
            OnPropertyChanged();
        }
    }

    public Visibility ParametersVisibility => Command?.Parameters?.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility AppVisibility => Command?.Name == nameof(UI.Commands.MainUICommand) ? Visibility.Visible : Visibility.Collapsed;

    public void Dispose()
    {
        Deactivate();
    }

    public void Reset()
    {
        Command = CommandBase.Empty;
    }

    public void Activate()
    {
        disposable = new CompositeDisposable();

        hotKeysConfig.KeyChanged
            .Where(key => Key == key)
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(_ => UpdateFromConfig())
            .DisposeWith(disposable);

        UpdateFromConfig();
    }

    public void Deactivate()
    {
        disposable?.Dispose();
        disposable = null;
    }

    public string? GetKeyText()
    {
        return Enum.GetName(Key);
    }

    private void UpdateFromConfig()
    {
        if (Command?.Name == hotKeysConfig[Key]?.Name && parameterInfo?.Value == hotKeysConfig[Key]?.Parameter)
        {
            return;
        }

        command = commandService.Resolve(hotKeysConfig[Key]?.Name) ?? CommandBase.Empty;
        parameterInfo = command.Parameters.FirstOrDefault(x => x.Value == hotKeysConfig[Key]?.Parameter);

        OnPropertyChanged(null);
    }
}
