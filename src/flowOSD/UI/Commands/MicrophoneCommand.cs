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

using System.ComponentModel;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;
using static flowOSD.Extensions.Common;

sealed class MicrophoneCommand : CommandBase
{
    private IConfig config;
    private IOsd osd;
    private IMicrophone microphone;

    public MicrophoneCommand(
        ITextResources textResources,
        IImageResources imageResources,
        IConfig config,
        IOsd osd,
        IMicrophone microphone) 
        : base(
            textResources,            
            imageResources)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.osd = osd ?? throw new ArgumentNullException(nameof(osd));
        this.microphone = microphone ?? throw new ArgumentNullException(nameof(microphone));

        Text = TextResources["Commands.Microphone.Description"];
        Description = Text;
        Enabled = true;
    }

    public override bool CanExecuteWithHotKey => true;

    public override async void Execute(object? parameter = null)
    {
        try
        {
            microphone.Toggle();

            if (!config.Notifications[NotificationType.Mic])
            {
                return;
            }

            await Task.Delay(500);

            var isMuted = microphone.IsMicMuted();
            osd.Show(new OsdMessage(
                isMuted ? TextResources["Notifications.MicOff"] : TextResources["Notifications.MicOn"],
                isMuted ? ImageResources["Hardware.MicMuted"] : ImageResources["Hardware.Mic"]));
        }
        catch (Exception ex)
        {
            TraceException(ex, TextResources["Errors.MicToggle"]);
        }
    }
}
