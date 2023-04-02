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

using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Windows.Forms;
using System.Windows.Input;
using flowOSD.Api;
using flowOSD.Api.Configs;
using flowOSD.Api.Hardware;
using flowOSD.Extensions;
using flowOSD.Native;
using flowOSD.UI.Commands;
using flowOSD.UI.Components;
using static flowOSD.Extensions.Common;
using static flowOSD.Extensions.Forms;
using static flowOSD.Native.Dwmapi;
using static flowOSD.Native.User32;

sealed class MainUI : IDisposable
{
    private Window? form;
    private IConfig config;
    private ISystemEvents systemEvents;
    private ICommandService commandService;
    private IPowerManagement powerManagement;
    private ICpu cpu;
    private IAtk atk;

    private IBattery battery;

    public MainUI(
        IConfig config,
        ISystemEvents systemEvents,
        ICommandService commandService,
        IHardwareService hardwareService)
    {
        this.config = config;
        this.systemEvents = systemEvents;
        this.systemEvents.Dpi.Subscribe(x => { form?.Dispose(); form = null; });

        this.commandService = commandService;

        battery = hardwareService.ResolveNotNull<IBattery>();
        powerManagement = hardwareService.ResolveNotNull<IPowerManagement>();
        cpu = hardwareService.ResolveNotNull<ICpu>();
        atk = hardwareService.ResolveNotNull<IAtk>();
    }

    void IDisposable.Dispose()
    {
        if (form != null && !form.IsDisposed)
        {
            form.Dispose();
            form = null;
        }
    }

    private IDisposable? d;

    public async void Show()
    {
        var screen = await systemEvents.PrimaryScreen.FirstOrDefaultAsync();
        if (screen == null)
        {
            return;
        }

        if (form == null)
        {
            form = new Window(this);
            form.Visible = false;
        }

        if (form.Visible)
        {
            Hide();
            return;
        }

        if ((DateTime.Now - form.LastHide).TotalMilliseconds < 500)
        {
            return;
        }

        var offset = form.DpiScale(10);

        form.Width = form.DpiScale(350);
        form.Height = form.DpiScale(300);

        form.Left = screen.WorkingArea.Width - form.Width - offset;
        form.Top = screen.WorkingArea.Height - form.Height - offset;

        const int delta = 100;

        form.Opacity = 0;
        form.Top += delta;

        ShowAndActivate(form.Handle);

        d?.Dispose();
        d = Observable
            .Timer(DateTimeOffset.Now, TimeSpan.FromMilliseconds(500 / 32))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(t =>
            {
                form.Opacity += (1 / (100f / 15));
                form.Location = new Point(form.Location.X, form.Location.Y - 15);

                if (form.Opacity == 1)
                {
                    d?.Dispose();
                }
            });
    }

    private void Hide()
    {
        d?.Dispose();

        if (form == null)
        {
            return;
        }

        d = Observable
            .Timer(DateTimeOffset.Now, TimeSpan.FromMilliseconds(500 / 32))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(t =>
            {
                form.Opacity -= (0.5 / (100f / 15));
                form.Location = new Point(form.Location.X, form.Location.Y + 15);

                if (form.Opacity < 0.5)
                {
                    form.Hide();
                    d?.Dispose();
                }
            });
    }

    private sealed class Window : Form
    {
        private CompositeDisposable? disposable = new CompositeDisposable();

        private IDisposable? batteryUpdate, cpuTemperatureUpdate;

        private CxTabListener tabListener = new CxTabListener();

        private MainUI owner;

        private ToolTip? toolTip;

        private CxButton? boostButton, refreshRateButton, dGpuButton, touchPadButton, performanceModeButton, powerModeButton;
        private CxLabel? boostLabel, refreshRateLabel, dGpuLabel, touchPadLabel, performanceModeLabel, powerModeLabel;
        private CxContextMenu? performanceModeMenu, powerModeMenu;
        private ICommand? performanceMenuItemCommand;

        private CxLabel? batteryLabel, cpuTemperatureLabel;

