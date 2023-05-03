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

namespace flowOSD.UI.Configs;

using System.Reactive.Disposables;
using System.Reactive.Linq;
using flowOSD.Core;
using flowOSD.Extensions;
using flowOSD.Native;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using static flowOSD.Native.User32;
using static flowOSD.Native.Styles;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Navigation;
using flowOSD.Core.Resources;

public sealed partial class ConfigWindow : Window, IDisposable
{
    private CompositeDisposable? disposable = new CompositeDisposable();

    public ConfigWindow(ISystemEvents systemEvents, IList<ConfigViewModelBase> configViewModels)
    {
        if (systemEvents == null)
        {
            throw new ArgumentNullException(nameof(systemEvents));
        }

        if (configViewModels == null)
        {
            throw new ArgumentNullException(nameof(configViewModels));
        }

        ConfigViewModels = new ReadOnlyCollection<ConfigViewModelBase>(configViewModels);
        InitializeComponent();

        navigationView.MenuItemsSource = ConfigViewModels.Where(i => !i.IsFooterItem);
        navigationView.FooterMenuItemsSource = ConfigViewModels.Where(i => i.IsFooterItem);

        generalConfig.DataContext = ConfigViewModels.FirstOrDefault(i => i.GetType() == typeof(GeneralViewModel));
        notificationsConfig.DataContext = ConfigViewModels.FirstOrDefault(i => i.GetType() == typeof(NotificationsViewModel));
        keyboardConfig.DataContext = ConfigViewModels.FirstOrDefault(i => i.GetType() == typeof(KeyboardViewModel));
        monitoringConfig.DataContext = ConfigViewModels.FirstOrDefault(i => i.GetType() == typeof(MonitoringViewModel));
        performanceConfig.DataContext = ConfigViewModels.FirstOrDefault(i => i.GetType() == typeof(PerformanceViewModel));
        tabletConfig.DataContext = ConfigViewModels.FirstOrDefault(i => i.GetType() == typeof(TabletViewModel));
        aboutConfig.DataContext = ConfigViewModels.FirstOrDefault(i => i.GetType() == typeof(AboutViewModel));

        SystemBackdrop = new MicaBackdrop();

        var presenter = OverlappedPresenter.CreateForDialog();
        presenter.IsResizable = true;
        AppWindow.SetPresenter(presenter);

        Title = Text.Instance.Config.Title;

        AddExStyle(this.GetHandle(), WS_EX_DLGMODALFRAME);

        systemEvents.AppsDarkMode
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(UpdateTheme)
            .DisposeWith(disposable);

        navigationView.SelectionChanged += OnSelectionChanged;
        AppWindow.Changed += AppWindow_Changed;
    }

    public IReadOnlyCollection<ConfigViewModelBase> ConfigViewModels { get; }

    public AboutViewModel AboutViewModel { get; }

    public void Dispose()
    {
        Bindings.StopTracking();

        disposable?.Dispose();
        disposable = null;

        foreach (var i in ConfigViewModels)
        {
            if (i is IDisposable disposableModel)
            {
                disposableModel.Dispose();
            }
        }

        AppWindow.Changed -= AppWindow_Changed;
        navigationView.SelectionChanged -= OnSelectionChanged;
    }

    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidVisibilityChange)
        {
            navigationView.SelectedItem = (navigationView.MenuItemsSource as IEnumerable<ConfigViewModelBase>)?.FirstOrDefault();
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

    private void OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        foreach (var model in ConfigViewModels)
        {
            model.IsSelected = model == args.SelectedItem;
            if (model.IsSelected)
            {
                navigationView.Header = model.Title;
            }
        }
    }

    private void CreateProfile(object sender, RoutedEventArgs e)
    {
        createProfileFlyout.Hide();

        (performanceConfig.DataContext as PerformanceViewModel)?.CreateProfile(createProfileName.Text);

        createProfileName.Text = "";
    }

    private void RenameProfile(object sender, RoutedEventArgs e)
    {
        renameProfileFlyout.Hide();

        (performanceConfig.DataContext as PerformanceViewModel)?.RenameProfile(renameProfileName.Text);

        renameProfileName.Text = "";
    }

    private void RemoveProfile(object sender, RoutedEventArgs e)
    {
        removeProfileFlyout.Hide();

        (performanceConfig.DataContext as PerformanceViewModel)?.RemoveProfile();
    }
}
