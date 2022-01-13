using System.Collections;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace DataStructuresInternals;

public class HashtableInternal
{
    /*
      This Hashtable uses double hashing.  There are hashsize buckets in the
      table, and each bucket can contain 0 or 1 element.  We use a bit to mark
      whether there's been a collision when we inserted multiple elements
      (ie, an inserted item was hashed at least a second time and we probed
      this bucket, but it was already in use).  Using the collision bit, we
      can terminate lookups & removes for elements that aren't in the hash
      table more quickly.  We steal the most significant bit from the hash code
      to store the collision bit.

      Our hash function is of the following form:

      h(key, n) = h1(key) + n*h2(key)

      where n is the number of times we've hit a collided bucket and rehashed
      (on this particular lookup).  Here are our hash functions:

      h1(key) = GetHash(key);  // default implementation calls key.GetHashCode();
      h2(key) = 1 + (((h1(key) >> 5) + 1) % (hashsize - 1));

      The h1 can return any number.  h2 must return a number between 1 and
      hashsize - 1 that is relatively prime to hashsize (not a problem if
      hashsize is prime).  (Knuth's Art of Computer Programming, Vol. 3, p. 528-9)
      If this is true, then we are guaranteed to visit every bucket in exactly
      hashsize probes, since the least common multiple of hashsize and h2(key)
      will be hashsize * h2(key).  (This is the first number where adding h2 to
      h1 mod hashsize will be 0 and we will search the same bucket twice).

      We previously used a different h2(key, n) that was not constant.  That is a
      horrifically bad idea, unless you can prove that series will never produce
      any identical numbers that overlap when you mod them by hashsize, for all
      subranges from i to i+hashsize, for all i.  It's not worth investigating,
      since there was no clear benefit from using that hash function, and it was
      broken.

      For efficiency reasons, we've implemented this by storing h1 and h2 in a
      temporary, and setting a variable called seed equal to h1.  We do a probe,
      and if we collided, we simply add h2 to seed each time through the loop.

      A good test for h2() is to subclass Hashtable, provide your own implementation
      of GetHash() that returns a constant, then add many items to the hash table.
      Make sure Count equals the number of items you inserted.

      Note that when we remove an item from the hash table, we set the key
      equal to buckets, if there was a collision in this bucket.  Otherwise
      we'd either wipe out the collision bit, or we'd still have an item in
      the hash table.

       --
    */

    private const int InitialSize = 3;

    private const string LoadFactorName = "LoadFactor"; // Do not rename (binary serialization)
    private const string VersionName = "Version"; // Do not rename (binary serialization)
    private const string ComparerName = "Comparer"; // Do not rename (binary serialization)
    private const string HashCodeProviderName = "HashCodeProvider"; // Do not rename (binary serialization)
    private const string HashSizeName = "HashSize"; // Must save buckets.Length. Do not rename (binary serialization)
    private const string KeysName = "Keys"; // Do not rename (binary serialization)
    private const string ValuesName = "Values"; // Do not rename (binary serialization)
    private const string KeyComparerName = "KeyComparer"; // Do not rename (binary serialization)

    // Deleted entries have their key set to buckets

    // The hash table data.
    // This cannot be serialized
    private struct bucket
    {
        public object? key;
        public object? val;
        public int hash_coll; // Store hash code; sign bit means there was a collision.
    }

    private bucket[] _buckets = null!;

    // The total number of entries in the hash table.
    private int _count;

    // The total number of collision bits set in the hashtable
    private int _occupancy;

    private int _loadsize;
    private float _loadFactor;

    private volatile int _version;
    private volatile bool _isWriterInProgress;

    private ICollection? _keys;
    private ICollection? _values;

    private IEqualityComparer? _keycomparer;

    // Note: this constructor is a bogus constructor that does nothing
    // and is for use only with SyncHashtable.
    internal HashtableInternal(bool trash)
    {
    }

