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
using System.Runtime.CompilerServices;
using flowOSD.Core;

sealed class UpdateCommand : CommandBase
{
    private IUpdater updater;
   // private UpdaterUI updaterUI;

    public UpdateCommand(IUpdater updater)
    {
        this.updater = updater ?? throw new ArgumentNullException(nameof(updater));
     //   this.updaterUI = updaterUI ?? throw new ArgumentNullException(nameof(updaterUI));

        Text = "Check for updates";
        Enabled = true;
    }

    public override bool CanExecuteWithHotKey => false;

    public override async void Execute(object? parameter = null)
    {
        Enabled = false;
        try
        {
            //var version = await updater.CheckUpdate();
            //if (version == null)
            //{
            //    return;
            //}

            //if (updater.IsUpdate(version) || parameter is bool showNoUpdate == false || showNoUpdate)
            //{
            //    updaterUI.Show(version, updater.IsUpdate(version));
            //}
        }
        finally
        {
            Enabled = true;
        }
    }
}
