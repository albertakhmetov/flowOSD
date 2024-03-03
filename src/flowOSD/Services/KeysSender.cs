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
namespace flowOSD.Services;

using System.ComponentModel;
using System.Runtime.InteropServices;
using flowOSD.Core;
using Windows.System;
using static flowOSD.Native.User32;

sealed class KeysSender : IKeysSender
{
    private readonly HashSet<VirtualKey> extendedKeys;

    public KeysSender()
    {
        extendedKeys = new HashSet<VirtualKey>(new VirtualKey[]
        {
            VirtualKey.Menu,
            VirtualKey.LeftMenu,
            VirtualKey.RightMenu,
            VirtualKey.Control,
            VirtualKey.RightControl,
            VirtualKey.Insert,
            VirtualKey.Delete,
            VirtualKey.Home,
            VirtualKey.End,
            VirtualKey.Right,
            VirtualKey.Up,
            VirtualKey.Left,
            VirtualKey.Down,
            VirtualKey.Cancel,
            VirtualKey.Snapshot,
            VirtualKey.Divide
        });
    }

    public void SendKeys(VirtualKey key, params VirtualKey[] modifierKeys)
    {
        var inputList = new List<INPUT>();

        foreach (var k in modifierKeys)
        {
            AddKeyboardInput(inputList,
                (UInt16)k,
                (UInt32)(extendedKeys.Contains(k) ? KeyboardFlags.KEYEVENTF_EXTENDEDKEY : 0));
        }

        AddKeyboardInput(inputList,
            (UInt16)key,
            (UInt32)(extendedKeys.Contains(key) ? (KeyboardFlags.KEYEVENTF_EXTENDEDKEY) : 0));
        AddKeyboardInput(inputList,
            (UInt16)key,
            (UInt32)(extendedKeys.Contains(key) ? (KeyboardFlags.KEYEVENTF_EXTENDEDKEY | KeyboardFlags.KEYEVENTF_KEYUP) : KeyboardFlags.KEYEVENTF_KEYUP));

        foreach (var k in modifierKeys.Reverse())
        {
            AddKeyboardInput(
                inputList,
                (UInt16)k,
                (UInt32)(extendedKeys.Contains(k) ? (KeyboardFlags.KEYEVENTF_EXTENDEDKEY | KeyboardFlags.KEYEVENTF_KEYUP) : KeyboardFlags.KEYEVENTF_KEYUP));
        }

        var inputs = inputList.ToArray();

        var count = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        if (count != inputs.Length)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    private static void AddKeyboardInput(List<INPUT> list, UInt16 keyCode, UInt32 flags)
    {
        list.Add(new INPUT
        {
            type = (UInt32)InputType.INPUT_KEYBOARD,
            union = new INPUTUNION
            {
                Keyboard = new KEYBDINPUT
                {
                    wVk = keyCode,
                    wScan = 0,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        });
    }
}