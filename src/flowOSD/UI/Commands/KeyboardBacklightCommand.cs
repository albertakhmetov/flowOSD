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

namespace flowOSD.UI.Commands;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;

sealed class KeyboardBacklightCommand : CommandBase
{
    public const string UP = "up";
    public const string DOWN = "down";

    private static IList<CommandParameterInfo>? parameters;

    private IConfig config;
    private IOsd osd;
    private IKeyboardBacklight keyboardBacklight;
    private IKeyboardBacklightControl? keyboardBacklightControl;
    private IDisplay display;

    public KeyboardBacklightCommand(
        ITextResources textResources,
        IImageResources imageResources,
        IConfig config,
        IOsd osd,
        IHardwareService hardwareService)
        : base(
            textResources,
            imageResources)
    {
        if (parameters == null)
        {
            parameters = CommandParameterInfo.Create(
                new CommandParameterInfo(
                    DOWN, 
                    TextResources["Commands.KeyboardBacklight.Down"]),
                new CommandParameterInfo(
                    UP, 
                    TextResources["Commands.KeyboardBacklight.Up"]));
        }

        if (hardwareService == null)
        {
            throw new ArgumentNullException(nameof(hardwareService));
        }

        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.osd = osd ?? throw new ArgumentNullException(nameof(osd));

        keyboardBacklight = hardwareService.ResolveNotNull<IKeyboardBacklight>();
        keyboardBacklightControl = hardwareService.Resolve<IKeyboardBacklightControl>();
        display = hardwareService.ResolveNotNull<IDisplay>();

        Text = TextResources["Commands.KeyboardBacklight.Description"];
        Description = Text;
        Enabled = true;
    }

    public override bool CanExecuteWithHotKey => true;

    public override IList<CommandParameterInfo> Parameters => parameters!;

    public override async void Execute(object? parameter = null)
    {
        if (keyboardBacklightControl == null || parameter is string direction == false || !(direction != UP || direction != DOWN))
        {
            return;
        }

        if (config.Common.KeyboardBacklightWithDisplay && await display.State.FirstOrDefaultAsync() != DeviceState.Enabled)
        {
            return;
        }

        if (direction == UP)
        {
            keyboardBacklightControl.LevelUp();
        }

        if (direction == DOWN)
        {
            keyboardBacklightControl.LevelDown();
        }

        var backlightLevel = await keyboardBacklight.Level.FirstOrDefaultAsync();

        var icon = direction == UP
            ? ImageResources["Hardware.KeyboardLightUp"]
            : ImageResources["Hardware.KeyboardLightDown"];

        osd.Show(new OsdValue((float)backlightLevel / (float)KeyboardBacklightLevel.High, icon));
    }
}
