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
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using flowOSD.Core.Hardware;
using static flowOSD.Extensions.Common;
using static flowOSD.Native.Kernel32;

namespace flowOSD.Services.Hardware.Hid;

sealed class Keyboard : IDisposable, IKeyboard
{
    public const int FEATURE_KBD_REPORT_ID = 0x5a;

    private HidDevice device;

    private CancellationTokenSource? cancellationTokenSource = new CancellationTokenSource();

    private readonly Task keyboardReader;

    private Subject<uint> activitySubject;
    private Subject<AtkKey> keyPressedSubject;

    public Keyboard(HidDevice device)
    {
        this.device = device ?? throw new ArgumentNullException(nameof(device));

        activitySubject = new Subject<uint>();
        keyPressedSubject = new Subject<AtkKey>();

        Activity = activitySubject.AsObservable();
        KeyPressed = keyPressedSubject.AsObservable();

        keyboardReader = Task.Factory.StartNew(
            async () => await ProcessKeyboardInput(cancellationTokenSource.Token),
            TaskCreationOptions.LongRunning);
    }

    public IObservable<uint> Activity { get; }

    public IObservable<AtkKey> KeyPressed { get; }

    public void Dispose()
    {
        if (cancellationTokenSource != null)
        {
            cancellationTokenSource.Cancel();
        }
    }

    private async Task ProcessKeyboardInput(CancellationToken token)
    {
        const int MAX_ATTEPTS = 10;

        try
        {
            var atteptNo = 0;

            while (!token.IsCancellationRequested)
            {
                byte[] data;

                try
                {
                    data = await this.device.ReadDataAsync(token);
                }
                catch (IOException)
                {
                    if (atteptNo < MAX_ATTEPTS)
                    {
                        await Task.Delay(500);

                        atteptNo++;
                        continue;
                    }
                    else
                    {
                        throw;
                    }
                }

                atteptNo = 0;

                if (data.Length > 1)
                {
                    activitySubject.OnNext(GetTickCount());
                }

                if (data.Length > 1 && data[0] == FEATURE_KBD_REPORT_ID && Enum.IsDefined(typeof(AtkKey), data[1]))
                {
                    keyPressedSubject.OnNext((AtkKey)data[1]);
                }
            }
        }
        catch (Exception ex)
        {
            TraceException(ex, "Keyboard listener exception");
        }
    }
}