        public Window(MainUI owner)
        {
            this.owner = owner;
            this.owner.systemEvents.SystemUI
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(UpdateTheme)
                .DisposeWith(disposable);

            FormBorderStyle = FormBorderStyle.None;

            ShowInTaskbar = false;
            DoubleBuffered = true;

            KeyPreview = true;
            LastHide = DateTime.MinValue;

            InitComponents();

            owner.config.Common.PropertyChanged
                .Where(propertyName => propertyName == nameof(CommonConfig.ShowBatteryChargeRate))
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(_ => UpdateBatteryVisiblity())
                .DisposeWith(disposable!);

            owner.config.Common.PropertyChanged
                .Where(propertyName => propertyName == nameof(CommonConfig.ShowCpuTemperature))
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(_ => UpdateCpuTemperatureVisiblity())
                .DisposeWith(disposable!);

            owner.config.Common.PropertyChanged
                .Where(propertyName => propertyName == nameof(CommonConfig.PerformanceModeOverride))
                .CombineLatest(owner.atk.PerformanceMode, (_, performanceMode) => performanceMode)
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(UpdatePerformanceModeOverride)
                .DisposeWith(disposable!);

            owner.atk.PerformanceMode
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(UpdatePerformanceModeOverride)
                .DisposeWith(disposable!);

            owner.powerManagement.PowerMode
                .CombineLatest(
                    owner.powerManagement.IsBatterySaver,
                    (powerMode, isBatterySaver) => new { powerMode, isBatterySaver })
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(x => UpdatePowerMode(x.powerMode, x.isBatterySaver))
                .DisposeWith(disposable!);

            UpdateBatteryVisiblity();
            UpdateCpuTemperatureVisiblity();
        }

        public DateTime LastHide { get; set; }

