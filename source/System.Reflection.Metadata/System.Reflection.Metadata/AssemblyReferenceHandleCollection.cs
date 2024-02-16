using System.Collections;
using System.Collections.Generic;

namespace System.Reflection.Metadata;

public readonly struct AssemblyReferenceHandleCollection : IReadOnlyCollection<AssemblyReferenceHandle>, IEnumerable<AssemblyReferenceHandle>, IEnumerable
{
	public struct Enumerator : IEnumerator<AssemblyReferenceHandle>, IEnumerator, IDisposable
	{
		private readonly MetadataReader _reader;

		private int _currentRowId;

		private int _virtualRowId;

		public AssemblyReferenceHandle Current
		{
			get
			{
				if (_virtualRowId >= 0)
				{
					if (_virtualRowId == 16777216)
					{
						return default(AssemblyReferenceHandle);
					}
					return AssemblyReferenceHandle.FromVirtualIndex((AssemblyReferenceHandle.VirtualIndex)_virtualRowId);
				}
				return AssemblyReferenceHandle.FromRowId((int)((long)_currentRowId & 0xFFFFFFL));
			}
		}

		object IEnumerator.Current => Current;

		internal Enumerator(MetadataReader reader)
		{
			_reader = reader;
			_currentRowId = 0;
			_virtualRowId = -1;
		}

		public bool MoveNext()
		{
			if (_currentRowId < _reader.AssemblyRefTable.NumberOfNonVirtualRows)
			{
				_currentRowId++;
				return true;
			}
			if (_virtualRowId < _reader.AssemblyRefTable.NumberOfVirtualRows - 1)
			{
				_virtualRowId++;
				return true;
			}
			_currentRowId = 16777216;
			_virtualRowId = 16777216;
			return false;
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

	public int Count => _reader.AssemblyRefTable.NumberOfNonVirtualRows + _reader.AssemblyRefTable.NumberOfVirtualRows;

	internal AssemblyReferenceHandleCollection(MetadataReader reader)
	{
		_reader = reader;
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(_reader);
	}

	IEnumerator<AssemblyReferenceHandle> IEnumerable<AssemblyReferenceHandle>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
