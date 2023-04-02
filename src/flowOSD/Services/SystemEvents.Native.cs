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
namespace flowOSD.Services;

using System.Runtime.InteropServices;
using Windows.UI;

partial class SystemEvents
{
    [DllImport("uxtheme.dll", EntryPoint = "#95")]
    private static extern uint GetImmersiveColorFromColorSetEx(
        uint dwImmersiveColorSet,
        uint dwImmersiveColorType,
        bool bIgnoreHighContrast,
        uint dwHighContrastCacheMode);

    [DllImport("uxtheme.dll", EntryPoint = "#96")]
    private static extern uint GetImmersiveColorTypeFromName(IntPtr pName);

    [DllImport("uxtheme.dll", EntryPoint = "#98")]
    private static extern uint GetImmersiveUserColorSetPreference(
        bool bForceCheckRegistry,
        bool bSkipCheckOnFail);

    public static Color GetAccentColor()
    {
        var colorSetEx = GetImmersiveColorFromColorSetEx(
          GetImmersiveUserColorSetPreference(false, false),
          GetImmersiveColorTypeFromName(Marshal.StringToHGlobalUni("ImmersiveStartSelectionBackground")),
          false, 0);

        return Color.FromArgb(255,
            (byte)(0xFFFFFF & colorSetEx),
            (byte)((0xFFFFFF & colorSetEx) >> 8),
            (byte)((0xFFFFFF & colorSetEx) >> 16));
    }
}