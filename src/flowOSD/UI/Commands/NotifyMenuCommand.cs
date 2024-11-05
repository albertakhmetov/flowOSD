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
using flowOSD.Extensions;
using flowOSD.Native;
using Microsoft.UI.Windowing;
using flowOSD.Extensions;
using static flowOSD.Native.User32;
using Windows.Foundation;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.UI.NotifyIcon;
using System.Runtime.InteropServices;
using System.Reactive.Linq;
using Microsoft.UI.Xaml;
using System.Reactive.Subjects;
using flowOSD.Core.Resources;

sealed class NotifyMenuCommand : CommandBase
{
    private IConfig config;
    private ISystemEvents systemEvents;
    private ICommandService commandService;
    private NotifyMenuWindow? window;

    private BehaviorSubject<bool> isWindowVisibleSubject;

    public NotifyMenuCommand(
        ITextResources textResources,
        IImageResources imageResources,
        IConfig config,
        ISystemEvents systemEvents,
        ICommandService commandService)
        : base(
            textResources,
            imageResources)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.systemEvents = systemEvents ?? throw new ArgumentNullException(nameof(systemEvents));
        this.commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
      
        isWindowVisibleSubject = new BehaviorSubject<bool>(false);

        CreateWindow();

        Text = string.Format(TextResources["Main.ShowApp"], this.config.ProductName);
        Description = Text;
        Enabled = true;
        IsWindowVisible = isWindowVisibleSubject.AsObservable();
    }

    public IObservable<bool> IsWindowVisible { get; }

    public override bool CanExecuteWithHotKey => false;

    public override async void Execute(object? parameter = null)
    {
        if (window == null)
        {
            CreateWindow();
        }

        await Task.Delay(100);
        const int offsetY = 5;

        window!.AppWindow?.Move(new Windows.Graphics.PointInt32(0, 0));

        var workArea = GetPrimaryWorkArea();

        var scale = GetDpiForWindow(window.GetHandle()) / 96f;

        if (window.Content is FrameworkElement root)
        {
            root.Measure(new Size(workArea.Width, workArea.Height));
        }
        else
        {
            return;
        }

        window.AppWindow?.Resize(new Windows.Graphics.SizeInt32(
            (int)(root.DesiredSize.Width * scale),
            (int)(root.DesiredSize.Height * scale)));

        if (parameter is Rect rect && !rect.IsEmpty
            && !workArea.Contains(new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2)))
        {
            window.AppWindow?.Move(new Windows.Graphics.PointInt32(
                (int)(rect.Left + (rect.Width - window.AppWindow.Size.Width) / 2),
                (int)(workArea.Bottom - window.AppWindow.Size.Height - offsetY)));
        }
        else
        {
            // icon isn't pinned or we can't get rectangle

            var pos = GetCursorPos();
            window.AppWindow?.Move(new Windows.Graphics.PointInt32(
                (int)(pos.X + window.AppWindow.Size.Width < workArea.Width ? pos.X : pos.X - window.AppWindow.Size.Width),
                (int)(pos.Y - window.AppWindow.Size.Height)));
        }

        ShowAndActivate(window);
        isWindowVisibleSubject.OnNext(true);
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
            window.Activated -= OnWindowActivated;
            if (window.AppWindow != null)
            {
                window.AppWindow.Closing -= OnWindowClosing;
                //  window.AppWindow.Destroy();
            }

            // window.Close();
            window.Dispose();

            window = null;
        }
    }

    private void CreateWindow()
    {
        window = new NotifyMenuWindow(this.systemEvents);
        window.Activated += OnWindowActivated;

        if (window.Content is FrameworkElement element)
        {
            element.DataContext = new NotifyMenuViewModel(TextResources, ImageResources, commandService);
        }

        var presenter = OverlappedPresenter.CreateForContextMenu();
        presenter.SetBorderAndTitleBar(true, false);
        presenter.IsAlwaysOnTop = true;

        window.AppWindow.Closing += OnWindowClosing;
        window.AppWindow.SetPresenter(presenter);

        Dwmapi.SetCornerPreference(window.GetHandle(), Dwmapi.DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND);
    }

    private void OnWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        args.Cancel = true;
        sender.Hide();
        isWindowVisibleSubject.OnNext(false);
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (window == null || args.WindowActivationState != WindowActivationState.Deactivated)
        {
            return;
        }

        window.AppWindow.Hide();
    }
}
