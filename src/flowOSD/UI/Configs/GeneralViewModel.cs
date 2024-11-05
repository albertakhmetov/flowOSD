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

namespace flowOSD.UI.Configs;

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;
using flowOSD.Extensions;

public class GeneralViewModel : ConfigViewModelBase, IDisposable
{
    private CompositeDisposable? disposable = null;

    private IHardwareFeatures hardwareFeatures;
    private IAtk atk;

    private bool bootSound;

    public GeneralViewModel(
        ITextResources textResources,
        IImageResources imageResources,
        IConfig config,
        IHardwareService hardwareService)
        : base(
            textResources,
            imageResources,
            config,
            "Config.General.Title",
            "Common.Home")
    {
        if (hardwareService == null)
        {
            throw new ArgumentNullException(nameof(hardwareService));
        }

        hardwareFeatures = hardwareService.ResolveNotNull<IHardwareFeatures>();
        atk = hardwareService.ResolveNotNull<IAtk>();
    }

    public bool IsOptimizationInfoVisible => hardwareFeatures.OptimizationService;

    public bool VariBrightControlEnabled => hardwareFeatures.AmdIntegratedGpu;

    public bool BootSoundControlEnabled => hardwareFeatures.BootSound;

    public string OptimizationPageUrl => TextResources["Links.Optimization"];

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

    public bool DisableVariBright
    {
        get => Config.Common.DisableVariBright;
        set => Config.Common.DisableVariBright = value;
    }

    public bool BootSound
    {
        get => bootSound;
        set => atk.SetBootSound(value ? DeviceState.Enabled : DeviceState.Disabled);
    }

    public void Dispose()
    {
        OnDeactivated();
    }

    protected override void OnActivated()
    {
        disposable = new CompositeDisposable();

        Config.Common.PropertyChanged
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(OnPropertyChanged)
            .DisposeWith(disposable);

        atk.BootSound
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(x =>
            {
                bootSound = x == DeviceState.Enabled;
                OnPropertyChanged(nameof(BootSound));
            })
            .DisposeWith(disposable);

        OnPropertyChanged(null);
    }

    protected override void OnDeactivated()
    {
        disposable?.Dispose();
        disposable = null;
    }
}
