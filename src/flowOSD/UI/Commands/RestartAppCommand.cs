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
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Core;

internal class RestartAppCommand : CommandBase
{
    private INotificationService notificationService;

    public RestartAppCommand(
        ITextResources textResources,
        IImageResources imageResources,
        INotificationService notificationService) 
        : base(
            textResources,
            imageResources)
    {
        this.notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

        Text = TextResources["Commands.Restart.Description"];
        Enabled = true;
    }

    public override bool CanExecuteWithHotKey => false;

    public override void Execute(object? parameter = null)
    {
        if (AppInstance.Restart(string.Empty) != AppRestartFailureReason.RestartPending)
        {
            notificationService.ShowError(TextResources["Commands.Restart.ErrorText"]);
        }
    }
}
