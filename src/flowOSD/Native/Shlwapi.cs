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

static class Shlwapi
{
    [DllImport(nameof(Shlwapi), SetLastError = true)]
    public static extern int ColorHLSToRGB(
        int hue,
        int luminance,
        int saturation);

    [DllImport(nameof(Shlwapi), SetLastError = true)]
    public static extern void ColorRGBToHLS(
        int rgb,
        out int hue,
        out int luminance,
        out int saturation);
}
