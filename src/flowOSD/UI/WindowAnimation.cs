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

namespace flowOSD.UI;

using System;
using System.Reactive.Linq;
using Microsoft.UI.Windowing;

internal static class WindowAnimation
{
    private const float ANIMATION_DURATION = 200f;

    public static IDisposable? Show(AppWindow window, int destY, int animationDelta, Action? afterAnimation = null)
    {
        window.Move(new Windows.Graphics.PointInt32(window.Position.X, window.Position.Y + animationDelta));
        window.Show(false);

        var delta = Convert.ToInt32(Math.Round(animationDelta / (ANIMATION_DURATION / 25)));

        if (window.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = false;
        }

        IDisposable? timer = null;
        timer = Observable.Interval(TimeSpan.FromMilliseconds(ANIMATION_DURATION / 25))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(_ =>
            {
                if (window.Position.Y <= destY)
                {
                    if (window.Presenter is OverlappedPresenter presenter)
                    {
                        presenter.IsAlwaysOnTop = true;
                    }

                    timer?.Dispose();
                    if (afterAnimation != null)
                    {
                        afterAnimation();
                    }
                }
                else
                {
                    window.Move(new Windows.Graphics.PointInt32(window.Position.X, Math.Max(destY, window.Position.Y - delta)));
                }
            });

        return timer;
    }

    public static IDisposable? Hide(AppWindow? window, int destY, int animationDelta)
    {
        if (window == null)
        {
            return null;
        }

        var delta = Convert.ToInt32(Math.Round(animationDelta / (ANIMATION_DURATION / 25)));

        if (window.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = false;
        }

        IDisposable? timer = null;
        timer = Observable.Interval(TimeSpan.FromMilliseconds(ANIMATION_DURATION / 25))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(_ =>
            {
                if (window.Position.Y >= destY + animationDelta)
                {
                    window.Hide();
                    timer?.Dispose();

                    if (window.Presenter is OverlappedPresenter presenter)
                    {
                        presenter.IsAlwaysOnTop = true;
                    }
                }
                else
                {
                    window.Move(new Windows.Graphics.PointInt32(window.Position.X, window.Position.Y + delta));
                }
            });

        return timer;
    }
}
