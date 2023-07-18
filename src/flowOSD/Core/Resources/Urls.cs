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

namespace flowOSD.Core.Resources;

public sealed class Urls
{
    public static readonly Urls Instance = new Urls();

    public string HomePage => "https://github.com/albertakhmetov/flowOSD";

    public string Optimization => "https://github.com/albertakhmetov/flowOSD/wiki/ASUS-Optimization";

    public string CustomFanCurves => "https://github.com/albertakhmetov/flowOSD/wiki/Custom-Fan-Curves";

    public string GitLatest => "https://github.com/albertakhmetov/flowOSD/releases/latest";
}
