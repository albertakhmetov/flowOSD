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
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;
using flowOSD.Extensions;
using flowOSD.UI.Commands;
using flowOSD.UI.Controls;

public class PerformanceViewModel : ConfigViewModelBase, IDisposable
{
    private readonly FanCurveDataSource cpu, gpu;

    private CompositeDisposable? disposable = null;

    private IAtk atk;
    private IPerformanceService performanceService;
    private IHardwareFeatures hardwareFeatures;

    private BehaviorSubject<bool> isDirtySubject;

    private IReadOnlyCollection<PerformanceProfile> profiles;
    private PerformanceProfile? currentProfile;

    private bool fanCurveError;
    private PerformanceMode performanceMode;
    private uint cpuLimit;
    private bool useCustomFanCurves;

    public PerformanceViewModel(
        ITextResources textResources,
        IImageResources imageResources,
        IConfig config,
        IHardwareService hardwareService)
        : base(
            textResources,
            imageResources,
            config,
            "Config.Performance.Title",
            "PerformanceMode.Performance")
    {
        if (hardwareService == null)
        {
            throw new ArgumentNullException(nameof(hardwareService));
        }

        atk = hardwareService.ResolveNotNull<IAtk>();
        performanceService = hardwareService.ResolveNotNull<IPerformanceService>();

        hardwareFeatures = hardwareService.ResolveNotNull<IHardwareFeatures>();

        isDirtySubject = new BehaviorSubject<bool>(false);

        cpu = new FanCurveDataSource();
        cpu.Changed += FanCurveChanged;

        gpu = new FanCurveDataSource();
        gpu.Changed += FanCurveChanged;

        PerformanceModes = new ReadOnlyCollection<PerformanceMode>(new PerformanceMode[]
        {
            PerformanceMode.Performance,
            PerformanceMode.Turbo,
            PerformanceMode.Silent,
        });

        UpdateProfiles(Guid.Empty);

        MinPowerLimit = atk.MinPowerLimit;
        MaxPowerLimit = atk.MaxPowerLimit;

        CreateProfileCommand = new RelayCommand(x => CreateProfile(x as string));
        IsFanCurvesWarningVisible = !config.Common.ForceCustomFanCurves;
    }

    public string CustomFanCurvesPageUrl => TextResources["Links.CustomFanCurves"];

    public bool IsFanCurvesWarningVisible { get; }

    public bool IsCpuPowerLimitVisible
    {
        get => hardwareFeatures.CpuPowerLimit && IsUserProfile;
    }

    public IReadOnlyCollection<PerformanceMode> PerformanceModes { get; }

    public IReadOnlyCollection<PerformanceProfile> Profiles
    {
        get => profiles;
        set => SetProperty(ref profiles, value);
    }

    public PerformanceProfile? CurrentProfile
    {
        get => currentProfile;
        set
        {
            currentProfile = value;
            if (currentProfile == null)
            {
                OnPropertyChanged(null);
                return;
            }

            performanceMode = currentProfile.PerformanceMode;
            cpuLimit = currentProfile.CpuLimit;

            OnPropertyChanged(null);

            if (CurrentProfile?.IsUserProfile == true)
            {
                Cpu.Set(CurrentProfile.CpuFanCurve, true);
                Gpu.Set(CurrentProfile.GpuFanCurve, true);
            }

            isDirtySubject.OnNext(false);
        }
    }

    public bool IsUserProfile => CurrentProfile?.IsUserProfile ?? false;

    public bool FanCurveError
    {
        get => fanCurveError;
        private set => SetProperty(ref fanCurveError, value);
    }

    public PerformanceMode PerformanceMode
    {
        get => performanceMode;
        set
        {
            SetProperty(ref performanceMode, value);
            isDirtySubject.OnNext(true);
        }
    }

    public uint CpuLimit
    {
        get => cpuLimit;
        set
        {
            SetProperty(ref cpuLimit, value);
            isDirtySubject.OnNext(true);
        }
    }

    public bool UseCustomFanCurves
    {
        get => useCustomFanCurves;
        set
        {
            SetProperty(ref useCustomFanCurves, value);
            isDirtySubject.OnNext(true);
        }
    }

    public uint MinPowerLimit { get; }

    public uint MaxPowerLimit { get; }

    public FanCurveDataSource Cpu => cpu;

    public FanCurveDataSource Gpu => gpu;

    public ICommand CreateProfileCommand { get; }

    public void Dispose()
    {
        OnDeactivated();
    }

    public bool CreateProfile(string? profileName)
    {
        if (string.IsNullOrEmpty(profileName))
        {
            return false;
        }

        var profile = new PerformanceProfile(
            Guid.NewGuid(),
            profileName,
            PerformanceMode.Performance,
            35,
            false,
            FanDataPoint.CreateDefaultCurve(),
            FanDataPoint.CreateDefaultCurve());

        Config.Performance[profile.Id] = profile;

        return true;
    }

    public void RenameProfile(string profileName)
    {
        SaveChanges(profileName);
    }

    public void RemoveProfile()
    {
        if (CurrentProfile?.IsUserProfile == true)
        {
            Config.Performance[CurrentProfile.Id] = null;
        }
    }

    protected override void OnActivated()
    {
        disposable = new CompositeDisposable();

        isDirtySubject
            .Throttle(TimeSpan.FromSeconds(2))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(_ => SaveChanges())
            .DisposeWith(disposable);

        Config.Performance.ProfileChanged
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(UpdateProfiles)
            .DisposeWith(disposable);

        UpdateProfiles(Guid.Empty);
    }

    protected override void OnDeactivated()
    {
        SaveChanges();

        disposable?.Dispose();
        disposable = null;
    }

    private void FanCurveChanged(object? sender, EventArgs e)
    {
        FanCurveError = !cpu.Any(i => i.Value > 0) || !gpu.Any(i => i.Value > 0);

        isDirtySubject.OnNext(true);
    }

    private void UpdateProfiles(Guid changedProfileId)
    {
        Profiles = new ReadOnlyCollection<PerformanceProfile>(Config.Performance.GetProfiles());

        var profile = performanceService.GetProfile(changedProfileId);

        if (profile.IsUserProfile)
        {
            CurrentProfile = profile;
        }
        else if (Profiles.Count > 0)
        {
            CurrentProfile = Profiles.First();
        }
        else
        {
            CurrentProfile = null;
        }
    }

    private void SaveChanges(string? newProfileName = null)
    {
        if (CurrentProfile?.IsUserProfile == true && (!string.IsNullOrEmpty(newProfileName) || isDirtySubject.Value == true))
        {
            var profile = new PerformanceProfile(
            CurrentProfile.Id,
            newProfileName ?? CurrentProfile.Name,
            PerformanceMode,
            CpuLimit,
            UseCustomFanCurves,
            Cpu.ToArray(),
            Gpu.ToArray());

            Config.Performance[profile.Id] = profile;
            isDirtySubject.OnNext(false);
        }
    }
}
