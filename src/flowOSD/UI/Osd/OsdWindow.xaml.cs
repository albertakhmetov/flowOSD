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

namespace flowOSD.UI.Osd;

using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Resources;
using flowOSD.Extensions;
using flowOSD.Native;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Windows.UI;
using static flowOSD.Native.User32;

public sealed partial class OsdWindow : Window, IDisposable
{
    private const int VISIBLE_TIMEOUT = 1500;

    private CompositeDisposable? disposable = new CompositeDisposable();
    private AcrylicSystemBackdrop backdrop;

    private IConfig config;
    private ISystemEvents systemEvents;

    private IDisposable? hideTimer, animationTimer;

    public OsdWindow(
        ITextResources textResources,
        IImageResources imageResources,
        IConfig config, 
        ISystemEvents systemEvents)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.systemEvents = systemEvents ?? throw new ArgumentNullException(nameof(systemEvents));

        ViewModel = new OsdViewModel(textResources, imageResources);

        InitializeComponent();
        if (Content is FrameworkElement element)
        {
            element.DataContext = ViewModel;
        }

        backdrop = new AcrylicSystemBackdrop(this, true).DisposeWith(disposable);
        backdrop.TrySet();

        var presenter = OverlappedPresenter.CreateForContextMenu();
        presenter.SetBorderAndTitleBar(true, false);
        presenter.IsAlwaysOnTop = true;

        AppWindow.SetPresenter(presenter);

        Dwmapi.SetCornerPreference(this.GetHandle(), Dwmapi.DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND);

        AddExStyle(this.GetHandle(), Styles.WS_EX_NOACTIVATE | Styles.WS_EX_TOOLWINDOW);

        this.systemEvents.SystemDarkMode
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(UpdateTheme)
            .DisposeWith(disposable);
    }

    public OsdViewModel ViewModel { get; }

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    public void Show(object data)
    {
        if (data is OsdMessage == false && data is OsdValue == false)
        {
            return;
        }

        hideTimer?.Dispose();
        hideTimer = null;
        animationTimer?.Dispose();

        ViewModel.Update(data);

        var workArea = GetPrimaryWorkArea();
        root.Measure(new Windows.Foundation.Size(workArea.Width, workArea.Height));
        UpdateSizeAndPosition();

        if (AppWindow.IsVisible != true)
        {
            animationTimer = WindowAnimation.Show(AppWindow, GetWindowY(), AppWindow.Size.Height / 2);
        }

        hideTimer = Observable
            .Timer(DateTimeOffset.Now.AddMilliseconds(VISIBLE_TIMEOUT), TimeSpan.FromMilliseconds(500 / 16))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(t =>
            {
                hideTimer?.Dispose();
                hideTimer = null;
                animationTimer?.Dispose();
                animationTimer = WindowAnimation.Hide(AppWindow, GetWindowY(), AppWindow.Size.Height / 2);
            });
    }

    private void UpdateSizeAndPosition()
    {
        if (AppWindow == null)
        {
            return;
        }

        var scale = GetDpiForWindow(this.GetHandle()) / 96f;
        var workArea = GetPrimaryWorkArea();

        var width = ViewModel.IsValue ? (int)(260) : (int)(root.DesiredSize.Width * scale);
        var height = (int)(72f);

        var x = (int)((workArea.Width - width) / 2);
        var y = GetWindowY();

        AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, width, height));
    }

    private int GetWindowY()
    {
        return Convert.ToInt32(GetPrimaryWorkArea().Bottom - 90);
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
