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

using System.Windows.Forms;
using flowOSD.Api;

sealed class ClipboardCopyPlainTextCommand : CommandBase
{
    private IKeysSender keysSender;

    public ClipboardCopyPlainTextCommand(IKeysSender keysSender)
    {
        this.keysSender = keysSender ?? throw new ArgumentNullException(nameof(keysSender));

        Description = "Copy to the clipboard";
        Enabled = true;
    }

    public override string Name => nameof(ClipboardCopyPlainTextCommand);

    public override void Execute(object? parameter = null)
    {
        keysSender.SendKeys(Keys.C, Keys.ControlKey);
    }
}
