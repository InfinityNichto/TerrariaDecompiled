using System.Collections;
using System.Collections.Generic;

namespace System.Reflection.Metadata;

public readonly struct ManifestResourceHandleCollection : IReadOnlyCollection<ManifestResourceHandle>, IEnumerable<ManifestResourceHandle>, IEnumerable
{
	public struct Enumerator : IEnumerator<ManifestResourceHandle>, IEnumerator, IDisposable
	{
		private readonly int _lastRowId;

		private int _currentRowId;

		public ManifestResourceHandle Current => ManifestResourceHandle.FromRowId((int)((long)_currentRowId & 0xFFFFFFL));

		object IEnumerator.Current => Current;

		internal Enumerator(int lastRowId)
		{
			_lastRowId = lastRowId;
			_currentRowId = 0;
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

	private readonly int _lastRowId;

	public int Count => _lastRowId;

	internal ManifestResourceHandleCollection(int lastRowId)
	{
		_lastRowId = lastRowId;
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(_lastRowId);
	}

	IEnumerator<ManifestResourceHandle> IEnumerable<ManifestResourceHandle>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
