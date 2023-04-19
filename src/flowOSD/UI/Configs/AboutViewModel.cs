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
using System.Reflection;
using System.Runtime.Versioning;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;
using flowOSD.UI.Commands;

public sealed class AboutViewModel : ConfigViewModelBase
{

    public AboutViewModel(IConfig config, ICommandService commandService)
        : base(config, Text.Instance.Config.About, Images.Info, isFooterItem: true)
    {
        if(commandService == null)
        {
            throw new ArgumentNullException(nameof(commandService));
        }

        UpdateCommand = commandService.ResolveNotNull<UpdateCommand>();

        ProductName = config.ProductName;
        ProductVersion = config.ProductVersion;
        Copyright = config.AppFileInfo.LegalCopyright ?? string.Empty;
        Comments = config.AppFileInfo.Comments ?? string.Empty;
        Runtime = $"{Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName}";

        HomePage = "Home page";
        HomePageUrl = "https://github.com/albertakhmetov/flowOSD";

        ModelName = config.ModelName;
    }

    public string ProductName { get; }

    public string ProductVersion { get; }

    public string Comments { get; }

    public string Copyright { get; }

    public string Runtime { get; }

    public string HomePage { get; }

    public string HomePageUrl { get; }

    public string ModelName { get; }

    public CommandBase UpdateCommand { get; } 
}
