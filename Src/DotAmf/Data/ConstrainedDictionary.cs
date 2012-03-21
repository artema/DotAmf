using System;
using System.Collections.Generic;
using System.Linq;

namespace DotAmf.Data
{
    /// <summary>
    /// A dictionary that can only has a defined set of keys.
    /// </summary>
    sealed internal class ConstrainedDictionary : IDictionary<string, object>
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="keys">A set of key that are allowed to be used for this dictionary.</param>
        public ConstrainedDictionary(IEnumerable<string> keys)
        {
            if (keys == null) throw new ArgumentNullException("keys");

            _properties = new Dictionary<string, object>();

            foreach (var key in keys)
                _properties[key] = null;
        }
        #endregion

        #region Data
        /// <summary>
        /// Wrapped dictionary that contains actual data.
        /// </summary>
        private readonly Dictionary<string, object> _properties;
        #endregion

        #region IDictionary implementation
        public void Add(string key, object value) { throw new InvalidOperationException(); }

        public bool ContainsKey(string key) { return _properties.ContainsKey(key); }

        public ICollection<string> Keys { get { return _properties.Keys; } }

        public bool Remove(string key) { throw new InvalidOperationException(); }

        public bool TryGetValue(string key, out object value) { return _properties.TryGetValue(key, out value); }

        public ICollection<object> Values { get { return _properties.Values; } }

        public object this[string key]
        {
            get { return _properties[key]; }
            set
            {
                if (!_properties.ContainsKey(key))
                    throw new ArgumentException(string.Format(Errors.SettingMissingProperty, key));

                _properties[key] = value;
            }
        }

        public void Add(KeyValuePair<string, object> item) { throw new InvalidOperationException(); }

        public void Clear() { throw new InvalidOperationException(); }

        public bool Contains(KeyValuePair<string, object> item) { return _properties.Contains(item); }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException("array");
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException("arrayIndex");
            if (array.Length - arrayIndex < _properties.Count) throw new ArgumentException("arrayIndex");

            var values = _properties.ToArray();

            for (int i = arrayIndex, j = 0; i < _properties.Count + arrayIndex; i++, j++)
            {
                array[i] = values[j];
            }
        }

        public int Count { get { return _properties.Count; } }

        public bool IsReadOnly { get { return true; } }

        public bool Remove(KeyValuePair<string, object> item) { throw new InvalidOperationException(); }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() { return _properties.GetEnumerator(); }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
        #endregion
    }
}
