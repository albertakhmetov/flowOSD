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

namespace flowOSD.Native;

using System;
using System.Runtime.InteropServices;

static class Comctl32
{
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
}
