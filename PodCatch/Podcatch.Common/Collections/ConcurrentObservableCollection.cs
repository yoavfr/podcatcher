using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace PodCatch.Common.Collections
{
    public class ObservableConcurrentCollection<T> : IProducerConsumerCollection<T>,
        IEnumerable<T>, ICollection, IEnumerable
    {
        /// <summary>
        /// The internal concurrent bag used for the 'heavy lifting' of the collection implementation
        /// </summary>
        private readonly ConcurrentDictionary<T, T> m_InternalDictionary;

        /// <summary>
        /// Initializes a new instance of the ConcurrentBag<T> class that will raise <see cref="INotifyCollectionChanged"/> events
        /// on the specified dispatcher
        /// </summary>
        public ObservableConcurrentCollection()
        {
            m_InternalDictionary = new ConcurrentDictionary<T, T>();
        }

        /// <summary>
        /// Initializes a new instance of the ConcurrentBag<T> class that contains elements copied from the specified collection
        /// that will raise <see cref="INotifyCollectionChanged"/> events on the specified dispatcher
        /// </summary>
        public ObservableConcurrentCollection(IEnumerable<T> collection)
        {
            m_InternalDictionary = new ConcurrentDictionary<T, T>();
            foreach (T item in collection)
            {
                m_InternalDictionary.TryAdd(item, item);
            }
        }

        /// <summary>
        /// Occurs when the collection changes
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Raises the <see cref="CollectionChanged"/> event
        /// </summary>
        /// <remarks>
        /// This method must only be raised on the dispatcher - use <see cref="RaiseCollectionChangedEventOnDispatcher" />
        /// to do
        /// </remarks>
        private void RaiseCollectionChangedEvent(NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventHandler handler = CollectionChanged;
            if (handler == null)
            {
                return;
            }
            CollectionChanged(this, e);
        }

        bool IProducerConsumerCollection<T>.TryAdd(T item)
        {
            bool result = ((IProducerConsumerCollection<T>)m_InternalDictionary).TryAdd(item);
            if (result)
            {
                RaiseCollectionChangedEvent(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
            }
            return result;
        }

        public void Add(T item)
        {
            m_InternalDictionary.TryAdd(item, item);
            RaiseCollectionChangedEvent(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        }

        public void AddAll(IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                m_InternalDictionary.TryAdd(item, item);
            }
            RaiseCollectionChangedEvent(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items));
        }

        public void Remove(T item)
        {
            T extracted;
            m_InternalDictionary.TryRemove(item, out extracted);
            RaiseCollectionChangedEvent(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
        }

        public void Clear()
        {
            List<T> removedItems = new List<T>(m_InternalDictionary.Keys);
            m_InternalDictionary.Clear();
            RaiseCollectionChangedEvent(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems));
        }

        public bool TryTake(out T item)
        {
            bool result = TryTake(out item);
            if (result)
            {
                RaiseCollectionChangedEvent(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
            }
            return result;
        }

        public int Count
        {
            get
            {
                return m_InternalDictionary.Count;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return m_InternalDictionary.IsEmpty;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return ((ICollection)m_InternalDictionary).IsSynchronized;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                return ((ICollection)m_InternalDictionary).SyncRoot;
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return m_InternalDictionary.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_InternalDictionary.Keys.GetEnumerator();
        }

        public T[] ToArray()
        {
            return new List<T>(m_InternalDictionary.Keys).ToArray();
        }

        void IProducerConsumerCollection<T>.CopyTo(T[] array, int index)
        {
            ((IProducerConsumerCollection<T>)m_InternalDictionary).CopyTo(array, index);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)m_InternalDictionary).CopyTo(array, index);
        }
    }
}