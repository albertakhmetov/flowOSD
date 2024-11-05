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

namespace flowOSD.UI.Commands;

using System;
using System.Diagnostics;
using System.Reactive.Linq;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;
using flowOSD.Extensions;

sealed class NotebookModeCommand : CommandBase
{
    private IConfig config;
    private IAtk atk;
    private INotebookModeService notebookModeService;
    private IElevatedService elevatedService;

    public NotebookModeCommand(
        ITextResources textResources,
        IImageResources imageResources,
        IConfig config,
        IAtk atk, 
        INotebookModeService notebookModeService,
        IElevatedService elevatedService) 
        : base(
            textResources,
            imageResources)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.atk = atk ?? throw new ArgumentNullException(nameof(atk));
        this.notebookModeService = notebookModeService ?? throw new ArgumentNullException(nameof(notebookModeService));
        this.elevatedService = elevatedService ?? throw new ArgumentNullException(nameof(elevatedService));

        Text = TextResources["Commands.NotebookMode.Description"];
        Description = TextResources["Commands.NotebookMode.Description"];
        Enabled = true;

        notebookModeService.State
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(x => IsChecked = x == DeviceState.Enabled)
            .DisposeWith(Disposable!);
    }

    public override bool CanExecuteWithHotKey => true;

    public override async void Execute(object? parameter = null)
    {
        if (!Enabled)
        {
            return;
        }

        Enabled = false;

        var state = await notebookModeService.State.FirstAsync();

        await Task.Factory.StartNew(() =>
        {
            if (state == DeviceState.Disabled)
            {
                elevatedService.EnableNotebookMode();
            }
            else
            {
                elevatedService.DisableNotebookMode();
            }
        });

        await notebookModeService.State.FirstAsync(x => x != state);

        Enabled = true;
    }
}
