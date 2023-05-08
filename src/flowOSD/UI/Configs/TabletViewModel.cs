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
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;
using flowOSD.Extensions;

public class TabletViewModel : ConfigViewModelBase, IDisposable
{
    private CompositeDisposable? disposable;

    private IPerformanceService performanceService;

    private IReadOnlyCollection<PerformanceProfile>? profiles;

    public TabletViewModel(IConfig config, IHardwareService hardwareService)
        : base(config, Text.Instance.Config.Tablet, Images.Instance.Hardware.Tablet)
    {
        if (hardwareService == null)
        {
            throw new ArgumentNullException(nameof(hardwareService));
        }

        performanceService = hardwareService.ResolveNotNull<IPerformanceService>();
    }

    public Text TextResources => Text.Instance;

    public bool DisableTouchPadInTabletMode
    {
        get => Config.Common.DisableTouchPadInTabletMode;
        set => Config.Common.DisableTouchPadInTabletMode = value;
    }

    public PerformanceProfile TabletProfile
    {
        get => performanceService.GetProfile(Config.Performance.TabletProfile);
        set
        {
            if (value == null)
            {
                return;
            }

            Config.Performance.TabletProfile = value.Id;
        }
    }

    public IReadOnlyCollection<PerformanceProfile>? Profiles
    {
        get => profiles;
        private set => SetProperty(ref profiles, value);
    }

    public void Dispose()
    {
        OnDeactivated();
    }

    protected override void OnActivated()
    {
        disposable = new CompositeDisposable();

        Config.Performance.ProfileChanged
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(UpdateProfiles)
            .DisposeWith(disposable);

        Config.Performance.PropertyChanged
            .Where(x => x == nameof(PerformanceConfig.TabletProfile))
            .SubscribeOn(SynchronizationContext.Current!)
            .Subscribe(OnPropertyChanged)
            .DisposeWith(disposable);

        Config.Common.PropertyChanged
            .SubscribeOn(SynchronizationContext.Current!)
            .Subscribe(OnPropertyChanged)
            .DisposeWith(disposable);

        UpdateProfiles(Guid.Empty);
    }

    protected override void OnDeactivated()
    {
        disposable?.Dispose();
        disposable = null;
    }

    private void UpdateProfiles(Guid changedProfileId)
    {
        Profiles = new ReadOnlyCollection<PerformanceProfile>(
            new PerformanceProfile[]
            {
                PerformanceProfile.Default,
                PerformanceProfile.Turbo,
                PerformanceProfile.Silent
            }.Union(Config.Performance.GetProfiles()).ToArray());

        if (changedProfileId == Guid.Empty || changedProfileId == TabletProfile.Id)
        {
            OnPropertyChanged(nameof(TabletProfile));
        }
    }
}
