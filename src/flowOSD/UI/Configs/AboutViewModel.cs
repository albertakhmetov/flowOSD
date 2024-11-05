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
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.Versioning;
using System.Windows.Input;
using flowOSD.Core;
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;
using flowOSD.Extensions;
using flowOSD.UI.Commands;

public sealed class AboutViewModel : ConfigViewModelBase
{
    private CompositeDisposable? disposable = null;
    private IDisposable? updateSubscription;

    private IUpdateService updateService;

    public AboutViewModel(
        ITextResources textResources,
        IImageResources imageResources,
        IConfig config,
        ICommandService commandService,
        IUpdateService updateService)
        : base(
            textResources,
            imageResources,
            config,
            "Config.About.Title",
            "Common.Info", 
            isFooterItem: true)
    {
        this.updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));

        if (commandService == null)
        {
            throw new ArgumentNullException(nameof(commandService));
        }

        UpdateCommand = commandService.ResolveNotNull<UpdateCommand>();

        ProductName = config.ProductName;
        ProductVersion = config.ProductVersion;
        Copyright = config.AppFileInfo.LegalCopyright ?? string.Empty;
        Comments = config.AppFileInfo.Comments ?? string.Empty;
        Runtime = $"{Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName}";

        HomePage = TextResources["Config.About.HomePage"];
        HomePageUrl = TextResources["Links.HomePage"];

        LicenseUrl = TextResources["Links.License"];

        ModelName = config.ModelName;

        updateSubscription = updateService.State
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(state => InfoCount = state == UpdateServiceState.ReadyToDownload ? 1 : 0);
    }

    public string ProductName { get; }

    public string ProductVersion { get; }

    public string Comments { get; }

    public string Copyright { get; }

    public string Runtime { get; }

    public string HomePage { get; }

    public string HomePageUrl { get; }

    public string LicenseUrl { get; }

    public string ModelName { get; }

    public UpdateCommand UpdateCommand { get; }

    public bool CheckForUpdates
    {
        get => Config.Common.CheckForUpdates;
        set => Config.Common.CheckForUpdates = value;
    }

    public void Dispose()
    {
        updateSubscription?.Dispose();
        updateSubscription = null;

        OnDeactivated();
    }

    protected async override void OnActivated()
    {
        var state = await updateService.State.FirstAsync();

        if (state == UpdateServiceState.None || state == UpdateServiceState.Updated)
        {
            await updateService.CheckUpdate();
        }

        disposable = new CompositeDisposable();

        Config.Common.PropertyChanged
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(OnPropertyChanged)
            .DisposeWith(disposable);

        OnPropertyChanged(null);
    }

    protected override void OnDeactivated()
    {
        disposable?.Dispose();
        disposable = null;
    }
}
