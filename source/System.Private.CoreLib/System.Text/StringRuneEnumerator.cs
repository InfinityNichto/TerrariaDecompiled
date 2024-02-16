using System.Collections;
using System.Collections.Generic;

namespace System.Text;

public struct StringRuneEnumerator : IEnumerable<Rune>, IEnumerable, IEnumerator<Rune>, IDisposable, IEnumerator
{
	private readonly string _string;

	private Rune _current;

	private int _nextIndex;

	public Rune Current => _current;

	object? IEnumerator.Current => _current;

	internal StringRuneEnumerator(string value)
	{
		_string = value;
		_current = default(Rune);
		_nextIndex = 0;
	}

	public StringRuneEnumerator GetEnumerator()
	{
		return this;
	}

	public bool MoveNext()
	{
		if ((uint)_nextIndex >= _string.Length)
		{
			_current = default(Rune);
			return false;
		}
		if (!Rune.TryGetRuneAt(_string, _nextIndex, out _current))
		{
			_current = Rune.ReplacementChar;
		}
		_nextIndex += _current.Utf16SequenceLength;
		return true;
	}

	void IDisposable.Dispose()
	{
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return this;
	}

	IEnumerator<Rune> IEnumerable<Rune>.GetEnumerator()
	{
		return this;
	}

	void IEnumerator.Reset()
	{
		_current = default(Rune);
		_nextIndex = 0;
	}
}
