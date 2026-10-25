using System;
using System.Collections.Generic;

namespace Blazor.WebAssembly.DynamicCulture.Internals;

internal class WeakRefCollection<T> where T : class
{
    private readonly List<WeakReference<T>> _collection = new();

    public void Add(T element)
    {
        lock (_collection)
        {
            SweepGarbageCollectedComponents();

            if (!_collection.Exists(cref => cref.TryGetTarget(out var c) && c == element))
            {
                _collection.Add(new WeakReference<T>(element));
            }
        }
    }

    public void ForEach(Action<T> action)
    {
        var elements = new List<T>();
        lock (_collection)
        {
            SweepGarbageCollectedComponents();

            foreach (var cref in _collection)
            {
                if (cref.TryGetTarget(out var element))
                {
                    elements.Add(element);
                }
            }
        }

        // Rendering can register more components; notify the current snapshot outside the lock.
        foreach (var element in elements)
        {
            action(element);
        }
    }

    private void SweepGarbageCollectedComponents()
    {
        lock (_collection)
        {
            for (var i = _collection.Count - 1; i >= 0; i--)
            {
                if (!_collection[i].TryGetTarget(out _))
                {
                    _collection.RemoveAt(i);
                }
            }
        }
    }
}
