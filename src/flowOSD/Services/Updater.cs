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

namespace flowOSD.Services;

using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using flowOSD.Core;
using flowOSD.Core.Configs;
using static flowOSD.Extensions.Common;

sealed class Updater : IUpdater
{
    private const string URL = "https://github.com/albertakhmetov/flowOSD/releases/latest";

    private IConfig config;

    public Updater(IConfig config)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public string ReleaseNotesLink => URL;

    public async Task<Version?> CheckUpdate()
    {
        try
        {
            using var client = new HttpClient();

            var i = await client.GetAsync(URL);
            if (i.IsSuccessStatusCode)
            {
                var v = i.RequestMessage?.RequestUri?.Segments.LastOrDefault();
                if (!string.IsNullOrEmpty(v) && Regex.IsMatch(v, "v[0-9]+.[0-9]+.[0-9]+"))
                {
                    return Version.Parse(v.Substring(1));
                }
            }
        }
        catch (Exception ex)
        {
            TraceException(ex, "Error due checking update");

        }

        return null;
    }

    public async Task<bool> Download(Version version, IProgress<int> progress, CancellationToken cancellationToken = default)
    {
        var installerFileName = $"{config.ProductName}-{version.Major}.{version.Minor}.{version.Build}.exe";

        try
        {
            using var client = new HttpClient();

            var response = await client.GetAsync(
                URL + $"/download/{installerFileName}",
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
                totalBytes => progress.Report((int)Math.Round(100f * totalBytes / response.Content.Headers.ContentLength.Value)));

            await download.CopyToAsync(file, bufferSize, relativeProgress, cancellationToken);
            progress.Report(100);

            return true;
        }
        catch (Exception ex)
        {
            TraceException(ex, "Error due downloading update");
            return false;
        }
    }

    public void Install(Version version)
    {
        var installerFileName = $"{config.ProductName}-{version.Major}.{version.Minor}.{version.Build}.exe";

        var info = new ProcessStartInfo();
        info.FileName = Path.Combine(Path.GetTempPath(), installerFileName);
        info.Arguments = "/SILENT";

        var i = Process.Start(info);

        if (i != null && !i.HasExited)
        {
            //Application.Exit();
        }
    }

    public bool IsUpdate(Version version) => version > config.FileVersion;

    public string GetReleaseNotesLink() => URL;
}
