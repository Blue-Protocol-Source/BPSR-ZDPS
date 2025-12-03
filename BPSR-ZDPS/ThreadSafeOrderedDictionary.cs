using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS
{
    public class ThreadSafeOrderedDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly OrderedDictionary _innerDictionary;
        private readonly ReaderWriterLockSlim _readerWriterLock;

        public ThreadSafeOrderedDictionary()
        {
            _innerDictionary = new OrderedDictionary();
            _readerWriterLock = new ReaderWriterLockSlim();
        }

        public void Add(TKey key, TValue value)
        {
            _readerWriterLock.EnterWriteLock();
            try
            {
                _innerDictionary.Add(key, value);
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }
        }

        public bool ContainsKey(TKey key)
        {
            _readerWriterLock.EnterReadLock();
            try
            {
                return _innerDictionary.Contains(key);
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
        }

        public bool Remove(TKey key)
        {
            _readerWriterLock.EnterWriteLock();
            try
            {
                if (_innerDictionary.Contains(key))
                {
                    _innerDictionary.Remove(key);
                    return true;
                }
                return false;
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            _readerWriterLock.EnterReadLock();
            try
            {
                if (_innerDictionary.Contains(key))
                {
                    value = (TValue)_innerDictionary[key];
                    return true;
                }
                value = default(TValue);
                return false;
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
        }

        // Indexer for accessing elements
        public TValue this[TKey key]
        {
            get
            {
                _readerWriterLock.EnterReadLock();
                try
                {
                    return (TValue)_innerDictionary[key];
                }
                finally
                {
                    _readerWriterLock.ExitReadLock();
                }
            }
            set
            {
                _readerWriterLock.EnterWriteLock();
                try
                {
                    _innerDictionary[key] = value;
                }
                finally
                {
                    _readerWriterLock.ExitWriteLock();
                }
            }
        }

        public TValue this[int key]
        {
            get
            {
                _readerWriterLock.EnterReadLock();
                try
                {
                    return (TValue)_innerDictionary[key];
                }
                finally
                {
                    _readerWriterLock.ExitReadLock();
                }
            }
            set
            {
                _readerWriterLock.EnterWriteLock();
                try
                {
                    _innerDictionary[key] = value;
                }
                finally
                {
                    _readerWriterLock.ExitWriteLock();
                }
            }
        }

        public int Count
        {
            get
            {
                _readerWriterLock.EnterReadLock();
                try
                {
                    return _innerDictionary.Count;
                }
                finally
                {
                    _readerWriterLock.ExitReadLock();
                }
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                _readerWriterLock.EnterReadLock();
                try
                {
                    // Return a copy to avoid concurrency issues during enumeration
                    var keys = new List<TKey>();
                    foreach (DictionaryEntry entry in _innerDictionary)
                    {
                        keys.Add((TKey)entry.Key);
                    }
                    return keys;
                }
                finally
                {
                    _readerWriterLock.ExitReadLock();
                }
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                _readerWriterLock.EnterReadLock();
                try
                {
                    // Return a copy to avoid concurrency issues during enumeration
                    var values = new List<TValue>();
                    foreach (DictionaryEntry entry in _innerDictionary)
                    {
                        values.Add((TValue)entry.Value);
                    }
                    return values;
                }
                finally
                {
                    _readerWriterLock.ExitReadLock();
                }
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            _readerWriterLock.EnterReadLock();
            try
            {
                // Return a snapshot to avoid concurrency issues during enumeration
                var list = new List<KeyValuePair<TKey, TValue>>();
                foreach (DictionaryEntry entry in _innerDictionary)
                {
                    list.Add(new KeyValuePair<TKey, TValue>((TKey)entry.Key, (TValue)entry.Value));
                }
                return list.GetEnumerator();
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool IsReadOnly => false;

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _readerWriterLock.EnterWriteLock();
            try
            {
                _innerDictionary.Clear();
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            _readerWriterLock.EnterReadLock();
            try
            {
                if (_innerDictionary.Contains(item.Key))
                {
                    return Equals(_innerDictionary[item.Key], item.Value);
                }
                return false;
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _readerWriterLock.EnterReadLock();
            try
            {
                if (array == null) throw new ArgumentNullException(nameof(array));
                if (arrayIndex < 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                if (array.Length - arrayIndex < Count) throw new ArgumentException("Insufficient space in array.");

                int i = arrayIndex;
                foreach (DictionaryEntry entry in _innerDictionary)
                {
                    array[i++] = new KeyValuePair<TKey, TValue>((TKey)entry.Key, (TValue)entry.Value);
                }
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            _readerWriterLock.EnterWriteLock();
            try
            {
                if (_innerDictionary.Contains(item.Key) && Equals(_innerDictionary[item.Key], item.Value))
                {
                    _innerDictionary.Remove(item.Key);
                    return true;
                }
                return false;
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }
        }
    }
}
