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

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;
using static flowOSD.Native.User32;

sealed class DisplayBrightnessCommand : CommandBase
{
    public const string UP = "up";
    public const string DOWN = "down";

    private static IList<CommandParameterInfo>? parameters;

    private IConfig config;
    private IOsd osd;
    private IDisplayBrightness displayBrightness;

    public DisplayBrightnessCommand(
        ITextResources textResources,
        IImageResources imageResources,
        IConfig config,
        IOsd osd,
        IDisplayBrightness displayBrightness)
        : base(
            textResources,
            imageResources)
    {
        if (parameters == null)
        {
            parameters = CommandParameterInfo.Create(
            new CommandParameterInfo(
                DOWN,
                TextResources["Commands.DisplayBrightness.Down"],
                ImageResources["Hardware.BrightnessDown"]),
            new CommandParameterInfo(
                UP,
                TextResources["Commands.DisplayBrightness.Up"],
                ImageResources["Hardware.BrightnessUp"]));
        }

        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.osd = osd ?? throw new ArgumentNullException(nameof(osd));
        this.displayBrightness = displayBrightness ?? throw new ArgumentNullException(nameof(displayBrightness));

        Text = TextResources["Commands.DisplayBrightness.Description"];
        Description = Text;
        Enabled = true;
    }

    public override bool CanExecuteWithHotKey => true;

    public override IList<CommandParameterInfo> Parameters => parameters!;

    public override void Execute(object? parameter = null)
    {
        if (parameter is string direction == false || !(direction != UP || direction != DOWN))
        {
            return;
        }

        if (direction == UP)
        {
            displayBrightness.LevelUp();
        }

        if (direction == DOWN)
        {
            displayBrightness.LevelDown();
        }

        if (ShowScreenBrightnessOsd())
        {
            return;
        }

        // fail back:

        var icon = direction == DOWN
            ? ImageResources["Hardware.BrightnessDown"]
            : ImageResources["Hardware.BrightnessUp"];

        osd.Show(new OsdValue(displayBrightness.GetLevel(), icon));
    }
}
