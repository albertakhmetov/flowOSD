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

using System.Runtime.InteropServices;
using flowOSD.Core;
using static flowOSD.Native.User32;

sealed class MessageQueue : IMessageQueue, IDisposable
{
    private Dictionary<int, ICollection<Action<int, IntPtr, IntPtr>>> subscriptions;
    private NativeWindow? nativeWindow;

    public MessageQueue()
    {
        subscriptions = new Dictionary<int, ICollection<Action<int, IntPtr, IntPtr>>>();
        nativeWindow = new NativeWindow(this);
    }

    public IntPtr Handle => nativeWindow?.Handle ?? throw new ObjectDisposedException(nameof(MessageQueue));

    public void Dispose()
    {
        nativeWindow?.Dispose();
        nativeWindow = null;
    }

    public IDisposable Subscribe(int messageId, Action<int, IntPtr, IntPtr> proc)
    {
        if (!subscriptions.ContainsKey(messageId))
        {
            subscriptions[messageId] = new List<Action<int, IntPtr, IntPtr>>();
        }

        subscriptions[messageId].Add(proc);

        return new Subscription(this, messageId, proc);
    }

    private void Remove(int messageId, Action<int, IntPtr, IntPtr> proc)
    {
        if (subscriptions.ContainsKey(messageId))
        {
            subscriptions[messageId].Remove(proc);
        }
    }

    private void Push(int msg, IntPtr wParam, IntPtr lParam)
    {
        if (subscriptions.ContainsKey(msg))
        {
            foreach (var proc in subscriptions[msg])
            {
                proc(msg, wParam, lParam);
            }
        }
    }

    private sealed class Subscription : IDisposable
    {
        private MessageQueue owner;
        private int messageId;
        private Action<int, IntPtr, IntPtr> proc;

        public Subscription(MessageQueue owner, int messageId, Action<int, IntPtr, IntPtr> proc)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.messageId = messageId;
            this.proc = proc;
        }

        void IDisposable.Dispose()
        {
            owner.Remove(messageId, proc);
        }
    }

    private sealed class NativeWindow : IDisposable
    {
        private const string ID = "flowOSD_messageQueue";

        private MessageQueue queue;
        private IntPtr handle;

        WNDPROC proc;

        public NativeWindow(MessageQueue queue)
        {
            this.queue = queue ?? throw new ArgumentNullException(nameof(queue));

            proc = OnWindowMessageReceived;

            var classInfo = new WNDCLASSEX()
            {
                cbSize = Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = proc,
                lpszClassName = ID,
            };

            RegisterClassEx(ref classInfo);

            handle = CreateWindowEx(
                dwExStyle: 0,
                lpClassName: ID,
                lpWindowName: ID,
                dwStyle: 0,
                X: 0,
                Y: 0,
                nWidth: 0,
                nHeight: 0,
                hWndParent: IntPtr.Zero,
                hMenu: IntPtr.Zero,
                hInstance: IntPtr.Zero,
                lpParam: IntPtr.Zero);
        }

        ~NativeWindow()
        {
            Dispose(false);
        }

        public IntPtr Handle => handle;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            UnregisterClass(ID, IntPtr.Zero);

            if (handle != IntPtr.Zero)
            {
                DestroyWindow(handle);
            }
        }

        private IntPtr OnWindowMessageReceived(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            queue.Push(msg, wParam, lParam);

            return DefWindowProc(hWnd, msg, wParam, lParam);
        }
    }
}