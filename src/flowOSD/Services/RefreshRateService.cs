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
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using flowOSD.Api;
using flowOSD.Api.Hardware;
using flowOSD.Extensions;
using static flowOSD.Extensions.Common;

namespace flowOSD.Services;

internal class RefreshRateService : IDisposable
{
    private CompositeDisposable? disposable = new CompositeDisposable();

    private IConfig config;
    private IDisplay display;
    private IPowerManagement powerManagement;

    public RefreshRateService(IConfig config, IDisplay display, IPowerManagement powerManagement)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.display = display ?? throw new ArgumentNullException(nameof(display));
        this.powerManagement = powerManagement ?? throw new ArgumentNullException(nameof(powerManagement));

        powerManagement.PowerSource
            .CombineLatest(display.State, (powerSource, displayState) => new { powerSource, displayState })
            .Throttle(TimeSpan.FromSeconds(5))
            .Subscribe(x => Update(x.powerSource, x.displayState))
            .DisposeWith(disposable);
    }

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    public async void Update()
    {
        Update(await powerManagement.PowerSource.FirstOrDefaultAsync(), await display.State.FirstOrDefaultAsync());
    }

    private async void Update(PowerSource powerSource, DeviceState displayState)
    {
        if (!config.UserConfig.ControlDisplayRefreshRate || displayState == DeviceState.Disabled)
        {
            return;
        }

        try
        {
            var refreshRates = await display.RefreshRates.FirstOrDefaultAsync();

            if (powerSource == PowerSource.Battery)
            {
                display.SetRefreshRate(refreshRates?.Low);
            }
            else
            {
                display.SetRefreshRate(refreshRates?.High);
            }
        }
        catch (Win32Exception ex)
        {
            TraceException(ex, "RefreshRateService");
        }
    }
}
