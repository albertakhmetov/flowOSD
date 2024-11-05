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

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using flowOSD.Extensions;
using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace flowOSD.Native;

static class User32
{
    public const int ENUM_CURRENT_SETTINGS = -1;

    public const uint DISP_CHANGE_SUCCESSFUL = 0x00000000;
    public const uint DISP_CHANGE_RESTART = 0x00000001;
    public const uint DISP_CHANGE_FAILED = 0x80000001;
    public const uint DISP_CHANGE_BADMODE = 0x80000002;
    public const uint DISP_CHANGE_NOTUPDATED = 0x80000003;
    public const uint DISP_CHANGE_BADFLAGS = 0x80000004;
    public const uint DISP_CHANGE_BADPARAM = 0x80000005;
    public const uint DISP_CHANGE_BADDUALVIEW = 0x80000006;

    public const int DM_DISPLAYFREQUENCY = 0x400000;

    public const int CDS_UPDATEREGISTRY = 0x1;

    public const int SM_CONVERTIBLESLATEMODE = 0x2003;

    public const int SM_CXSCREEN = 0;
    public const int SM_CYSCREEN = 1;
    public const int SSM_CXFULLSCREEN = 16;
    public const int SSM_CYFULLSCREEN = 17;

    public const int SPI_GETWORKAREA = 0x0030;

    public delegate void WINEVENTPROC(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime);

    public delegate IntPtr WNDPROC(
        IntPtr hWnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam);

    public struct ICONDIR
    {
        public ushort idReserved;   // Reserved (must be 0)
        public ushort idType;       // Resource Type (1 for icons)
        public ushort idCount;      // How many images?
        public ICONDIRENTRY[] idEntries;   // An entry for each image (idCount of 'em)
    };

