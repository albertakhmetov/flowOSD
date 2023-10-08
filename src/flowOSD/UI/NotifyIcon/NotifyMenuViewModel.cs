using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using flowOSD.Core;
using flowOSD.Core.Resources;
using flowOSD.UI.Commands;

namespace flowOSD.UI.NotifyIcon;

public sealed class NotifyMenuViewModel : ViewModelBase
{
    public NotifyMenuViewModel(ICommandService commandService)
    {
        if (commandService == null)
        {
            throw new ArgumentNullException(nameof(commandService));
        }

        MainUICommand = commandService.ResolveNotNull<MainUICommand>();
        ConfigCommand = commandService.ResolveNotNull<ConfigCommand>();
        RestartAppCommand = commandService.ResolveNotNull<RestartAppCommand>();
        ExitCommand = commandService.ResolveNotNull<ExitCommand>();
    }

    public Text TextResources => Text.Instance;

    public CommandBase MainUICommand { get; }

    public CommandBase ConfigCommand { get; }

    public CommandBase RestartAppCommand { get; }

    public CommandBase ExitCommand { get; }
}
