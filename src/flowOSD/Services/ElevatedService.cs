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

using System;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using flowOSD.Core;
using flowOSD.Core.Hardware;
using flowOSD.Extensions;
using flowOSD.Services.Hardware;
using static flowOSD.Native.Shell32;
using static flowOSD.Native.Kernel32;
using System.Runtime.CompilerServices;
using Microsoft.Win32;

sealed class ElevatedService : IElevatedService
{
    public ElevatedService()
    {
        IsElevated = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
    }

    public bool IsElevated { get; }

    public bool IsElevatedRequest()
    {
        var args = Environment.GetCommandLineArgs();

        if (args.Length == 1)
        {
            return false;
        }

        switch (args[1])
        {
            case nameof(DisableNotebookMode):
                {
                    SetNotebookMode(DeviceState.Disabled, true);
                    break;
                }

            case nameof(EnableNotebookMode):
                {
                    SetNotebookMode(DeviceState.Enabled, true);
                    break;
                }

            case nameof(DisableSlateMode):
                {
                    DisableSlateMode();
                    break;
                }
        }

        return true;
    }

    public void DisableNotebookMode()
    {
        if (IsElevated)
        {
            SetNotebookMode(DeviceState.Disabled, false);
        }
        else
        {
            RunElevated();
        }
    }

    public void EnableNotebookMode()
    {
        if (IsElevated)
        {
            SetNotebookMode(DeviceState.Enabled, false);
        }
        else
        {
            RunElevated();
        }
    }

    public void DisableSlateMode()
    {
        if (IsElevated)
        {
            using var key = Registry.LocalMachine.OpenSubKey(NotebookModeService.SLATE_KEY, true);
            key?.SetValue(NotebookModeService.SLATE_PROPERTY, 1);
        }
        else
        {
            RunElevated();
        }
    }

    private static void SetNotebookMode(DeviceState state, bool isElevatedRequest)
    {
        try
        {
            if (state == DeviceState.Enabled)
            {
                StopService(NotebookModeService.SENSOR_MONITORING_SERVICE, !isElevatedRequest);
                StopService(NotebookModeService.SENSOR_SERVICE, !isElevatedRequest);
            }
            else
            {
                StartService(NotebookModeService.SENSOR_MONITORING_SERVICE, true);
                StartService(NotebookModeService.SENSOR_SERVICE, false);
            }
        }
        catch (Exception ex)
        {
            flowOSD.Native.Comctl32.Error("Error", "Error during changing notebook mode", ex.Message);
        }
    }

    private static void StopService(string serviceName, bool disableService)
    {
        var controller = new ServiceController(serviceName);

        WaitForPermanentStatus(controller);

        if (controller.Status == ServiceControllerStatus.Paused || controller.Status == ServiceControllerStatus.Running)
        {
            controller.Stop();
        }

        WaitForPermanentStatus(controller);

        if (disableService)
        {
            using var ManagementObj = new ManagementObject($"Win32_Service.Name='{serviceName}'");
            ManagementObj.InvokeMethod("ChangeStartMode", new object[] { "Disabled" });
        }
    }

    private static void StartService(string serviceName, bool isAutoStart)
    {
        using var ManagementObj = new ManagementObject($"Win32_Service.Name='{serviceName}'");
        ManagementObj.InvokeMethod("ChangeStartMode", new object[] { (isAutoStart ? "Automatic" : "Manual") });

        var controller = new ServiceController(serviceName);

        WaitForPermanentStatus(controller);

        if (controller.Status == ServiceControllerStatus.Paused || controller.Status == ServiceControllerStatus.Stopped)
        {
            controller.Start();
        }

        WaitForPermanentStatus(controller);
    }

    private static void WaitForPermanentStatus(ServiceController controller)
    {
        var timeout = TimeSpan.FromSeconds(5);

        while (true)
        {
            switch (controller.Status)
            {
                case ServiceControllerStatus.PausePending:
                    controller.WaitForStatus(ServiceControllerStatus.Paused, timeout);
                    break;

                case ServiceControllerStatus.ContinuePending:
                case ServiceControllerStatus.StartPending:
                    controller.WaitForStatus(ServiceControllerStatus.Running, timeout);
                    break;

                case ServiceControllerStatus.StopPending:
                    controller.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                    break;

                default:
                    return;
            }
        }
    }

    private static void RunElevated([CallerMemberName] string command = null)
    {
        var exeName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

        SHELLEXECUTEINFO shExInfo = new()
        {
            cbSize = Marshal.SizeOf<SHELLEXECUTEINFO>(),
            fMask = SEE_MASK_NOCLOSEPROCESS,
            hWnd = 0,
            lpVerb = "runas",         // Operation to perform
            lpFile = exeName,         // Application to start    
            lpParameters = command,   // Additional parameters
            lpDirectory = null,
            nShow = SW_SHOW,
            hInstApp = 0
        };

        if (ShellExecuteEx(ref shExInfo))
        {
            WaitForSingleObject(shExInfo.hProcess, INFINITE);
            CloseHandle(shExInfo.hProcess);
        }
    }
}
