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
namespace flowOSD;

using System.Diagnostics;
using System.Reactive.Disposables;
using flowOSD.Extensions;
using flowOSD.Services;
using flowOSD.UI;
using static flowOSD.Extensions.Common;

public static class _Program
{
    [STAThread]
    static void Main()
    {
        var instanceMutex = new Mutex(true, "com.albertakhmetov.flowosd", out bool isMutexCreated);
        if (!isMutexCreated)
        {
            instanceMutex = null;
            return;
        }

        var disposable = new CompositeDisposable();
        var config = new ConfigService().DisposeWith(disposable);

        var listener = default(TextWriterTraceListener);

        try
        {
#if !DEBUG
            var logFileName = Path.Combine(config.DataDirectory.FullName, "log.txt");
            listener = new TextWriterTraceListener(logFileName);
            Trace.Listeners.Add(listener);
#endif
            listener?.DisposeWith(disposable);

            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

            var app = new _App(config).DisposeWith(disposable);

            Application.Run(app.ApplicationContext);
        }
        catch (Exception ex)
        {
            TraceException(ex, "General Failure");
            MessageBox.Show(ex.Message, "General Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            instanceMutex?.ReleaseMutex();

            listener?.Flush();

            disposable.Dispose();
        }
    }
}