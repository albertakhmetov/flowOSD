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
using flowOSD.Core.Hardware;
using flowOSD.Core.Configs;
using flowOSD.Core.Resources;

public sealed partial class MainWindow : Window, IDisposable
{
    private const UIntPtr ID = 600;

    private CompositeDisposable? disposable = new CompositeDisposable();
    private AcrylicSystemBackdrop backdrop;
    private Monitoring monitoring;

    private IConfig config;
    private ISystemEvents systemEvents;
    private IHardwareService hardwareServices;

    public MainWindow(IConfig config, ISystemEvents systemEvents, IHardwareService hardwareServices, MainViewModel viewModel)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.systemEvents = systemEvents ?? throw new ArgumentNullException(nameof(systemEvents));
        this.hardwareServices = hardwareServices ?? throw new ArgumentNullException(nameof(hardwareServices));

        monitoring = new Monitoring(this, config, hardwareServices).DisposeWith(disposable);

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
            .Subscribe(_=>UpdatePerformanceProfilesMenu())
            .DisposeWith(disposable);

        UpdatePerformanceProfilesMenu();
    }

    private void UpdatePerformanceProfilesMenu()
    {
        performanceProfilesMenu.Items.Clear();

        performanceProfilesMenu.Items.Add(new MenuFlyoutItem
        {
            Command = ViewModel.PerformanceCommand,
            CommandParameter = PerformanceProfile.Default.Id,
            Icon = new FontIcon { Glyph = Images.Instance.ToImage(PerformanceMode.Default) },
            Text = Text.ToText(PerformanceMode.Default),
        });

        performanceProfilesMenu.Items.Add(new MenuFlyoutItem
        {
            Command = ViewModel.PerformanceCommand,
            CommandParameter = PerformanceProfile.Turbo.Id,
            Icon = new FontIcon { Glyph = Images.Instance.ToImage(PerformanceMode.Turbo) },
            Text = Text.ToText(PerformanceMode.Turbo),
        });

        performanceProfilesMenu.Items.Add(new MenuFlyoutItem
        {
            Command = ViewModel.PerformanceCommand,
            CommandParameter = PerformanceProfile.Silent.Id,
            Icon = new FontIcon { Glyph = Images.Instance.ToImage(PerformanceMode.Silent) },
            Text = Text.ToText(PerformanceMode.Silent),
        });

        var userProfiles = config.Performance.GetProfiles();
        if(userProfiles.Count == 0)
        {
            return;
        }

        performanceProfilesMenu.Items.Add(new MenuFlyoutSeparator());

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

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (ViewModel == null)
        {
            return;
        }

        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            monitoring.Deactivate();
            ViewModel.Deactivate();
        }
        else
        {
            monitoring.Activate();
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

    private sealed class Monitoring : IDisposable
    {
        private MainWindow window;

        private IConfig config;
        private IBattery battery;
        private IAtk atk;

        private CompositeDisposable? disposable;

        public Monitoring(MainWindow window, IConfig config, IHardwareService hardwareService)
        {
            this.window = window ?? throw new ArgumentNullException(nameof(window));
            this.config = config ?? throw new ArgumentNullException(nameof(config));

            if (hardwareService == null)
            {
                throw new ArgumentNullException(nameof(hardwareService));
            }

            battery = hardwareService.ResolveNotNull<IBattery>();
            atk = hardwareService.ResolveNotNull<IAtk>();
        }

        public void Activate()
        {
            disposable?.Dispose();
            disposable = new CompositeDisposable();

            if (config.Common.ShowBatteryChargeRate)
            {
                battery.Rate.DistinctUntilChanged()
                    .CombineLatest(
                        battery.Capacity.DistinctUntilChanged(),
                        battery.PowerState.DistinctUntilChanged(),
                        battery.EstimatedTime.DistinctUntilChanged(),
                        (rate, capacity, powerState, estimatedTime) => new { rate, capacity, powerState, estimatedTime })
                    .ObserveOn(SynchronizationContext.Current!)
                    .Subscribe(x => UpdateBattery(x.rate, x.capacity, x.powerState, x.estimatedTime))
                    .DisposeWith(disposable);
            }

            if (config.Common.ShowCpuTemperature)
            {
                atk.CpuTemperature
                    .ObserveOn(SynchronizationContext.Current!)
                    .Subscribe(value => UpdateCpuTemperature(value))
                    .DisposeWith(disposable);
            }
        }

        public void Deactivate()
        {
            disposable?.Dispose();
            disposable = null;
        }

        public void Dispose()
        {
            Deactivate();
        }

        private void UpdateBattery(int rate, uint capacity, BatteryPowerState powerState, uint estimatedTime)
        {
            var isEmptyRate = Math.Abs(rate) < 100;

            window.batteryChargeRateIcon.Glyph = Images.Instance.GetBatteryIcon(capacity, battery.FullChargedCapacity, powerState);
            window.batteryChargeRate.Text = isEmptyRate ? "" : $"{rate / 1000f:N1} W";

            /* var time = TimeSpan.FromSeconds(estimatedTime);
             var builder = new StringBuilder();
             builder.Append($"{capacity * 100f / battery.FullChargedCapacity:N0}%");

             if (isEmptyRate)
             {
                 builder.Append("");
             }
             else if ((powerState & BatteryPowerState.Discharging) == BatteryPowerState.Discharging)
             {
                 builder.Append(" remaining");
             }
             else if ((powerState & BatteryPowerState.Charging) == BatteryPowerState.Charging)
             {
                 builder.Append(" available");
             }

             if ((powerState & BatteryPowerState.PowerOnLine) == BatteryPowerState.PowerOnLine)
             {
                 builder.Append(" (plugged in)");
             }

             if (!isEmptyRate && (powerState & BatteryPowerState.Discharging) == BatteryPowerState.Discharging && time.TotalMinutes > 1)
             {
                 builder.AppendLine();

                 if (time.Hours > 0)
                 {
                     builder.Append($"{time.Hours}h ");
                 }

                 builder.Append($"{time.Minutes.ToString().PadLeft(2, '0')}min");
             }

             BatteryChargeRateInfo = builder.ToString();*/
        }

        private void UpdateCpuTemperature(int value)
        {
            window.cpuTemperature.Text = value == 0 ? string.Empty : $"{value} °C";
        }
    }
}
