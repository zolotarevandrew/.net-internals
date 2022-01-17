using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DataStructuresInternals;

public class PriorityQueueInternal<TElement, TPriority>
{
  private (TElement Element, TPriority Priority)[] _nodes;
  private readonly IComparer<TPriority> _comparer;
  private int _size;
  private int _version;

  public int Count => this._size;
  
  public void Enqueue(TElement element, TPriority priority)
  {
    int nodeIndex = this._size++;
    ++this._version;
    if (this._nodes.Length == nodeIndex)
      this.Grow(nodeIndex + 1);
    this.MoveUpDefaultComparer((element, priority), nodeIndex);
  }

  public int EnsureCapacity(int capacity)
  {
    if (capacity < 0)
      throw new ArgumentOutOfRangeException(nameof(capacity), (object) capacity, "");
    if (this._nodes.Length < capacity)
    {
      this.Grow(capacity);
      ++this._version;
    }

    return this._nodes.Length;
  }

  private void Grow(int minCapacity)
  {
    int val1 = 2 * this._nodes.Length;
    if ((long) (uint) val1 > (long) Array.MaxLength)
      val1 = Array.MaxLength;
    int newSize = Math.Max(val1, this._nodes.Length + 4);
    if (newSize < minCapacity)
      newSize = minCapacity;
    Array.Resize<(TElement, TPriority)>(ref this._nodes, newSize);
  }

  private void RemoveRootNode()
  {
    int index = --this._size;
    ++this._version;
    if (index > 0)
    {
      (TElement, TPriority) node = this._nodes[index];
      this.MoveDownDefaultComparer(node, 0);
    }

    if (!RuntimeHelpers.IsReferenceOrContainsReferences<(TElement, TPriority)>())
      return;
    //this._nodes[index] = ();
  }

  private int GetParentIndex(int index) => index - 1 >> 2;
  private int GetFirstChildIndex(int index) => (index << 2) + 1;

  private void MoveUpDefaultComparer((TElement Element, TPriority Priority) node, int nodeIndex)
  {
    (TElement Element, TPriority Priority)[] nodes = this._nodes;
    int parentIndex;
    for (; nodeIndex > 0; nodeIndex = parentIndex)
    {
      parentIndex = this.GetParentIndex(nodeIndex);
      (TElement Element, TPriority Priority) tuple = nodes[parentIndex];
      if (System.Collections.Generic.Comparer<TPriority>.Default.Compare(node.Priority, tuple.Priority) < 0)
        nodes[nodeIndex] = tuple;
      else
        break;
    }

    nodes[nodeIndex] = node;
  }
  
  private void MoveDownDefaultComparer((TElement Element, TPriority Priority) node, int nodeIndex)
  {
    (TElement Element, TPriority Priority)[] nodes = this._nodes;
    int firstChildIndex;
    int num1;
    for (int size = this._size; (firstChildIndex = this.GetFirstChildIndex(nodeIndex)) < size; nodeIndex = num1)
    {
      (TElement, TPriority) valueTuple = nodes[firstChildIndex];
      num1 = firstChildIndex;
      int num2 = Math.Min(firstChildIndex + 4, size);
      while (++firstChildIndex < num2)
      {
        (TElement Element, TPriority Priority) tuple = nodes[firstChildIndex];
        if (System.Collections.Generic.Comparer<TPriority>.Default.Compare(tuple.Priority, valueTuple.Item2) < 0)
        {
          valueTuple = tuple;
          num1 = firstChildIndex;
        }
      }

      if (System.Collections.Generic.Comparer<TPriority>.Default.Compare(node.Priority, valueTuple.Item2) > 0)
        nodes[nodeIndex] = valueTuple;
      else
        break;
    }

    nodes[nodeIndex] = node;
  }
}