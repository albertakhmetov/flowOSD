﻿namespace flowOSD.Native;

internal class Styles
{
    public const int GWL_EXSTYLE = -20;
    public const int GWL_STYLE = -16;

    public const int WS_BORDER = 0x00800000;
    public const int WS_CAPTION = 0x00C00000;
    public const int WS_CHILD = 0x40000000;
    public const int WS_CHILDWINDOW = 0x40000000;
    public const int WS_CLIPCHILDREN = 0x02000000;
    public const int WS_CLIPSIBLINGS = 0x04000000;
    public const int WS_DISABLED = 0x08000000;
    public const int WS_DLGFRAME = 0x00400000;
    public const int WS_GROUP = 0x00020000;

    public const int WS_HSCROLL = 0x00100000;
    public const int WS_ICONIC = 0x20000000;
    public const int WS_MAXIMIZE = 0x01000000;
    public const int WS_MAXIMIZEBOX = 0x00010000;
    public const int WS_MINIMIZE = 0x20000000;
    public const int WS_MINIMIZEBOX = 0x00020000;
    public const int WS_OVERLAPPED = 0x00000000;
    public const int WS_OVERLAPPEDWINDOW = (WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
   // public const int WS_POPUP = 0x80000000;
   // public const int WS_POPUPWINDOW = (WS_POPUP | WS_BORDER | WS_SYSMENU);
    public const int WS_SIZEBOX = 0x00040000;
    public const int WS_SYSMENU = 0x00080000;
    public const int WS_TABSTOP = 0x00010000;

    public const int WS_THICKFRAME = 0x00040000;
    public const int WS_TILED = 0x00000000;
    public const int WS_TILEDWINDOW = (WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
    public const int WS_VISIBLE = 0x10000000;

    public const int WS_VSCROLL = 0x00200000;

    public const int WS_EX_ACCEPTFILES = 0x00000010;
    public const int WS_EX_APPWINDOW = 0x00040000;
    public const int WS_EX_CLIENTEDGE = 0x00000200;
    public const int WS_EX_COMPOSITED = 0x02000000;
    public const int WS_EX_CONTEXTHELP = 0x00000400;
    public const int WS_EX_CONTROLPARENT = 0x00010000;
    public const int WS_EX_DLGMODALFRAME = 0x00000001;
    public const int WS_EX_LAYERED = 0x00080000;
    public const int WS_EX_LAYOUTRTL = 0x00400000;
    public const int WS_EX_LEFT = 0x00000000;
    public const int WS_EX_LEFTSCROLLBAR = 0x00004000;
    public const int WS_EX_LTRREADING = 0x00000000;
    public const int WS_EX_MDICHILD = 0x00000040;
    public const int WS_EX_NOACTIVATE = 0x08000000;

    public const int WS_EX_NOINHERITLAYOUT = 0x00100000;
    public const int WS_EX_NOPARENTNOTIFY = 0x00000004;
    public const int WS_EX_NOREDIRECTIONBITMAP = 0x00200000;
    public const int WS_EX_OVERLAPPEDWINDOW = (WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE);
    public const int WS_EX_PALETTEWINDOW = (WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST);
    public const int WS_EX_RIGHT = 0x00001000;
    public const int WS_EX_RIGHTSCROLLBAR = 0x00000000;
    public const int WS_EX_RTLREADING = 0x00002000;
    public const int WS_EX_STATICEDGE = 0x00020000;
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WS_EX_TOPMOST = 0x00000008;
    public const int WS_EX_TRANSPARENT = 0x00000020;
    public const int WS_EX_WINDOWEDGE = 0x00000100;
}
