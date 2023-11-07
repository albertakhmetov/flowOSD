using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace flowOSD;

using System;
using System.Runtime.InteropServices;
using flowOSD.Native;
using static flowOSD.Native.Shell32;
using static flowOSD.Native.Kernel32;
using flowOSD.Core.Hardware;
using flowOSD.Services.Hardware;
using System.ServiceProcess;
using System.Management;

static class AppHelpers
{
    public const string NOTEBOOK_MODE_ENABLE = nameof(NOTEBOOK_MODE_ENABLE);
    public const string NOTEBOOK_MODE_DISABLE = nameof(NOTEBOOK_MODE_DISABLE);

    public static void RunElevated(string command)
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

    public static bool ProcessCommandLineArgs()
    {
        var args = Environment.GetCommandLineArgs();

        if (args.Length == 1)
        {
            return false;
        }

        switch (args[1])
        {
            case NOTEBOOK_MODE_DISABLE:
                {
                    SetNotebookMode(DeviceState.Disabled);
                    break;
                }

            case NOTEBOOK_MODE_ENABLE:
                {
                    SetNotebookMode(DeviceState.Enabled);
                    break;
                }
        }

        return true;
    }

    public static void SetNotebookMode(DeviceState state)
    {
        try
        {
            if (state == DeviceState.Enabled)
            {
                StopService(NotebookModeService.SENSOR_MONITORING_SERVICE);
                StopService(NotebookModeService.SENSOR_SERVICE);
            }
            else
            {
                StartService(NotebookModeService.SENSOR_MONITORING_SERVICE, true);
                StartService(NotebookModeService.SENSOR_SERVICE, false);
            }
        }
        catch(Exception ex)
        {
            flowOSD.Native.Comctl32.Error("Error", "Error during changing notebook mode", ex.Message);
        }
    }

    private static bool IsStarted(string serviceName)
    {
        var ManagementObj = new ManagementObject($"Win32_Service.Name='{serviceName}'");
        ManagementObj["StartMode"].ToString();

        return true;
    }

    private static void StopService(string serviceName)
    {
        var controller = new ServiceController(serviceName);

        WaitForPermanentStatus(controller);

        if (controller.Status == ServiceControllerStatus.Paused || controller.Status == ServiceControllerStatus.Running)
        {
            controller.Stop();
        }

        WaitForPermanentStatus(controller);

        using var ManagementObj = new ManagementObject($"Win32_Service.Name='{serviceName}'");
        ManagementObj.InvokeMethod("ChangeStartMode", new object[] { "Disabled" });
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
}
