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
using flowOSD.Api;
using flowOSD.Api.Configs;
using flowOSD.UI.Components;
using static flowOSD.Extensions.Common;
using static flowOSD.Extensions.Forms;

sealed class UpdaterUI : IDisposable
{
    private CompositeDisposable? disposable = new CompositeDisposable();

    private IConfig config;
    private ISystemEvents systemEvents;
    private IUpdater updater;

    private Window? instance;

    public UpdaterUI(IConfig config, ISystemEvents systemEvents, IUpdater updater)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.systemEvents = systemEvents ?? throw new ArgumentNullException(nameof(systemEvents));
        this.updater = updater ?? throw new ArgumentNullException(nameof(updater));

        systemEvents?.AppUI
            .Subscribe(x => instance?.UpdateUI(x))
            .DisposeWith(disposable);
    }

    public async void Show(Version version, bool hasUpdate)
    {
        if (instance != null && !instance.IsDisposed)
        {
            instance.BringToFront();
            instance.Activate();
        }
        else
        {
            instance = new Window(this, version, hasUpdate);

            instance.UpdateSize();

            instance.Location = new Point(
                (Screen.PrimaryScreen.WorkingArea.Width - instance.Width) / 2,
                (Screen.PrimaryScreen.WorkingArea.Height - instance.Height) / 2);

            instance.UpdateUI(await systemEvents.AppUI.FirstOrDefaultAsync());
            instance.Show();
            instance.BringToFront();
            instance.Activate();
        }
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

    private sealed class Window : Form, IProgress<int>
    {
        private CxTabListener tabListener { get; } = new CxTabListener();
        private CancellationTokenSource? cancellationTokenSource = new CancellationTokenSource();

        private CxLabel? label, linkLabel;

        public Window(UpdaterUI owner, Version version, bool hasUpdate)
        {
            MouseClick += OnMouseClick;

            var layout = Create<TableLayoutPanel>(x =>
            {
                x.MouseClick += OnMouseClick;
                x.Dock = DockStyle.Fill;

                x.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                x.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                x.Padding = new Padding(0);
            });

            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize, 100));
            layout.Add<CxLabel>(0, 0, 1, 2, x =>
            {
                x.TabListener = tabListener;
                x.AutoSize = true;
                x.Margin = new Padding(10, 10, 5, 5);
                x.TextAlign = ContentAlignment.MiddleLeft;

                x.Icon = UIImages.Updater;
                x.IconFont = new Font(UIParameters.IconFontName, 20);
            });

            layout.Add<CxLabel>(1, 0, x =>
            {
                x.Text = hasUpdate
                    ? $"A new version of {owner.config.ProductName} - {version.ToString(3)} is available."
                    : $"You have the latest version of {owner.config.ProductName}.";

                x.TabListener = tabListener;
                x.UseClearType = true;
                x.AutoSize = true;
                x.Margin = new Padding(15, 15, 5, 10);
                x.TextAlign = ContentAlignment.MiddleLeft;

                label = x;
            });

            if (hasUpdate)
            {
                layout.Add<CxLabel>(1, 1, x =>
                {
                    var link = owner.updater.ReleaseNotesLink;

                    x.Text = "View release notes";

                    x.TabListener = tabListener;
                    x.UseClearType = true;
                    x.AutoSize = true;
                    x.Margin = new Padding(15, 5, 5, 10);
                    x.TextAlign = ContentAlignment.MiddleLeft;

                    x.ForeColor = Color.Blue;
                    x.Cursor = Cursors.Hand;
                    x.Click += (sender, e) => { Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = link }); };

                    linkLabel = x;
                });
            }

            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Add<FlowLayoutPanel>(0, layout.RowStyles.Count - 1, 2, 1, panel =>
            {
                panel.MouseClick += OnMouseClick;
                panel.Anchor = AnchorStyles.Right | AnchorStyles.Top;
                panel.AutoSize = true;
                panel.Margin = new Padding(0, 10, 0, 5);
                panel.Padding = new Padding(0);
                panel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                panel.AutoSize = true;

                if (hasUpdate)
                {
                    panel.Add<CxButton>(x =>
                    {
                        x.BorderRadius = IsWindows11 ? CornerRadius.Small : CornerRadius.Off;
                        x.TabListener = tabListener;
                        x.Padding = new Padding(20, 5, 5, 5);
                        x.Margin = new Padding(-2);

                        x.Text = "Download and install";
                        x.AutoSize = true;
                        x.Click += async (sender, e) =>
                        {
                            linkLabel!.Visible = false;
                            label!.Text = "Preparing to downloading...";
                            cancellationTokenSource = new CancellationTokenSource();

                            x.Visible = false;
                            var isOk = await owner.updater.Download(version, this, cancellationTokenSource.Token);
                            if (!isOk)
                            {
                                label!.Text = "Something went wrong";
                                if (CancelButton is CxButton button)
                                {
                                    button.Text = "Exit";
                                }
                            }
                            else
                            {
                                owner.updater.Install(version);
                            }
                        };
                    });
                }

                panel.Add<CxButton>(x =>
                {
                    x.BorderRadius = IsWindows11 ? CornerRadius.Small : CornerRadius.Off;
                    x.TabListener = tabListener;
                    x.Padding = new Padding(20, 5, 5, 5);
                    x.Margin = new Padding(-2);

                    x.Text = hasUpdate ? "Cancel" : "Exit";
                    x.AutoSize = true;
                    x.Click += (sender, e) =>
                    {
                        cancellationTokenSource?.Cancel();
                        Close();
                    };

                    CancelButton = x;
                });
            });

            this.Add(layout);

            Padding = new Padding(10, 10, 10, 10);
            DoubleBuffered = true;

            AutoSize = true;
            Text = "Update";
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            FormBorderStyle = FormBorderStyle.FixedSingle;

            Font = new Font(UIParameters.FontName, this.DpiScale(12), GraphicsUnit.Pixel);
        }

        public void UpdateUI(UIParameters uiParameters)
        {
            if (IsDisposed)
            {
                return;
            }

            if (uiParameters == null)
            {
                return;
            }


            BackColor = uiParameters.BackgroundColor;
            ForeColor = uiParameters.TextColor;

            Native.Dwmapi.UseDarkMode(Handle, uiParameters.IsDarkMode);
            CxTheme.Apply(this, uiParameters);

            if (linkLabel != null && uiParameters != null)
            {
                linkLabel.ForeColor = uiParameters.AccentColor;
            }
        }

        public void UpdateSize()
        {
            MinimumSize = this.DpiScale(new Size(400, 150));
            Size = this.DpiScale(new Size(400, 150));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
            }

            base.Dispose(disposing);
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

        private void OnMouseClick(object? sender, MouseEventArgs e)
        {
            if (tabListener != null)
            {
                tabListener.ShowKeyboardFocus = false;
            }
        }

        void IProgress<int>.Report(int value)
        {
            label.Text = $"Downloading: {value} %";
        }
    }
}
