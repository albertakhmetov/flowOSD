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

using flowOSD.Api.Configs;
using flowOSD.Extensions;
using flowOSD.UI.Commands;
using flowOSD.UI.Components;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using static flowOSD.Extensions.Common;

internal class KeyboardConfigPage : ConfigPageBase
{
    public KeyboardConfigPage(IConfig config, CxTabListener tabListener)
        : base(config, tabListener, isAvailable: !config.UseOptimizationMode)
    {
        Text = "Keyboard";

        var grid = AddConfig<int>(
            "Backlight timeout",
            nameof(config.Common.KeyboardBacklightTimeout),
            value => GetTimeoutText(value),
            () => CreateContextMenu(value => config.Common.KeyboardBacklightTimeout = value));

        grid.ColumnStyles[0].Width = 2;
        grid.ColumnStyles[1].Width = 1;
    }

    private CxContextMenu CreateContextMenu(Action<int> setValue)
    {
        var menu = new CxContextMenu();
        menu.BorderRadius = CornerRadius.Small;

        var relayCommand = new RelayCommand(x =>
        {
            if (x is int timeout)
            {
                setValue(timeout);
            }
        });

        menu.AddMenuItem(GetTimeoutText(5), relayCommand, 5);
        menu.AddMenuItem(GetTimeoutText(15), relayCommand, 15);
        menu.AddMenuItem(GetTimeoutText(30), relayCommand, 30);
        menu.AddMenuItem(GetTimeoutText(60), relayCommand, 60);
        menu.AddMenuItem(GetTimeoutText(300), relayCommand, 300);
        menu.AddMenuItem(GetTimeoutText(0), relayCommand, 0);

        menu.VisibleChanged += (sender, _) =>
        {
            foreach (var i in menu.Items)
            {
                if (i is ToolStripMenuItem menuItem && menuItem.CommandParameter is int menuItemValue)
                {
                    menuItem.Checked = menuItemValue == Config.Common.KeyboardBacklightTimeout;
                }
            }
        };

        return menu;
    }


}