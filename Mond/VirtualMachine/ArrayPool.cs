﻿using System;
using System.Collections.Generic;

namespace Mond.VirtualMachine;

internal class ArrayPool<T>
{
    private readonly Stack<T[]> _arrays;
    private readonly int _maxPooled;
    private readonly int _maxSize;
    private int _taken;
    private int _returned;

    public ArrayPool(int maxPooled, int maxSize)
    {
        _arrays = new Stack<T[]>(maxPooled);
        _maxPooled = maxPooled;
        _maxSize = maxSize;

        for (var i = 0; i < maxPooled; i++)
        {
            _arrays.Push(new T[maxSize]);
        }
    }

    public ArrayPoolHandle<T> Rent(int size)
    {
        var array = RentRaw(size);
        return new ArrayPoolHandle<T>(this, array, size);
    }

    public T[] RentRaw(int size)
    {
        if (size == 0)
        {
            return [];
        }

        if (size > _maxSize)
        {
            // too big for us to keep in the pool
            return new T[size];
        }

        if (!_arrays.TryPop(out var array))
        {
            array = new T[_maxSize];
        }

        return array;
    }

    public void Return(T[] array)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        if (array.Length != _maxSize || _arrays.Count >= _maxPooled)
        {
            return;
        }

        _arrays.Push(array);
    }
}

internal ref struct ArrayPoolHandle<T>(ArrayPool<T> pool, T[] array, int size) : IDisposable
{
    private T[] _array = array;
    private Span<T> _span = new(array, 0, size);

    public Span<T> Span => _span;

    public void Dispose()
    {
        if (pool == null)
        {
            return;
        }

        _span.Clear();
        _span = default;
        pool.Return(_array);
        _array = null;
    }
}
