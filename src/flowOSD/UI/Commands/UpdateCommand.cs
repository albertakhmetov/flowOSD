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

namespace flowOSD.UI.Commands;

using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using flowOSD.Core;
using flowOSD.Core.Resources;
using flowOSD.Extensions;

public sealed class UpdateCommand : CommandBase
{
    private IUpdateService updateService;

    private int progress;
    private string updateState;

    private Subject<int>? progressSubject;
    private CancellationTokenSource? cts;
    private CompositeDisposable? disposable;

    public UpdateCommand(IUpdateService updateService)
    {
        this.updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));

        Text = string.Empty;
        UpdateState = string.Empty;

        updateService.State
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(OnUpdateServiceStateUpdated)
            .DisposeWith(Disposable!);
    }

    public override bool CanExecuteWithHotKey => false;

    public int Progress
    {
        get => progress;
        set => SetProperty(ref progress, value);
    }

    public string UpdateState
    {
        get => updateState;
        set => SetProperty(ref updateState, value);
    }

    public string ReleaseNotesUrl => Urls.Instance.GitLatest;

    public override void Dispose()
    {
        disposable?.Dispose();
        disposable = null;

        base.Dispose();
    }

    public async override void Execute(object? parameter)
    {
        var state = await updateService.State.FirstAsync();

        switch (state)
        {
            case UpdateServiceState.None:
            case UpdateServiceState.Updated:
                await updateService.CheckUpdate(true);
                break;

            case UpdateServiceState.ReadyToDownload:
                disposable?.Dispose();
                cts?.Cancel();

                progressSubject = new Subject<int>();
                disposable = new CompositeDisposable(
                    progressSubject,
                    progressSubject.DistinctUntilChanged().ObserveOn(SynchronizationContext.Current!).Subscribe(x => Progress = x));

                cts = new CancellationTokenSource();

                await updateService.Download(progressSubject, cts.Token);
                break;

            case UpdateServiceState.Downloading:
                cts?.Cancel();
                break;

            case UpdateServiceState.ReadyToInstall:
                updateService.Install();
                break;
        }
    }

    private void OnUpdateServiceStateUpdated(UpdateServiceState state)
    {
        UpdateState = state.ToString();

        Enabled = state != UpdateServiceState.Checking;

        switch (state)
        {
            case UpdateServiceState.None:
            case UpdateServiceState.Updated:
            case UpdateServiceState.Checking:
                Text = Core.Resources.Text.Instance.Commands.Update.CheckForUpdate;
                break;

            case UpdateServiceState.ReadyToDownload:
                Text = Core.Resources.Text.Instance.Commands.Update.DownloadUpdate;
                break;

            case UpdateServiceState.Downloading:
                Text = Core.Resources.Text.Instance.Commands.Update.CancelDownload;
                break;

            case UpdateServiceState.ReadyToInstall:
                Text = Core.Resources.Text.Instance.Commands.Update.Install;
                break;
        }
    }
}
