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

    private CompositeDisposable? disposable = new CompositeDisposable();

    private IAtk atk;

    private IReadOnlyCollection<PerformanceProfile> profiles;
    private PerformanceProfile currentProfile;

    private uint cpuLimit;
    private string name;
    private bool isDirty;

    public PerformanceViewModel(IConfig config, IAtk atk)
        : base(config, Text.Instance.Config.Profiles, Images.Notification)
    {
        this.atk = atk ?? throw new ArgumentNullException(nameof(atk));

        cpu = new FanCurveDataSource();
        cpu.Changed += FanCurveChanged;

        gpu = new FanCurveDataSource();
        gpu.Changed += FanCurveChanged;

        UpdateProfiles(Guid.Empty);
        CurrentProfile = PerformanceProfile.Default;

        CpuLimitMin = 5;
        CpuLimitMax = 80;

        Config.Performance.ProfileChanged
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(UpdateProfiles)
            .DisposeWith(disposable);
    }

    private void UpdateProfiles(Guid changedProfileId)
    {
        var p = new List<PerformanceProfile>
        {
            PerformanceProfile.Default,
            PerformanceProfile.Turbo,
            PerformanceProfile.Silent
        };

        p.AddRange(Config.Performance.GetProfiles());

        Profiles = new ReadOnlyCollection<PerformanceProfile>(p);
        CurrentProfile = Config.Performance[changedProfileId] ?? PerformanceProfile.Default;
    }

    private void FanCurveChanged(object? sender, EventArgs e)
    {
        IsDirty = true;
    }

    public bool IsDirty
    {
        get => isDirty;
        set => SetProperty(ref isDirty, value);
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

            name = currentProfile.Name;
            cpuLimit = currentProfile.CpuLimit;

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

            IsDirty = false;
        }
    }

    public bool IsUserProfile => CurrentProfile.IsUserProfile;

    public string Name
    {
        get => name;
        set
        {
            SetProperty(ref name, value);
            IsDirty = true;
        }
    }

    public uint CpuLimit
    {
        get => cpuLimit;
        set
        {
            SetProperty(ref cpuLimit, value);
            IsDirty = true;
        }
    }


    public uint CpuLimitMin { get; }

    public uint CpuLimitMax { get; }

    public FanCurveDataSource Cpu => cpu;

    public FanCurveDataSource Gpu => gpu;

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    public void CreateProfile(string profileName)
    {
        var profile = new PerformanceProfile(
            Guid.NewGuid(),
            profileName,
            35,
            Cpu.ToArray(),
            Gpu.ToArray());

        Config.Performance[profile.Id] = profile;
    }

    public void RenameProfile(string profileName)
    {
        if (CurrentProfile.IsUserProfile)
        {
            var profile = CurrentProfile.Rename(profileName);

            Config.Performance[profile.Id] = profile;
        }
    }

    public void RemoveProfile()
    {
        if (CurrentProfile.IsUserProfile)
        {
            Config.Performance[CurrentProfile.Id] = null;
        }
    }

    public void SaveChanges(out bool isCorrected)
    {
        var profile = new PerformanceProfile(
            CurrentProfile.Id,
            CurrentProfile.Name,
            CpuLimit,
            FanDataPoint.MakeSafe(Cpu, out var isCpuCorrected),
            FanDataPoint.MakeSafe(Gpu, out var isGpuCorrected));

        isCorrected = isCpuCorrected || isGpuCorrected;

        Config.Performance[profile.Id] = profile;
        IsDirty = false;
    }
}
