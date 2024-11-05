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
using System.Collections.Concurrent;
using System.Management;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.ServiceProcess;
using flowOSD.Core;

sealed class ServiceWatcher : IDisposable, IServiceWatcher
{
    private ManagementEventWatcher? watcher;
    private ConcurrentDictionary<string, HashSet<Action<string, bool>>> subscriptions;
    private Dictionary<string, ServiceController> controllers;

    public ServiceWatcher()
    {
        subscriptions = new ConcurrentDictionary<string, HashSet<Action<string, bool>>>();
        controllers = new Dictionary<string, ServiceController>();

        var query = $"SELECT * FROM __InstanceModificationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Service'";

        watcher = new ManagementEventWatcher(query);
        watcher.EventArrived += OnWmiEvent;
        watcher.Start();
    }

    public void Dispose()
    {
        watcher?.Dispose();
        watcher = null;
    }

    public bool IsStarted(string serviceName)
    {
        if (!controllers.ContainsKey(serviceName))
        {
            controllers[serviceName] = new ServiceController(serviceName);
        }

        return controllers[serviceName].Status != ServiceControllerStatus.Stopped;
    }

    public IDisposable Subscribe(string serviceName, Action<string, bool> callback, bool onlyChanges = true)
    {
        if (subscriptions.TryGetValue(serviceName, out var callbacks))
        {
            callbacks.Add(callback);
        }
        else
        {

            var hashSet = new HashSet<Action<string, bool>>();
            hashSet.Add(callback);
            subscriptions[serviceName] = hashSet;
        }

        if (!onlyChanges)
        {
            callback(serviceName, IsStarted(serviceName));
        }

        return Disposable.Create(() =>
        {
            if (subscriptions.TryGetValue(serviceName, out var callbacks))
            {
                callbacks.Remove(callback);
            }
        });
    }

    private void OnWmiEvent(object sender, EventArrivedEventArgs e)
    {
        if (e.NewEvent["TargetInstance"] is ManagementBaseObject obj && obj["Name"] is string name && obj["Started"] is bool isStarted)
        {
            if (!string.IsNullOrEmpty(name) && subscriptions.TryGetValue(name, out var callbacks))
            {
                foreach (var i in callbacks.ToArray())
                {
                    i(name, isStarted);
                }
            }
        }
    }
}
