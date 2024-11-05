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
using System.Collections.ObjectModel;
using flowOSD.Core.Configs;
using flowOSD.Core.Resources;

public abstract class ConfigViewModelBase : ViewModelBase
{
    private bool isSelected;
    private int infoCount;

    protected ConfigViewModelBase(
        ITextResources textResources,
        IImageResources imageResources,
        IConfig config,
        string titleKey,
        string? iconKey,
        bool isFooterItem = false)
        : base(
            textResources,
            imageResources)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));

        IsFooterItem = isFooterItem;
        Title = titleKey == null ? string.Empty : TextResources[titleKey];
        Icon = iconKey == null ? string.Empty : ImageResources[iconKey];
        InfoCount = 0;
    }

    public int InfoCount
    {
        get => infoCount;
        set => SetProperty(ref infoCount, value);
    }

    public bool IsSelected
    {
        get => isSelected;
        set
        {
            if (isSelected == value)
            {
                return;
            }

            if (isSelected)
            {
                OnDeactivated();
            }

            isSelected = value;
            OnPropertyChanged();

            if (isSelected)
            {
                OnActivated();
            }
        }
    }

    public bool IsFooterItem { get; }

    public string Title { get; }

    public string Icon { get; }

    protected IConfig Config { get; }

    protected virtual void OnActivated()
    { }

    protected virtual void OnDeactivated()
    { }
}
