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

namespace flowOSD.Core.Configs;

using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json.Serialization;
using flowOSD.Core.Hardware;

public sealed class PerformanceConfig : ConfigBase
{
    private Guid chargerProfile, batteryProfile, tabletProfile;

    private Dictionary<Guid, PerformanceProfile> profiles;
    private Subject<Guid> profileChangedSubject;

    public PerformanceConfig(IList<PerformanceProfile>? profiles)
    {
        this.profiles = new Dictionary<Guid, PerformanceProfile>();

        if (profiles != null)
        {
            foreach (var i in profiles)
            {
                this.profiles[i.Id] = i;
            }
        }

        profileChangedSubject = new Subject<Guid>();
        ProfileChanged = profileChangedSubject.AsObservable();

        chargerProfile = PerformanceProfile.DefaultId;
        batteryProfile = PerformanceProfile.DefaultId;
        tabletProfile = PerformanceProfile.SilentId;
    }

    public IObservable<Guid> ProfileChanged { get; }

    public Guid ChargerProfile
    {
        get => chargerProfile;
        set => SetProperty(ref chargerProfile, value);
    }

    public Guid BatteryProfile
    {
        get => batteryProfile;
        set => SetProperty(ref batteryProfile, value);
    }

    public Guid TabletProfile
    {
        get => tabletProfile;
        set => SetProperty(ref tabletProfile, value);
    }

    public PerformanceProfile? this[Guid id]
    {
        get => profiles.ContainsKey(id) ? profiles[id] : null;
        set
        {
            if (value == null)
            {
                profiles.Remove(id);
            }
            else
            {
                profiles[id] = value;
            }

            profileChangedSubject.OnNext(id);
            OnPropertyChanged();
        }
    }

    public IList<PerformanceProfile> GetProfiles()
    {
        return profiles.Values.ToArray();
    }
}
