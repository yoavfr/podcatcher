using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace PodCatch.Common.Collections
{
    public class ConcurrentObservableCollection<T> : IProducerConsumerCollection<T>,
        IEnumerable<T>, ICollection, IEnumerable
    {
        /// <summary>
        /// The internal concurrent dictionary used for the 'heavy lifting' of the collection implementation
        /// </summary>
        private readonly ConcurrentDictionary<T, T> m_InternalDictionary;

        /// <summary>
        /// Key selector for determining order of returned items
        /// </summary>
        private readonly Func<T, object> m_KeySelector;

        private bool m_HoldNotifications;

        /// <summary>
        /// Initializes a new instance of the ConcurrentBag<T> class that will raise <see cref="INotifyCollectionChanged"/> events
        /// on the specified dispatcher
        /// </summary>
        public ConcurrentObservableCollection(Func<T, object> keySelector = null, bool holdNotifications = false)
        {
            m_InternalDictionary = new ConcurrentDictionary<T, T>();
            m_KeySelector = keySelector;
            HoldNotifications = holdNotifications;
        }

        /// <summary>
        /// Initializes a new instance of the ConcurrentBag<T> class that contains elements copied from the specified collection
        /// that will raise <see cref="INotifyCollectionChanged"/> events on the specified dispatcher
        /// </summary>
        public ConcurrentObservableCollection(IEnumerable<T> collection, Func<T, object> keySelector = null, bool holdNotifications = false)
        {
            m_InternalDictionary = new ConcurrentDictionary<T, T>();
            m_KeySelector = keySelector;
            HoldNotifications = holdNotifications;

            foreach (T item in collection)
            {
                m_InternalDictionary.TryAdd(item, item);
            }
        }

        public bool HoldNotifications
        {
            get
            {
                return m_HoldNotifications;
            }
            set
            {
                if (m_HoldNotifications != value)
                {
                    m_HoldNotifications = value;
                    if (m_HoldNotifications == false)
                    {
                        NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                        RaiseCollectionChangedEvent(args);
                        IList newItems;
                        if (m_KeySelector != null)
                        {
                            newItems = m_InternalDictionary.Keys.OrderBy(m_KeySelector).ToList();
                        }
                        else
                        {
                            newItems = m_InternalDictionary.Keys.ToList();
                        }
                        args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems);
                        RaiseCollectionChangedEvent(args);
                    }
                }
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
            if (handler == null || HoldNotifications)
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

        public void RemoveFirst(Func<T, bool> predicate)
        {
            m_InternalDictionary.RemoveFirst((KeyValuePair<T,T> keyValuePair)=>
            {
                return predicate(keyValuePair.Key);
            });
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
            if (m_KeySelector != null)
            {
                return m_InternalDictionary.Keys.OrderBy(m_KeySelector).GetEnumerator();
            }
            return m_InternalDictionary.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (m_KeySelector != null)
            {
                return m_InternalDictionary.Keys.OrderBy(m_KeySelector).GetEnumerator();
            }
            return m_InternalDictionary.Keys.GetEnumerator();
        }

        public T[] ToArray()
        {
            if (m_KeySelector != null)
            {
                return m_InternalDictionary.Keys.OrderBy(m_KeySelector).ToArray();
            }
            return new List<T>(m_InternalDictionary.Keys).ToArray();
        }

        void IProducerConsumerCollection<T>.CopyTo(T[] array, int index)
        {
            if (m_KeySelector != null)
            {
                ((IProducerConsumerCollection<T>)m_InternalDictionary.Keys.OrderBy(m_KeySelector)).CopyTo(array, index);
            }
            ((IProducerConsumerCollection<T>)m_InternalDictionary.Keys).CopyTo(array, index);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (m_KeySelector != null)
            {
                ((ICollection)m_InternalDictionary.Keys.OrderBy(m_KeySelector)).CopyTo(array, index);
            }
            ((ICollection)m_InternalDictionary.Keys).CopyTo(array, index);
        }
    }
}