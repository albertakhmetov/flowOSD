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

using System.Runtime.InteropServices;
using System.Security;
using flowOSD.Extensions;

namespace flowOSD.Native;

static class Dwmapi
{
    public const uint DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    public const uint DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    public static void UseDarkMode(IntPtr hWnd, bool enabled)
    {
        var isDark = enabled ? 1 : 0;

        var value = GCHandle.Alloc(isDark, GCHandleType.Pinned);
        var result = DwmSetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE, value.AddrOfPinnedObject(), sizeof(uint));
        value.Free();

        if (result != 0)
        {
            throw Marshal.GetExceptionForHR(result) ?? throw new ApplicationException("Can't switch dark mode setting");
        }
    }

    public static void SetCornerPreference(IntPtr hWnd, DWM_WINDOW_CORNER_PREFERENCE cornerPreference)
    {
        if (!Common.IsWindows11)
        {
            return;
        }

        var value = GCHandle.Alloc((uint)cornerPreference, GCHandleType.Pinned);
        var result = DwmSetWindowAttribute(hWnd, DWMWA_WINDOW_CORNER_PREFERENCE, value.AddrOfPinnedObject(), sizeof(uint));
        value.Free();
        if (result != 0)
        {
            throw Marshal.GetExceptionForHR(result) ?? throw new ApplicationException("Can't set window corner setting");
        }

    }

    [SecurityCritical]
    [DllImport("dwmapi.dll", SetLastError = false, ExactSpelling = true)]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, uint dwAttribute, [In] IntPtr pvAttribute, int cbAttribute);

    public enum DWM_WINDOW_CORNER_PREFERENCE : uint
    {
        DWMWCP_DEFAULT = 0,
        DWMWCP_DONOTROUND = 1,
        DWMWCP_ROUND = 2,
        DWMWCP_ROUNDSMALL = 3
    }
}