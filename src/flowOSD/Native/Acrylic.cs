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

using System.Runtime.InteropServices;
using flowOSD.Extensions;
using Microsoft.UI.Xaml;
using Windows.UI;

static class Acrylic
{
    public static void EnableAcrylic(Window window, Color blurColor)
    {
        if (window is null) throw new ArgumentNullException(nameof(window));

        var accentPolicy = new AccentPolicy
        {
            AccentState = ACCENT.ENABLE_ACRYLICBLURBEHIND,
            GradientColor = ToAbgr(blurColor)
        };

        unsafe
        {
            SetWindowCompositionAttribute(
                new HandleRef(window, window.GetHandle()),
                new WindowCompositionAttributeData
                {
                    Attribute = WCA.ACCENT_POLICY,
                    Data = &accentPolicy,
                    DataLength = Marshal.SizeOf<AccentPolicy>()
                });
        }
    }

    private static uint ToAbgr(Color color)
    {
        return (uint)color.A << 24
            | (uint)color.B << 16
            | (uint)color.G << 8
            | color.R;
    }

    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(HandleRef hWnd, in WindowCompositionAttributeData data);

    private unsafe struct WindowCompositionAttributeData
    {
        public WCA Attribute;
        public void* Data;
        public int DataLength;
    }

    private enum WCA
    {
        ACCENT_POLICY = 19
    }

    private enum ACCENT
    {
        DISABLED = 0,
        ENABLE_GRADIENT = 1,
        ENABLE_TRANSPARENTGRADIENT = 2,
        ENABLE_BLURBEHIND = 3,
        ENABLE_ACRYLICBLURBEHIND = 4,
        INVALID_STATE = 5
    }

    private struct AccentPolicy
    {
        public ACCENT AccentState;
        public uint AccentFlags;
        public uint GradientColor;
        public uint AnimationId;
    }
}
