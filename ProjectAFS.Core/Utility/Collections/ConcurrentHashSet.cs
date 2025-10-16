using System.Collections;
using Newtonsoft.Json;

namespace ProjectAFS.Core.Utility.Collections;

/// <summary>
/// Represents a thread-safe, unordered collection of unique elements.
/// </summary>
/// <typeparam name="T">The type of elements in the set. Must be non-nullable.</typeparam>
[JsonArray, JsonConverter(typeof(ConcurrentHashSetConverter<>))]
public class ConcurrentHashSet<T> : ISet<T>, IReadOnlyCollection<T>, ICollection<T>, ICollection, IEnumerable<T>, IEnumerable where T : notnull
{
	private class Node
	{
		public T Key;
		public Node? Next;

		public Node(T key, Node? next)
		{
			Key = key;
			Next = next;
		}
	}

	private const int DefaultCapacity = 31;
	private const int LoadFactor = 4; // expand threshold = buckets.Length * LoadFactor

	private Node?[] _buckets;
	private object[] _locks; // one lock object per bucket
	private int _count; // using Interlocked to add/subtract
	private readonly IEqualityComparer<T> _comparer;
	private readonly ReaderWriterLockSlim _resizeLock = new(LockRecursionPolicy.NoRecursion);

	public ConcurrentHashSet() : this(EqualityComparer<T>.Default, DefaultCapacity)
	{
	}

	public ConcurrentHashSet(IEqualityComparer<T> comparer) : this(comparer, DefaultCapacity)
	{
	}

	public ConcurrentHashSet(IEnumerable<T> collection, IEqualityComparer<T>? comparer = null) : this(comparer ?? EqualityComparer<T>.Default, DefaultCapacity)
	{
		foreach (var item in collection) TryAdd(item);
	}

	private ConcurrentHashSet(IEqualityComparer<T> comparer, int capacity)
	{
		_comparer = comparer ?? EqualityComparer<T>.Default;
		int size = Math.Max(3, capacity);
		_buckets = new Node?[size];
		_locks = new object[size];
		for (int i = 0; i < size; i++) _locks[i] = new object();
	}

	private int GetIndex(T item, Node?[] buckets) => (int) ((uint) (_comparer.GetHashCode(item) & 0x7FFFFFFF) % (uint) buckets.Length);

	public int Count => Volatile.Read(ref _count);
	public bool IsReadOnly => false;
	public IEqualityComparer<T> Comparer => _comparer;
	public object SyncRoot => this;
	public bool IsSynchronized => false;

	public bool TryAdd(T item)
	{
		ArgumentNullException.ThrowIfNull(item);
		_resizeLock.EnterUpgradeableReadLock();
		try
		{
			var buckets = _buckets;
			int idx = GetIndex(item, buckets);
			var bucketLock = _locks[idx];

			lock (bucketLock)
			{
				var node = buckets[idx];
				while (node != null)
				{
					if (_comparer.Equals(node.Key, item)) return false; // already exists
					node = node.Next;
				}

				// insert at head
				buckets[idx] = new Node(item, buckets[idx]);
				Interlocked.Increment(ref _count);
			}

			if (Volatile.Read(ref _count) > buckets.Length * LoadFactor)
			{
				_resizeLock.EnterWriteLock();
				try
				{
					if (_buckets.Length == buckets.Length)
					{
						Resize(buckets.Length * 2 + 1);
					}
				}
				finally
				{
					_resizeLock.ExitReadLock();
				}
			}

			return true;
		}
		finally
		{
			_resizeLock.ExitUpgradeableReadLock();
		}
	}

	bool ISet<T>.Add(T item) => TryAdd(item);
	void ICollection<T>.Add(T item) => TryAdd(item);

	public bool Remove(T item)
	{
		ArgumentNullException.ThrowIfNull(item);

		_resizeLock.EnterUpgradeableReadLock();
		try
		{
			var buckets = _buckets;
			int idx = GetIndex(item, buckets);
			var bucketLock = _locks[idx];

			lock (bucketLock)
			{
				Node? prev = null;
				var node = buckets[idx];
				while (node != null)
				{
					if (_comparer.Equals(node.Key, item))
					{
						if (prev == null) buckets[idx] = node.Next;
						else prev.Next = node.Next;
						Interlocked.Decrement(ref _count);
						return true;
					}

					prev = node;
					node = node.Next;
				}
			}

			return false;
		}
		finally
		{
			_resizeLock.ExitUpgradeableReadLock();
		}
	}

	public void Clear()
	{
		_resizeLock.EnterWriteLock();
		try
		{
			for (int i = 0; i < _buckets.Length; i++)
			{
				_buckets[i] = null;
			}

			Interlocked.Exchange(ref _count, 0);
			// here we don't need to replace _locks array
		}
		finally
		{
			_resizeLock.ExitWriteLock();
		}
	}