    // Constructs a new hashtable. The hashtable is created with an initial
    // capacity of zero and a load factor of 1.0.
    public HashtableInternal() : this(0, 1.0f)
    {
    }

    // Constructs a new hashtable with the given initial capacity and a load
    // factor of 1.0. The capacity argument serves as an indication of
    // the number of entries the hashtable will contain. When this number (or
    // an approximation) is known, specifying it in the constructor can
    // eliminate a number of resizing operations that would otherwise be
    // performed when elements are added to the hashtable.
    //
    public HashtableInternal(int capacity) : this(capacity, 1.0f)
    {
    }

    // Constructs a new hashtable with the given initial capacity and load
    // factor. The capacity argument serves as an indication of the
    // number of entries the hashtable will contain. When this number (or an
    // approximation) is known, specifying it in the constructor can eliminate
    // a number of resizing operations that would otherwise be performed when
    // elements are added to the hashtable. The loadFactor argument
    // indicates the maximum ratio of hashtable entries to hashtable buckets.
    // Smaller load factors cause faster average lookup times at the cost of
    // increased memory consumption. A load factor of 1.0 generally provides
    // the best balance between speed and size.
    //
    public HashtableInternal(int capacity, float loadFactor)
    {
        if (capacity < 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "");
        if (!(loadFactor >= 0.1f && loadFactor <= 1.0f))
            throw new ArgumentOutOfRangeException("");
        ;

        // Based on perf work, .72 is the optimal load factor for this table.
        _loadFactor = 0.72f * loadFactor;

        double rawsize = capacity / _loadFactor;
        if (rawsize > int.MaxValue)
            throw new ArgumentException("", nameof(capacity));

        // Avoid awfully small sizes
        int hashsize = (rawsize > InitialSize) ? HashHelpers.GetPrime((int) rawsize) : InitialSize;
        _buckets = new bucket[hashsize];

        _loadsize = (int) (_loadFactor * hashsize);
        _isWriterInProgress = false;
        // Based on the current algorithm, loadsize must be less than hashsize.
    }

    public HashtableInternal(int capacity, float loadFactor, IEqualityComparer? equalityComparer) : this(capacity,
        loadFactor)
    {
        _keycomparer = equalityComparer;
    }

    // ?InitHash? is basically an implementation of classic DoubleHashing (see http://en.wikipedia.org/wiki/Double_hashing)
    //
    // 1) The only ?correctness? requirement is that the ?increment? used to probe
    //    a. Be non-zero
    //    b. Be relatively prime to the table size ?hashSize?. (This is needed to insure you probe all entries in the table before you ?wrap? and visit entries already probed)
    // 2) Because we choose table sizes to be primes, we just need to insure that the increment is 0 < incr < hashSize
    //
    // Thus this function would work: Incr = 1 + (seed % (hashSize-1))
    //
    // While this works well for ?uniformly distributed? keys, in practice, non-uniformity is common.
    // In particular in practice we can see ?mostly sequential? where you get long clusters of keys that ?pack?.
    // To avoid bad behavior you want it to be the case that the increment is ?large? even for ?small? values (because small
    // values tend to happen more in practice). Thus we multiply ?seed? by a number that will make these small values
    // bigger (and not hurt large values). We picked HashPrime (101) because it was prime, and if ?hashSize-1? is not a multiple of HashPrime
    // (enforced in GetPrime), then incr has the potential of being every value from 1 to hashSize-1. The choice was largely arbitrary.
    //
    // Computes the hash function:  H(key, i) = h1(key) + i*h2(key, hashSize).
    // The out parameter seed is h1(key), while the out parameter
    // incr is h2(key, hashSize).  Callers of this function should
    // add incr each time through a loop.
    private uint InitHash(object key, int hashsize, out uint seed, out uint incr)
    {
        // Hashcode must be positive.  Also, we must not use the sign bit, since
        // that is used for the collision bit.
        uint hashcode = (uint) GetHash(key) & 0x7FFFFFFF;
        seed = (uint) hashcode;
        // Restriction: incr MUST be between 1 and hashsize - 1, inclusive for
        // the modular arithmetic to work correctly.  This guarantees you'll
        // visit every bucket in the table exactly once within hashsize
        // iterations.  Violate this and it'll cause obscure bugs forever.
        // If you change this calculation for h2(key), update putEntry too!
        incr = (uint) (1 + ((seed * HashHelpers.HashPrime) % ((uint) hashsize - 1)));
        return hashcode;
    }

