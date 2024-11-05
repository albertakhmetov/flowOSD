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
using flowOSD.Core.Hardware;
using flowOSD.Core.Configs;
using flowOSD.Core.Resources;
using System.Text;

public sealed partial class MainWindow : Window, IDisposable
{
    private const UIntPtr ID = 600;

    private CompositeDisposable? disposable = new CompositeDisposable();
    private AcrylicSystemBackdrop backdrop;

    private IConfig config;
    private ISystemEvents systemEvents;
    private IHardwareService hardwareServices;

    private IAtk atk;

    public MainWindow(IConfig config, ISystemEvents systemEvents, IHardwareService hardwareServices, MainViewModel viewModel)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.systemEvents = systemEvents ?? throw new ArgumentNullException(nameof(systemEvents));
        this.hardwareServices = hardwareServices ?? throw new ArgumentNullException(nameof(hardwareServices));

        atk = this.hardwareServices.ResolveNotNull<IAtk>();

        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();

        if (Content is FrameworkElement element)
        {
            element.DataContext = ViewModel;
        }

        backdrop = new AcrylicSystemBackdrop(this).DisposeWith(disposable);
        backdrop.TrySet();

        AddExStyle(this.GetHandle(), WS_EX_TOOLWINDOW);

        Activated += MainWindow_Activated;

        systemEvents.SystemDarkMode
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(UpdateTheme)
            .DisposeWith(disposable);

        config.Performance.ProfileChanged
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(_ => UpdatePerformanceProfilesMenu())
            .DisposeWith(disposable);

        atk.GpuMode
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(_ => UpdatePerformanceProfilesMenu())
            .DisposeWith(disposable);

        UpdatePerformanceProfilesMenu();
        UpdatePowerModesMenu();
    }

    private void UpdatePerformanceProfilesMenu()
    {
        if (performanceProfilesMenu.Items.Count == 0)
        {
            performanceProfilesMenu.Items.Add(new MenuFlyoutItem
            {
                Command = ViewModel.PerformanceCommand,
                CommandParameter = PerformanceProfile.DefaultId,
                Icon = new FontIcon { Glyph = ViewModel.ImageResources["PerformanceMode.Performance"] },
                Text = ViewModel.TextResources["PerformanceMode.Performance"]
            });

            performanceProfilesMenu.Items.Add(new MenuFlyoutItem
            {
                Command = ViewModel.PerformanceCommand,
                CommandParameter = PerformanceProfile.TurboId,
                Icon = new FontIcon { Glyph = ViewModel.ImageResources["PerformanceMode.Turbo"] },
                Text = ViewModel.TextResources["PerformanceMode.Turbo"]
            });

            performanceProfilesMenu.Items.Add(new MenuFlyoutItem
            {
                Command = ViewModel.PerformanceCommand,
                CommandParameter = PerformanceProfile.SilentId,
                Icon = new FontIcon { Glyph = ViewModel.ImageResources["PerformanceMode.Silent"] },
                Text = ViewModel.TextResources["PerformanceMode.Silent"]
            });
        }

        var userProfiles = config.Performance.GetProfiles().ToList();
        if (userProfiles.Count == 0)
        {
            while (performanceProfilesMenu.Items.Count > 3)
            {
                performanceProfilesMenu.Items.RemoveAt(performanceProfilesMenu.Items.Count - 1);
            }

            return;
        }

        if (performanceProfilesMenu.Items.Count == 3)
        {
            performanceProfilesMenu.Items.Add(new MenuFlyoutSeparator());
        }

        var i = 4; // skip standard profiles + separator
        while (i < performanceProfilesMenu.Items.Count)
        {
            var menuItem = performanceProfilesMenu.Items[i] as MenuFlyoutItem;
            if (menuItem?.CommandParameter is Guid id)
            {
                var profile = userProfiles.FirstOrDefault(i => i.Id == id);
                if (profile == null)
                {
                    performanceProfilesMenu.Items.RemoveAt(i);
                    continue;
                }

                if (profile.Name != menuItem.Text)
                {
                    menuItem.Text = profile.Name;
                }

                userProfiles.Remove(profile);
                i++;
            }
            else
            {
                performanceProfilesMenu.Items.RemoveAt(i);
            }
        }

        foreach (var profile in userProfiles)
        {
            performanceProfilesMenu.Items.Add(new MenuFlyoutItem
            {
                Command = ViewModel.PerformanceCommand,
                CommandParameter = profile.Id,
                Text = profile.Name,
            });
        }
    }

    private void UpdatePowerModesMenu()
    {
        powerModesMenu.Items.Clear();

        powerModesMenu.Items.Add(new MenuFlyoutItem
        {
            Command = ViewModel.PowerModeCommand,
            CommandParameter = PowerMode.BestPowerEfficiency,
            Icon = new FontIcon { Glyph = ViewModel.ImageResources["PowerMode.BestPowerEfficiency"] },
            Text = ViewModel.TextResources["PowerMode.BestPowerEfficiency"]
        });

        powerModesMenu.Items.Add(new MenuFlyoutItem
        {
            Command = ViewModel.PowerModeCommand,
            CommandParameter = PowerMode.Balanced,
            Icon = new FontIcon { Glyph = ViewModel.ImageResources["PowerMode.Balanced"] },
            Text = ViewModel.TextResources["PowerMode.Balanced"]
        });

        powerModesMenu.Items.Add(new MenuFlyoutItem
        {
            Command = ViewModel.PowerModeCommand,
            CommandParameter = PowerMode.BestPerformance,
            Icon = new FontIcon { Glyph = ViewModel.ImageResources["PowerMode.BestPerformance"] },
            Text = ViewModel.TextResources["PowerMode.BestPerformance"]
        });
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
            ViewModel.Activate();
        }
    }

    public MainViewModel ViewModel { get; }

    public void Dispose()
    {
        disposable?.Dispose();
    }

    private void UpdateTheme(bool isDark)
    {
        Dwmapi.UseDarkMode(this.GetHandle(), isDark);

        if (Content is FrameworkElement content)
        {
            content.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
        }
    }

    private void BatteryToolTip_Opened(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null && sender is ToolTip t)
        {
            var sb = new StringBuilder();

            sb.Append($"{ViewModel.TextResources["Main.BatteryRemaining"]}: {100F * ViewModel.Capacity / ViewModel.FullChargedCapacity:N0}%");
            if (ViewModel.Rate < 0)
            {
                sb.AppendLine();
                sb.Append($"{ViewModel.TextResources["Config.Battery.EstimatedTime"]}: {TimeSpan.FromSeconds(ViewModel.EstimatedTime).ToString(@"hh\:mm")}");
            }

            if (ViewModel.IsLowPower)
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.Append(ViewModel.TextResources["Charger.LowPower"].ToUpper());
            }

            t.Content = sb.ToString();
        }
    }

    private void FanSpeedToolTip_Opened(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null && sender is ToolTip t)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"{ViewModel.TextResources["Main.CpuFanSpeed"]}: {ViewModel.CpuFanSpeed}%");
            sb.Append($"{ViewModel.TextResources["Main.GpuFanSpeed"]}: {ViewModel.GpuFanSpeed}%");

            t.Content = sb.ToString();
        }
    }
}
