using System.Collections;
using System.Collections.Generic;

namespace System.Reflection.Metadata;

public readonly struct GenericParameterHandleCollection : IReadOnlyList<GenericParameterHandle>, IEnumerable<GenericParameterHandle>, IEnumerable, IReadOnlyCollection<GenericParameterHandle>
{
	public struct Enumerator : IEnumerator<GenericParameterHandle>, IEnumerator, IDisposable
	{
		private readonly int _lastRowId;

		private int _currentRowId;

		public GenericParameterHandle Current => GenericParameterHandle.FromRowId((int)((long)_currentRowId & 0xFFFFFFL));

		object IEnumerator.Current => Current;

		internal Enumerator(int firstRowId, int lastRowId)
		{
			_currentRowId = firstRowId - 1;
			_lastRowId = lastRowId;
		}

		public bool MoveNext()
		{
			if (_currentRowId >= _lastRowId)
			{
				_currentRowId = 16777216;
				return false;
			}
			_currentRowId++;
			return true;
		}

		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		void IDisposable.Dispose()
		{
		}
	}

	private readonly int _firstRowId;

	private readonly ushort _count;

	public int Count => _count;

	public GenericParameterHandle this[int index]
	{
		get
		{
			if (index < 0 || index >= _count)
			{
				Throw.IndexOutOfRange();
			}
			return GenericParameterHandle.FromRowId(_firstRowId + index);
		}
	}

	internal GenericParameterHandleCollection(int firstRowId, ushort count)
	{
		_firstRowId = firstRowId;
		_count = count;
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(_firstRowId, _firstRowId + _count - 1);
	}

	IEnumerator<GenericParameterHandle> IEnumerable<GenericParameterHandle>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
