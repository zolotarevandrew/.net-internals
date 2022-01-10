namespace DataStructuresInternals;

public class SortedList<TKey, TValue>
{
    private TKey[] keys;
    private TValue[] values;
    private int _size;
    private int version;
    private IComparer<TKey> comparer;
    private Object _syncRoot;

    static TKey[] emptyKeys = new TKey[0];
    static TValue[] emptyValues = new TValue[0];

    private const int _defaultCapacity = 4;

    // Constructs a new sorted list. The sorted list is initially empty and has
    // a capacity of zero. Upon adding the first element to the sorted list the
    // capacity is increased to _defaultCapacity, and then increased in multiples of two as
    // required. The elements of the sorted list are ordered according to the
    // IComparable interface, which must be implemented by the keys of
    // all entries added to the sorted list.
    public SortedList()
    {
        keys = emptyKeys;
        values = emptyValues;
        _size = 0;
        comparer = Comparer<TKey>.Default;
    }

    // Constructs a new sorted list. The sorted list is initially empty and has
    // a capacity of zero. Upon adding the first element to the sorted list the
    // capacity is increased to 16, and then increased in multiples of two as
    // required. The elements of the sorted list are ordered according to the
    // IComparable interface, which must be implemented by the keys of
    // all entries added to the sorted list.
    //
    public SortedList(int capacity)
    {
        //if (capacity < 0) ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity, ExceptionResource.ArgumentOutOfRange_NeedNonNegNumRequired);
        keys = new TKey[capacity];
        values = new TValue[capacity];
        comparer = Comparer<TKey>.Default;
    }

    // Constructs a new sorted list with a given IComparer
    // implementation. The sorted list is initially empty and has a capacity of
    // zero. Upon adding the first element to the sorted list the capacity is
    // increased to 16, and then increased in multiples of two as required. The
    // elements of the sorted list are ordered according to the given
    // IComparer implementation. If comparer is null, the
    // elements are compared to each other using the IComparable
    // interface, which in that case must be implemented by the keys of all
    // entries added to the sorted list.
    // 
    public SortedList(IComparer<TKey> comparer)
        : this()
    {
        if (comparer != null)
        {
            this.comparer = comparer;
        }
    }

    // Constructs a new sorted dictionary with a given IComparer
    // implementation and a given initial capacity. The sorted list is
    // initially empty, but will have room for the given number of elements
    // before any reallocations are required. The elements of the sorted list
    // are ordered according to the given IComparer implementation. If
    // comparer is null, the elements are compared to each other using
    // the IComparable interface, which in that case must be implemented
    // by the keys of all entries added to the sorted list.
    // 
    public SortedList(int capacity, IComparer<TKey> comparer)
        : this(comparer)
    {
        Capacity = capacity;
    }

    // Adds an entry with the given key and value to this sorted list. An
    // ArgumentException is thrown if the key is already present in the sorted list.
    // 
    public void Add(TKey key, TValue value)
    {
        //if (key == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
        int i = Array.BinarySearch<TKey>(keys, 0, _size, key, comparer);
        //if (i >= 0) ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_AddingDuplicate);
        Insert(~i, key, value);
    }

    // Returns the capacity of this sorted list. The capacity of a sorted list
    // represents the allocated length of the internal arrays used to store the
    // keys and values of the list, and thus also indicates the maximum number
    // of entries the list can contain before a reallocation of the internal
    // arrays is required.
    // 
    public int Capacity
    {
        get { return keys.Length; }
        set
        {
            if (value != keys.Length)
            {
                if (value < _size)
                {
                    //ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value,
//                        ExceptionResource.ArgumentOutOfRange_SmallCapacity);
                }

                if (value > 0)
                {
                    TKey[] newKeys = new TKey[value];
                    TValue[] newValues = new TValue[value];
                    if (_size > 0)
                    {
                        Array.Copy(keys, 0, newKeys, 0, _size);
                        Array.Copy(values, 0, newValues, 0, _size);
                    }

                    keys = newKeys;
                    values = newValues;
                }
                else
                {
                    keys = emptyKeys;
                    values = emptyValues;
                }
            }
        }
    }

    public IComparer<TKey> Comparer
    {
        get { return comparer; }
    }

    // Returns the number of entries in this sorted list.
    // 
    public int Count
    {
        get { return _size; }
    }

