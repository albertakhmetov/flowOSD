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
using flowOSD.Api;

namespace flowOSD.UI.Components;

internal static class CxTheme
{
    public static void Apply(Control? control, UIParameters? uiParameters)
    {
        if (control == null || uiParameters == null)
        {
            return;
        }

        foreach (var c in control.Controls)
        {
            if (c is Control child)
            {
                Apply(child, uiParameters);
            }
        }

        if (control is CxButtonBase buttonBase)
        {
            buttonBase.AccentColor = uiParameters.AccentColor;
            buttonBase.ForeColor = uiParameters.TextGrayColor;
            buttonBase.BackColor = uiParameters.BackgroundColor;
            buttonBase.FocusColor = uiParameters.FocusColor;
        }

        if (control is CxButton button)
        {
            button.TextColor = uiParameters.ButtonTextColor;
            button.TextBrightColor = uiParameters.ButtonTextBrightColor;
            button.BackColor = uiParameters.ButtonBackgroundColor;

            if (button.DropDownMenu != null)
            {
                Apply(button.DropDownMenu, uiParameters);
            }
        }

        if (control is CxLabel label)
        {
            label.ForeColor = uiParameters.TextColor;
            label.BackColor = Color.Transparent;
        }

        if (control is CxGrid grid)
        {
            grid.ForeColor = uiParameters.TextColor;
            grid.BackColor = uiParameters.PanelBackgroundColor;
        }

        if (control is CxContextMenu menu)
        {
            menu.BackgroundColor = uiParameters.MenuBackgroundColor;
            menu.BackgroundCheckedColor = uiParameters.NavigationMenuBackgroundHoverColor;
            menu.BackgroundHoverColor = uiParameters.MenuBackgroundHoverColor;
            menu.TextColor = uiParameters.MenuTextColor;
            menu.TextBrightColor = uiParameters.MenuTextBrightColor;
            menu.TextDisabledColor = uiParameters.MenuTextDisabledColor;
            menu.AccentColor = uiParameters.AccentColor;           
        }

        if(control is CxPanel panel)
        {
            panel.ScrollerColor = uiParameters.BackgroundColor;
        }
    }
}
