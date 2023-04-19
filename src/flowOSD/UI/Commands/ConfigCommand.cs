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

namespace flowOSD.UI.Commands;

using System.ComponentModel;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Extensions;
using flowOSD.UI.Configs;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using static flowOSD.Native.User32;

sealed class ConfigCommand : CommandBase, IDisposable
{
    private IConfig config;
    private ISystemEvents systemEvents;
    private ICommandService commandService;

    private ConfigWindow? window;

    public ConfigCommand(
        IConfig config,
        ISystemEvents systemEvents,
        ICommandService commandService,
        IHardwareService hardwareService)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.systemEvents = systemEvents ?? throw new ArgumentNullException(nameof(systemEvents));
        this.commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

        Text = "Settings...";
        Enabled = true;
    }

    public override bool CanExecuteWithHotKey => false;

    public override void Execute(object? parameter = null)
    {
        if (window == null)
        {
            var viewModels = new ConfigViewModelBase[]
            {
                new GeneralViewModel(config),
                new NotificationsViewModel(config),
                new KeyboardViewModel(config, commandService),
                new MonitoringViewModel(config),
            };

            window = new ConfigWindow(systemEvents, viewModels);
            window.AppWindow.Closing += OnWindowClosing;

            var scale = GetDpiForWindow(window.GetHandle()) / 96f;

            window.AppWindow.Resize(new Windows.Graphics.SizeInt32(
                (int)(800 * scale),
                (int)(500 * scale)));
        }

        window.Activate();
    }

    public override void Dispose()
    {
        DisposeWindow();

        base.Dispose();
    }

    private void DisposeWindow()
    {
        if (window != null)
        {
            if (window.AppWindow != null)
            {
                window.AppWindow.Closing -= OnWindowClosing;
            }

            window.Dispose();
            window.Close();
            window = null;
        }
    }

    private void OnWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        args.Cancel = true;
        sender.Hide();
    }
}
