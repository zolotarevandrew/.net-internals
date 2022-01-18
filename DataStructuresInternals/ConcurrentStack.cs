namespace DataStructuresInternals;

public class ConcurrentStack<T>
{
  private volatile ConcurrentStack<T>.Node _head;

  public void Push(T item)
  {
    Node node = new Node(item);
    node._next = this._head;
    if (Interlocked.CompareExchange<Node>(ref this._head, node, node._next) == node._next)
      return;
    this.PushCore(node, node);
  }

  private void PushCore(ConcurrentStack<T>.Node head, ConcurrentStack<T>.Node tail)
  {
    SpinWait spinWait = new SpinWait();
    do
    {
      spinWait.SpinOnce(-1);
      tail._next = this._head;
    } while (Interlocked.CompareExchange<ConcurrentStack<T>.Node>(ref this._head, head, tail._next) != tail._next);
  }

  private static void ValidatePushPopRangeInput(T[] items, int startIndex, int count)
  {
    if (items == null)
      throw new ArgumentNullException(nameof(items));
    if (count < 0)
      throw new ArgumentOutOfRangeException(nameof(count), "");
    int length = items.Length;
    if (startIndex >= length || startIndex < 0)
      throw new ArgumentOutOfRangeException(nameof(startIndex), "");
    if (length - count < startIndex)
      throw new ArgumentException("");
  }
  
  
  public bool TryPeek(out T result)
  {
    ConcurrentStack<T>.Node head = this._head;
    if (head == null)
    {
      result = default(T);
      return false;
    }

    result = head._value;
    return true;
  }

  public bool TryPop(out T result)
  {
    ConcurrentStack<T>.Node head = this._head;
    if (head == null)
    {
      result = default(T);
      return false;
    }

    if (Interlocked.CompareExchange<ConcurrentStack<T>.Node>(ref this._head, head._next, head) != head)
      return this.TryPopCore(out result);
    result = head._value;
    return true;
  }

  public int TryPopRange(T[] items) => items != null
    ? this.TryPopRange(items, 0, items.Length)
    : throw new ArgumentNullException(nameof(items));

  public int TryPopRange(T[] items, int startIndex, int count)
  {
    ConcurrentStack<T>.ValidatePushPopRangeInput(items, startIndex, count);
    if (count == 0)
      return 0;
    ConcurrentStack<T>.Node poppedHead;
    int nodesCount = this.TryPopCore(count, out poppedHead);
    if (nodesCount > 0)
      ConcurrentStack<T>.CopyRemovedItems(poppedHead, items, startIndex, nodesCount);
    return nodesCount;
  }


#nullable disable
  private bool TryPopCore(out T result)
  {
    ConcurrentStack<T>.Node poppedHead;
    if (this.TryPopCore(1, out poppedHead) == 1)
    {
      result = poppedHead._value;
      return true;
    }

    result = default(T);
    return false;
  }

  private int TryPopCore(int count, out ConcurrentStack<T>.Node poppedHead)
  {
    SpinWait spinWait = new SpinWait();
    int num1 = 1;
    ConcurrentStack<T>.Node head;
    int num2;
    while (true)
    {
      head = this._head;
      if (head != null)
      {
        ConcurrentStack<T>.Node node = head;
        for (num2 = 1; num2 < count && node._next != null; ++num2)
          node = node._next;
        if (Interlocked.CompareExchange<ConcurrentStack<T>.Node>(ref this._head, node._next, head) != head)
        {
          for (int index = 0; index < num1; ++index)
            spinWait.SpinOnce(-1);
          if (spinWait.NextSpinWillYield)
            num1 = Random.Shared.Next(1, 8);
          else
            num1 *= 2;
        }
        else
          goto label_9;
      }
      else
        break;
    }

    
    poppedHead = (ConcurrentStack<T>.Node) null;
    return 0;
    label_9:
    poppedHead = head;
    return num2;
  }

  private static void CopyRemovedItems(
    ConcurrentStack<T>.Node head,
    T[] collection,
    int startIndex,
    int nodesCount)
  {
    ConcurrentStack<T>.Node node = head;
    for (int index = startIndex; index < startIndex + nodesCount; ++index)
    {
      collection[index] = node._value;
      node = node._next;
    }
  }
  private sealed class Node
  {
    internal readonly T _value;
    internal Node _next;

    internal Node(T value)
    {
      this._value = value;
      this._next = null;
    }
  }
}