	public bool Contains(T item)
	{
		ArgumentNullException.ThrowIfNull(item);

		_resizeLock.EnterReadLock();
		try
		{
			var buckets = _buckets;
			int idx = GetIndex(item, buckets);
			var bucketLock = _locks[idx];

			lock (bucketLock)
			{
				var node = buckets[idx];
				while (node != null)
				{
					if (_comparer.Equals(node.Key, item)) return true;
					node = node.Next;
				}
			}

			return false;
		}
		finally
		{
			_resizeLock.ExitReadLock();
		}
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		ArgumentNullException.ThrowIfNull(array);
		var arr = ToArray();
		Array.Copy(arr, 0, array, arrayIndex, arr.Length);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		ArgumentNullException.ThrowIfNull(array);
		var arr = ToArray();
		Array.Copy(arr, 0, array, index, arr.Length);
	}

	public IEnumerator<T> GetEnumerator()
	{
		// Return a snapshot enumerator to avoid impacts from concurrent modifications
		foreach (var item in ToArray())
		{
			yield return item;
		}
	}
	
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public void UnionWith(IEnumerable<T> other)
	{
		ArgumentNullException.ThrowIfNull(other);
		_resizeLock.EnterWriteLock();

		try
		{
			foreach (var item in other)
			{
				int idx = GetIndex(item, _buckets);
				lock (_locks[idx])
				{
					var node = _buckets[idx];
					bool found = false;
					while (node != null)
					{
						if (_comparer.Equals(node.Key, item))
						{
							found = true;
							break;
						}

						node = node.Next;
					}

					if (!found)
					{
						_buckets[idx] = new Node(item, _buckets[idx]);
						Interlocked.Increment(ref _count);
					}
				}

				// may need to expand (in write lock, no need to re-acquire)
				if (Volatile.Read(ref _count) > _buckets.Length * LoadFactor)
				{
					Resize(_buckets.Length * 2 + 1);
				}
			}
		}
		finally
		{
			_resizeLock.ExitWriteLock();
		}
	}

	public void IntersectWith(IEnumerable<T> other)
	{
		ArgumentNullException.ThrowIfNull(other);
		_resizeLock.EnterWriteLock();
		try
		{
			var otherSet = new HashSet<T>(other, _comparer);
			for (int i = 0; i < _buckets.Length; i++)
			{
				lock (_locks[i])
				{
					Node? prev = null;
					var node = _buckets[i];
					while (node != null)
					{
						if (!otherSet.Contains(node.Key))
						{
							if (prev == null) _buckets[i] = node.Next;
							else prev.Next = node.Next;
							Interlocked.Decrement(ref _count);
							node = (prev == null) ? _buckets[i] : prev.Next;
						}
						else
						{
							prev = node;
							node = node.Next;
						}
					}
				}
			}
		}
		finally
		{
			_resizeLock.ExitWriteLock();
		}
	}

	public void ExceptWith(IEnumerable<T> other)
	{
		ArgumentNullException.ThrowIfNull(other);
		_resizeLock.EnterWriteLock();
		try
		{
			foreach (var item in other)
			{
				int idx = GetIndex(item, _buckets);
				lock (_locks[idx])
				{
					Node? prev = null;
					var node = _buckets[idx];
					while (node != null)
					{
						if (_comparer.Equals(node.Key, item))
						{
							if (prev == null) _buckets[idx] = node.Next;
							else prev.Next = node.Next;
							Interlocked.Decrement(ref _count);
							break;
						}

						prev = node;
						node = node.Next;
					}
				}
			}
		}
		finally
		{
			_resizeLock.ExitWriteLock();
		}
	}

	public void SymmetricExceptWith(IEnumerable<T> other)
	{
		ArgumentNullException.ThrowIfNull(other);
		_resizeLock.EnterWriteLock();
		try
		{
			foreach (var item in other)
			{
				int idx = GetIndex(item, _buckets);
				lock (_locks[idx])
				{
					Node? prev = null;
					var node = _buckets[idx];
					bool found = false;
					while (node != null)
					{
						if (_comparer.Equals(node.Key, item))
						{
							if (prev == null) _buckets[idx] = node.Next;
							else prev.Next = node.Next;
							Interlocked.Decrement(ref _count);
							found = true;
							break;
						}

						prev = node;
						node = node.Next;
					}

					if (!found)
					{
						// add
						_buckets[idx] = new Node(item, _buckets[idx]);
						Interlocked.Increment(ref _count);
					}
				}
			}
		}
		finally
		{
			_resizeLock.ExitWriteLock();
		}
	}

	public bool IsSubsetOf(IEnumerable<T> other)
	{
		ArgumentNullException.ThrowIfNull(other);
		var otherSet = new HashSet<T>(other, _comparer);
		_resizeLock.EnterReadLock();
		try
		{
			for (int i = 0; i < _buckets.Length; i++)
			{
				lock (_locks[i])
				{
					var node = _buckets[i];
					while (node != null)
					{
						if (!otherSet.Contains(node.Key)) return false;
						node = node.Next;
					}
				}
			}

			return true;
		}
		finally
		{
			_resizeLock.ExitReadLock();
		}
	}