        private void InitComponents()
        {
            toolTip = new ToolTip().DisposeWith(disposable!);

            var layout = Create<TableLayoutPanel>(x =>
            {
                x.BackColor = Color.Transparent;
                x.Dock = DockStyle.Fill;

                x.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                x.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                x.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                x.Padding = new Padding(0);
            });

            layout.Add<TableLayoutPanel>(0, 0, x =>
            {
                x.Margin = this.DpiScale(new Padding(14, 14, 14, 14));

                x.Dock = DockStyle.None;
                x.AutoSize = true;
                x.AutoSizeMode = AutoSizeMode.GrowAndShrink;

                x.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100 / 3f));
                x.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100 / 3f));
                x.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100 / 3f));

                x.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                x.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                x.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                x.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                var iconFont = new Font(UIParameters.IconFontName, this.DpiScale(16), GraphicsUnit.Pixel);

                boostButton = CreateButton(iconFont,
                    UIImages.Hardware_Cpu,
                    command: owner.commandService.Resolve<ToggleBoostCommand>());

                refreshRateButton = CreateButton(
                    iconFont,
                    UIImages.Hardware_Screen,
                    command: owner.commandService.Resolve<DisplayRefreshRateCommand>());

                dGpuButton = CreateButton(
                    iconFont,
                    UIImages.Hardware_Gpu,
                    command: owner.commandService.Resolve<GpuCommand>());

                touchPadButton = CreateButton(
                    iconFont,
                    UIImages.Hardware_TouchPad,
                    command: owner.commandService.Resolve<TouchPadCommand>());

                performanceModeButton = CreateButton(
                    iconFont,
                    "",
                    command: owner.commandService.Resolve<PerformanceModeCommand>(),
                    commandParameter: owner.config.Common.PerformanceModeOverride);

                performanceMenuItemCommand = new RelayCommand(x =>
                {
                    if (x is PerformanceMode performanceMode)
                    {
                        owner.commandService.Resolve<PerformanceModeCommand>()?.Execute(x);
                        owner.config.Common.PerformanceModeOverride = performanceMode;
                        owner.config.Common.PerformanceModeOverrideEnabled = performanceMode != PerformanceMode.Default;
                    }
                });

                performanceModeMenu = new CxContextMenu();
                performanceModeMenu.Font = new Font(UIParameters.FontName, this.DpiScale(13), GraphicsUnit.Pixel);
                performanceModeMenu.AddMenuItem(
                    PerformanceMode.Silent.ToText(),
                    performanceMenuItemCommand,
                    PerformanceMode.Silent);
                performanceModeMenu.AddMenuItem(
                    PerformanceMode.Turbo.ToText(),
                    performanceMenuItemCommand,
                    PerformanceMode.Turbo);
                performanceModeButton.DropDownMenu = performanceModeMenu;

                powerModeButton = CreateButton(
                    iconFont,
                    "");
                powerModeButton.IsToggle = false;

                powerModeMenu = new CxContextMenu();
                powerModeMenu.Font = new Font(UIParameters.FontName, this.DpiScale(13), GraphicsUnit.Pixel);

                powerModeMenu.AddMenuItem(
                    PowerMode.BestPowerEfficiency.ToText(),
                    owner.commandService.ResolveNotNull<PowerModeCommand>(),
                    PowerMode.BestPowerEfficiency);
                powerModeMenu.AddMenuItem(
                    PowerMode.Balanced.ToText(),
                    owner.commandService.ResolveNotNull<PowerModeCommand>(),
                    PowerMode.Balanced);
                powerModeMenu.AddMenuItem(
                    PowerMode.BestPerformance.ToText(),
                    owner.commandService.ResolveNotNull<PowerModeCommand>(),
                    PowerMode.BestPerformance);

                powerModeButton.DropDownMenu = powerModeMenu;

                x.Add(0, 0, boostButton);
                x.Add(1, 0, performanceModeButton);
                x.Add(2, 0, powerModeButton);
                x.Add(0, 2, refreshRateButton);
                x.Add(1, 2, dGpuButton);
                x.Add(2, 2, touchPadButton);

                var textFont = new Font(UIParameters.FontName, this.DpiScale(12), GraphicsUnit.Pixel);

                boostLabel = CreateLabel(textFont, UIText.MainUI_CpuBoost);
                refreshRateLabel = CreateLabel(textFont, UIText.MainUI_HighRefreshRate);
                dGpuLabel = CreateLabel(textFont, UIText.MainUI_Gpu);
                touchPadLabel = CreateLabel(textFont, UIText.MainUI_TouchPad);
                performanceModeLabel = CreateLabel(textFont, "<performance mode>");
                powerModeLabel = CreateLabel(textFont, "<power mode>");

                x.Add(0, 1, boostLabel);
                x.Add(1, 1, performanceModeLabel);
                x.Add(2, 1, powerModeLabel);
                x.Add(0, 3, refreshRateLabel);
                x.Add(1, 3, dGpuLabel);
                x.Add(2, 3, touchPadLabel);

            });

            layout.Add<TableLayoutPanel>(0, 1, x =>
            {
                x.Margin = this.DpiScale(new Padding(5));

                x.Dock = DockStyle.Fill;
                x.AutoSize = true;
                x.AutoSizeMode = AutoSizeMode.GrowAndShrink;

                x.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                x.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                x.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                x.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

                x.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                x.Add<CxLabel>(0, 0, label =>
                {
                    label.Margin = this.DpiScale(new Padding(10, 15, 0, 0));
                    label.TextAlign = ContentAlignment.MiddleLeft;
                    label.Font = new Font(UIParameters.FontName, this.DpiScale(10), GraphicsUnit.Pixel);
                    label.IconFont = new Font(UIParameters.IconFontName, this.DpiScale(13), GraphicsUnit.Pixel);

                    label.LinkAs(ref batteryLabel);
                });

                x.Add<CxLabel>(1, 0, label =>
                {
                    label.Margin = this.DpiScale(new Padding(10, 15, 0, 0));
                    label.TextAlign = ContentAlignment.MiddleLeft;
                    label.Size = this.DpiScale(new Size(60, 40));
                    label.Font = new Font(UIParameters.FontName, this.DpiScale(10), GraphicsUnit.Pixel);
                    label.Icon = UIImages.Temperature;
                    label.IconFont = new Font(UIParameters.IconFontName, this.DpiScale(13), GraphicsUnit.Pixel);

                    label.LinkAs(ref cpuTemperatureLabel);
                });

                x.Add<CxButton>(3, 0, button =>
                {
                    button.Margin = this.DpiScale(new Padding(3));

                    button.Size = this.DpiScale(new Size(48, 48));
                    button.Icon = UIImages.Settings;
                    button.IconFont = new Font(UIParameters.IconFontName, this.DpiScale(17), GraphicsUnit.Pixel);
                    button.IsToggle = false;
                    button.IsTransparent = true;
                    button.TabListener = tabListener;
                    button.BorderRadius = IsWindows11 ? CornerRadius.Round : CornerRadius.Off;

                    button.Command = owner.commandService.Resolve<SettingsCommand>();
                });
            });

            Controls.Add(layout);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                disposable?.Dispose();
                disposable = null;

                batteryUpdate?.Dispose();
                batteryUpdate = null;
            }

            base.Dispose(disposing);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            if (IsWindows11)
            {
                SetCornerPreference(Handle, DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND);
            }

            base.OnHandleCreated(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Transparent);
        }

        protected override void OnActivated(EventArgs e)
        {
            if (owner.config.Common.ShowBatteryChargeRate)
            {
                batteryUpdate = owner.battery.Rate
                    .CombineLatest(
                        owner.battery.Capacity,
                        owner.battery.PowerState,
                        owner.battery.EstimatedTime,
                        (rate, capacity, powerState, estimatedTime) => new { rate, capacity, powerState, estimatedTime })
                    .ObserveOn(SynchronizationContext.Current!)
                    .Subscribe(x => UpdateBattery(x.rate, x.capacity, x.powerState, x.estimatedTime));
            }

            if (owner.cpu.IsAvailable && owner.config.Common.ShowCpuTemperature)
            {
                cpuTemperatureUpdate = owner.cpu.Temperature
                      .ObserveOn(SynchronizationContext.Current!)
                      .Subscribe(UpdateCpuTemperature);
            }

            base.OnActivated(e);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            batteryUpdate?.Dispose();
            batteryUpdate = null;

            cpuTemperatureUpdate?.Dispose();
            cpuTemperatureUpdate = null;

            LastHide = DateTime.Now;

            owner.Hide();

            base.OnDeactivate(e);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (e.KeyChar == 27)
            {
                Hide();
            }

            base.OnKeyPress(e);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            if (Visible)
            {
                tabListener.ShowKeyboardFocus = false;
            }

            base.OnVisibleChanged(e);
        }

        private CxLabel CreateLabel(Font textFont, string? text = null)
        {
            var x = new CxLabel();

            x.AutoSize = true;
            x.Margin = this.DpiScale(new Padding(0, 0, 0, 14));
            x.TextAlign = ContentAlignment.MiddleCenter;
            x.Dock = DockStyle.Fill;
            x.Text = text;
            x.Font = textFont;
            x.TabListener = tabListener;

            return x;
        }

        private CxButton CreateButton(
            Font iconFont,
            string icon,
            Font? textFont = null,
            string? text = null,
            CommandBase? command = null,
            object? commandParameter = null)
        {
            var x = new CxButton();

            x.Margin = this.DpiScale(new Padding(4));
            x.Size = this.DpiScale(new Size(120, 55));
            x.Icon = icon;
            x.IconFont = iconFont;
            x.Text = text;
            x.Font = textFont;
            x.IsToggle = true;
            x.IsTransparent = false;
            x.TabListener = tabListener;
            x.BorderRadius = IsWindows11 ? CornerRadius.Round : CornerRadius.Off;

            if (command != null)
            {
                x.Command = command;
                x.CommandParameter = commandParameter;
                x.DataBindings.Add("IsChecked", command, "IsChecked");
            }

            return x;
        }

        private void UpdateTheme(UIParameters uiParameters)
        {
            if (uiParameters == null)
            {
                return;
            }

            BackColor = uiParameters.BackgroundColor;
            Acrylic.EnableAcrylic(this, uiParameters.BackgroundColor.SetAlpha(210));

            CxTheme.Apply(this, uiParameters);

            Invalidate();
        }

        private void UpdateBattery(int rate, uint capacity, BatteryPowerState powerState, uint estimatedTime)
        {
            if (batteryLabel == null || toolTip == null)
            {
                return;
            }

            var isEmptyRate = Math.Abs(rate) < 100;

            batteryLabel.Icon = GetBatteryIcon(capacity, powerState);
            batteryLabel.Text = isEmptyRate ? "" : $"{rate / 1000f:N1} W";

            if (string.IsNullOrEmpty(batteryLabel.Text))
            {
                batteryLabel.Size = this.DpiScale(new Size(15, 40));
            }
            else
            {
                batteryLabel.Size = this.DpiScale(new Size(55, 40));
            }


            var time = TimeSpan.FromSeconds(estimatedTime);
            var builder = new StringBuilder();
            builder.Append($"{capacity * 100f / owner.battery.FullChargedCapacity:N0}%");

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

            toolTip.SetToolTip(batteryLabel, builder.ToString());
        }

        private string GetBatteryIcon(uint capacity, BatteryPowerState powerState)
        {
            var power = (powerState & BatteryPowerState.PowerOnLine) == BatteryPowerState.PowerOnLine
                ? 11
                : 0;
            var c = Math.Round((capacity * 10f) / owner.battery.FullChargedCapacity);

            return new string((char)(0xf5f2 + c + power), 1);
        }

        private void UpdateBatteryVisiblity()
        {
            if (batteryLabel == null)
            {
                return;
            }

            batteryLabel.Visible = owner.config.Common.ShowBatteryChargeRate;
        }

        private void UpdateCpuTemperature(uint value)
        {
            if (cpuTemperatureLabel == null)
            {
                return;
            }

            cpuTemperatureLabel.Visible = value > 0 && owner.cpu.IsAvailable && owner.config.Common.ShowCpuTemperature;
            cpuTemperatureLabel.Text = value == 0 ? string.Empty : $"{value} °C";
        }

        private void UpdateCpuTemperatureVisiblity()
        {
            if (cpuTemperatureLabel != null)
            {
                cpuTemperatureLabel.Visible = owner.cpu.IsAvailable && owner.config.Common.ShowCpuTemperature;
            }
        }

        private void UpdatePerformanceModeOverride(PerformanceMode performanceMode)
        {
            if (performanceModeButton == null || performanceModeLabel == null)
            {
                return;
            }

            switch (owner.config.Common.PerformanceModeOverride)
            {
                case PerformanceMode.Silent:
                    performanceModeButton.Icon = UIImages.Performance_Silent;
                    performanceModeButton.CommandParameter = performanceMode == PerformanceMode.Default
                        ? PerformanceMode.Silent
                        : PerformanceMode.Default;
                    break;

                case PerformanceMode.Turbo:
                    performanceModeButton.Icon = UIImages.Performance_Turbo;
                    performanceModeButton.CommandParameter = performanceMode == PerformanceMode.Default
                        ? PerformanceMode.Turbo
                        : PerformanceMode.Default;
                    break;
            }

            performanceModeLabel.Text = owner.config.Common.PerformanceModeOverride.ToText();
        }

        private void UpdatePowerMode(PowerMode powerMode, bool isBatterySaver)
        {
            if (powerModeButton == null || powerModeLabel == null)
            {
                return;
            }

            powerModeButton.Enabled = !isBatterySaver;

            if (isBatterySaver)
            {
                powerModeButton.DropDownMenu = null;
                powerModeButton.Icon = UIImages.Power_BatterySaver;
                powerModeLabel.Text = UIText.Power_BatterySaver;
            }
            else
            {
                powerModeButton.DropDownMenu = powerModeMenu;
                switch (powerMode)
                {
                    case PowerMode.BestPowerEfficiency:
                        powerModeButton.Icon = UIImages.Power_BestPowerEfficiency;
                        powerModeLabel.Text = powerMode.ToText();
                        break;

                    case PowerMode.Balanced:
                        powerModeButton.Icon = UIImages.Power_Balanced;
                        powerModeLabel.Text = powerMode.ToText();
                        break;

                    case PowerMode.BestPerformance:
                        powerModeButton.Icon = UIImages.Power_BestPerformance;
                        powerModeLabel.Text = powerMode.ToText();
                        break;
                }
            }
        }
    }
}
