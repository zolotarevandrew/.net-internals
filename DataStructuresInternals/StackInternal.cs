using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DataStructuresInternals;

public class StackInternal<T>
{

#nullable disable
  private T[] _array;
  private int _size;
  private int _version;

  public void TrimExcess()
  {
    if (this._size >= (int) ((double) this._array.Length * 0.9))
      return;
    Array.Resize<T>(ref this._array, this._size);
    ++this._version;
  }


#nullable enable
  public T Peek()
  {
    int index = this._size - 1;
    T[] array = this._array;
    if ((uint) index >= (uint) array.Length)
      this.ThrowForEmptyStack();
    return array[index];
  }

  void ThrowForEmptyStack()
  {

  }

  public bool TryPeek([MaybeNullWhen(false)] out T result)
  {
    int index = this._size - 1;
    T[] array = this._array;
    if ((uint) index >= (uint) array.Length)
    {
      result = default(T);
      return false;
    }

    result = array[index];
    return true;
  }

  public T Pop()
  {
    int index = this._size - 1;
    T[] array = this._array;
    if ((uint) index >= (uint) array.Length)
      this.ThrowForEmptyStack();
    ++this._version;
    this._size = index;
    T obj = array[index];
    if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
      array[index] = default(T);
    return obj;
  }

  public bool TryPop([MaybeNullWhen(false)] out T result)
  {
    int index = this._size - 1;
    T[] array = this._array;
    if ((uint) index >= (uint) array.Length)
    {
      result = default(T);
      return false;
    }

    ++this._version;
    this._size = index;
    result = array[index];
    if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
      array[index] = default(T);
    return true;
  }

  public void Push(T item)
  {
    int size = this._size;
    T[] array = this._array;
    if ((uint) size < (uint) array.Length)
    {
      array[size] = item;
      ++this._version;
      this._size = size + 1;
    }
    else
      this.PushWithResize(item);
  }


  [MethodImpl(MethodImplOptions.NoInlining)]
  private void PushWithResize(T item)
  {
    this.Grow(this._size + 1);
    this._array[this._size] = item;
    ++this._version;
    ++this._size;
  }

  public int EnsureCapacity(int capacity)
  {
    if (capacity < 0)
      throw new ArgumentOutOfRangeException(nameof(capacity), (object) capacity, "");
    if (this._array.Length < capacity)
    {
      this.Grow(capacity);
      ++this._version;
    }

    return this._array.Length;
  }

  private void Grow(int capacity)
  {
    int newSize = this._array.Length == 0 ? 4 : 2 * this._array.Length;
    if ((long) (uint) newSize > (long) Array.MaxLength)
      newSize = Array.MaxLength;
    if (newSize < capacity)
      newSize = capacity;
    Array.Resize<T>(ref this._array, newSize);
  }
}