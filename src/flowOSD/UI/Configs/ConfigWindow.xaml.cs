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

public sealed partial class ConfigWindow : Window, IDisposable
{
    private CompositeDisposable? disposable = new CompositeDisposable();
    private Dictionary<string, Page> pages = new Dictionary<string, Page>();

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
        foreach (var viewModel in configViewModels)
        {
            if (viewModel is IDisposable disposableViewModel)
            {
                disposableViewModel.DisposeWith(disposable);
            }
        }

        this.InitializeComponent();

        SystemBackdrop = new MicaBackdrop();

        var presenter = OverlappedPresenter.CreateForDialog();
        presenter.IsResizable = true;
        AppWindow.SetPresenter(presenter);

        AddExStyle(this.GetHandle(), WS_EX_DLGMODALFRAME);

        systemEvents.AppsDarkMode
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(UpdateTheme)
            .DisposeWith(disposable);

        VisibilityChanged += ConfigWindow_VisibilityChanged;

    }

    public IReadOnlyCollection<ConfigViewModelBase> ConfigViewModels { get; }

    public void Dispose()
    {
        Bindings.StopTracking();

        disposable?.Dispose();
        disposable = null;

        foreach (var i in pages.Values)
        {
            if (i is IDisposable disposablePage)
            {
                disposablePage.Dispose();
            }
        }

        pages.Clear();

        foreach (var i in ConfigViewModels)
        {
            if (i is IDisposable disposableModel)
            {
                disposableModel.Dispose();
            }
        }
    }

    private void ConfigWindow_VisibilityChanged(object sender, WindowVisibilityChangedEventArgs args)
    {
        if (args.Visible)
        {
            navigationView.SelectedItem = (navigationView.MenuItemsSource as IEnumerable<ConfigViewModelBase>)?.FirstOrDefault();
        }
    }

    private void UpdateTheme(bool isDark)
    {
        Dwmapi.UseDarkMode(this.GetHandle(), isDark);
    }

    private void OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var options = new FrameNavigationOptions
        {
            TransitionInfoOverride = args.RecommendedNavigationTransitionInfo,
            IsNavigationStackEnabled = false,
        };

        Type pageType;

        if (args.SelectedItem is GeneralViewModel)
        {
            pageType = typeof(GeneralPage);
        }
        else if (args.SelectedItem is NotificationsViewModel)
        {
            pageType = typeof(NotificationsPage);
        }
        else if (args.SelectedItem is KeyboardViewModel)
        {
            pageType = typeof(KeyboardPage);
        }
        else if (args.SelectedItem is MonitoringViewModel)
        {
            pageType = typeof(MonitoringPage);
        }
        else
        {
            return;
        }

        if (!pages.ContainsKey(pageType.Name))
        {
            pages[pageType.Name] = (Activator.CreateInstance(pageType) as Page)!;
            pages[pageType.Name].DataContext = args.SelectedItem;
        }

        contentFrame.Content = pages[pageType.Name];
    }

}
