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

namespace flowOSD.Services;

using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Resources;
using static flowOSD.Extensions.Common;

sealed class UpdateService : IUpdateService
{
    private ITextResources textResources;
    private IConfig config;

    private DateTime? lastCheckTime;

    private BehaviorSubject<Version> latestVersionSubject;
    private BehaviorSubject<UpdateServiceState> stateSubject;

    public UpdateService(
        ITextResources textResources,
        IConfig config)
    {
        this.textResources = textResources ?? throw new ArgumentNullException(nameof(textResources));
        this.config = config ?? throw new ArgumentNullException(nameof(config));

        latestVersionSubject = new BehaviorSubject<Version>(config.FileVersion);
        stateSubject = new BehaviorSubject<UpdateServiceState>(UpdateServiceState.None);

        State = stateSubject.AsObservable();
        LatestVersion = latestVersionSubject.AsObservable();
    }

    public IObservable<UpdateServiceState> State { get; }

    public IObservable<Version> LatestVersion { get; }

    public async Task<bool> CheckUpdate(bool force = false)
    {
        if (lastCheckTime != null && (DateTime.Now - lastCheckTime).Value.TotalMinutes < 5 && !force)
        {
            return false;
        }

        var state = stateSubject.Value;

        try
        {
            stateSubject.OnNext(UpdateServiceState.Checking);

            using var client = new HttpClient();

            var i = await client.GetAsync(textResources["Links.GitLatest"]);
            if (i.IsSuccessStatusCode)
            {
                var v = i.RequestMessage?.RequestUri?.Segments.LastOrDefault();
                if (v != null && Regex.IsMatch(v, "v[0-9]+.[0-9]+.[0-9]+") && Version.TryParse(v.Substring(1), out var latestVersion))
                {
                    latestVersionSubject.OnNext(latestVersion);

                    if (config.FileVersion < latestVersion)
                    {
                        stateSubject.OnNext(UpdateServiceState.ReadyToDownload);
                    }
                    else
                    {
                        stateSubject.OnNext(UpdateServiceState.Updated);
                    }

                    lastCheckTime = DateTime.Now;

                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            stateSubject.OnNext(state);

            TraceException(ex, "Error due checking update");
        }

        return false;
    }

    public async Task<bool> Download(ISubject<int> progress, CancellationToken cancellationToken = default)
    {
        var version = latestVersionSubject.Value;
        if (version == config.FileVersion || stateSubject.Value != UpdateServiceState.ReadyToDownload)
        {
            return false;
        }

        var installerFileName = $"{config.ProductName}-{version.Major}.{version.Minor}.{version.Build}.exe";

        try
        {
            stateSubject.OnNext(UpdateServiceState.Downloading);

            using var client = new HttpClient();

            var response = await client.GetAsync(
                textResources["Links.GitLatest"] + $"/download/{installerFileName}",
                HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength == null)
            {
                return false;
            }

            using var download = await response.Content.ReadAsStreamAsync();

            var destFile = new FileInfo(Path.Combine(Path.GetTempPath(), installerFileName));
            if (destFile.Exists)
            {
                destFile.Delete();
            }

            using var file = destFile.Create();

            const uint bufferSize = 81920;

            var relativeProgress = new Progress<long>(
                totalBytes => progress.OnNext((int)Math.Round(100f * totalBytes / response.Content.Headers.ContentLength.Value)));

            await download.CopyToAsync(file, bufferSize, relativeProgress, cancellationToken);
            progress.OnNext(100);
            progress.OnCompleted();

            stateSubject.OnNext(UpdateServiceState.ReadyToInstall);

            return true;
        }
        catch (Exception ex)
        {
            TraceException(ex, "Error due downloading update");
            stateSubject.OnNext(UpdateServiceState.ReadyToDownload);

            return false;
        }
    }

    public bool Install()
    {
        var version = latestVersionSubject.Value;

        if (version == config.FileVersion || stateSubject.Value != UpdateServiceState.ReadyToInstall)
        {
            return false;
        }

        var installerFileName = $"{config.ProductName}-{version.Major}.{version.Minor}.{version.Build}.exe";

        var info = new ProcessStartInfo();
        info.FileName = Path.Combine(Path.GetTempPath(), installerFileName);
        if (!File.Exists(info.FileName))
        {
            return false;
        }

        var i = Process.Start(info);

        return true;
    }
}
