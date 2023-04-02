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
using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;

internal class GeneralConfigPage : ConfigPageBase
{
    private CxLabel platformLabel, siteLink;

    public GeneralConfigPage(IConfig config, CxTabListener cxTabListener)
        : base(config, cxTabListener)
    {
        Text = "General";

        AddAbout();

        AddConfig(
            "",
            "Run at logon",
            nameof(CommonConfig.RunAtStartup));

        AddConfig(
            "",
            "Disable TouchPad in tablet mode",
            nameof(CommonConfig.DisableTouchPadInTabletMode));

        AddConfig(
            "",
            "Control display refresh rate",
            nameof(CommonConfig.ControlDisplayRefreshRate));

        AddConfig(
            "",
            "Confirm GPU change",
            nameof(CommonConfig.ConfirmGpuModeChange));
        
        AddConfig(
            "",
            "Check for updates at startup",
            nameof(CommonConfig.CheckForUpdates));
    }

    protected override void UpdateUI()
    {
        base.UpdateUI();

        if (siteLink != null && UIParameters != null)
        {
            siteLink.ForeColor = UIParameters.AccentColor;
        }

        if (platformLabel != null && UIParameters != null)
        {
            platformLabel.ForeColor = UIParameters.TextGrayColor;
        }
    }

    private void AddAbout()
    {
        RowStyles.Add(new RowStyle(SizeType.AutoSize));
        this.Add<CxGrid>(0, 0, grid =>
        {
            grid.TabListener = TabListener;
            grid.MouseClick += OnMouseClick;
            grid.Padding = new Padding(20, 20, 20, 20);
            grid.Dock = DockStyle.Top;
            grid.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            grid.AutoSize = true;

            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            grid.Add<CxLabel>(0, 0, 1, 2, x =>
            {
                x.TabListener = TabListener;
                x.UseClearType = true;
                x.AutoSize = true;
                x.Margin = new Padding(5, 5, 20, 20);
                x.Text = Config.AppFileInfo.ProductName;
                x.Anchor = AnchorStyles.Left | AnchorStyles.Top;
                x.ForeColor = SystemColors.ControlText;
                x.Font = new Font(Font.FontFamily, 20);
            });

            grid.Add<CxLabel>(1, 0, x =>
            {
                var sb = new StringBuilder();
#if !DEBUG
                sb.AppendLine($"Version: {Config.AppFileInfo.ProductVersion}");
#else
                sb.AppendLine($"Version: {Config.AppFileInfo.ProductVersion} [DEBUG BUILD]");
#endif
                sb.AppendLine($"{Config.AppFileInfo.LegalCopyright}");

                x.TabListener = TabListener;
                x.UseClearType = true;
                x.Text = sb.ToString();
                x.AutoSize = true;
                x.Margin = new Padding(5, 15, 20, 3);
            });

            grid.Add<CxLabel>(1, 1, x =>
            {
                x.Text =$"Runtime: {Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName}";

                x.TabListener = TabListener;
                x.UseClearType = true;
                x.AutoSize = true;
                x.Margin = new Padding(5, 15, 20, 3);

                x.LinkAs(ref platformLabel);
            });

            grid.Add<CxLabel>(1, 2, x =>
            {
                x.UseClearType = true;
                x.Text = "https://github.com/albertakhmetov/flowOSD";
                x.ForeColor = Color.Blue;
                x.Cursor = Cursors.Hand;
                x.Click += (sender, e) => { Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = x.Text }); };

                x.TabListener = TabListener;
                x.AutoSize = true;
                x.Margin = new Padding(5, 3, 0, 3);

                x.LinkAs(ref siteLink);
            });
        });
    }
}
