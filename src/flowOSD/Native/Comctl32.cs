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

using System;
using System.Runtime.InteropServices;

static class Comctl32
{
    public const int TDCBF_OK_BUTTON = 0x0001;
    public const int TDCBF_YES_BUTTON = 0x0002;
    public const int TDCBF_NO_BUTTON = 0x0004;
    public const int TDCBF_CANCEL_BUTTON = 0x0008;
    public const int TDCBF_RETRY_BUTTON = 0x0010;
    public const int TDCBF_CLOSE_BUTTON = 0x0020;

    public const int IDOK = 0x0001;
    public const int IDCANCEL = 0x0002;
    public const int IDYES = 0x0006;
    public const int IDNO = 0x0007;
    public const int IDRETRY = 0x0010;

    public delegate IntPtr SUBCLASSPROC(
        IntPtr hWnd,
        int uMsg,
        IntPtr wParam,
        IntPtr lParam,
        UIntPtr uIdSubclass,
        UIntPtr dwRefData);

    [DllImport(nameof(Comctl32), SetLastError = true)]
    public static extern bool SetWindowSubclass(
        IntPtr hWnd,
        SUBCLASSPROC pfnSubclass,
        UIntPtr uIdSubclass,
        UIntPtr dwRefData);

    [DllImport(nameof(Comctl32), SetLastError = true)]
    public static extern bool RemoveWindowSubclass(
        IntPtr hWnd,
        SUBCLASSPROC pfnSubclass,
        UIntPtr uIdSubclass);

    [DllImport(nameof(Comctl32), SetLastError = true)]
    public static extern IntPtr DefSubclassProc(
        IntPtr hWnd,
        int uMsg,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport(nameof(Comctl32), CharSet = CharSet.Unicode, EntryPoint = "TaskDialog")]
    public static extern int TaskDialog(
        IntPtr hWndParent,
        IntPtr hInstance,
        string pszWindowTitle,
        string pszMainInstruction,
        string pszContent,
        int dwCommonButtons,
        IntPtr pszIcon,
        out int pnButton);

    public static bool Confirm(string title, string message, string details)
    {
        var r = TaskDialog(
             IntPtr.Zero,
             IntPtr.Zero,
             title,
             message,
             details,
             TDCBF_YES_BUTTON | TDCBF_NO_BUTTON,
             IntPtr.Zero,
             out var buttonId);

        return r == 0 && buttonId == IDYES;
    }

    public static bool Warning(string title, string message, string details)
    {
        var r = TaskDialog(
             IntPtr.Zero,
             IntPtr.Zero,
             title,
             message,
             details,
             TDCBF_OK_BUTTON,
             IntPtr.Zero,
             out var buttonId);

        return r == 0 && buttonId == IDYES;
    }

    public static void Error(string title, string message, string details)
    {
        var r = TaskDialog(
             IntPtr.Zero,
             IntPtr.Zero,
             title,
             message,
             details,
             TDCBF_OK_BUTTON,
             IntPtr.Zero,
             out var buttonId);
    }
}