    public struct ICONDIRENTRY
    {
        public byte bWidth;          // Width, in pixels, of the image
        public byte bHeight;         // Height, in pixels, of the image
        public byte bColorCount;     // Number of colors in image (0 if >=8bpp)
        public byte bReserved;       // Reserved ( must be 0)
        public ushort wPlanes;       // Color Planes
        public ushort wBitCount;     // Bits per pixel
        public uint dwBytesInRes;    // How many bytes in this resource?
        public uint dwImageOffset;   // Where in the file is this image?
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct WNDCLASSEX
    {
        public int cbSize;
        public uint style;
        public WNDPROC lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public UInt16 wVk;
        public UInt16 wScan;
        public UInt32 dwFlags;
        public UInt32 time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public Int32 dx;
        public Int32 dy;
        public UInt32 mouseData;
        public UInt32 dwFlags;
        public UInt32 time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HARDWAREINPUT
    {
        public UInt32 uMsg;
        public UInt16 wParamL;
        public UInt16 wParamH;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct INPUTUNION
    {
        [FieldOffset(0)] public MOUSEINPUT Mouse;
        [FieldOffset(0)] public KEYBDINPUT Keyboard;
        [FieldOffset(0)] public HARDWAREINPUT Hardware;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public UInt32 type;
        public INPUTUNION union;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
    }

    [Flags]
    public enum KeyboardFlags
    {
        KEYEVENTF_KEYDOWN = 0x0000,
        KEYEVENTF_EXTENDEDKEY = 0x0001,
        KEYEVENTF_KEYUP = 0x0002,
        KEYEVENTF_UNICODE = 0x0004,
        KEYEVENTF_SCANCODE = 0x0008
    }

    [Flags]
    public enum InputType
    {
        INPUT_MOUSE = 0,
        INPUT_KEYBOARD = 1,
        INPUT_HARDWARE = 2
    }

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern bool SystemParametersInfo(
        uint uiAction,
        uint uiParam,
        IntPtr pvParam,
        uint fWinIni);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern int GetSystemMetrics(
        int nIndex);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern IntPtr CreateIconFromResourceEx(
        byte[] presbits,
        int dwResSize,
        bool fIcon,
        uint dwVer,
        int cxDesired,
        int cyDesired,
        uint Flags);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern bool DestroyIcon(
        IntPtr hIcon);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern uint SendInput(
        uint nInputs,
        INPUT[] pInputs,
        int cbSize);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern bool GetLastInputInfo(
        ref LASTINPUTINFO plii);

    [DllImport(nameof(User32), CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool EnumDisplayDevices(
        string? lpDevice,
        uint iDevNum,
        ref DISPLAY_DEVICE lpDisplayDevice,
        uint dwFlags);

    [DllImport(nameof(User32), CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool EnumDisplaySettings(
        string lpszDeviceName,
        int iModeNum,
        ref DEVMODE lpDevMode);

    [DllImport(nameof(User32), CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern uint ChangeDisplaySettingsEx(
        string lpszDeviceName,
        ref DEVMODE lpDevMode,
        IntPtr hwnd,
        int dwFlags,
        IntPtr lParam);

    [DllImport(nameof(User32), CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr LoadImage(
        IntPtr hinst,
        string lpszName,
        uint uType,
        int cxDesired,
        int cyDesired,
        uint fuLoad);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern int SendMessage(
        IntPtr hWnd,
        int wMsg,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport(nameof(User32), CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int RegisterWindowMessage(
        string lpString);

    [DllImport(nameof(User32), CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr GetWindowThreadProcessId(
        IntPtr hWnd,
        IntPtr lpdwProcessId);

    [DllImport(nameof(User32), CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr GetWindowThreadProcessId(
        IntPtr hWnd,
        out int lpdwProcessId);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern bool ShowWindow(
        IntPtr hWnd,
        int nCmdShow);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern IntPtr GetForegroundWindow();

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern bool SetForegroundWindow(
        IntPtr hWnd);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern bool BringWindowToTop(
        IntPtr hWnd);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern IntPtr SetActiveWindow(
        IntPtr hWnd);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern IntPtr FindWindowEx(
        IntPtr parentHandle,
        IntPtr hWndChildAfter,
        string className,
        string? windowTitle);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern bool AttachThreadInput(
        IntPtr idAttach,
        IntPtr idAttachTo,
        bool fAttach);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern IntPtr GetCurrentThreadId();

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern int GetDpiForSystem();

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern int GetDpiForWindow(
        IntPtr hwnd);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern bool GetCursorPos(
        out POINT lpPoint);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern IntPtr SetWinEventHook(
        uint eventMin,
        uint eventMax,
        IntPtr hmodWinEventProc,
        WINEVENTPROC lpfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint dwFlags);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern bool UnhookWinEvent(
        IntPtr hWinEventHook);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern IntPtr SetWindowLongPtr(
        IntPtr hWnd,
        int nIndex,
        IntPtr dwNewLong);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern IntPtr GetWindowLongPtr(
        IntPtr hWnd,
        int nIndex);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern short RegisterClassEx(
        ref WNDCLASSEX lpwcx);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern bool UnregisterClass(
        string lpClassName,
        IntPtr hInstance);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern IntPtr CreateWindowEx(
        ushort dwExStyle,
        string lpClassName,
        string lpWindowName,
        ushort dwStyle,
        int X,
        int Y,
        int nWidth,
        int nHeight,
        IntPtr hWndParent,
        IntPtr hMenu,
        IntPtr hInstance,
        IntPtr lpParam
        );

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern bool DestroyWindow(
        IntPtr hwnd);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern IntPtr DefWindowProc(
        IntPtr hWnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("user32.dll", SetLastError = false)]
    public static extern IntPtr GetShellWindow();

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetClassName(
        IntPtr hWnd,
        StringBuilder lpClassName,
        int nMaxCount);

    public static IntPtr AddStyle(IntPtr handle, int style)
    {
        var current = GetWindowLongPtr(handle, Styles.GWL_STYLE);

        return SetWindowLongPtr(handle, Styles.GWL_STYLE, current | style);
    }

    public static IntPtr RemoveStyle(IntPtr handle, int style)
    {
        var current = GetWindowLongPtr(handle, Styles.GWL_STYLE);

        return SetWindowLongPtr(handle, Styles.GWL_STYLE, current & ~style);
    }

    public static IntPtr AddExStyle(IntPtr handle, int style)
    {
        var current = GetWindowLongPtr(handle, Styles.GWL_EXSTYLE);

        return SetWindowLongPtr(handle, Styles.GWL_EXSTYLE, current | style);
    }

    public static IntPtr RemoveExStyle(IntPtr handle, int style)
    {
        var current = GetWindowLongPtr(handle, Styles.GWL_EXSTYLE);

        return SetWindowLongPtr(handle, Styles.GWL_EXSTYLE, current & ~style);
    }

    public static Point GetCursorPos()
    {
        GetCursorPos(out POINT p);

        return new Point(p.x, p.y);
    }

    public static void ShowAndActivate(Window window)
    {
        var handle = window.GetHandle();

        var currentlyFocusedWindowProcessId = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
        var appThread = GetWindowThreadProcessId(handle, IntPtr.Zero);

        if (currentlyFocusedWindowProcessId != appThread)
        {
            AttachThreadInput(currentlyFocusedWindowProcessId, appThread, true);
            BringWindowToTop(handle);
            window.AppWindow.Show();
            AttachThreadInput(currentlyFocusedWindowProcessId, appThread, false);
        }
        else
        {
            BringWindowToTop(handle);
            window.AppWindow.Show();
        }
    }

    public static string GetWindowClassName(IntPtr hWnd)
    {
        var className = new StringBuilder(256);

        return GetClassName(hWnd, className, className.Capacity) != 0 ? className.ToString() : string.Empty;
    }

    public static int GetShellProcessId()
    {
        var hWndShell = GetShellWindow();
        GetWindowThreadProcessId(hWndShell, out int pid);

        return Process.GetProcessesByName("explorer").FirstOrDefault(p => p.Id == pid)?.Id ?? 0;
    }

    public static Rect GetPrimaryWorkArea()
    {
        IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<Rect>());
        try
        {
            SystemParametersInfo(SPI_GETWORKAREA, 0, ptr, 0);

            var rect = Marshal.PtrToStructure<RECT>(ptr);
            return new Rect(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    public static bool ShowScreenBrightnessOsd()
    {
        var hostHandle = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Shell_TrayWnd", "");
        if (hostHandle > 0 && (hostHandle = FindWindowEx(hostHandle, IntPtr.Zero, "ReBarWindow32", "")) > 0)
        {
            var shellHandle = FindWindowEx(hostHandle, IntPtr.Zero, "MSTaskSwWClass", null);
            if (shellHandle > 0)
            {
                SendMessage(shellHandle, Messages.WM_SHELLHOOK, 0x37, 0);
                return true;
            }
        }

        return false;
    }
}
