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

using System.Collections.ObjectModel;

namespace flowOSD.UI.Commands;

public sealed class CommandParameterInfo
{
    public CommandParameterInfo(string value, string description, string? icon = null)
    {
        Value = value;
        Description = description;
        Icon = icon;
    }

    public string Value { get; }

    public string Description { get; }

    public string? Icon { get; }

    public static ReadOnlyCollection<CommandParameterInfo> Create(params CommandParameterInfo[] parameters)
    {
        return new ReadOnlyCollection<CommandParameterInfo>(parameters);
    }
}
