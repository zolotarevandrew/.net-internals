﻿
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Runtime.Serialization;
    using System.Security;
    using System.Security.Permissions;


    public static class HashHelpers
    {
        public const int HashPrime = 101;
        public static int ExpandPrime(int r)
        {
            throw new NotImplementedException();
        }

        public static bool IsPrime(int r)
        {
            throw new NotImplementedException();
        }
        
        public static int GetPrime(int r)
        {
            throw new NotImplementedException();
        }

        public static uint FastMod(uint hashcode, uint bucketsLength, ulong fastModBucketsMultiplier)
        {
            throw new NotImplementedException();
        }

        public static ulong GetFastModMultiplier(uint bucketsLength)
        {
            throw new NotImplementedException();
        }
    }
    
    public class HashSet<T>
    {
        // store lower 31 bits of hash code
        private const int Lower31BitMask = 0x7FFFFFFF;
        // cutoff point, above which we won't do stackallocs. This corresponds to 100 integers.
        private const int StackAllocThreshold = 100;
        // when constructing a hashset from an existing collection, it may contain duplicates, 
        // so this is used as the max acceptable excess ratio of capacity to count. Note that
        // this is only used on the ctor and not to automatically shrink if the hashset has, e.g,
        // a lot of adds followed by removes. Users must explicitly shrink by calling TrimExcess.
        // This is set to 3 because capacity is acceptable as 2x rounded up to nearest prime.
        private const int ShrinkThreshold = 3;
        
        // constants for serialization
        private const String CapacityName = "Capacity";
        private const String ElementsName = "Elements";
        private const String ComparerName = "Comparer";
        private const String VersionName = "Version";

        private int[] m_buckets;
        private Slot[] m_slots;
        private int m_count;
        private int m_lastIndex;
        private int m_freeList;
        private IEqualityComparer<T> m_comparer;
        private int m_version;
        

        /// <summary>
        /// Remove all items from this set. This clears the elements but not the underlying 
        /// buckets and slots array. Follow this call by TrimExcess to release these.
        /// </summary>
        public void Clear() {
            if (m_lastIndex > 0) {
                Debug.Assert(m_buckets != null, "m_buckets was null but m_lastIndex > 0");
 
                // clear the elements so that the gc can reclaim the references.
                // clear only up to m_lastIndex for m_slots 
                Array.Clear(m_slots, 0, m_lastIndex);
                Array.Clear(m_buckets, 0, m_buckets.Length);
                m_lastIndex = 0;
                m_count = 0;
                m_freeList = -1;
            }
            m_version++;
        }
 
        /// <summary>
        /// Checks if this hashset contains the item
        /// </summary>
        /// <param name="item">item to check for containment</param>
        /// <returns>true if item contained; false if not</returns>
        public bool Contains(T item) {
            if (m_buckets != null) {
                int hashCode = InternalGetHashCode(item);
                // see note at "HashSet" level describing why "- 1" appears in for loop
                for (int i = m_buckets[hashCode % m_buckets.Length] - 1; i >= 0; i = m_slots[i].next) {
                    if (m_slots[i].hashCode == hashCode && m_comparer.Equals(m_slots[i].value, item)) {
                        return true;
                    }
                }
            }
            // either m_buckets is null or wasn't found
            return false;
        }

        /// <summary>
        /// Remove item from this hashset
        /// </summary>
        /// <param name="item">item to remove</param>
        /// <returns>true if removed; false if not (i.e. if the item wasn't in the HashSet)</returns>
        public bool Remove(T item) {
            if (m_buckets != null) {
                int hashCode = InternalGetHashCode(item);
                int bucket = hashCode % m_buckets.Length;
                int last = -1;
                for (int i = m_buckets[bucket] - 1; i >= 0; last = i, i = m_slots[i].next) {
                    if (m_slots[i].hashCode == hashCode && m_comparer.Equals(m_slots[i].value, item)) {
                        if (last < 0) {
                            // first iteration; update buckets
                            m_buckets[bucket] = m_slots[i].next + 1;
                        }
                        else {
                            // subsequent iterations; update 'next' pointers
                            m_slots[last].next = m_slots[i].next;
                        }
                        m_slots[i].hashCode = -1;
                        m_slots[i].value = default(T);
                        m_slots[i].next = m_freeList;
 
                        m_count--;
                        m_version++;
                        if (m_count == 0) {
                            m_lastIndex = 0;
                            m_freeList = -1;
                        }
                        else {
                            m_freeList = i;
                        }
                        return true;
                    }
                }
            }
            // either m_buckets is null or wasn't found
            return false;
        }
 
        /// <summary>
        /// Number of elements in this hashset
        /// </summary>
        public int Count {
            get { return m_count; }
        }
        

        /// <summary>
        /// Add item to this HashSet. Returns bool indicating whether item was added (won't be 
        /// added if already present)
        /// </summary>
        /// <param name="item"></param>
        /// <returns>true if added, false if already present</returns>
        public bool Add(T item) {
            return AddIfNotPresent(item);
        }
 
        /// <summary>
        /// Searches the set for a given value and returns the equal value it finds, if any.
        /// </summary>
        /// <param name="equalValue">The value to search for.</param>
        /// <param name="actualValue">The value from the set that the search found, or the default value of <typeparamref name="T"/> when the search yielded no match.</param>
        /// <returns>A value indicating whether the search was successful.</returns>
        /// <remarks>
        /// This can be useful when you want to reuse a previously stored reference instead of 
        /// a newly constructed one (so that more sharing of references can occur) or to look up
        /// a value that has more complete data than the value you currently have, although their
        /// comparer functions indicate they are equal.
        /// </remarks>
        public bool TryGetValue(T equalValue, out T actualValue) {
            if (m_buckets != null) {
                int i = InternalIndexOf(equalValue);
                if (i >= 0) {
                    actualValue = m_slots[i].value;
                    return true;
                }
            }
            actualValue = default(T);
            return false;
        }
 
        /// <summary>
        /// Take the union of this HashSet with other. Modifies this set.
        /// 
        /// Implementation note: GetSuggestedCapacity (to increase capacity in advance avoiding 
        /// multiple resizes ended up not being useful in practice; quickly gets to the 
        /// point where it's a wasteful check.
        /// </summary>
        /// <param name="other">enumerable with items to add</param>
        public void UnionWith(IEnumerable<T> other) {
            if (other == null) {
                throw new ArgumentNullException("other");
            }
            Contract.EndContractBlock();
 
            foreach (T item in other) {
                AddIfNotPresent(item);
            }
        }
 
        /// <summary>
        /// Takes the intersection of this set with other. Modifies this set.
        /// 
        /// Implementation Notes: 
        /// We get better perf if other is a hashset using same equality comparer, because we 
        /// get constant contains check in other. Resulting cost is O(n1) to iterate over this.
        /// 
        /// If we can't go above route, iterate over the other and mark intersection by checking
        /// contains in this. Then loop over and delete any unmarked elements. Total cost is n2+n1. 
        /// 
        /// Attempts to return early based on counts alone, using the property that the 
        /// intersection of anything with the empty set is the empty set.
        /// </summary>
        /// <param name="other">enumerable with items to add </param>
        public void IntersectWith(IEnumerable<T> other) {
            if (other == null) {
                throw new ArgumentNullException("other");
            }
            Contract.EndContractBlock();
 
            // intersection of anything with empty set is empty set, so return if count is 0
            if (m_count == 0) {
                return;
            }
 
            // if other is empty, intersection is empty set; remove all elements and we're done
            // can only figure this out if implements ICollection<T>. (IEnumerable<T> has no count)
            ICollection<T> otherAsCollection = other as ICollection<T>;
            if (otherAsCollection != null) {
                if (otherAsCollection.Count == 0) {
                    Clear();
                    return;
                }
 
                HashSet<T> otherAsSet = other as HashSet<T>;
                // faster if other is a hashset using same equality comparer; so check 
                // that other is a hashset using the same equality comparer.
                if (otherAsSet != null && AreEqualityComparersEqual(this, otherAsSet)) {
                    IntersectWithHashSetWithSameEC(otherAsSet);
                    return;
                }
            }
        }
 
        /// <summary>
        /// Remove items in other from this set. Modifies this set.
        /// </summary>
        /// <param name="other">enumerable with items to remove</param>
        public void ExceptWith(IEnumerable<T> other) {
            if (other == null) {
                throw new ArgumentNullException("other");
            }
            Contract.EndContractBlock();
 
            // this is already the enpty set; return
            if (m_count == 0) {
                return;
            }
 
            // special case if other is this; a set minus itself is the empty set
            if (other == this) {
                Clear();
                return;
            }
 
            // remove every element in other from this
            foreach (T element in other) {
                Remove(element);
            }
        }
 
        /// <summary>
        /// Takes symmetric difference (XOR) with other and this set. Modifies this set.
        /// </summary>
        /// <param name="other">enumerable with items to XOR</param>
        public void SymmetricExceptWith(IEnumerable<T> other) {
            if (other == null) {
                throw new ArgumentNullException("other");
            }
            Contract.EndContractBlock();
 
            // if set is empty, then symmetric difference is other
            if (m_count == 0) {
                UnionWith(other);
                return;
            }
 
            // special case this; the symmetric difference of a set with itself is the empty set
            if (other == this) {
                Clear();
                return;
            }
 
            HashSet<T> otherAsSet = other as HashSet<T>;
            // If other is a HashSet, it has unique elements according to its equality comparer,
            // but if they're using different equality comparers, then assumption of uniqueness
            // will fail. So first check if other is a hashset using the same equality comparer;
            // symmetric except is a lot faster and avoids bit array allocations if we can assume
            // uniqueness
            if (otherAsSet != null && AreEqualityComparersEqual(this, otherAsSet)) {
                //SymmetricExceptWithUniqueHashSet(otherAsSet);
            }
            else {
                //SymmetricExceptWithEnumerable(other);
            }
        }
 
        /// <summary>
        /// Checks if this is a subset of other.
        /// 
        /// Implementation Notes:
        /// The following properties are used up-front to avoid element-wise checks:
        /// 1. If this is the empty set, then it's a subset of anything, including the empty set
        /// 2. If other has unique elements according to this equality comparer, and this has more
        /// elements than other, then it can't be a subset.
        /// 
        /// Furthermore, if other is a hashset using the same equality comparer, we can use a 
        /// faster element-wise check.
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if this is a subset of other; false if not</returns>
        public bool IsSubsetOf(IEnumerable<T> other) {
            if (other == null) {
                throw new ArgumentNullException("other");
            }
            Contract.EndContractBlock();
 
            // The empty set is a subset of any set
            if (m_count == 0) {
                return true;
            }
 
            HashSet<T> otherAsSet = other as HashSet<T>;
            // faster if other has unique elements according to this equality comparer; so check 
            // that other is a hashset using the same equality comparer.
            if (otherAsSet != null && AreEqualityComparersEqual(this, otherAsSet)) {
                // if this has more elements then it can't be a subset
                if (m_count > otherAsSet.Count) {
                    return false;
                }
 
                // already checked that we're using same equality comparer. simply check that 
                // each element in this is contained in other.
                return IsSubsetOfHashSetWithSameEC(otherAsSet);
            }
            else {
                //ElementCount result = CheckUniqueAndUnfoundElements(other, false);
                //return (result.uniqueCount == m_count && result.unfoundCount >= 0);
                return false;
            }
        }
 
        /// <summary>
        /// Checks if this is a proper subset of other (i.e. strictly contained in)
        /// 
        /// Implementation Notes:
        /// The following properties are used up-front to avoid element-wise checks:
        /// 1. If this is the empty set, then it's a proper subset of a set that contains at least
        /// one element, but it's not a proper subset of the empty set.
        /// 2. If other has unique elements according to this equality comparer, and this has >=
        /// the number of elements in other, then this can't be a proper subset.
        /// 
        /// Furthermore, if other is a hashset using the same equality comparer, we can use a 
        /// faster element-wise check.
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if this is a proper subset of other; false if not</returns>
        public bool IsProperSubsetOf(IEnumerable<T> other) {
            if (other == null) {
                throw new ArgumentNullException("other");
            }
            Contract.EndContractBlock();
 
            ICollection<T> otherAsCollection = other as ICollection<T>;
            if (otherAsCollection != null) {
                // the empty set is a proper subset of anything but the empty set
                if (m_count == 0) {
                    return otherAsCollection.Count > 0;
                }
                HashSet<T> otherAsSet = other as HashSet<T>;
                // faster if other is a hashset (and we're using same equality comparer)
                if (otherAsSet != null && AreEqualityComparersEqual(this, otherAsSet)) {
                    if (m_count >= otherAsSet.Count) {
                        return false;
                    }
                    // this has strictly less than number of items in other, so the following
                    // check suffices for proper subset.
                    return IsSubsetOfHashSetWithSameEC(otherAsSet);
                }
            }

            return false;
        }
 
        /// <summary>
        /// Checks if this is a superset of other
        /// 
        /// Implementation Notes:
        /// The following properties are used up-front to avoid element-wise checks:
        /// 1. If other has no elements (it's the empty set), then this is a superset, even if this
        /// is also the empty set.
        /// 2. If other has unique elements according to this equality comparer, and this has less 
        /// than the number of elements in other, then this can't be a superset
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if this is a superset of other; false if not</returns>
        public bool IsSupersetOf(IEnumerable<T> other) {
            if (other == null) {
                throw new ArgumentNullException("other");
            }
            Contract.EndContractBlock();
 
            // try to fall out early based on counts
            ICollection<T> otherAsCollection = other as ICollection<T>;
            if (otherAsCollection != null) {
                // if other is the empty set then this is a superset
                if (otherAsCollection.Count == 0) {
                    return true;
                }
                HashSet<T> otherAsSet = other as HashSet<T>;
                // try to compare based on counts alone if other is a hashset with
                // same equality comparer
                if (otherAsSet != null && AreEqualityComparersEqual(this, otherAsSet)) {
                    if (otherAsSet.Count > m_count) {
                        return false;
                    }
                }
            }
 
            return ContainsAllElements(other as HashSet<T>);
        }
 
        /// <summary>
        /// Checks if this is a proper superset of other (i.e. other strictly contained in this)
        /// 
        /// Implementation Notes: 
        /// This is slightly more complicated than above because we have to keep track if there
        /// was at least one element not contained in other.
        /// 
        /// The following properties are used up-front to avoid element-wise checks:
        /// 1. If this is the empty set, then it can't be a proper superset of any set, even if 
        /// other is the empty set.
        /// 2. If other is an empty set and this contains at least 1 element, then this is a proper
        /// superset.
        /// 3. If other has unique elements according to this equality comparer, and other's count
        /// is greater than or equal to this count, then this can't be a proper superset
        /// 
        /// Furthermore, if other has unique elements according to this equality comparer, we can
        /// use a faster element-wise check.
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if this is a proper superset of other; false if not</returns>
        public bool IsProperSupersetOf(IEnumerable<T> other) {
            if (other == null) {
                throw new ArgumentNullException("other");
            }
            Contract.EndContractBlock();
 
            // the empty set isn't a proper superset of any set.
            if (m_count == 0) {
                return false;
            }
 
            ICollection<T> otherAsCollection = other as ICollection<T>;
            if (otherAsCollection != null) {
                // if other is the empty set then this is a superset
                if (otherAsCollection.Count == 0) {
                    // note that this has at least one element, based on above check
                    return true;
                }
                HashSet<T> otherAsSet = other as HashSet<T>;
                // faster if other is a hashset with the same equality comparer
                if (otherAsSet != null && AreEqualityComparersEqual(this, otherAsSet)) {
                    if (otherAsSet.Count >= m_count) {
                        return false;
                    }
                    // now perform element check
                    return ContainsAllElements(otherAsSet);
                }
            }

            return false;
        }
 
        /// <summary>
        /// Checks if this set overlaps other (i.e. they share at least one item)
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if these have at least one common element; false if disjoint</returns>
        public bool Overlaps(IEnumerable<T> other) {
            if (other == null) {
                throw new ArgumentNullException("other");
            }
            Contract.EndContractBlock();
 
            if (m_count == 0) {
                return false;
            }
 
            foreach (T element in other) {
                if (Contains(element)) {
                    return true;
                }
            }
            return false;
        }
 
        /// <summary>
        /// Checks if this and other contain the same elements. This is set equality: 
        /// duplicates and order are ignored
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool SetEquals(IEnumerable<T> other) {
            if (other == null) {
                throw new ArgumentNullException("other");
            }
            Contract.EndContractBlock();
 
            HashSet<T> otherAsSet = other as HashSet<T>;
            // faster if other is a hashset and we're using same equality comparer
            if (otherAsSet != null && AreEqualityComparersEqual(this, otherAsSet)) {
                // attempt to return early: since both contain unique elements, if they have 
                // different counts, then they can't be equal
                if (m_count != otherAsSet.Count) {
                    return false;
                }
 
                // already confirmed that the sets have the same number of distinct elements, so if
                // one is a superset of the other then they must be equal
                return ContainsAllElements(otherAsSet);
            }
            else {
                ICollection<T> otherAsCollection = other as ICollection<T>;
                if (otherAsCollection != null) {
                    // if this count is 0 but other contains at least one element, they can't be equal
                    if (m_count == 0 && otherAsCollection.Count > 0) {
                        return false;
                    }
                }
                return false;
            }
        }
 
        public void CopyTo(T[] array) { CopyTo(array, 0, m_count); }
 
        public void CopyTo(T[] array, int arrayIndex, int count) {
            if (array == null) {
                throw new ArgumentNullException("array");
            }
            Contract.EndContractBlock();
 
            // check array index valid index into array
            if (arrayIndex < 0) {
                throw new ArgumentOutOfRangeException("arrayIndex");
            }
 
            // also throw if count less than 0
            if (count < 0) {
                throw new ArgumentOutOfRangeException("count");
            }
 
            // will array, starting at arrayIndex, be able to hold elements? Note: not
            // checking arrayIndex >= array.Length (consistency with list of allowing
            // count of 0; subsequent check takes care of the rest)
            if (arrayIndex > array.Length || count > array.Length - arrayIndex) {
                throw new ArgumentException("");
            }
 
            int numCopied = 0;
            for (int i = 0; i < m_lastIndex && numCopied < count; i++) {
                if (m_slots[i].hashCode >= 0) {
                    array[arrayIndex + numCopied] = m_slots[i].value;
                    numCopied++;
                }
            }
        }
 
        /// <summary>
        /// Remove elements that match specified predicate. Returns the number of elements removed
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        public int RemoveWhere(Predicate<T> match) {
            if (match == null) {
                throw new ArgumentNullException("match");
            }
            Contract.EndContractBlock();
 
            int numRemoved = 0;
            for (int i = 0; i < m_lastIndex; i++) {
                if (m_slots[i].hashCode >= 0) {
                    // cache value in case delegate removes it
                    T value = m_slots[i].value;
                    if (match(value)) {
                        // check again that remove actually removed it
                        if (Remove(value)) {
                            numRemoved++;
                        }
                    }
                }
            }
            return numRemoved;
        }
 
        /// <summary>
        /// Gets the IEqualityComparer that is used to determine equality of keys for 
        /// the HashSet.
        /// </summary>
        public IEqualityComparer<T> Comparer {
            get {
                return m_comparer;
            }
        }
 
        /// <summary>
        /// Sets the capacity of this list to the size of the list (rounded up to nearest prime),
        /// unless count is 0, in which case we release references.
        /// 
        /// This method can be used to minimize a list's memory overhead once it is known that no
        /// new elements will be added to the list. To completely clear a list and release all 
        /// memory referenced by the list, execute the following statements:
        /// 
        /// list.Clear();
        /// list.TrimExcess(); 
        /// </summary>
        public void TrimExcess() {
            Debug.Assert(m_count >= 0, "m_count is negative");
 
            if (m_count == 0) {
                // if count is zero, clear references
                m_buckets = null;
                m_slots = null;
                m_version++;
            }
            else {
                Debug.Assert(m_buckets != null, "m_buckets was null but m_count > 0");
 
                // similar to IncreaseCapacity but moves down elements in case add/remove/etc
                // caused fragmentation
                int newSize = HashHelpers.GetPrime(m_count);
                Slot[] newSlots = new Slot[newSize];
                int[] newBuckets = new int[newSize];
 
                // move down slots and rehash at the same time. newIndex keeps track of current 
                // position in newSlots array
                int newIndex = 0;
                for (int i = 0; i < m_lastIndex; i++) {
                    if (m_slots[i].hashCode >= 0) {
                        newSlots[newIndex] = m_slots[i];
 
                        // rehash
                        int bucket = newSlots[newIndex].hashCode % newSize;
                        newSlots[newIndex].next = newBuckets[bucket] - 1;
                        newBuckets[bucket] = newIndex + 1;
 
                        newIndex++;
                    }
                }
 
                Debug.Assert(newSlots.Length <= m_slots.Length, "capacity increased after TrimExcess");
 
                m_lastIndex = newIndex;
                m_slots = newSlots;
                m_buckets = newBuckets;
                m_freeList = -1;
            }
        }
 
        /// <summary>
        /// Initializes buckets and slots arrays. Uses suggested capacity by finding next prime
        /// greater than or equal to capacity.
        /// </summary>
        /// <param name="capacity"></param>
        private void Initialize(int capacity) {
            Debug.Assert(m_buckets == null, "Initialize was called but m_buckets was non-null");
 
            int size = HashHelpers.GetPrime(capacity);
 
            m_buckets = new int[size];
            m_slots = new Slot[size];
        }
 
        /// <summary>
        /// Expand to new capacity. New capacity is next prime greater than or equal to suggested 
        /// size. This is called when the underlying array is filled. This performs no 
        /// defragmentation, allowing faster execution; note that this is reasonable since 
        /// AddIfNotPresent attempts to insert new elements in re-opened spots.
        /// </summary>
        /// <param name="sizeSuggestion"></param>
        private void IncreaseCapacity() {
            Debug.Assert(m_buckets != null, "IncreaseCapacity called on a set with no elements");
 
            int newSize = HashHelpers.ExpandPrime(m_count);
            if (newSize <= m_count) {
                throw new ArgumentException("");
            }
 
            // Able to increase capacity; copy elements to larger array and rehash
            SetCapacity(newSize, false);
        }
 
        /// <summary>
        /// Set the underlying buckets array to size newSize and rehash.  Note that newSize
        /// *must* be a prime.  It is very likely that you want to call IncreaseCapacity()
        /// instead of this method.
        /// </summary>
        private void SetCapacity(int newSize, bool forceNewHashCodes) { 
            Contract.Assert(HashHelpers.IsPrime(newSize), "New size is not prime!");
 
            Contract.Assert(m_buckets != null, "SetCapacity called on a set with no elements");
 
            Slot[] newSlots = new Slot[newSize];
            if (m_slots != null) {
                Array.Copy(m_slots, 0, newSlots, 0, m_lastIndex);
            }
 
            if(forceNewHashCodes) {
                for(int i = 0; i < m_lastIndex; i++) {
                    if(newSlots[i].hashCode != -1) {
                        newSlots[i].hashCode = InternalGetHashCode(newSlots[i].value);
                    }
                }
            }
 
            int[] newBuckets = new int[newSize];
            for (int i = 0; i < m_lastIndex; i++) {
                int bucket = newSlots[i].hashCode % newSize;
                newSlots[i].next = newBuckets[bucket] - 1;
                newBuckets[bucket] = i + 1;
            }
            m_slots = newSlots;
            m_buckets = newBuckets;
        }
 
        /// <summary>
        /// Adds value to HashSet if not contained already
        /// Returns true if added and false if already present
        /// </summary>
        /// <param name="value">value to find</param>
        /// <returns></returns>
        private bool AddIfNotPresent(T value) {
            if (m_buckets == null) {
                Initialize(0);
            }
 
            int hashCode = InternalGetHashCode(value);
            int bucket = hashCode % m_buckets.Length;
#if FEATURE_RANDOMIZED_STRING_HASHING && !FEATURE_NETCORE
            int collisionCount = 0;
#endif
            for (int i = m_buckets[hashCode % m_buckets.Length] - 1; i >= 0; i = m_slots[i].next) {
                if (m_slots[i].hashCode == hashCode && m_comparer.Equals(m_slots[i].value, value)) {
                    return false;
                }
#if FEATURE_RANDOMIZED_STRING_HASHING && !FEATURE_NETCORE
                collisionCount++;
#endif
            }
 
            int index;
            if (m_freeList >= 0) {
                index = m_freeList;
                m_freeList = m_slots[index].next;
            }
            else {
                if (m_lastIndex == m_slots.Length) {
                    IncreaseCapacity();
                    // this will change during resize
                    bucket = hashCode % m_buckets.Length;
                }
                index = m_lastIndex;
                m_lastIndex++;
            }
            m_slots[index].hashCode = hashCode;
            m_slots[index].value = value;
            m_slots[index].next = m_buckets[bucket] - 1;
            m_buckets[bucket] = index + 1;
            m_count++;
            m_version++;
 
#if FEATURE_RANDOMIZED_STRING_HASHING && !FEATURE_NETCORE
            if(collisionCount > HashHelpers.HashCollisionThreshold && HashHelpers.IsWellKnownEqualityComparer(m_comparer)) {
                m_comparer = (IEqualityComparer<T>) HashHelpers.GetRandomizedEqualityComparer(m_comparer);
                SetCapacity(m_buckets.Length, true);
            }
#endif // FEATURE_RANDOMIZED_STRING_HASHING
 
            return true;
        }
 
        // Add value at known index with known hash code. Used only
        // when constructing from another HashSet.
        private void AddValue(int index, int hashCode, T value) {
            int bucket = hashCode % m_buckets.Length;
 
#if DEBUG
            Debug.Assert(InternalGetHashCode(value) == hashCode);
            for (int i = m_buckets[bucket] - 1; i >= 0; i = m_slots[i].next) {
                Debug.Assert(!m_comparer.Equals(m_slots[i].value, value));
            }
#endif
 
            Debug.Assert(m_freeList == -1);
            m_slots[index].hashCode = hashCode;
            m_slots[index].value = value;
            m_slots[index].next = m_buckets[bucket] - 1;
            m_buckets[bucket] = index + 1;
        }
 
        /// <summary>
        /// Checks if this contains of other's elements. Iterates over other's elements and 
        /// returns false as soon as it finds an element in other that's not in this.
        /// Used by SupersetOf, ProperSupersetOf, and SetEquals.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        private bool ContainsAllElements(HashSet<T> other) {
            foreach (T element in other.ToArray()) {
                if (!Contains(element)) {
                    return false;
                }
            }
            return true;
        }
 
        /// <summary>
        /// Implementation Notes:
        /// If other is a hashset and is using same equality comparer, then checking subset is 
        /// faster. Simply check that each element in this is in other.
        /// 
        /// Note: if other doesn't use same equality comparer, then Contains check is invalid,
        /// which is why callers must take are of this.
        /// 
        /// If callers are concerned about whether this is a proper subset, they take care of that.
        ///
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        private bool IsSubsetOfHashSetWithSameEC(HashSet<T> other) {
 
            foreach (T item in this.ToArray()) {
                if (!other.Contains(item)) {
                    return false;
                }
            }
            return true;
        }
 
        /// <summary>
        /// If other is a hashset that uses same equality comparer, intersect is much faster 
        /// because we can use other's Contains
        /// </summary>
        /// <param name="other"></param>
        private void IntersectWithHashSetWithSameEC(HashSet<T> other) {
            for (int i = 0; i < m_lastIndex; i++) {
                if (m_slots[i].hashCode >= 0) {
                    T item = m_slots[i].value;
                    if (!other.Contains(item)) {
                        Remove(item);
                    }
                }
            }
        }

        /// <summary>
        /// Used internally by set operations which have to rely on bit array marking. This is like
        /// Contains but returns index in slots array. 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private int InternalIndexOf(T item) {
            Debug.Assert(m_buckets != null, "m_buckets was null; callers should check first");
 
            int hashCode = InternalGetHashCode(item);
            for (int i = m_buckets[hashCode % m_buckets.Length] - 1; i >= 0; i = m_slots[i].next) {
                if ((m_slots[i].hashCode) == hashCode && m_comparer.Equals(m_slots[i].value, item)) {
                    return i;
                }
            }
            // wasn't found
            return -1;
        }

        /// <summary>
        /// Add if not already in hashset. Returns an out param indicating index where added. This 
        /// is used by SymmetricExcept because it needs to know the following things:
        /// - whether the item was already present in the collection or added from other
        /// - where it's located (if already present, it will get marked for removal, otherwise
        /// marked for keeping)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        private bool AddOrGetLocation(T value, out int location) {
            Debug.Assert(m_buckets != null, "m_buckets is null, callers should have checked");
 
            int hashCode = InternalGetHashCode(value);
            int bucket = hashCode % m_buckets.Length;
            for (int i = m_buckets[hashCode % m_buckets.Length] - 1; i >= 0; i = m_slots[i].next) {
                if (m_slots[i].hashCode == hashCode && m_comparer.Equals(m_slots[i].value, value)) {
                    location = i;
                    return false; //already present
                }
            }
            int index;
            if (m_freeList >= 0) {
                index = m_freeList;
                m_freeList = m_slots[index].next;
            }
            else {
                if (m_lastIndex == m_slots.Length) {
                    IncreaseCapacity();
                    // this will change during resize
                    bucket = hashCode % m_buckets.Length;
                }
                index = m_lastIndex;
                m_lastIndex++;
            }
            m_slots[index].hashCode = hashCode;
            m_slots[index].value = value;
            m_slots[index].next = m_buckets[bucket] - 1;
            m_buckets[bucket] = index + 1;
            m_count++;
            m_version++;
            location = index;
            return true;
        }
        
 
        /// <summary>
        /// Copies this to an array. Used for DebugView
        /// </summary>
        /// <returns></returns>
        internal T[] ToArray() {
            T[] newArray = new T[Count];
            CopyTo(newArray);
            return newArray;
        }

        /// <summary>
        /// Checks if equality comparers are equal. This is used for algorithms that can
        /// speed up if it knows the other item has unique elements. I.e. if they're using 
        /// different equality comparers, then uniqueness assumption between sets break.
        /// </summary>
        /// <param name="set1"></param>
        /// <param name="set2"></param>
        /// <returns></returns>
        private static bool AreEqualityComparersEqual(HashSet<T> set1, HashSet<T> set2) {
            return set1.Comparer.Equals(set2.Comparer);
        }
 
        /// <summary>
        /// Workaround Comparers that throw ArgumentNullException for GetHashCode(null).
        /// </summary>
        /// <param name="item"></param>
        /// <returns>hash code</returns>
        private int InternalGetHashCode(T item) {
            if (item == null) {
                return 0;
            } 
            return m_comparer.GetHashCode(item) & Lower31BitMask;
        }

        internal struct Slot {
            internal int hashCode;      // Lower 31 bits of hash code, -1 if unused
            internal int next;          // Index of next entry, -1 if last
            internal T value;
        }

    }