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

namespace flowOSD.UI.Main;

using System;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using flowOSD.Extensions;
using static flowOSD.Native.Comctl32;
using static flowOSD.Native.Messages;
using static flowOSD.Native.Styles;
using static flowOSD.Native.User32;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using System.Runtime.InteropServices;
using Microsoft.UI.Composition.SystemBackdrops;
using WinRT;
using flowOSD.Native;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using flowOSD.Core;

public sealed partial class MainWindow : Window, IDisposable
{
    private const UIntPtr ID = 600;

    private CompositeDisposable? disposable = new CompositeDisposable();
    private AcrylicSystemBackdrop backdrop;

    private ISystemEvents systemEvents;

    public MainWindow(ISystemEvents systemEvents, MainViewModel viewModel)
    {
        this.systemEvents = systemEvents ?? throw new ArgumentNullException(nameof(systemEvents));

        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        InitializeComponent();

        backdrop = new AcrylicSystemBackdrop(this).DisposeWith(disposable);
        backdrop.TrySet();

        AddExStyle(this.GetHandle(), WS_EX_TOOLWINDOW);

        this.systemEvents.SystemDarkMode
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(UpdateTheme)
            .DisposeWith(disposable);

        Activated += MainWindow_Activated;
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (ViewModel == null)
        {
            return;
        }

        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            ViewModel.Deactivate();
        }
        else
        {
            Bindings.Update();
            ViewModel.Activate();
        }
    }

    ~MainWindow()
    {
        Dispose(disposing: false);
    }

    public MainViewModel ViewModel { get; }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
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
}
