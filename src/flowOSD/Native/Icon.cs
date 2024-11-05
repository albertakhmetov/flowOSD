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

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using static flowOSD.Native.User32;

public sealed partial class Icon : IDisposable
{
    private IntPtr handler;
    private int iconWidth, iconHeight;

    public Icon(Stream stream, int dpi)
    {
        iconWidth = (int)Math.Round(16 * (dpi / 96f));
        iconHeight = (int)Math.Round(16 * (dpi / 96f));

        using var ms = new MemoryStream();
        stream.CopyTo(ms);

        var buffer = GetIconBuffer(ms);
        handler = CreateIconFromResourceEx(
            buffer,
            buffer.Length,
            true,
            0x30000,
            iconWidth,
            iconHeight,
            0);
    }

    ~Icon()
    {
        Dispose(disposing: false);
    }

    public IntPtr Handler => handler;

    public static Icon? LoadFromResource(string resourceName, int dpi)
    {
        using var stream = typeof(Icon).Assembly.GetManifestResourceStream(resourceName);

        return stream == null ? null : new Icon(stream, dpi);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (handler != IntPtr.Zero)
        {
            DestroyIcon(handler);
            handler = IntPtr.Zero;
        }
    }

    private byte[] GetIconBuffer(MemoryStream stream)
    {
        var dir = LoadIconDir(stream.GetBuffer());

        var i = 0;
        do
        {
            if (iconWidth > dir.idEntries[i].bWidth || iconHeight > dir.idEntries[i].bHeight)
            {
                i++;
            }
            else
            {
                break;
            }
        }
        while (i < dir.idCount - 1);

        var buffer = new byte[dir.idEntries[i].dwBytesInRes];
        stream.Position = dir.idEntries[i].dwImageOffset;
        stream.Read(buffer, 0, buffer.Length);

        return buffer;
    }

    private ICONDIR LoadIconDir(byte[] buffer)
    {
        ICONDIR dir = default;
        dir.idReserved = BitConverter.ToUInt16(buffer, 0);
        dir.idType = BitConverter.ToUInt16(buffer, 2);
        dir.idCount = BitConverter.ToUInt16(buffer, 4);
        dir.idEntries = new ICONDIRENTRY[dir.idCount];

        for (var i = 0; i < dir.idCount; i++)
        {
            var offset = 6 + i * Marshal.SizeOf<ICONDIRENTRY>();
            dir.idEntries[i] = new ICONDIRENTRY
            {
                bWidth = buffer[offset],
                bHeight = buffer[offset + 1],
                bColorCount = buffer[offset + 2],
                bReserved = buffer[offset + 3],
                wPlanes = BitConverter.ToUInt16(buffer, offset + 4),
                wBitCount = BitConverter.ToUInt16(buffer, offset + 6),
                dwBytesInRes = BitConverter.ToUInt16(buffer, offset + 8),
                dwImageOffset = BitConverter.ToUInt16(buffer, offset + 12),
            };
        }

        return dir;
    }
}
