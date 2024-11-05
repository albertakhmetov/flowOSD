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

    public NotifyMenuWindow(ISystemEvents systemEvents)
    {
        this.systemEvents = systemEvents ?? throw new ArgumentNullException(nameof(systemEvents));

        InitializeComponent();

        backdrop = new AcrylicSystemBackdrop(this).DisposeWith(disposable);
        backdrop.TrySet();

        AddExStyle(this.GetHandle(), WS_EX_TOOLWINDOW);

        this.systemEvents.SystemDarkMode
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(UpdateTheme)
            .DisposeWith(disposable);
    }

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    private void UpdateTheme(bool isDark)
    {
        Dwmapi.UseDarkMode(this.GetHandle(), isDark);

        if (Content is FrameworkElement content)
        {
            content.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
        }
    }

    private void OnMenuFlyoutItemClick(object sender, RoutedEventArgs e)
    {
        AppWindow.Hide();
    }
}
