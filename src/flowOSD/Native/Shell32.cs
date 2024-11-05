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

namespace flowOSD.Native;

static class Shell32
{
    public const uint NIF_MESSAGE = 0x00000001;
    public const uint NIF_ICON = 0x00000002;
    public const uint NIF_TIP = 0x00000004;
    public const uint NIF_STATE = 0x00000008;
    public const uint NIF_INFO = 0x00000010;
    public const uint NIF_GUID = 0x00000020;
    public const uint NIF_REALTIME = 0x00000040;
    public const uint NIF_SHOWTIP = 0x00000080;

    public const uint NIM_ADD = 0x00000000;
    public const uint NIM_MODIFY = 0x00000001;
    public const uint NIM_DELETE = 0x00000002;
    public const uint NIM_SETFOCUS = 0x00000003;
    public const uint NIM_SETVERSION = 0x00000004;

    public const int NIN_BALLOONHIDE = 0x403;
    public const int NIN_BALLOONSHOW = 0x402;
    public const int NIN_BALLOONTIMEOUT = 0x404;
    public const int NIN_BALLOONUSERCLICK = 0x405;
    public const int NIN_KEYSELECT = 0x403;
    public const int NIN_SELECT = 0x400;
    public const int NIN_POPUPOPEN = 0x406;
    public const int NIN_POPUPCLOSE = 0x407;

    [StructLayout(LayoutKind.Sequential)]
    public struct NOTIFYICONDATA
    {
        public int cbSize;

        public IntPtr hWnd;

        public uint uID;

        public uint uFlags;

        public int uCallbackMessage;

        public IntPtr hIcon;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;

        public uint dwState;

        public uint dwStateMask;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;

        public int uVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;

        public uint dwInfoFlags;

        public Guid guidItem;

        public IntPtr hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NOTIFYICONIDENTIFIER
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public Guid guidItem;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SHELLEXECUTEINFO
    {
        public int cbSize;
        public uint fMask;
        public IntPtr hWnd;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string lpVerb;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string lpFile;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string lpParameters;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string? lpDirectory;
        public int nShow;
        public IntPtr hInstApp;
        public IntPtr lpIDList;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string lpClass;
        public IntPtr hKeyClass;
        public uint dwHotKey;
        public IntPtr hIcon;
        public IntPtr hProcess;
    }

    public const int SW_HIDE = 0;
    public const int SW_SHOWNORMAL = 1;
    public const int SW_NORMAL = 1;
    public const int SW_SHOWMINIMIZED = 2;
    public const int SW_SHOWMAXIMIZED = 3;
    public const int SW_MAXIMIZE = 3;
    public const int SW_SHOWNOACTIVATE = 4;
    public const int SW_SHOW = 5;
    public const int SW_MINIMIZE = 6;
    public const int SW_SHOWMINNOACTIVE = 7;
    public const int SW_SHOWNA = 8;
    public const int SW_RESTORE = 9;
    public const int SW_SHOWDEFAULT = 10;
    public const int SW_FORCEMINIMIZE = 11;
    public const int SW_MAX = 1;

    public const uint SEE_MASK_DEFAULT = 0x00000000;
    public const uint SEE_MASK_CLASSNAME = 0x00000001;
    public const uint SEE_MASK_CLASSKEY = 0x00000003;
    public const uint SEE_MASK_IDLIST = 0x00000004;
    public const uint SEE_MASK_INVOKEIDLIST = 0x0000000c;   // Note SEE_MASK_INVOKEIDLIST(0xC) implies SEE_MASK_IDLIST(0x04);
    public const uint SEE_MASK_HOTKEY = 0x00000020;
    public const uint SEE_MASK_NOCLOSEPROCESS = 0x00000040;
    public const uint SEE_MASK_CONNECTNETDRV = 0x00000080;
    public const uint SEE_MASK_NOASYNC = 0x00000100;
    public const uint SEE_MASK_FLAG_DDEWAIT = SEE_MASK_NOASYNC;
    public const uint SEE_MASK_DOENVSUBST = 0x00000200;
    public const uint SEE_MASK_FLAG_NO_UI = 0x00000400;
    public const uint SEE_MASK_UNICODE = 0x00004000;
    public const uint SEE_MASK_NO_CONSOLE = 0x00008000;
    public const uint SEE_MASK_ASYNCOK = 0x00100000;
    public const uint SEE_MASK_HMONITOR = 0x00200000;
    public const uint SEE_MASK_NOZONECHECKS = 0x00800000;
    public const uint SEE_MASK_NOQUERYCLASSSTORE = 0x01000000;
    public const uint SEE_MASK_WAITFORINPUTIDLE = 0x02000000;
    public const uint SEE_MASK_FLAG_LOG_USAGE = 0x04000000;

    // See: https://learn.microsoft.com/en-us/windows/win32/api/shellapi/ns-shellapi-notifyicondataw

    [DllImport(nameof(Shell32), CallingConvention = CallingConvention.StdCall, SetLastError = true)]
    public static extern bool Shell_NotifyIcon(
        uint dwMessage, 
        ref NOTIFYICONDATA lpData);

    [DllImport(nameof(Shell32), SetLastError = true)]
    public static extern int Shell_NotifyIconGetRect(
        ref NOTIFYICONIDENTIFIER identifier,
        out RECT iconLocation);

    [DllImport(nameof(Shell32), CharSet = CharSet.Auto)]
    public static extern bool ShellExecuteEx(
        ref SHELLEXECUTEINFO lpExecInfo);
}