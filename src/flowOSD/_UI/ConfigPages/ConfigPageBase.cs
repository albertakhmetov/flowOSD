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
namespace flowOSD.UI.ConfigPages;

using flowOSD.Api;
using flowOSD.Api.Configs;
using flowOSD.Extensions;
using flowOSD.UI.Components;
using System.Drawing.Drawing2D;
using System.Reactive.Disposables;
using static flowOSD.Extensions.Common;
using static flowOSD.Extensions.Forms;

internal class ConfigPageBase : TableLayoutPanel
{
    protected static readonly Padding CheckBoxMargin = new Padding(20, 5, 0, 5);
    protected static readonly Padding LabelMargin = new Padding(15, 10, 0, 15);

    private UIParameters? uiParameters;

    protected ConfigPageBase(IConfig config, CxTabListener? tabListener = null, bool isAvailable = true)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        TabListener = tabListener ?? throw new ArgumentNullException(nameof(tabListener));
        IsAvailable = isAvailable;

        AutoScroll = false;
        AutoSize = true;
        // AutoSizeMode = AutoSizeMode.GrowAndShrink;
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        Padding = new Padding(3);
        IconFont = new Font(UIParameters.IconFontName, 16, GraphicsUnit.Point);

        MouseClick += OnMouseClick;
    }

    public UIParameters? UIParameters
    {
        get => uiParameters;
        set
        {
            uiParameters = value;
            UpdateUI();
        }
    }

    public bool IsAvailable { get; }

    protected IConfig Config { get; }

    protected CxTabListener? TabListener { get; }

    protected Font IconFont { get; }

    protected void OnMouseClick(object? sender, MouseEventArgs e)
    {
        if (TabListener != null)
        {
            TabListener.ShowKeyboardFocus = false;
        }
    }

    protected void AddConfig(string icon, string text, string propertyName)
    {
        RowStyles.Add(new RowStyle(SizeType.AutoSize, 100));
        this.Add<CxGrid>(0, RowStyles.Count - 1, x =>
        {
            x.TabListener = TabListener;
            x.Padding = new Padding(10, 5, 10, 5);
            x.Dock = DockStyle.Top;
            x.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            x.AutoSize = true;
            x.BorderRadius = IsWindows11 ? CornerRadius.Small : CornerRadius.Off;

            x.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            x.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            x.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            x.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            if (!string.IsNullOrEmpty(icon))
            {
                x.Add<CxLabel>(0, 0, y =>
                {
                    y.TabListener = TabListener;
                    y.AutoSize = true;
                    y.Margin = LabelMargin;
                    y.Padding = new Padding(0, 10, 0, 0);
                    y.Anchor = AnchorStyles.Left;
                    y.ForeColor = Color.Wheat;
                    y.Icon = icon;
                    y.IconFont = IconFont;
                });
            }

            x.Add<CxLabel>(1, 0, y =>
            {
                y.TabListener = TabListener;
                y.AutoSize = true;
                y.Margin = LabelMargin;
                y.Text = text;
                y.Anchor = AnchorStyles.Left;
                y.ForeColor = SystemColors.ControlDarkDark;
                y.UseClearType = true;
            });

            x.Add<CxToggle>(2, 0, y =>
            {
                y.TabListener = TabListener;
                y.BackColor = SystemColors.Control;
                y.ForeColor = SystemColors.WindowText;
                y.Margin = CheckBoxMargin;
                y.Anchor = AnchorStyles.Right;
                y.DataBindings.Add(
                    "IsChecked",
                    Config.Common,
                    propertyName,
                    false,
                    DataSourceUpdateMode.OnPropertyChanged);
            });
        });
    }

    protected CxToggle AddConfig(string icon, string text, Func<bool> getValue, Action<bool> setValue)
    {
        var toogle = default(CxToggle);

        RowStyles.Add(new RowStyle(SizeType.AutoSize, 100));
        this.Add<CxGrid>(0, RowStyles.Count - 1, x =>
        {
            x.TabListener = TabListener;
            x.Padding = new Padding(10, 5, 10, 5);
            x.Dock = DockStyle.Top;
            x.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            x.AutoSize = true;
            x.BorderRadius = IsWindows11 ? CornerRadius.Small : CornerRadius.Off;

            x.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            x.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            x.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            x.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            if (!string.IsNullOrEmpty(icon))
            {
                x.Add<CxLabel>(0, 0, y =>
                {
                    y.TabListener = TabListener;
                    y.AutoSize = true;
                    y.Margin = LabelMargin;
                    y.Padding = new Padding(0, 10, 0, 0);
                    y.Anchor = AnchorStyles.Left;
                    y.ForeColor = Color.Wheat;
                    y.Icon = icon;
                    y.IconFont = IconFont;
                });
            }

            x.Add<CxLabel>(1, 0, y =>
            {
                y.TabListener = TabListener;
                y.AutoSize = true;
                y.Margin = LabelMargin;
                y.Text = text;
                y.Anchor = AnchorStyles.Left;
                y.ForeColor = SystemColors.ControlDarkDark;
                y.UseClearType = true;
            });

            x.Add<CxToggle>(2, 0, y =>
            {
                y.TabListener = TabListener;
                y.BackColor = SystemColors.Control;
                y.ForeColor = SystemColors.WindowText;
                y.Margin = CheckBoxMargin;
                y.Anchor = AnchorStyles.Right;
                y.IsChecked = getValue();
                y.IsCheckedChanged += (_, _) => setValue(y.IsChecked);

                toogle = y;
            });
        });

        return toogle!;
    }

    protected CxGrid AddConfig(string text, Func<CxContextMenu> getMenu, string icon = null)
    {
        var layout = Create<CxGrid>(grid =>
        {
            grid.TabListener = TabListener;
            grid.Padding = new Padding(10, 5, 10, 5);
            grid.Dock = DockStyle.Top;
            grid.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            grid.AutoSize = true;
            grid.BorderRadius = IsWindows11 ? CornerRadius.Small : CornerRadius.Off;

            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 1));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 2));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 4));
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            if (!string.IsNullOrEmpty(icon))
            {
                grid.Add<CxLabel>(0, 0, x =>
                {
                    x.AutoSize = true;
                    x.MinimumSize = new Size(70, 30);
                    x.TabListener = TabListener;
                    x.Margin = new Padding(5, 10, 10, 10);
                    x.Padding = new Padding(10);
                    x.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                    x.ForeColor = SystemColors.ControlText;
                    x.TextAlign = ContentAlignment.MiddleCenter;

                    if (icon.Length > 4)
                    {
                        x.Text = icon;
                        x.ShowKeys = true;
                        x.BlackAndWhite = true;
                        x.UseClearType = true;
                    }
                    else
                    {
                        x.Icon = icon;
                        x.IconFont = IconFont;
                    }
                });
            }

            grid.Add<CxLabel>(1, 0, x =>
            {
                x.AutoSize = true;
                x.MinimumSize = new Size(100, 30);
                x.TabListener = TabListener;
                x.Margin = new Padding(5, 10, 20, 10);
                x.Padding = new Padding(10);
                x.Text = text;
                x.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                x.ForeColor = SystemColors.ControlText;
                x.UseClearType = true;
                x.ShowKeys = true;
                x.TextAlign = ContentAlignment.MiddleCenter;
            });

            grid.Add<CxButton>(2, 0, x =>
            {
                x.AutoSize = true;
                x.BorderRadius = IsWindows11 ? CornerRadius.Small : CornerRadius.Off;
                x.TabListener = TabListener;
                x.Margin = new Padding(0, 5, 0, 5);
                x.Padding = new Padding(10, 10, 15, 10);
                x.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                x.DropDownMenu = getMenu();

                x.TextAlign = ContentAlignment.MiddleLeft;
                x.IconFont = IconFont;
            });
        });

        RowStyles.Add(new RowStyle(SizeType.AutoSize));
        this.Add(0, RowStyles.Count - 1, layout);

        return layout;
    }

    protected CxGrid AddConfig<T>(string text, string propertyName, Func<T?, string> getTextValue, Func<CxContextMenu> getMenu)
    {
        var r = default(CxGrid);

        RowStyles.Add(new RowStyle(SizeType.AutoSize));
        this.Add<CxGrid>(0, RowStyles.Count - 1, grid =>
        {
            grid.TabListener = TabListener;
            grid.Padding = new Padding(10, 5, 10, 5);
            grid.Dock = DockStyle.Top;
            grid.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            grid.AutoSize = true;
            grid.BorderRadius = IsWindows11 ? CornerRadius.Small : CornerRadius.Off;

            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 1));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 3));
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            grid.Add<CxLabel>(0, 0, x =>
            {
                x.AutoSize = true;
                x.MinimumSize = new Size(100, 30);
                x.TabListener = TabListener;
                x.Margin = new Padding(5, 10, 20, 10);
                x.Padding = new Padding(10);
                x.Text = text;
                x.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                x.ForeColor = SystemColors.ControlText;
                x.UseClearType = true;
                x.ShowKeys = true;
            });

            grid.Add<CxButton>(1, 0, x =>
            {
                x.AutoSize = true;
                x.BorderRadius = IsWindows11 ? CornerRadius.Small : CornerRadius.Off;
                x.TabListener = TabListener;
                x.Margin = new Padding(0, 5, 0, 5);
                x.Padding = new Padding(10, 10, 15, 10);
                x.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                x.DropDownMenu = getMenu();

                x.TextAlign = ContentAlignment.MiddleLeft;
                x.IconFont = IconFont;

                var binding = new Binding("Text", Config.Common, propertyName, true, DataSourceUpdateMode.Never);
                binding.Format += (_, e) => e.Value = getTextValue(e.Value is T ? (T)e.Value : default(T));

                x.DataBindings.Add(binding);
            });

            r = grid;
        });

        return r!;
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        if (Visible)
        {
            UpdateUI();
        }

        base.OnVisibleChanged(e);
    }

    protected virtual void UpdateUI()
    {
        if (UIParameters == null)
        {
            return;
        }

        BackColor = UIParameters.BackgroundColor;

        CxTheme.Apply(this, UIParameters);
    }
}
