
    using System.Diagnostics;

    public class LinkedList<T>
    {
        // This LinkedList is a doubly-Linked circular list.
        internal LinkedListNode<T> head;
        internal int count;
        internal int version;
        private Object _syncRoot;


        // names for serialization
        const String VersionName = "Version";
        const String CountName = "Count";
        const String ValuesName = "Data";

        public LinkedList()
        {
        }

        public int Count
        {
            get { return count; }
        }

        public LinkedListNode<T> First
        {
            get { return head; }
        }

        public LinkedListNode<T> Last
        {
            get { return head == null ? null : head.prev; }
        }

        public LinkedListNode<T> AddAfter(LinkedListNode<T> node, T value)
        {
            ValidateNode(node);
            LinkedListNode<T> result = new LinkedListNode<T>(node.list, value);
            InternalInsertNodeBefore(node.next, result);
            return result;
        }

        public void AddAfter(LinkedListNode<T> node, LinkedListNode<T> newNode)
        {
            ValidateNode(node);
            ValidateNewNode(newNode);
            InternalInsertNodeBefore(node.next, newNode);
            newNode.list = this;
        }

        public LinkedListNode<T> AddBefore(LinkedListNode<T> node, T value)
        {
            ValidateNode(node);
            LinkedListNode<T> result = new LinkedListNode<T>(node.list, value);
            InternalInsertNodeBefore(node, result);
            if (node == head)
            {
                head = result;
            }

            return result;
        }

        public void AddBefore(LinkedListNode<T> node, LinkedListNode<T> newNode)
        {
            ValidateNode(node);
            ValidateNewNode(newNode);
            InternalInsertNodeBefore(node, newNode);
            newNode.list = this;
            if (node == head)
            {
                head = newNode;
            }
        }

        public LinkedListNode<T> AddFirst(T value)
        {
            LinkedListNode<T> result = new LinkedListNode<T>(this, value);
            if (head == null)
            {
                InternalInsertNodeToEmptyList(result);
            }
            else
            {
                InternalInsertNodeBefore(head, result);
                head = result;
            }

            return result;
        }

        public void AddFirst(LinkedListNode<T> node)
        {
            ValidateNewNode(node);

            if (head == null)
            {
                InternalInsertNodeToEmptyList(node);
            }
            else
            {
                InternalInsertNodeBefore(head, node);
                head = node;
            }

            node.list = this;
        }

        public LinkedListNode<T> AddLast(T value)
        {
            LinkedListNode<T> result = new LinkedListNode<T>(this, value);
            if (head == null)
            {
                InternalInsertNodeToEmptyList(result);
            }
            else
            {
                InternalInsertNodeBefore(head, result);
            }

            return result;
        }

        public void AddLast(LinkedListNode<T> node)
        {
            ValidateNewNode(node);

            if (head == null)
            {
                InternalInsertNodeToEmptyList(node);
            }
            else
            {
                InternalInsertNodeBefore(head, node);
            }

            node.list = this;
        }

        public void Clear()
        {
            LinkedListNode<T> current = head;
            while (current != null)
            {
                LinkedListNode<T> temp = current;
                current = current.Next; // use Next the instead of "next", otherwise it will loop forever
                temp.Invalidate();
            }

            head = null;
            count = 0;
            version++;
        }

        public bool Contains(T value)
        {
            return Find(value) != null;
        }

        public LinkedListNode<T> Find(T value)
        {
            LinkedListNode<T> node = head;
            EqualityComparer<T> c = EqualityComparer<T>.Default;
            if (node != null)
            {
                if (value != null)
                {
                    do
                    {
                        if (c.Equals(node.item, value))
                        {
                            return node;
                        }

                        node = node.next;
                    } while (node != head);
                }
                else
                {
                    do
                    {
                        if (node.item == null)
                        {
                            return node;
                        }

                        node = node.next;
                    } while (node != head);
                }
            }

            return null;
        }

        public LinkedListNode<T> FindLast(T value)
        {
            if (head == null) return null;

            LinkedListNode<T> last = head.prev;
            LinkedListNode<T> node = last;
            EqualityComparer<T> c = EqualityComparer<T>.Default;
            if (node != null)
            {
                if (value != null)
                {
                    do
                    {
                        if (c.Equals(node.item, value))
                        {
                            return node;
                        }

                        node = node.prev;
                    } while (node != last);
                }
                else
                {
                    do
                    {
                        if (node.item == null)
                        {
                            return node;
                        }

                        node = node.prev;
                    } while (node != last);
                }
            }

            return null;
        }

        public bool Remove(T value)
        {
            LinkedListNode<T> node = Find(value);
            if (node != null)
            {
                InternalRemoveNode(node);
                return true;
            }

            return false;
        }

        public void Remove(LinkedListNode<T> node)
        {
            ValidateNode(node);
            InternalRemoveNode(node);
        }

        public void RemoveFirst()
        {
            if (head == null)
            {
                throw new InvalidOperationException("");
            }

            InternalRemoveNode(head);
        }

        public void RemoveLast()
        {
            if (head == null)
            {
                throw new InvalidOperationException("");
            }

            InternalRemoveNode(head.prev);
        }

        private void InternalInsertNodeBefore(LinkedListNode<T> node, LinkedListNode<T> newNode)
        {
            newNode.next = node;
            newNode.prev = node.prev;
            node.prev.next = newNode;
            node.prev = newNode;
            version++;
            count++;
        }

        private void InternalInsertNodeToEmptyList(LinkedListNode<T> newNode)
        {
            Debug.Assert(head == null && count == 0, "LinkedList must be empty when this method is called!");
            newNode.next = newNode;
            newNode.prev = newNode;
            head = newNode;
            version++;
            count++;
        }

        internal void InternalRemoveNode(LinkedListNode<T> node)
        {
            Debug.Assert(node.list == this, "Deleting the node from another list!");
            Debug.Assert(head != null, "This method shouldn't be called on empty list!");
            if (node.next == node)
            {
                Debug.Assert(count == 1 && head == node, "this should only be true for a list with only one node");
                head = null;
            }
            else
            {
                node.next.prev = node.prev;
                node.prev.next = node.next;
                if (head == node)
                {
                    head = node.next;
                }
            }

            node.Invalidate();
            count--;
            version++;
        }

        internal void ValidateNewNode(LinkedListNode<T> node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            if (node.list != null)
            {
                throw new InvalidOperationException("");
            }
        }


        internal void ValidateNode(LinkedListNode<T> node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            if (node.list != this)
            {
                throw new InvalidOperationException("");
            }
        }
    }

    public sealed class LinkedListNode<T> {
        internal LinkedList<T> list;
        internal LinkedListNode<T> next;
        internal LinkedListNode<T> prev;
        internal T item;
        
        public LinkedListNode( T value) {
            this.item = value;
        }
 
        internal LinkedListNode(LinkedList<T> list, T value) {
            this.list = list;
            this.item = value;
        }

        public LinkedListNode<T> Next {
            get { return next == null || next == list.head? null: next;}
        }
 
        public LinkedListNode<T> Previous {
            get { return prev == null || this == list.head? null: prev;}
        }
 
        public T Value {
            get { return item;}
            set { item = value;}
        }
 
        internal void Invalidate() {
            list = null;
            next = null;
            prev = null;
        }           
    }