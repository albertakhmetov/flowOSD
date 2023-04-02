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

using System.Collections.ObjectModel;
using System.Drawing.Drawing2D;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using flowOSD.Api;
using flowOSD.Extensions;
using flowOSD.Native;
using flowOSD.UI.Components;
using flowOSD.UI.ConfigPages;
using static flowOSD.Extensions.Forms;
using static flowOSD.Extensions.Common;
using flowOSD.Api.Hardware;
using flowOSD.Api.Configs;

sealed class ConfigUI : IDisposable
{
    private CompositeDisposable? disposable = new CompositeDisposable();

    private Window? instance;
    private IConfig config;
    private ICommandService commandService;
    private ISystemEvents systemEvents;
    private IHardwareService hardwareService;

    public ConfigUI(IConfig config, ICommandService commandService, ISystemEvents systemEvents, IHardwareService hardwareService)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
        this.systemEvents = systemEvents ?? throw new ArgumentNullException(nameof(systemEvents));
        this.hardwareService = hardwareService ?? throw new ArgumentNullException(nameof(hardwareService));

        systemEvents?.AppUI
            .Subscribe(x => instance?.UpdateUI(x))
            .DisposeWith(disposable);
    }

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;

        if (instance != null && !instance.IsDisposed)
        {
            instance.Dispose();
            instance = null;
        }
    }

    public async void Show()
    {
        if (instance != null && !instance.IsDisposed)
        {
            instance.Activate();
        }
        else
        {
            var tabListener = new CxTabListener();
            instance = new Window(tabListener,
                new ConfigPages.GeneralConfigPage(config, tabListener),
                new ConfigPages.NotificationsConfigPage(config, tabListener),
                new ConfigPages.KeyboardConfigPage(config, tabListener),
                new ConfigPages.HotKeysConfigPage(config, tabListener, commandService),
                new ConfigPages.MonitoringConfigPage(config, tabListener, hardwareService.Resolve<ICpu>()));

            instance.UpdateUI(await systemEvents.AppUI.FirstOrDefaultAsync());
            instance.Show();
        }
    }

    private sealed class Window : Form
    {
        private CompositeDisposable? disposable = new CompositeDisposable();

        private ReadOnlyCollection<ConfigPageBase> pages;

        private CxPanel pageContainer;
        private ListBox listBox;
        private UIParameters? uiParameters;
        private CxTabListener tabListener;

        public Window(CxTabListener tabListener, params ConfigPageBase[] pages)
        {
            this.tabListener = tabListener;
            this.tabListener.ShowKeyboardFocusChanged += TabListener_ShowKeyboardFocusChanged;
            this.pages = new ReadOnlyCollection<ConfigPageBase>(pages.Where(i => i.IsAvailable).ToList());

            pageContainer = Init(disposable);

            CurrentPage = pages.FirstOrDefault();
        }

        private void TabListener_ShowKeyboardFocusChanged(object? sender, EventArgs e)
        {
            listBox?.Invalidate();
        }

        public Control? CurrentPage
        {
            get => pageContainer.Content;
            set
            {
                pageContainer.Content = value;
            }
        }

        public void UpdateUI(UIParameters uiParameters)
        {
            if (IsDisposed)
            {
                return;
            }

            this.uiParameters = uiParameters;

            foreach (var page in pages)
            {
                page.UIParameters = uiParameters;
            }

            if (uiParameters == null)
            {
                return;
            }

            BackColor = uiParameters.BackgroundColor;
            ForeColor = uiParameters.TextColor;

            listBox.BackColor = uiParameters.BackgroundColor;

            Native.Dwmapi.UseDarkMode(Handle, uiParameters.IsDarkMode);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            disposable?.Dispose();
            disposable = null;

            base.OnClosed(e);
        }

        protected override void OnShown(EventArgs e)
        {
            UpdateSize();

            base.OnShown(e);
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            UpdateSize();

            base.OnDpiChanged(e);
        }

        private void UpdateSize()
        {
            MinimumSize = this.DpiScale(new Size(600, 400));
            Size = this.DpiScale(new Size(600, 500));
        }

        private CxPanel Init(CompositeDisposable uiDisposable)
        {
            const int listWidth = 150;
            const int listItemHeight = 40;

            var layout = Create<TableLayoutPanel>(x =>
            {
                x.Dock = DockStyle.Fill;

                x.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                x.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                x.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                x.RowStyles.Add(new RowStyle(SizeType.AutoSize, 100));
                x.RowStyles.Add(new RowStyle(SizeType.AutoSize, 100));
            }).DisposeWith(uiDisposable);

            var container = Create<CxPanel>(x =>
            {
                x.Dock = DockStyle.Fill;
            });
            layout.Add(1, 0, container);

            layout.Add<ListBox>(0, 0, x =>
            {
                x.BorderStyle = BorderStyle.None;
                x.Width = this.DpiScale(listWidth);
                x.DrawMode = DrawMode.OwnerDrawVariable;
                x.IntegralHeight = false;
                x.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;

                x.DataSource = pages;
                x.DisplayMember = nameof(Control.Text);

                x.SelectedIndexChanged += (_, _) =>
                {
                    var page = x.SelectedIndex < 0 ? null : pages[x.SelectedIndex];
                    if (page != CurrentPage)
                    {
                        CurrentPage = page;
                    }
                };

                x.PreviewKeyDown += (_, e) =>
                {
                    if (tabListener != null && e.KeyCode == Keys.Tab)
                    {
                        tabListener.ShowKeyboardFocus = true;
                    }
                };

                x.MouseClick += (_, _) =>
                {
                    if (tabListener != null)
                    {
                        tabListener.ShowKeyboardFocus = false;
                    }
                };

                x.DrawItem += (_, e) =>
                {
                    if (uiParameters == null || e.Font == null)
                    {
                        return;
                    }

                    // Clear
                    using var backgroundBrush = new SolidBrush(uiParameters.BackgroundColor);
                    e.Graphics.FillRectangle(backgroundBrush, e.Bounds);

                    const int FOCUS_SPACE = 6;

                    var text = pages[e.Index].Text;
                    var textSize = e.Graphics.MeasureString(text, e.Font);

                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                    var drawingAreaRect = new Rectangle(
                         e.Bounds.Left + FOCUS_SPACE,
                         e.Bounds.Top + FOCUS_SPACE,
                         e.Bounds.Width - 1 - FOCUS_SPACE * 2,
                         e.Bounds.Height - 1 - FOCUS_SPACE * 2);

                    var color = (e.State & DrawItemState.Selected) == DrawItemState.Selected
                        ? uiParameters.NavigationMenuBackgroundHoverColor
                        : uiParameters.BackgroundColor;

                    using var brush = new SolidBrush(color);
                    e.Graphics.FillRoundedRectangle(
                        brush,
                        drawingAreaRect,
                        (int)(IsWindows11 ? CornerRadius.Small : CornerRadius.Off));

                    if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                    {
                        using var accentBrush = new SolidBrush(uiParameters.AccentColor);
                        e.Graphics.FillRoundedRectangle(accentBrush,
                            drawingAreaRect.Left,
                            drawingAreaRect.Top + drawingAreaRect.Height / 8,
                            8,
                            drawingAreaRect.Height - drawingAreaRect.Height / 4,
                            (int)(IsWindows11 ? CornerRadius.Small : CornerRadius.Off));

                    }

                    if ((e.State & DrawItemState.Focus) == DrawItemState.Focus && tabListener?.ShowKeyboardFocus == true)
                    {
                        using var pen = new Pen(uiParameters.FocusColor, 2);
                        e.Graphics.DrawRoundedRectangle(pen,
                            e.Bounds.Left + 1,
                            e.Bounds.Top + 1,
                            e.Bounds.Width - 3,
                            e.Bounds.Height - 3,
                            (int)(IsWindows11 ? CornerRadius.Small : CornerRadius.Off));

                    }

                    var textBrush = color.IsBright()
                        ? new SolidBrush(uiParameters.MenuTextBrightColor)
                        : new SolidBrush(uiParameters.MenuTextColor);

                    e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    e.Graphics.DrawString(
                        text,
                        e.Font,
                        textBrush,
                        drawingAreaRect.Left + drawingAreaRect.Height / 2,
                        drawingAreaRect.Top + (drawingAreaRect.Height - textSize.Height) / 2);
                };

                x.MeasureItem += (_, e) =>
                {
                    e.ItemHeight = this.DpiScale(listItemHeight);
                    e.ItemWidth = this.DpiScale(listWidth);
                };

                x.DpiChangedAfterParent += (_, _) =>
                {
                    x.Width = this.DpiScale(listWidth);
                };

                x.DisposeWith(uiDisposable);
                x.LinkAs(ref listBox);
            });

            this.Add(layout);

            Padding = new Padding(10, 10, 5, 10);
            DoubleBuffered = true;

            Text = "Settings";
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.FixedSingle;

            Font = new Font(UIParameters.FontName, this.DpiScale(12), GraphicsUnit.Pixel);
            UpdateSize();

            Location = new Point(
                (Screen.PrimaryScreen.WorkingArea.Width - Width) / 2,
                (Screen.PrimaryScreen.WorkingArea.Height - Height) / 2);

            return container;
        }
    }
}