    // Adds an entry with the given key and value to this hashtable. An
    // ArgumentException is thrown if the key is null or if the key is already
    // present in the hashtable.
    //
    public virtual void Add(object key, object? value)
    {
        Insert(key, value, true);
    }

    // Removes all entries from this hashtable.
    public virtual void Clear()
    {
        Debug.Assert(!_isWriterInProgress,
            "Race condition detected in usages of Hashtable - multiple threads appear to be writing to a Hashtable instance simultaneously!  Don't do that - use Hashtable.Synchronized.");

        if (_count == 0 && _occupancy == 0)
            return;

        _isWriterInProgress = true;
        for (int i = 0; i < _buckets.Length; i++)
        {
            _buckets[i].hash_coll = 0;
            _buckets[i].key = null;
            _buckets[i].val = null;
        }

        _count = 0;
        _occupancy = 0;
        UpdateVersion();
        _isWriterInProgress = false;
    }

    // Checks if this hashtable contains the given key.
    public virtual bool Contains(object key)
    {
        return ContainsKey(key);
    }

    // Checks if this hashtable contains an entry with the given key.  This is
    // an O(1) operation.
    //
    public virtual bool ContainsKey(object key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key), "");
        }

        // Take a snapshot of buckets, in case another thread resizes table
        bucket[] lbuckets = _buckets;
        uint hashcode = InitHash(key, lbuckets.Length, out uint seed, out uint incr);
        int ntry = 0;

        bucket b;
        int bucketNumber = (int) (seed % (uint) lbuckets.Length);
        do
        {
            b = lbuckets[bucketNumber];
            if (b.key == null)
            {
                return false;
            }

            if (((b.hash_coll & 0x7FFFFFFF) == hashcode) &&
                KeyEquals(b.key, key))
                return true;
            bucketNumber = (int) (((long) bucketNumber + incr) % (uint) lbuckets.Length);
        } while (b.hash_coll < 0 && ++ntry < lbuckets.Length);

        return false;
    }

    // Checks if this hashtable contains an entry with the given value. The
    // values of the entries of the hashtable are compared to the given value
    // using the Object.Equals method. This method performs a linear
    // search and is thus be substantially slower than the ContainsKey
    // method.
    //
    public virtual bool ContainsValue(object? value)
    {
        if (value == null)
        {
            for (int i = _buckets.Length; --i >= 0;)
            {
                if (_buckets[i].key != null && _buckets[i].key != _buckets && _buckets[i].val == null)
                    return true;
            }
        }
        else
        {
            for (int i = _buckets.Length; --i >= 0;)
            {
                object? val = _buckets[i].val;
                if (val != null && val.Equals(value))
                    return true;
            }
        }

        return false;
    }

    // Returns the value associated with the given key. If an entry with the
    // given key is not found, the returned value is null.
    //
    public virtual object? this[object key]
    {
        get
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key), "");
            }


            // Take a snapshot of buckets, in case another thread does a resize
            bucket[] lbuckets = _buckets;
            uint hashcode = InitHash(key, lbuckets.Length, out uint seed, out uint incr);
            int ntry = 0;

            bucket b;
            int bucketNumber = (int) (seed % (uint) lbuckets.Length);
            do
            {
                int currentversion;

                // A read operation on hashtable has three steps:
                //        (1) calculate the hash and find the slot number.
                //        (2) compare the hashcode, if equal, go to step 3. Otherwise end.
                //        (3) compare the key, if equal, go to step 4. Otherwise end.
                //        (4) return the value contained in the bucket.
                //     After step 3 and before step 4. A writer can kick in a remove the old item and add a new one
                //     in the same bucket. So in the reader we need to check if the hash table is modified during above steps.
                //
                // Writers (Insert, Remove, Clear) will set 'isWriterInProgress' flag before it starts modifying
                // the hashtable and will clear the flag when it is done.  When the flag is cleared, the 'version'
                // will be increased.  We will repeat the reading if a writer is in progress or done with the modification
                // during the read.
                //
                // Our memory model guarantee if we pick up the change in bucket from another processor,
                // we will see the 'isWriterProgress' flag to be true or 'version' is changed in the reader.
                //
                SpinWait spin = default;
                while (true)
                {
                    // this is volatile read, following memory accesses can not be moved ahead of it.
                    currentversion = _version;
                    b = lbuckets[bucketNumber];

                    if (!_isWriterInProgress && (currentversion == _version))
                        break;

                    spin.SpinOnce();
                }

                if (b.key == null)
                {
                    return null;
                }

                if (((b.hash_coll & 0x7FFFFFFF) == hashcode) &&
                    KeyEquals(b.key, key))
                    return b.val;
                bucketNumber = (int) (((long) bucketNumber + incr) % (uint) lbuckets.Length);
            } while (b.hash_coll < 0 && ++ntry < lbuckets.Length);

            return null;
        }

        set => Insert(key, value, false);
    }

    // Increases the bucket count of this hashtable. This method is called from
    // the Insert method when the actual load factor of the hashtable reaches
    // the upper limit specified when the hashtable was constructed. The number
    // of buckets in the hashtable is increased to the smallest prime number
    // that is larger than twice the current number of buckets, and the entries
    // in the hashtable are redistributed into the new buckets using the cached
    // hashcodes.
    private void expand()
    {
        int rawsize = HashHelpers.ExpandPrime(_buckets.Length);
        rehash(rawsize);
    }

    // We occasionally need to rehash the table to clean up the collision bits.
    private void rehash()
    {
        rehash(_buckets.Length);
    }

    private void UpdateVersion()
    {
        // Version might become negative when version is int.MaxValue, but the oddity will be still be correct.
        // So we don't need to special case this.
        _version++;
    }

    private void rehash(int newsize)
    {
        // reset occupancy
        _occupancy = 0;

        // Don't replace any internal state until we've finished adding to the
        // new bucket[].  This serves two purposes:
        //   1) Allow concurrent readers to see valid hashtable contents
        //      at all times
        //   2) Protect against an OutOfMemoryException while allocating this
        //      new bucket[].
        bucket[] newBuckets = new bucket[newsize];

        // rehash table into new buckets
        int nb;
        for (nb = 0; nb < _buckets.Length; nb++)
        {
            bucket oldb = _buckets[nb];
            if ((oldb.key != null) && (oldb.key != _buckets))
            {
                int hashcode = oldb.hash_coll & 0x7FFFFFFF;
                putEntry(newBuckets, oldb.key, oldb.val, hashcode);
            }
        }

        // New bucket[] is good to go - replace buckets and other internal state.
        _isWriterInProgress = true;
        _buckets = newBuckets;
        _loadsize = (int) (_loadFactor * newsize);
        UpdateVersion();
        _isWriterInProgress = false;
        // minimum size of hashtable is 3 now and maximum loadFactor is 0.72 now.
        Debug.Assert(_loadsize < newsize, "Our current implementation means this is not possible.");
    }


    // Internal method to get the hash code for an Object.  This will call
    // GetHashCode() on each object if you haven't provided an IHashCodeProvider
    // instance.  Otherwise, it calls hcp.GetHashCode(obj).
    protected virtual int GetHash(object key)
    {
        if (_keycomparer != null)
            return _keycomparer.GetHashCode(key);
        return key.GetHashCode();
    }

    // Is this Hashtable read-only?
    public virtual bool IsReadOnly => false;

    public virtual bool IsFixedSize => false;

    // Is this Hashtable synchronized?  See SyncRoot property
    public virtual bool IsSynchronized => false;

    // Internal method to compare two keys.  If you have provided an IComparer
    // instance in the constructor, this method will call comparer.Compare(item, key).
    // Otherwise, it will call item.Equals(key).
    //
    protected virtual bool KeyEquals(object? item, object key)
    {
        Debug.Assert(key != null, "key can't be null here!");
        if (object.ReferenceEquals(_buckets, item))
        {
            return false;
        }

        if (object.ReferenceEquals(item, key))
            return true;

        if (_keycomparer != null)
            return _keycomparer.Equals(item, key);
        return item == null ? false : item.Equals(key);
    }

    // Inserts an entry into this hashtable. This method is called from the Set
    // and Add methods. If the add parameter is true and the given key already
    // exists in the hashtable, an exception is thrown.
    private void Insert(object key, object? nvalue, bool add)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key), "");
        }

        if (_count >= _loadsize)
        {
            expand();
        }
        else if (_occupancy > _loadsize && _count > 100)
        {
            rehash();
        }

        // Assume we only have one thread writing concurrently.  Modify
        // buckets to contain new data, as long as we insert in the right order.
        uint hashcode = InitHash(key, _buckets.Length, out uint seed, out uint incr);
        int ntry = 0;
        int emptySlotNumber = -1; // We use the empty slot number to cache the first empty slot. We chose to reuse slots
        // create by remove that have the collision bit set over using up new slots.
        int bucketNumber = (int) (seed % (uint) _buckets.Length);
        do
        {
            // Set emptySlot number to current bucket if it is the first available bucket that we have seen
            // that once contained an entry and also has had a collision.
            // We need to search this entire collision chain because we have to ensure that there are no
            // duplicate entries in the table.
            if (emptySlotNumber == -1 && (_buckets[bucketNumber].key == _buckets) &&
                (_buckets[bucketNumber].hash_coll <
                 0)) // (((buckets[bucketNumber].hash_coll & unchecked(0x80000000))!=0)))
                emptySlotNumber = bucketNumber;

            // Insert the key/value pair into this bucket if this bucket is empty and has never contained an entry
            // OR
            // This bucket once contained an entry but there has never been a collision
            if ((_buckets[bucketNumber].key == null) ||
                (_buckets[bucketNumber].key == _buckets &&
                 ((_buckets[bucketNumber].hash_coll & unchecked(0x80000000)) == 0)))
            {
                // If we have found an available bucket that has never had a collision, but we've seen an available
                // bucket in the past that has the collision bit set, use the previous bucket instead
                if (emptySlotNumber != -1) // Reuse slot
                    bucketNumber = emptySlotNumber;

                // We pretty much have to insert in this order.  Don't set hash
                // code until the value & key are set appropriately.
                _isWriterInProgress = true;
                _buckets[bucketNumber].val = nvalue;
                _buckets[bucketNumber].key = key;
                _buckets[bucketNumber].hash_coll |= (int) hashcode;
                _count++;
                UpdateVersion();
                _isWriterInProgress = false;

                return;
            }

            // The current bucket is in use
            // OR
            // it is available, but has had the collision bit set and we have already found an available bucket
            if (((_buckets[bucketNumber].hash_coll & 0x7FFFFFFF) == hashcode) &&
                KeyEquals(_buckets[bucketNumber].key, key))
            {
                if (add)
                {
                    throw new ArgumentException("");
                }

                _isWriterInProgress = true;
                _buckets[bucketNumber].val = nvalue;
                UpdateVersion();
                _isWriterInProgress = false;

                return;
            }

            // The current bucket is full, and we have therefore collided.  We need to set the collision bit
            // unless we have remembered an available slot previously.
            if (emptySlotNumber == -1)
            {
                // We don't need to set the collision bit here since we already have an empty slot
                if (_buckets[bucketNumber].hash_coll >= 0)
                {
                    _buckets[bucketNumber].hash_coll |= unchecked((int) 0x80000000);
                    _occupancy++;
                }
            }

            bucketNumber = (int) (((long) bucketNumber + incr) % (uint) _buckets.Length);
        } while (++ntry < _buckets.Length);

        // This code is here if and only if there were no buckets without a collision bit set in the entire table
        if (emptySlotNumber != -1)
        {
            // We pretty much have to insert in this order.  Don't set hash
            // code until the value & key are set appropriately.
            _isWriterInProgress = true;
            _buckets[emptySlotNumber].val = nvalue;
            _buckets[emptySlotNumber].key = key;
            _buckets[emptySlotNumber].hash_coll |= (int) hashcode;
            _count++;
            UpdateVersion();
            _isWriterInProgress = false;

            return;
        }

        // If you see this assert, make sure load factor & count are reasonable.
        // Then verify that our double hash function (h2, described at top of file)
        // meets the requirements described above. You should never see this assert.
        Debug.Fail("hash table insert failed!  Load factor too high, or our double hashing function is incorrect.");
        throw new InvalidOperationException("");
    }

    private void putEntry(bucket[] newBuckets, object key, object? nvalue, int hashcode)
    {
        Debug.Assert(hashcode >= 0, "hashcode >= 0"); // make sure collision bit (sign bit) wasn't set.

        uint seed = (uint) hashcode;
        uint incr = unchecked((uint) (1 + ((seed * HashHelpers.HashPrime) % ((uint) newBuckets.Length - 1))));
        int bucketNumber = (int) (seed % (uint) newBuckets.Length);
        while (true)
        {
            if ((newBuckets[bucketNumber].key == null) || (newBuckets[bucketNumber].key == _buckets))
            {
                newBuckets[bucketNumber].val = nvalue;
                newBuckets[bucketNumber].key = key;
                newBuckets[bucketNumber].hash_coll |= hashcode;
                return;
            }

            if (newBuckets[bucketNumber].hash_coll >= 0)
            {
                newBuckets[bucketNumber].hash_coll |= unchecked((int) 0x80000000);
                _occupancy++;
            }

            bucketNumber = (int) (((long) bucketNumber + incr) % (uint) newBuckets.Length);
        }
    }

    // Removes an entry from this hashtable. If an entry with the specified
    // key exists in the hashtable, it is removed. An ArgumentException is
    // thrown if the key is null.
    //
    public virtual void Remove(object key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key), "");
        }

        Debug.Assert(!_isWriterInProgress,
            "Race condition detected in usages of Hashtable - multiple threads appear to be writing to a Hashtable instance simultaneously!  Don't do that - use Hashtable.Synchronized.");

        // Assuming only one concurrent writer, write directly into buckets.
        uint hashcode = InitHash(key, _buckets.Length, out uint seed, out uint incr);
        int ntry = 0;

        bucket b;
        int bn = (int) (seed % (uint) _buckets.Length); // bucketNumber
        do
        {
            b = _buckets[bn];
            if (((b.hash_coll & 0x7FFFFFFF) == hashcode) &&
                KeyEquals(b.key, key))
            {
                _isWriterInProgress = true;
                // Clear hash_coll field, then key, then value
                _buckets[bn].hash_coll &= unchecked((int) 0x80000000);
                if (_buckets[bn].hash_coll != 0)
                {
                    _buckets[bn].key = _buckets;
                }
                else
                {
                    _buckets[bn].key = null;
                }

                _buckets[bn].val = null; // Free object references sooner & simplify ContainsValue.
                _count--;
                UpdateVersion();
                _isWriterInProgress = false;
                return;
            }

            bn = (int) (((long) bn + incr) % (uint) _buckets.Length);
        } while (b.hash_coll < 0 && ++ntry < _buckets.Length);
    }

    // Returns the number of associations in this hashtable.
    //
    public virtual int Count => _count;
}