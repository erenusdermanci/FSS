using System.Collections.Generic;

namespace Utils
{
    public class UniqueQueue<T> : IEnumerable<T>
    {
        private readonly HashSet<T> _set;
        private readonly Queue<T> _queue;


        public UniqueQueue()
        {
            _set = new HashSet<T>();
            _queue = new Queue<T>();
        }


        public int Count => _set.Count;

        public void Clear()
        {
            _set.Clear();
            _queue.Clear();
        }


        public bool Contains(T item)
        {
            return _set.Contains(item);
        }


        public void Enqueue(T item)
        {
            if (_set.Add(item))
            {
                _queue.Enqueue(item);
            }
        }

        public T Dequeue()
        {
            var item = _queue.Dequeue();
            _set.Remove(item);
            return item;
        }


        public T Peek()
        {
            return _queue.Peek();
        }


        public IEnumerator<T> GetEnumerator()
        {
            return _queue.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _queue.GetEnumerator();
        }
    }
}