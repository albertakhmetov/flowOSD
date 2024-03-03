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

namespace flowOSD.Core.Hardware;

public enum AtkKey : byte
{
    Rog = 0x38,
    Mic = 0x7c,
    BacklightDown = 0xc5,
    BacklightUp = 0xc4,
    Aura = 0xb3,
    Fan = 0xae,
    BrightnessDown = 0x10,
    BrightnessUp = 0x20,
    TouchPad = 0x6b,
    Sleep = 0x6c,
    Wireless = 0x88,
    Copy = 0x9e,
    Paste = 0x8a
}