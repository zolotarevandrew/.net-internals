using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

public class List<T>
{
    private const int DefaultCapacity = 4;

    internal T[] _items; // Do not rename (binary serialization)
    internal int _size; // Do not rename (binary serialization)
    private int _version; // Do not rename (binary serialization)

#pragma warning disable CA1825 // avoid the extra generic instantiation for Array.Empty<T>()
    private static readonly T[] s_emptyArray = new T[0];
#pragma warning restore CA1825

    // Constructs a List. The list is initially empty and has a capacity
    // of zero. Upon adding the first element to the list the capacity is
    // increased to DefaultCapacity, and then increased in multiples of two
    // as required.
    public List()
    {
        _items = s_emptyArray;
    }
    
    public int Capacity
    {
        get => _items.Length;
        set
        {
            if (value < _size)
            {
                
            }

            if (value != _items.Length)
            {
                if (value > 0)
                {
                    T[] newItems = new T[value];
                    if (_size > 0)
                    {
                        Array.Copy(_items, newItems, _size);
                    }

                    _items = newItems;
                }
                else
                {
                    _items = s_emptyArray;
                }
            }
        }
    }

    // Read-only property describing how many elements are in the List.
    public int Count => _size;
    

    // Sets or Gets the element at the given index

    // Adds the given object to the end of this list. The size of the list is
    // increased by one. If required, the capacity of the list is doubled
    // before adding the new element.
    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        _version++;
        T[] array = _items;
        int size = _size;
        if ((uint) size < (uint) array.Length)
        {
            _size = size + 1;
            array[size] = item;
        }
        else
        {
            AddWithResize(item);
        }
    }

    // Non-inline from List.Add to improve its code quality as uncommon path
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AddWithResize(T item)
    {
        Debug.Assert(_size == _items.Length);
        int size = _size;
        Grow(size + 1);
        _size = size + 1;
        _items[size] = item;
    }
    
    // Clears the contents of List.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        _version++;
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            int size = _size;
            _size = 0;
            if (size > 0)
            {
                Array.Clear(_items, 0, size); // Clear the elements so that the gc can reclaim the references.
            }
        }
        else
        {
            _size = 0;
        }
    }

    /// <summary>
    /// Ensures that the capacity of this list is at least the specified <paramref name="capacity"/>.
    /// If the current capacity of the list is less than specified <paramref name="capacity"/>,
    /// the capacity is increased by continuously twice current capacity until it is at least the specified <paramref name="capacity"/>.
    /// </summary>
    /// <param name="capacity">The minimum capacity to ensure.</param>
    public int EnsureCapacity(int capacity)
    {
        if (capacity < 0)
        {
            
        }

        if (_items.Length < capacity)
        {
            Grow(capacity);
            _version++;
        }

        return _items.Length;
    }

    /// <summary>
    /// Increase the capacity of this list to at least the specified <paramref name="capacity"/>.
    /// </summary>
    /// <param name="capacity">The minimum capacity to ensure.</param>
    private void Grow(int capacity)
    {
        Debug.Assert(_items.Length < capacity);

        int newcapacity = _items.Length == 0 ? DefaultCapacity : 2 * _items.Length;

        // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
        // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
        if ((uint) newcapacity > Array.MaxLength) newcapacity = Array.MaxLength;

        // If the computed capacity is still less than specified, set to the original argument.
        // Capacities exceeding Array.MaxLength will be surfaced as OutOfMemoryException by Array.Resize.
        if (newcapacity < capacity) newcapacity = capacity;

        Capacity = newcapacity;
    }

    public T? Find(Predicate<T> match)
    {
        if (match == null)
        {
            //ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
        }

        for (int i = 0; i < _size; i++)
        {
            if (match(_items[i]))
            {
                return _items[i];
            }
        }

        return default;
    }

    public List<T> FindAll(Predicate<T> match)
    {
        if (match == null)
        {
            //.ThrowArgumentNullException(ExceptionArgument.match);
        }

        List<T> list = new List<T>();
        for (int i = 0; i < _size; i++)
        {
            if (match(_items[i]))
            {
                list.Add(_items[i]);
            }
        }

        return list;
    }

    // Removes the element at the given index. The size of the list is
    // decreased by one.
    public void RemoveAt(int index)
    {
        if ((uint) index >= (uint) _size)
        {
            //ThrowHelper.ThrowArgumentOutOfRange_IndexException();
        }

        _size--;
        if (index < _size)
        {
            Array.Copy(_items, index + 1, _items, index, _size - index);
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            _items[_size] = default!;
        }

        _version++;
    }
}