    // Removes all entries from this sorted list.
    public void Clear()
    {
        // clear does not change the capacity
        version++;
        // Don't need to doc this but we clear the elements so that the gc can reclaim the references.
        Array.Clear(keys, 0, _size);
        Array.Clear(values, 0, _size);
        _size = 0;
    }
    

    // Checks if this sorted list contains an entry with the given key.
    // 
    public bool ContainsKey(TKey key)
    {
        return IndexOfKey(key) >= 0;
    }

    // Checks if this sorted list contains an entry with the given value. The
    // values of the entries of the sorted list are compared to the given value
    // using the Object.Equals method. This method performs a linear
    // search and is substantially slower than the Contains
    // method.
    // 
    public bool ContainsValue(TValue value)
    {
        return IndexOfValue(value) >= 0;
    }

    private const int MaxArrayLength = 0X7FEFFFFF;

    // Ensures that the capacity of this sorted list is at least the given
    // minimum value. If the currect capacity of the list is less than
    // min, the capacity is increased to twice the current capacity or
    // to min, whichever is larger.
    private void EnsureCapacity(int min)
    {
        int newCapacity = keys.Length == 0 ? _defaultCapacity : keys.Length * 2;
        // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
        // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
        if ((uint) newCapacity > MaxArrayLength) newCapacity = MaxArrayLength;
        if (newCapacity < min) newCapacity = min;
        Capacity = newCapacity;
    }


    // Returns the key of the entry at the given index.
    // 
    private TKey GetKey(int index)
    {
        //if (index < 0 || index >= _size) ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_Index);
        return keys[index];
    }

    // Returns the value associated with the given key. If an entry with the
    // given key is not found, the returned value is null.
    // 
    public TValue this[TKey key]
    {
        get
        {
            int i = IndexOfKey(key);
            if (i >= 0)
                return values[i];

            //ThrowHelper.ThrowKeyNotFoundException();
            return default(TValue);
        }
        set
        {
            //if (((Object) key) == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            int i = Array.BinarySearch<TKey>(keys, 0, _size, key, comparer);
            if (i >= 0)
            {
                values[i] = value;
                version++;
                return;
            }

            Insert(~i, key, value);
        }
    }
    
    // Returns the index of the entry with a given key in this sorted list. The
    // key is located through a binary search, and thus the average execution
    // time of this method is proportional to Log2(size), where
    // size is the size of this sorted list. The returned value is -1 if
    // the given key does not occur in this sorted list. Null is an invalid 
    // key value.
    // 
    public int IndexOfKey(TKey key)
    {
        //if (key == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
        int ret = Array.BinarySearch<TKey>(keys, 0, _size, key, comparer);
        return ret >= 0 ? ret : -1;
    }

    // Returns the index of the first occurrence of an entry with a given value
    // in this sorted list. The entry is located through a linear search, and
    // thus the average execution time of this method is proportional to the
    // size of this sorted list. The elements of the list are compared to the
    // given value using the Object.Equals method.
    // 
    public int IndexOfValue(TValue value)
    {
        return Array.IndexOf(values, value, 0, _size);
    }

    // Inserts an entry with a given key and value at a given index.
    private void Insert(int index, TKey key, TValue value)
    {
        if (_size == keys.Length) EnsureCapacity(_size + 1);
        if (index < _size)
        {
            Array.Copy(keys, index, keys, index + 1, _size - index);
            Array.Copy(values, index, values, index + 1, _size - index);
        }

        keys[index] = key;
        values[index] = value;
        _size++;
        version++;
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        int i = IndexOfKey(key);
        if (i >= 0)
        {
            value = values[i];
            return true;
        }

        value = default(TValue);
        return false;
    }

    // Removes the entry at the given index. The size of the sorted list is
    // decreased by one.
    // 
    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _size)
            //ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index,
//                ExceptionResource.ArgumentOutOfRange_Index);
        _size--;
        if (index < _size)
        {
            Array.Copy(keys, index + 1, keys, index, _size - index);
            Array.Copy(values, index + 1, values, index, _size - index);
        }

        keys[_size] = default(TKey);
        values[_size] = default(TValue);
        version++;
    }

    // Removes an entry from this sorted list. If an entry with the specified
    // key exists in the sorted list, it is removed. An ArgumentException is
    // thrown if the key is null.
    // 
    public bool Remove(TKey key)
    {
        int i = IndexOfKey(key);
        if (i >= 0)
            RemoveAt(i);
        return i >= 0;
    }
}