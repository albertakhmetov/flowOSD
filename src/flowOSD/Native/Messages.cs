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
namespace flowOSD.Native;

using static flowOSD.Native.User32;

static class Messages
{
    public static readonly int WM_SHELLHOOK = RegisterWindowMessage("SHELLHOOK");
    public static readonly int WM_TASKBARCREATED = RegisterWindowMessage("TaskbarCreated");

    public const int HWND_BROADCAST = 0xFFFF;

    public const int WM_WININICHANGE = 0x001A;
    public const int WM_DISPLAYCHANGE = 0x7E;
    public const int WM_DEVICECHANGE = 0x219;
    public const int WM_DPICHANGED = 0x02E0;
    public const int WM_DPICHANGED_BEFOREPARENT = 0x02E2;

    // https://learn.microsoft.com/en-us/windows/win32/inputdev/mouse-input-notifications

    public const int WM_LBUTTONDOWN = 0x0201;
    public const int WM_LBUTTONUP = 0x0202;
    public const int WM_LBUTTONDBLCLK = 0x0203;
    public const int WM_RBUTTONDOWN = 0x0204;
    public const int WM_RBUTTONUP = 0x0205;
    public const int WM_RBUTTONDBLCLK = 0x0206;
    public const int WM_MBUTTONDOWN = 0x0207;
    public const int WM_MBUTTONUP = 0x0208;
    public const int WM_MBUTTONDBLCLK = 0x0209;

    public const int WM_MOUSEWHEEL = 0x020A;
    public const int WM_MOUSEHWHEEL = 0x020E;
    public const int WM_MOUSELEAVE = 0x02A3;
    public const int WM_MOUSEMOVE = 0x0200;

    public const int WM_CONTEXTMENU = 0x007B;

    public const int WM_ACTIVATE = 0x0006;

    public static int HiWord(IntPtr ptr)
    {
        var val = (int)ptr;
        if ((val & 0x80000000) == 0x80000000)
            return (val >> 16);
        else
            return (val >> 16) & 0xffff;
    }
}
