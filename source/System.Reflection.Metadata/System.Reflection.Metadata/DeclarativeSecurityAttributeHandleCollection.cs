using System.Collections;
using System.Collections.Generic;

namespace System.Reflection.Metadata;

public readonly struct DeclarativeSecurityAttributeHandleCollection : IReadOnlyCollection<DeclarativeSecurityAttributeHandle>, IEnumerable<DeclarativeSecurityAttributeHandle>, IEnumerable
{
	public struct Enumerator : IEnumerator<DeclarativeSecurityAttributeHandle>, IEnumerator, IDisposable
	{
		private readonly MetadataReader _reader;

		private readonly int _lastRowId;

		private int _currentRowId;

		public DeclarativeSecurityAttributeHandle Current => DeclarativeSecurityAttributeHandle.FromRowId((int)((long)_currentRowId & 0xFFFFFFL));

		object IEnumerator.Current => Current;

		internal Enumerator(MetadataReader reader, int firstRowId, int lastRowId)
		{
			_reader = reader;
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

	private readonly MetadataReader _reader;

	private readonly int _firstRowId;

	private readonly int _lastRowId;

	public int Count => _lastRowId - _firstRowId + 1;

	internal DeclarativeSecurityAttributeHandleCollection(MetadataReader reader)
	{
		_reader = reader;
		_firstRowId = 1;
		_lastRowId = reader.DeclSecurityTable.NumberOfRows;
	}

	internal DeclarativeSecurityAttributeHandleCollection(MetadataReader reader, EntityHandle handle)
	{
		_reader = reader;
		reader.DeclSecurityTable.GetAttributeRange(handle, out _firstRowId, out _lastRowId);
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(_reader, _firstRowId, _lastRowId);
	}

	IEnumerator<DeclarativeSecurityAttributeHandle> IEnumerable<DeclarativeSecurityAttributeHandle>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
