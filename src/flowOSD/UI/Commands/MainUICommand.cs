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

using System.Reactive.Linq;
using System.Reactive.Subjects;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Resources;
using flowOSD.Extensions;
using flowOSD.Native;
using flowOSD.UI.Main;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using static flowOSD.Native.Kernel32;
using static flowOSD.Native.User32;


sealed class MainUICommand : CommandBase
{
    private const int OFFSET_X = 10;
    private const int OFFSET_Y = 10;
    private const int ANIMATION_DELTA = 200;

    private IConfig config;
    private ISystemEvents systemEvents;
    private ICommandService commandService;
    private IHardwareService hardwareService;
    private IElevatedService elevatedService;
    private MainWindow? window;

    private uint deactivateTime;
    private BehaviorSubject<bool> isWindowVisibleSubject;

    public MainUICommand(
        ITextResources textResources,
        IImageResources imageResources,
        IConfig config,
        ISystemEvents systemEvents,
        ICommandService commandService,
        IHardwareService hardwareService,
        IElevatedService elevatedService) 
        : base(
            textResources,
            imageResources)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.systemEvents = systemEvents ?? throw new ArgumentNullException(nameof(systemEvents));
        this.commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
        this.hardwareService = hardwareService ?? throw new ArgumentNullException(nameof(hardwareService));
        this.elevatedService = elevatedService ?? throw new ArgumentNullException(nameof(elevatedService));

        isWindowVisibleSubject = new BehaviorSubject<bool>(false);

        Text = string.Format(TextResources["Main.ShowApp"], this.config.ProductName);
        Description = Text;
        Enabled = true;
        IsWindowVisible = isWindowVisibleSubject.AsObservable();
    }

    public IObservable<bool> IsWindowVisible { get; }

    public override bool CanExecuteWithHotKey => true;

    public override void Execute(object? parameter = null)
    {
        if (GetTickCount() - deactivateTime < 500)
        {
            return;
        }

        if (window == null)
        {
            CreateWindow();
        }

        if (window!.AppWindow?.IsVisible == true)
        {
            WindowAnimation.Hide(window.AppWindow, GetWindowY(), ANIMATION_DELTA);

            isWindowVisibleSubject.OnNext(false);
            return;
        }

        window.AppWindow?.Move(new Windows.Graphics.PointInt32(0, 0));

        var scale = GetDpiForWindow(window.GetHandle()) / 96f;
        var workArea = GetPrimaryWorkArea();

        window.AppWindow?.Resize(new Windows.Graphics.SizeInt32(
            (int)(370 * scale),
            (int)(360 * scale)));
        window.AppWindow?.Move(new Windows.Graphics.PointInt32(
            (int)(workArea.Width - window.AppWindow.Size.Width - OFFSET_X),
            (int)(workArea.Height - window.AppWindow.Size.Height - OFFSET_Y)));

        if (window.AppWindow != null)
        {
            WindowAnimation.Show(window.AppWindow, GetWindowY(), ANIMATION_DELTA, () => ShowAndActivate(window));
        }

        isWindowVisibleSubject.OnNext(true);
    }

    public override void Dispose()
    {
        DisposeWindow();

        base.Dispose();
    }

    private int GetWindowY()
    {
        return window == null ? 0 : Convert.ToInt32(GetPrimaryWorkArea().Height - window.AppWindow.Size.Height - OFFSET_Y);
    }

    private void DisposeWindow()
    {
        if (window != null)
        {
            window.Activated -= OnWindowActivated;
            if (window.AppWindow != null)
            {
                window.AppWindow.Closing -= OnWindowClosing;
            }

            window.Dispose();
            window.Close();
            window = null;
        }
    }

    private void CreateWindow()
    {
        window = new MainWindow(
            config,
            this.systemEvents,
            hardwareService,
            new MainViewModel(
                TextResources, 
                ImageResources,
                config, 
                commandService, 
                hardwareService,
                elevatedService).DisposeWith(Disposable!));
        window.Activated += OnWindowActivated;

        var presenter = OverlappedPresenter.CreateForDialog();
        presenter.SetBorderAndTitleBar(true, false);
        presenter.IsAlwaysOnTop = true;

        window.AppWindow.Closing += OnWindowClosing;
        window.AppWindow.SetPresenter(presenter);

        Dwmapi.SetCornerPreference(window.GetHandle(), Dwmapi.DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND);
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
        WindowAnimation.Hide(window.AppWindow, GetWindowY(), ANIMATION_DELTA);
        isWindowVisibleSubject.OnNext(false);
    }
}
