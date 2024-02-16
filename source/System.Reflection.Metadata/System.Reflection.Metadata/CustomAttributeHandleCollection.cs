using System.Collections;
using System.Collections.Generic;

namespace System.Reflection.Metadata;

public readonly struct CustomAttributeHandleCollection : IReadOnlyCollection<CustomAttributeHandle>, IEnumerable<CustomAttributeHandle>, IEnumerable
{
	public struct Enumerator : IEnumerator<CustomAttributeHandle>, IEnumerator, IDisposable
	{
		private readonly MetadataReader _reader;

		private readonly int _lastRowId;

		private int _currentRowId;

		public CustomAttributeHandle Current
		{
			get
			{
				if (_reader.CustomAttributeTable.PtrTable != null)
				{
					return GetCurrentCustomAttributeIndirect();
				}
				return CustomAttributeHandle.FromRowId((int)((long)_currentRowId & 0xFFFFFFL));
			}
		}

		object IEnumerator.Current => Current;

		internal Enumerator(MetadataReader reader, int firstRowId, int lastRowId)
		{
			_reader = reader;
			_currentRowId = firstRowId - 1;
			_lastRowId = lastRowId;
		}

		private CustomAttributeHandle GetCurrentCustomAttributeIndirect()
		{
			return CustomAttributeHandle.FromRowId(_reader.CustomAttributeTable.PtrTable[(_currentRowId & 0xFFFFFF) - 1]);
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

	private readonly MetadataReader _reader;

	private readonly int _firstRowId;

	private readonly int _lastRowId;

	public int Count => _lastRowId - _firstRowId + 1;

	internal CustomAttributeHandleCollection(MetadataReader reader)
	{
		_reader = reader;
		_firstRowId = 1;
		_lastRowId = reader.CustomAttributeTable.NumberOfRows;
	}

	internal CustomAttributeHandleCollection(MetadataReader reader, EntityHandle handle)
	{
		_reader = reader;
		reader.CustomAttributeTable.GetAttributeRange(handle, out _firstRowId, out _lastRowId);
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(_reader, _firstRowId, _lastRowId);
	}

	IEnumerator<CustomAttributeHandle> IEnumerable<CustomAttributeHandle>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
