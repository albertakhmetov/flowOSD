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
namespace flowOSD.Extensions;

using System.Collections;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Reactive.Disposables;
using flowOSD.Api;
using static Native.User32;
using static Native.Messages;

static partial class Forms
{
    public static T Create<T>(Action<T> initializator) where T : new()
    {
        var obj = Activator.CreateInstance<T>();
        initializator(obj);

        return obj;
    }

    public static T Create<T>() where T : new()
    {
        var obj = Activator.CreateInstance<T>();

        return obj;
    }

    public static bool FindChild<T>(this Control control, out T? childControl) where T : Control
    {
        childControl = null;

        if (control == null)
        {
            return false;
        }

        foreach (var child in control.Controls)
        {
            if (child is T)
            {
                childControl = child as T;
                return true;
            }

            if (child is Control c && FindChild(c, out childControl))
            {
                return true;
            }
        }

        return false;
    }

    public static T Add<T>(this Panel control, Action<T> initializator) where T : Control
    {
        var obj = Activator.CreateInstance<T>();
        initializator(obj);

        control.Controls.Add(obj);

        return obj;
    }

    public static T Add<T>(this T control, params Control[] controls) where T : Control
    {
        control.Controls.AddRange(controls);

        return control;
    }

    public static TableLayoutPanel Add<T>(this TableLayoutPanel panel, int column, int row, Action<T> initializator)
        where T : Control, new()
    {
        var obj = Activator.CreateInstance<T>();
        initializator(obj);
        return panel.Add(column, row, obj);
    }

    public static TableLayoutPanel Add<T>(this TableLayoutPanel panel, int column, int row, int columnSpan, int rowSpan, Action<T> initializator)
        where T : Control, new()
    {
        var obj = Activator.CreateInstance<T>();
        initializator(obj);
        return panel.Add(column, row, columnSpan, rowSpan, obj);
    }

    public static TableLayoutPanel Add(this TableLayoutPanel panel, int column, int row, Control control)
    {
        panel.Controls.Add(control, column, row);

        return panel;
    }

    public static TableLayoutPanel Add(this TableLayoutPanel panel, int column, int row, int columnSpan, int rowSpan, Control control)
    {
        panel.Controls.Add(control, column, row);
        panel.SetColumnSpan(control, columnSpan);
        panel.SetRowSpan(control, rowSpan);

        return panel;
    }

    public static IDisposable SubscribeToUpdateDpi(this IMessageQueue messageQueue, Control control)
    {
        return messageQueue
            .Subscribe(WM_DPICHANGED, (x, w, l) => SendMessage(control.Handle, WM_DPICHANGED_BEFOREPARENT, w, l));
    }

    public static int DpiScale(this IntPtr handle, int value)
    {
        return (int)Math.Round(value * (GetDpiForWindow(handle) / 96f));
    }

    public static int DpiScale(this Control control, int value)
    {
        return (int)Math.Round(value * (GetDpiForWindow(control.Handle) / 94f));
    }

    public static Size DpiScale(this Control control, Size size)
    {
        return new Size(control.DpiScale(size.Width), control.DpiScale(size.Height));
    }

    public static Padding DpiScale(this Control control, Padding padding)
    {
        return new Padding(
            control.DpiScale(padding.Left),
            control.DpiScale(padding.Top),
            control.DpiScale(padding.Right),
            control.DpiScale(padding.Bottom));
    }
}