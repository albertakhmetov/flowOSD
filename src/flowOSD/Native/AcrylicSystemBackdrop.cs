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

using System;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using System.Runtime.InteropServices;
using WinRT;

sealed class AcrylicSystemBackdrop : IDisposable
{
    private Window? window;
    private bool alwaysActive;

    private Helper m_wsdqHelper = new Helper();
    private DesktopAcrylicController? controller;
    private SystemBackdropConfiguration? configuration;

    public AcrylicSystemBackdrop(Window window, bool alwaysActive = false)
    {
        this.window = window ?? throw new ArgumentNullException(nameof(window));
        this.alwaysActive = alwaysActive;
    }

    public bool TrySet()
    {
        if (window != null && DesktopAcrylicController.IsSupported())
        {
            m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

            configuration = new SystemBackdropConfiguration();

            if (!alwaysActive)
            {
                window.Activated += Window_Activated;
            }

            if (window.Content is FrameworkElement root)
            {
                root.ActualThemeChanged += Window_ThemeChanged;
            }

            configuration.IsInputActive = true;

            controller = new DesktopAcrylicController();
            controller.LuminosityOpacity = .9f;

            SetConfigurationSourceTheme();

            // Enable the system backdrop.
            controller.AddSystemBackdropTarget(window.As<ICompositionSupportsSystemBackdrop>());
            controller.SetSystemBackdropConfiguration(configuration);

            return true;
        }

        return false;
    }

    public void Dispose()
    {
        if (controller != null)
        {
            controller.Dispose();
            controller = null;
        }

        configuration = null;

        if (window?.Content is FrameworkElement root)
        {
            root.ActualThemeChanged -= Window_ThemeChanged;
        }

        if (window != null)
        {
            window.Activated -= Window_Activated;
            window = null;
        }
    }

    private void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (configuration != null)
        {
            configuration.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }
    }

    private void Window_ThemeChanged(FrameworkElement sender, object args)
    {
        if (configuration != null)
        {
            SetConfigurationSourceTheme();
        }
    }

    private void SetConfigurationSourceTheme()
    {
        if (controller != null && configuration != null && window?.Content is FrameworkElement root)
        {
            switch (root.ActualTheme)
            {
                case ElementTheme.Dark:
                    controller.TintColor = Color.FromArgb(255, 32, 32, 32);
                    configuration.Theme = SystemBackdropTheme.Dark;
                    break;

                case ElementTheme.Light:
                    controller.TintColor = Color.FromArgb(255, 243, 243, 243);
                    configuration.Theme = SystemBackdropTheme.Light;
                    break;

                case ElementTheme.Default:
                    configuration.Theme = SystemBackdropTheme.Default;
                    break;
            }
        }
    }

    private sealed class Helper
    {
        [StructLayout(LayoutKind.Sequential)]
        struct DispatcherQueueOptions
        {
            internal int dwSize;
            internal int threadType;
            internal int apartmentType;
        }

        [DllImport("CoreMessaging.dll")]
        private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object? dispatcherQueueController);

        object? m_dispatcherQueueController = null;

        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (Windows.System.DispatcherQueue.GetForCurrentThread() != null)
            {
                // one already exists, so we'll just use it.
                return;
            }

            if (m_dispatcherQueueController == null)
            {
                DispatcherQueueOptions options;
                options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
                options.threadType = 2;    // DQTYPE_THREAD_CURRENT
                options.apartmentType = 2; // DQTAT_COM_STA

                CreateDispatcherQueueController(options, ref m_dispatcherQueueController);
            }
        }
    }
}
