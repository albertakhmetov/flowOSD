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
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Extensions;
using flowOSD.Native;
using flowOSD.UI.Main;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using static flowOSD.Native.Kernel32;
using static flowOSD.Native.User32;


sealed class MainUICommand : CommandBase
{
    private IConfig config;
    private ISystemEvents systemEvents;
    private MainWindow? window;

    private uint deactivateTime;

    public MainUICommand(IConfig config, ISystemEvents systemEvents, ICommandService commandService, IHardwareService hardwareService)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.systemEvents = systemEvents ?? throw new ArgumentNullException(nameof(systemEvents));

        window = new MainWindow(
            config,
            this.systemEvents,
           hardwareService,
            new MainViewModel(config, commandService, hardwareService).DisposeWith(Disposable!));
        window.Activated += OnWindowActivated;

        var presenter = OverlappedPresenter.CreateForDialog();
        presenter.SetBorderAndTitleBar(true, false);
        presenter.IsAlwaysOnTop = true;

        window.AppWindow.Closing += OnWindowClosing;
        window.AppWindow.SetPresenter(presenter);

        Dwmapi.SetCornerPreference(window.GetHandle(), Dwmapi.DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND);

        Text = $"Show {this.config.ProductName}";
        Description = Text;
        Enabled = true;
    }

    public override bool CanExecuteWithHotKey => true;

    public override void Execute(object? parameter = null)
    {
        if (window?.AppWindow == null || GetTickCount() - deactivateTime < 100)
        {
            return;
        }

        if (window.AppWindow?.IsVisible == true)
        {
            window.AppWindow.Hide();
            return;
        }

        const int offsetX = 10;
        const int offsetY = 10;

        window.AppWindow.Move(new Windows.Graphics.PointInt32(0, 0));

        var scale = GetDpiForWindow(window.GetHandle()) / 96f;
        var workArea = GetPrimaryWorkArea();

        window.AppWindow.Resize(new Windows.Graphics.SizeInt32(
            (int)(370 * scale),
            (int)(300 * scale)));
        window.AppWindow.Move(new Windows.Graphics.PointInt32(
            (int)(workArea.Width - window.AppWindow.Size.Width - offsetX),
            (int)(workArea.Height - window.AppWindow.Size.Height - offsetY)));

        ShowAndActivate(window);
    }

    public override void Dispose()
    {
        if (window != null)
        {
            window.Activated -= OnWindowActivated;
            if (window.AppWindow != null)
            {
                window.AppWindow.Closing -= OnWindowClosing;
            }

            window.AppWindow?.Destroy();
            window.Dispose();
            //  window.Close();
            window = null;
        }

        base.Dispose();
    }

    private void OnWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (Application.Current is App app)
        {
            app.ShutDown();
        }
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (window == null || args.WindowActivationState != WindowActivationState.Deactivated)
        {
            return;
        }

        deactivateTime = GetTickCount();
        window.AppWindow.Hide();
    }
}
