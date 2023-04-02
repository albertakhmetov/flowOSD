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

namespace flowOSD.UI;

using flowOSD.Native;
using Microsoft.UI.Xaml;
using flowOSD.Extensions;
using static flowOSD.Native.Messages;
using static flowOSD.Native.Comctl32;
using static flowOSD.Native.User32;
using static flowOSD.Native.Styles;
using flowOSD.UI.Commands;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using flowOSD.Core;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class NotifyMenuWindow : Window, IDisposable
{
    private const UIntPtr ID = 500;

    private CompositeDisposable? disposable = new CompositeDisposable();
    private AcrylicSystemBackdrop backdrop;

    private ISystemEvents systemEvents;
    private ICommandService commandService;

    public NotifyMenuWindow(ISystemEvents systemEvents, ICommandService commandService)
    {
        this.systemEvents = systemEvents ?? throw new ArgumentNullException(nameof(systemEvents));
        this.commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

        this.InitializeComponent();
        backdrop = new AcrylicSystemBackdrop(this).DisposeWith(disposable);
        backdrop.TrySet();

        MainUICommand = this.commandService.ResolveNotNull<MainUICommand>();
        ConfigCommand = this.commandService.ResolveNotNull<ConfigCommand>();
        ExitCommand = this.commandService.ResolveNotNull<ExitCommand>();

        root.LayoutUpdated += Root_LayoutUpdated;

        AddExStyle(this.GetHandle(), WS_EX_TOOLWINDOW);

        this.systemEvents.SystemDarkMode
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(UpdateTheme)
            .DisposeWith(disposable);
    }

    ~NotifyMenuWindow()
    {
        Dispose(disposing: false);
    }

    public CommandBase MainUICommand { get; }

    public CommandBase ConfigCommand { get; }

    public CommandBase ExitCommand { get; }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public void UpdateSize()
    {
        if (AppWindow == null)
        {
            return;
        }

        var scale = GetDpiForWindow(this.GetHandle()) / 96f;

        AppWindow.Resize(new Windows.Graphics.SizeInt32(
            (int)(root.DesiredSize.Width * scale),
            (int)(root.DesiredSize.Height * scale)));
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            disposable?.Dispose();
            disposable = null;
        }
    }

    private void UpdateTheme(bool isDark)
    {
        Dwmapi.UseDarkMode(this.GetHandle(), isDark);

        if (Content is FrameworkElement content)
        {
            content.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
        }
    }

    private void Root_LayoutUpdated(object? sender, object e)
    {
        UpdateSize();
    }

    private void OnMenuFlyoutItemClick(object sender, RoutedEventArgs e)
    {
        AppWindow.Hide();
    }
}
