using System;
using System.Collections;
using System.Collections.Generic;

namespace Qtl.DisplayCapture.Collections;

public sealed class DisposableReadonlyList<T> : IDisposableReadonlyList<T> where T : IDisposable
{
    private readonly List<T> _values;

    private bool _isDisposed;

    public DisposableReadonlyList(List<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _values = values;
    }

    public T this[int index] => _values[index];

    public int Count => _values.Count;

    private void Dispose(bool disposing)
    {
        if (_isDisposed) { return; }
        _isDisposed = true;

        foreach (var value in _values)
        {
            value.Dispose();
        }

        if (disposing)
        {
            _values.Clear();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~DisposableReadonlyList()
    {
        Dispose(false);
    }

    public IEnumerator<T> GetEnumerator() => _values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();
}
