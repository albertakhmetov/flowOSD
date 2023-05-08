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

namespace flowOSD.UI.Configs;

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using flowOSD.Core.Configs;
using flowOSD.Core.Resources;
using flowOSD.Extensions;

public class GeneralViewModel : ConfigViewModelBase, IDisposable
{
    private CompositeDisposable? disposable = null;

    public GeneralViewModel(IConfig config)
        : base(config, Text.Instance.Config.General, Images.Instance.Common.Home)
    {
    }

    public Text TextResources => Text.Instance;

    public bool RunAtStartup
    {
        get => Config.Common.RunAtStartup;
        set => Config.Common.RunAtStartup = value;
    }

    public bool ControlDisplayRefreshRate
    {
        get => Config.Common.ControlDisplayRefreshRate;
        set => Config.Common.ControlDisplayRefreshRate = value;
    }

    public bool ConfirmGpuModeChange
    {
        get => Config.Common.ConfirmGpuModeChange;
        set => Config.Common.ConfirmGpuModeChange = value;
    }

    public bool CheckForUpdates
    {
        get => Config.Common.CheckForUpdates;
        set => Config.Common.CheckForUpdates = value;
    }

    public void Dispose()
    {
        OnDeactivated();
    }

    protected override void OnActivated()
    {
        disposable = new CompositeDisposable();

        Config.Common.PropertyChanged
            .SubscribeOn(SynchronizationContext.Current!)
            .Subscribe(OnPropertyChanged)
            .DisposeWith(disposable);
    }

    protected override void OnDeactivated()
    {
        disposable?.Dispose();
        disposable = null;
    }
}
