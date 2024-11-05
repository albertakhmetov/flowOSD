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

namespace flowOSD.UI.NotifyIcon;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using flowOSD.Core;
using flowOSD.Core.Resources;
using flowOSD.UI.Commands;


public sealed class NotifyMenuViewModel : ViewModelBase
{
    public NotifyMenuViewModel(
        ITextResources textResources,
        IImageResources imageResources,
        ICommandService commandService)
        : base(
            textResources,
            imageResources)
    {
        if (commandService == null)
        {
            throw new ArgumentNullException(nameof(commandService));
        }

        MainUICommand = commandService.ResolveNotNull<MainUICommand>();
        ConfigCommand = commandService.ResolveNotNull<ConfigCommand>();
        RestartAppCommand = commandService.ResolveNotNull<RestartAppCommand>();
        ExitCommand = commandService.ResolveNotNull<ExitCommand>();
    }

    public CommandBase MainUICommand { get; }

    public CommandBase ConfigCommand { get; }

    public CommandBase RestartAppCommand { get; }

    public CommandBase ExitCommand { get; }
}