	public bool IsSupersetOf(IEnumerable<T> other)
	{
		ArgumentNullException.ThrowIfNull(other);
		_resizeLock.EnterReadLock();
		try
		{
			foreach (var item in other)
			{
				int idx = GetIndex(item, _buckets);
				lock (_locks[idx])
				{
					var node = _buckets[idx];
					bool found = false;
					while (node != null)
					{
						if (_comparer.Equals(node.Key, item))
						{
							found = true;
							break;
						}

						node = node.Next;
					}

					if (!found) return false;
				}
			}

			return true;
		}
		finally
		{
			_resizeLock.ExitReadLock();
		}
	}

	public bool IsProperSupersetOf(IEnumerable<T> other)
	{
		ArgumentNullException.ThrowIfNull(other);
		var otherSet = new HashSet<T>(other, _comparer);
		return IsSupersetOf(otherSet) && Count > otherSet.Count;
	}
	
	public bool IsProperSubsetOf(IEnumerable<T> other)
	{
		ArgumentNullException.ThrowIfNull(other);
		var otherSet = new HashSet<T>(other, _comparer);
		return IsSubsetOf(otherSet) && Count < otherSet.Count;
	}
	
	public bool Overlaps(IEnumerable<T> other)
	{
		ArgumentNullException.ThrowIfNull(other);
		_resizeLock.EnterReadLock();
		try
		{
			foreach (var item in other)
			{
				int idx = GetIndex(item, _buckets);
				lock (_locks[idx])
				{
					var node = _buckets[idx];
					while (node != null)
					{
						if (_comparer.Equals(node.Key, item)) return true;
						node = node.Next;
					}
				}
			}

			return false;
		}
		finally
		{
			_resizeLock.ExitReadLock();
		}
	}
	
	public bool SetEquals(IEnumerable<T> other)
	{
		ArgumentNullException.ThrowIfNull(other);
		var otherSet = new HashSet<T>(other, _comparer);
		if (otherSet.Count != Count) return false;
		_resizeLock.EnterReadLock();
		try
		{
			for (int i = 0; i < _buckets.Length; i++)
			{
				lock (_locks[i])
				{
					var node = _buckets[i];
					while (node != null)
					{
						if (!otherSet.Contains(node.Key)) return false;
						node = node.Next;
					}
				}
			}
			return true;
		}
		finally
		{
			_resizeLock.ExitReadLock();
		}
	}

	public T[] ToArray()
	{
		_resizeLock.EnterWriteLock();
		try
		{
			// To avoid concurrent modifications, lock all buckets first (in order to prevent deadlock), then copy
			// ReSharper disable ForCanBeConvertedToForeach
			for (int i = 0; i < _locks.Length; i++) Monitor.Enter(_locks[i]);
			try
			{
				var result = new T[Count];
				int pos = 0;
				for (int i = 0; i < _buckets.Length; i++)
				{
					var node = _buckets[i];
					while (node != null)
					{
						result[pos++] = node.Key;
						node = node.Next;
					}
				}

				if (pos != result.Length)
				{
					// count may different with the actual items (seldom, but possible), resize the array
					Array.Resize(ref result, pos);
				}
				return result;
			}
			finally
			{
				// exit all locks in reverse order
				for (int i = _locks.Length - 1; i >= 0; i--) Monitor.Exit(_locks[i]);
			}
			// ReSharper restore ForCanBeConvertedToForeach
		}
		finally
		{
			_resizeLock.ExitWriteLock();
		}
	}
	
	public bool TryGetAny(out T item)
	{
		_resizeLock.EnterReadLock();
		try
		{
			for (int i = 0; i < _buckets.Length; i++)
			{
				var bucketLock = _locks[i];
				lock (bucketLock)
				{
					var node = _buckets[i];
					if (node != null)
					{
						item = node.Key;
						return true;
					}
				}
			}

			item = default!;
			return false;
		}
		finally
		{
			_resizeLock.ExitReadLock();
		}
	}

	private void Resize(int newSize)
	{
		// ReSharper disable ForCanBeConvertedToForeach
		var newBuckets = new Node?[newSize];
		var newLocks = new object[newSize];
		for (int i = 0; i < newSize; i++) newLocks[i] = new object();
		
		var oldLocks = _locks;
		// for security reason, lock all old buckets by their index, then migrate (lock in order to avoid deadlock)
		for (int i = 0; i < _locks.Length; i++) Monitor.Enter(oldLocks[i]);
		try
		{
			for (int i = 0; i < _buckets.Length; i++)
			{
				var node = _buckets[i];
				while (node != null)
				{
					var next = node.Next;
					int idx = (int) ((uint) (_comparer.GetHashCode(node.Key) & 0x7FFFFFFF) % (uint) newSize);
					node.Next = newBuckets[idx];
					newBuckets[idx] = node;
					node = next;
				}
			}

			// replace references (thread-safe / write-lock-guaranteed)
			_buckets = newBuckets;
			_locks = newLocks;
		}
		finally
		{
			for (int i = oldLocks.Length - 1; i >= 0; i--)
			{
				Monitor.Exit(oldLocks[i]);
			}
		}
	}
}