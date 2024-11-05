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
namespace flowOSD;

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

internal sealed class CountableSubject<T> : ISubject<T>, IDisposable
{
    private ISubject<T>? baseSubject;
    private BehaviorSubject<int>? countSubject;
    private int count;

    public CountableSubject()
        : this(new Subject<T>())
    { }

    public CountableSubject(T value)
        : this(new BehaviorSubject<T>(value))
    { }

    public CountableSubject(ISubject<T> baseSubject)
    {
        this.baseSubject = baseSubject ?? throw new ArgumentNullException(nameof(baseSubject));

        count = 0;
        countSubject = new BehaviorSubject<int>(count);

        Count = countSubject.AsObservable();
    }

    public IObservable<int> Count { get; }

    public bool IsDisposed => baseSubject == null;

    public void Dispose()
    {
        if (!IsDisposed)
        {
            (baseSubject as IDisposable)?.Dispose();
            baseSubject = null;

            countSubject?.Dispose();
            countSubject = null;
        }
    }

    public void OnCompleted()
    {
        baseSubject?.OnCompleted();
    }

    public void OnError(Exception error)
    {
        baseSubject?.OnError(error);
    }

    public void OnNext(T value)
    {
        baseSubject?.OnNext(value);
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(nameof(CountableSubject<T>));
        }

        countSubject?.OnNext(Interlocked.Increment(ref count));

        var decrement = Disposable.Create(() =>
        {
            countSubject?.OnNext(Interlocked.Decrement(ref count));
        });

        return new CompositeDisposable(baseSubject?.Subscribe(observer), decrement);
    }

}
