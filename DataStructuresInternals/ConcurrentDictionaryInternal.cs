using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DataStructuresInternals;

public class ConcurrentDictionaryInternal<TKey, TValue>
  where TKey : notnull
{

#nullable disable
  private volatile ConcurrentDictionaryInternal<TKey, TValue>.Tables _tables;
  private readonly IEqualityComparer<TKey> _comparer;
  private readonly EqualityComparer<TKey> _defaultComparer;
  private readonly bool _growLockArray;
  private int _budget;
  private static readonly bool s_isValueWriteAtomic = ConcurrentDictionaryInternal<TKey, TValue>.IsValueWriteAtomic();

  private static bool IsValueWriteAtomic()
  {
    if (!typeof(TValue).IsValueType || typeof(TValue) == typeof(IntPtr) || typeof(TValue) == typeof(UIntPtr))
      return true;
    switch (Type.GetTypeCode(typeof(TValue)))
    {
      case TypeCode.Boolean:
      case TypeCode.Char:
      case TypeCode.SByte:
      case TypeCode.Byte:
      case TypeCode.Int16:
      case TypeCode.UInt16:
      case TypeCode.Int32:
      case TypeCode.UInt32:
      case TypeCode.Single:
        return true;
      case TypeCode.Int64:
      case TypeCode.UInt64:
      case TypeCode.Double:
        return IntPtr.Size == 8;
      default:
        return false;
    }
  }
  
  private bool TryRemoveInternal(TKey key, [MaybeNullWhen(false)] out TValue value, bool matchValue, TValue oldValue)
  {
    IEqualityComparer<TKey> comparer = this._comparer;
    int hashcode = comparer == null ? key.GetHashCode() : comparer.GetHashCode(key);
    label_1:
    ConcurrentDictionaryInternal<TKey, TValue>.Tables tables = this._tables;
    object[] locks = tables._locks;
    uint lockNo;
    ref ConcurrentDictionaryInternal<TKey, TValue>.Node local = ref tables.GetBucketAndLock(hashcode, out lockNo);
    lock (locks[(int) lockNo])
    {
      if (tables == this._tables)
      {
        ConcurrentDictionaryInternal<TKey, TValue>.Node node1 = (ConcurrentDictionaryInternal<TKey, TValue>.Node) null;
        for (ConcurrentDictionaryInternal<TKey, TValue>.Node node2 = local; node2 != null; node2 = node2._next)
        {
          if (hashcode == node2._hashcode && (comparer == null
                ? (this._defaultComparer.Equals(node2._key, key) ? 1 : 0)
                : (comparer.Equals(node2._key, key) ? 1 : 0)) != 0)
          {
            if (matchValue && !EqualityComparer<TValue>.Default.Equals(oldValue, node2._value))
            {
              value = default(TValue);
              return false;
            }

            if (node1 == null)
              Volatile.Write<ConcurrentDictionaryInternal<TKey, TValue>.Node>(ref local, node2._next);
            else
              node1._next = node2._next;
            value = node2._value;
            --tables._countPerLock[(int) lockNo];
            return true;
          }

          node1 = node2;
        }
      }
      else
        goto label_1;
    }

    value = default(TValue);
    return false;
  }


#nullable enable
  public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
  {
    if ((object) key == null)
      throw new ArgumentNullException();
    ConcurrentDictionaryInternal<TKey, TValue>.Tables tables = this._tables;
    IEqualityComparer<TKey> comparer = this._comparer;
    int hashCode = key.GetHashCode();
    for (ConcurrentDictionaryInternal<TKey, TValue>.Node node =
           Volatile.Read<ConcurrentDictionaryInternal<TKey, TValue>.Node>(ref tables.GetBucket(hashCode));
         node != null;
         node = node._next)
    {
      if (hashCode == node._hashcode && this._defaultComparer.Equals(node._key, key))
      {
        value = node._value;
        return true;
      }
    }

    value = default(TValue);
    return false;
  }

  private bool TryUpdateInternal(
    TKey key,
    int? nullableHashcode,
    TValue newValue,
    TValue comparisonValue)
  {
    IEqualityComparer<TKey> comparer = this._comparer;
    int hashcode = nullableHashcode ?? (comparer == null ? key.GetHashCode() : comparer.GetHashCode(key));
    EqualityComparer<TValue> equalityComparer = EqualityComparer<TValue>.Default;
    label_1:
    Tables tables = this._tables;
    object[] locks = tables._locks;
    uint lockNo;
    ref Node local = ref tables.GetBucketAndLock(hashcode, out lockNo);
    lock (locks[(int) lockNo])
    {
      if (tables == this._tables)
      {
        Node node1 = null;
        for (Node node2 = local; node2 != null; node2 = node2._next)
        {
          if (hashcode == node2._hashcode && (comparer == null
                ? (this._defaultComparer.Equals(node2._key, key) ? 1 : 0)
                : (comparer.Equals(node2._key, key) ? 1 : 0)) != 0)
          {
            if (!equalityComparer.Equals(node2._value, comparisonValue))  return false;
            Node node3 = new Node(node2._key, newValue, hashcode, node2._next);
            if (s_isValueWriteAtomic)
            {
              node2._value = newValue;
            }
            else
            {
              if (node1 == null)
                Volatile.Write(ref local, node3);
              else
                node1._next = node3;
            }

            return true;
          }

          node1 = node2;
        }

        return false;
      }

      goto label_1;
    }
  }

  public void Clear()
  {
    int locksAcquired = 0;
    try
    {
      this.AcquireAllLocks(ref locksAcquired);
      if (this.AreAllBucketsEmpty())
        return;
      ConcurrentDictionaryInternal<TKey, TValue>.Tables tables1 = this._tables;
      ConcurrentDictionaryInternal<TKey, TValue>.Tables tables2 = new ConcurrentDictionaryInternal<TKey, TValue>.Tables(
        new ConcurrentDictionaryInternal<TKey, TValue>.Node[31], tables1._locks, new int[tables1._countPerLock.Length]);
      this._tables = tables2;
      this._budget = Math.Max(1, tables2._buckets.Length / tables2._locks.Length);
    }
    finally
    {
      this.ReleaseLocks(0, locksAcquired);
    }
  }
  
  public IEqualityComparer<TKey> Comparer => this._comparer ?? (IEqualityComparer<TKey>) this._defaultComparer;

  public int Count
  {
    get
    {
      int locksAcquired = 0;
      try
      {
        this.AcquireAllLocks(ref locksAcquired);
        return this.GetCountInternal();
      }
      finally
      {
        this.ReleaseLocks(0, locksAcquired);
      }
    }
  }

  private int GetCountInternal()
  {
    int countInternal = 0;
    foreach (int num in this._tables._countPerLock)
      countInternal += num;
    return countInternal;
  }

  private bool AreAllBucketsEmpty()
  {
    foreach (int num in this._tables._countPerLock)
    {
      if (num != 0)
        return false;
    }

    return true;
  }
  
  private void GrowTable(ConcurrentDictionaryInternal<TKey, TValue>.Tables tables)
  {
    
  }

  private static int DefaultConcurrencyLevel => Environment.ProcessorCount;

  private void AcquireAllLocks(ref int locksAcquired)
  {
    this.AcquireLocks(0, 1, ref locksAcquired);
    this.AcquireLocks(1, this._tables._locks.Length, ref locksAcquired);
  }

  private void AcquireLocks(int fromInclusive, int toExclusive, ref int locksAcquired)
  {
    object[] locks = this._tables._locks;
    for (int index = fromInclusive; index < toExclusive; ++index)
    {
      bool lockTaken = false;
      try
      {
        Monitor.Enter(locks[index], ref lockTaken);
      }
      finally
      {
        if (lockTaken)
          ++locksAcquired;
      }
    }
  }

  private void ReleaseLocks(int fromInclusive, int toExclusive)
  {
    ConcurrentDictionaryInternal<TKey, TValue>.Tables tables = this._tables;
    for (int index = fromInclusive; index < toExclusive; ++index)
      Monitor.Exit(tables._locks[index]);
  }

  private sealed class Node
  {
    internal readonly TKey _key;
    internal TValue _value;
    internal volatile ConcurrentDictionaryInternal<TKey, TValue>.Node _next;
    internal readonly int _hashcode;

    internal Node(
      TKey key,
      TValue value,
      int hashcode,
      ConcurrentDictionaryInternal<TKey, TValue>.Node next)
    {
      this._key = key;
      this._value = value;
      this._next = next;
      this._hashcode = hashcode;
    }
  }

  private sealed class Tables
  {
    internal readonly ConcurrentDictionaryInternal<TKey, TValue>.Node[] _buckets;
    internal readonly object[] _locks;
    internal readonly int[] _countPerLock;
    internal readonly ulong _fastModBucketsMultiplier;

    internal Tables(
      ConcurrentDictionaryInternal<TKey, TValue>.Node[] buckets,
      object[] locks,
      int[] countPerLock)
    {
      this._buckets = buckets;
      this._locks = locks;
      this._countPerLock = countPerLock;
      int size = IntPtr.Size;
      this._fastModBucketsMultiplier = HashHelpers.GetFastModMultiplier((uint) buckets.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref ConcurrentDictionaryInternal<TKey, TValue>.Node GetBucket(
      int hashcode)
    {
      ConcurrentDictionaryInternal<TKey, TValue>.Node[] buckets = this._buckets;
      return ref (IntPtr.Size == 8
        ? ref buckets[(int) HashHelpers.FastMod((uint) hashcode, (uint) buckets.Length, this._fastModBucketsMultiplier)]
        : ref buckets[(int) ((uint) hashcode % (uint) buckets.Length)]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref Node GetBucketAndLock(
      int hashcode,
      out uint lockNo)
    {
      Node[] buckets = this._buckets;
      uint index = IntPtr.Size != 8
        ? (uint) hashcode % (uint) buckets.Length
        : HashHelpers.FastMod((uint) hashcode, (uint) buckets.Length, this._fastModBucketsMultiplier);
      lockNo = index % (uint) this._locks.Length;
      return ref buckets[(int) index];
    }
  }
}