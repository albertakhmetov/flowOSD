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

    private BehaviorSubject<bool> isDirtySubject;

    private IReadOnlyCollection<PerformanceProfile> profiles;
    private PerformanceProfile currentProfile;

    private uint cpuLimit, apuLimit;

    public PerformanceViewModel(IConfig config, IHardwareService hardwareService)
        : base(config, Text.Instance.Config.Performance, Images.Instance.Performance.Performance)
    {
        if (hardwareService == null)
        {
            throw new ArgumentNullException(nameof(hardwareService));
        }

        atk = hardwareService.ResolveNotNull<IAtk>();
        performanceService = hardwareService.ResolveNotNull<IPerformanceService>();

        isDirtySubject = new BehaviorSubject<bool>(false);

        cpu = new FanCurveDataSource();
        cpu.Changed += FanCurveChanged;

        gpu = new FanCurveDataSource();
        gpu.Changed += FanCurveChanged;

        UpdateProfiles(Guid.Empty);

        MinPowerLimit = atk.MinPowerLimit;
        MaxPowerLimit = atk.MaxPowerLimit;
    }

    private void FanCurveChanged(object? sender, EventArgs e)
    {
        isDirtySubject.OnNext(true);
    }

    public IReadOnlyCollection<PerformanceProfile> Profiles
    {
        get => profiles;
        set => SetProperty(ref profiles, value);
    }

    public PerformanceProfile CurrentProfile
    {
        get => currentProfile;
        set
        {
            currentProfile = value;
            if (currentProfile == null)
            {
                return;
            }

            cpuLimit = currentProfile.CpuLimit;
            apuLimit = currentProfile.ApuLimit;

            OnPropertyChanged(null);

            if (CurrentProfile.IsUserProfile)
            {
                Cpu.Set(CurrentProfile.CpuFanCurve, true);
                Gpu.Set(CurrentProfile.GpuFanCurve, true);
            }
            else
            {
                PerformanceMode mode;
                if (CurrentProfile.Id == PerformanceProfile.Turbo.Id)
                {
                    mode = PerformanceMode.Turbo;
                }
                else if (CurrentProfile.Id == PerformanceProfile.Silent.Id)
                {
                    mode = PerformanceMode.Silent;
                }
                else
                {
                    mode = PerformanceMode.Default;
                }

                Cpu.Set(atk.GetFanCurve(FanType.Cpu, mode), false);
                Gpu.Set(atk.GetFanCurve(FanType.Gpu, mode), false);
            }

            isDirtySubject.OnNext(false);
        }
    }

    public bool IsUserProfile => CurrentProfile.IsUserProfile;

    public uint CpuLimit
    {
        get => cpuLimit;
        set
        {
            SetProperty(ref cpuLimit, value);
            isDirtySubject.OnNext(true);
        }
    }

    public uint ApuLimit
    {
        get => apuLimit;
        set
        {
            SetProperty(ref apuLimit, value);
            isDirtySubject.OnNext(true);
        }
    }

    public uint MinPowerLimit { get; }

    public uint MaxPowerLimit { get; }

    public FanCurveDataSource Cpu => cpu;

    public FanCurveDataSource Gpu => gpu;

    public void Dispose()
    {
        OnDeactivated();
    }

    public void CreateProfile(string profileName)
    {
        var profile = new PerformanceProfile(
            Guid.NewGuid(),
            profileName,
            35,
            80,
            FanDataPoint.CreateDefaultCurve(),
            FanDataPoint.CreateDefaultCurve());

        Config.Performance[profile.Id] = profile;
    }

    public void RenameProfile(string profileName)
    {
        SaveChanges(profileName);
    }

    public void RemoveProfile()
    {
        if (CurrentProfile.IsUserProfile)
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
            CreateProfile(Text.Instance.Config.UserProfileName);
        }
    }

    private void SaveChanges(string? newProfileName = null)
    {
        if (CurrentProfile.IsUserProfile && (!string.IsNullOrEmpty(newProfileName) || isDirtySubject.Value == true))
        {
            var profile = new PerformanceProfile(
            CurrentProfile.Id,
            newProfileName ?? CurrentProfile.Name,
            CpuLimit,
            ApuLimit,
            Cpu.ToArray(),
            Gpu.ToArray());

            Config.Performance[profile.Id] = profile;
            isDirtySubject.OnNext(false);
        }
    }
}
