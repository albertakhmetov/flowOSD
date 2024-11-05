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

using System;
using flowOSD.Core;
using flowOSD.Core.Resources;
using static Native.User32;

sealed class DisplayOffCommand : CommandBase
{
    const int SC_MONITORPOWER = 0xF170;
    const int WM_SYSCOMMAND = 0x0112;
    const int MONITOR_OFF = 2;

    private readonly IMessageQueue messageQueue;

    public DisplayOffCommand(
        ITextResources textResources,
        IImageResources imageResources,
        IMessageQueue messageQueue
        )
        : base(
            textResources,
            imageResources)
    {
        this.messageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));

        Text = TextResources["Commands.DisplayOff.Description"];
        Description = Text;
        Enabled = true;
    }

    public override bool CanExecuteWithHotKey => true;

    public override void Execute(object? parameter = null)
    {
        SendMessage(messageQueue.Handle, WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)MONITOR_OFF);
    }
}
