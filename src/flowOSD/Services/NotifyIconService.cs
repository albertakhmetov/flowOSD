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

using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using Windows.Foundation;
using flowOSD.Extensions;
using flowOSD.Native;
using static flowOSD.Native.Shell32;
using static Native.Messages;
using flowOSD.UI.Commands;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;

sealed partial class NotifyIconService : IDisposable
{
    private CompositeDisposable? disposable = new CompositeDisposable();

#if DEBUG
    private static readonly Guid IconGuid = new Guid("EF27BC18-C13D-4056-BE35-000000000000");
#else
    private static readonly Guid IconGuid = new Guid("EF27BC18-C13D-4056-BE35-3603AB766796");
#endif

    private static readonly int MessageId = 5800;

    private IConfig config;
    private IMessageQueue messageQueue;
    private ISystemEvents systemEvents;
    private ICommandService commandService;
    private IAtk atk;

    private string? text;
    private Icon? icon;

    public NotifyIconService(
        IConfig config,
        IMessageQueue messageQueue,
        ISystemEvents systemEvents,
        ICommandService commandService,
        IAtk atk)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.messageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
        this.systemEvents = systemEvents ?? throw new ArgumentNullException(nameof(systemEvents));
        this.commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
        this.atk = atk ?? throw new ArgumentNullException(nameof(atk));

        this.atk.TabletMode
            .CombineLatest(
                this.systemEvents.SystemDarkMode,
                this.systemEvents.Dpi,
                (tabletMode, isDarkMode, dpi) => new { tabletMode, isDarkMode, dpi })
            .Throttle(TimeSpan.FromMilliseconds(100))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(x => UpdateNotifyIcon(x.tabletMode, x.isDarkMode, x.dpi))
            .DisposeWith(disposable);

        messageQueue.Subscribe(WM_TASKBARCREATED, ProcessMessage).DisposeWith(disposable);
        messageQueue.Subscribe(MessageId, ProcessMessage).DisposeWith(disposable);
    }

    public string? Text
    {
        get => text;
        set
        {
            if (text == value)
            {
                return;
            }

            text = value;
            Update();
        }
    }

    public Rect GetIconRectangle()
    {
        var notifyIcon = new NOTIFYICONIDENTIFIER();
        notifyIcon.cbSize = (uint)Marshal.SizeOf<NOTIFYICONIDENTIFIER>();
        notifyIcon.hWnd = messageQueue.Handle;
        notifyIcon.uID = 1;
        notifyIcon.guidItem = IconGuid;

        if (Shell_NotifyIconGetRect(ref notifyIcon, out RECT rect) != 0)
        {
            return Rect.Empty;
        }
        else
        {
            return new Rect(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
        }
    }

    public void Dispose()
    {
        icon?.Dispose();
        icon = null;

        disposable?.Dispose();
        disposable = null;
    }

    public void Show()
    {
        var notifyIconData = GetIconData();
        if (icon == null)
        {
            notifyIconData.uFlags &= ~NIF_ICON;
        }

        Shell_NotifyIcon(NIM_ADD, ref notifyIconData);
    }

    public void Hide()
    {
        var notifyIconData = GetIconData();
        Shell_NotifyIcon(NIM_DELETE, ref notifyIconData);
    }

    private void Update()
    {
        var notifyIconData = GetIconData();
        if (icon == null)
        {
            notifyIconData.uFlags &= ~NIF_ICON;
        }

        Shell_NotifyIcon(NIM_MODIFY, ref notifyIconData);
    }

    private void UpdateNotifyIcon(TabletMode tabletMode, bool isDarkMode, int dpi)
    {
        var iconName = tabletMode != TabletMode.Notebook ? "tablet" : "notebook";
        if (isDarkMode)
        {
            iconName += "-white";
        }

        icon?.Dispose();
        icon = Icon.LoadFromResource($"flowOSD.Resources.{iconName}.ico", dpi);
        Update();
    }

    private NOTIFYICONDATA GetIconData()
    {
        return new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            uID = 1,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP | NIF_GUID,
            dwState = 0x0,
            hIcon = icon?.Handler ?? IntPtr.Zero,
            hWnd = messageQueue.Handle,
            uCallbackMessage = MessageId,
            szTip = text ?? string.Empty,
            uVersion = 5,
            guidItem = IconGuid
        };
    }

    private void ProcessMessage(int messageId, IntPtr wParam, IntPtr lParam)
    {
        if (messageId == WM_TASKBARCREATED)
        {
            Show();
            return;
        }

        if (messageId != MessageId)
        {
            return;
        }

        switch (lParam.Low())
        {
            case WM_LBUTTONUP:
                ShowMainWindow();
                break;

            case WM_RBUTTONUP:
                ShowContextMenu();
                break;
        }
    }

    private void ShowMainWindow()
    {
        commandService.Resolve<MainUICommand>()?.Execute();
    }

    private void ShowContextMenu()
    {
        commandService.Resolve<NotifyMenuCommand>()?.Execute(GetIconRectangle());
    }
}
