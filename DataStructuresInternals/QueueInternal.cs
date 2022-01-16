using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DataStructuresInternals;

public class QueueInternal<T> 
{
    private T[] _array;
    private int _head; // The index from which to dequeue if the queue isn't empty.
    private int _tail; // The index at which to enqueue if the queue isn't full.
    private int _size; // Number of elements.
    private int _version;

    public int Count
    {
        get { return _size; }
    }

    // Adds item to the tail of the queue.
    public void Enqueue(T item)
    {
        if (_size == _array.Length)
        {
            Grow(_size + 1);
        }

        _array[_tail] = item;
        MoveNext(ref _tail);
        _size++;
        _version++;
    }

    // Removes the object at the head of the queue and returns it. If the queue
    // is empty, this method throws an
    // InvalidOperationException.
    public T Dequeue()
    {
        int head = _head;
        T[] array = _array;

        if (_size == 0)
        {
            ThrowForEmptyQueue();
        }

        T removed = array[head];
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            array[head] = default!;
        }

        MoveNext(ref _head);
        _size--;
        _version++;
        return removed;
    }

    public bool TryDequeue([MaybeNullWhen(false)] out T result)
    {
        int head = _head;
        T[] array = _array;

        if (_size == 0)
        {
            result = default!;
            return false;
        }

        result = array[head];
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            array[head] = default!;
        }

        MoveNext(ref _head);
        _size--;
        _version++;
        return true;
    }

    // Returns the object at the head of the queue. The object remains in the
    // queue. If the queue is empty, this method throws an
    // InvalidOperationException.
    public T Peek()
    {
        if (_size == 0)
        {
            ThrowForEmptyQueue();
        }

        return _array[_head];
    }

    public bool TryPeek([MaybeNullWhen(false)] out T result)
    {
        if (_size == 0)
        {
            result = default!;
            return false;
        }

        result = _array[_head];
        return true;
    }

    // PRIVATE Grows or shrinks the buffer to hold capacity objects. Capacity
    // must be >= _size.
    private void SetCapacity(int capacity)
    {
        T[] newarray = new T[capacity];
        if (_size > 0)
        {
            if (_head < _tail)
            {
                Array.Copy(_array, _head, newarray, 0, _size);
            }
            else
            {
                Array.Copy(_array, _head, newarray, 0, _array.Length - _head);
                Array.Copy(_array, 0, newarray, _array.Length - _head, _tail);
            }
        }

        _array = newarray;
        _head = 0;
        _tail = (_size == capacity) ? 0 : _size;
        _version++;
    }

    // Increments the index wrapping it if necessary.
    private void MoveNext(ref int index)
    {
        // It is tempting to use the remainder operator here but it is actually much slower
        // than a simple comparison and a rarely taken branch.
        // JIT produces better code than with ternary operator ?:
        int tmp = index + 1;
        if (tmp == _array.Length)
        {
            tmp = 0;
        }

        index = tmp;
    }

    private void ThrowForEmptyQueue()
    {
        
        throw new InvalidOperationException("");
    }

    public void TrimExcess()
    {
        int threshold = (int) (_array.Length * 0.9);
        if (_size < threshold)
        {
            SetCapacity(_size);
        }
    }

    /// <summary>
    /// Ensures that the capacity of this Queue is at least the specified <paramref name="capacity"/>.
    /// </summary>
    /// <param name="capacity">The minimum capacity to ensure.</param>
    public int EnsureCapacity(int capacity)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "");
        }

        if (_array.Length < capacity)
        {
            Grow(capacity);
        }

        return _array.Length;
    }

    private void Grow(int capacity)
    {
        const int GrowFactor = 2;
        const int MinimumGrow = 4;

        int newcapacity = GrowFactor * _array.Length;

        // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
        // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
        if ((uint) newcapacity > Array.MaxLength) newcapacity = Array.MaxLength;

        // Ensure minimum growth is respected.
        newcapacity = Math.Max(newcapacity, _array.Length + MinimumGrow);

        // If the computed capacity is still less than specified, set to the original argument.
        // Capacities exceeding Array.MaxLength will be surfaced as OutOfMemoryException by Array.Resize.
        if (newcapacity < capacity) newcapacity = capacity;

        SetCapacity(newcapacity);
    }
}