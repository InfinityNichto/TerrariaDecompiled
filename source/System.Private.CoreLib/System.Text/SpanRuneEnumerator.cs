namespace System.Text;

public ref struct SpanRuneEnumerator
{
	private ReadOnlySpan<char> _remaining;

	private Rune _current;

	public Rune Current => _current;

	internal SpanRuneEnumerator(ReadOnlySpan<char> buffer)
	{
		_remaining = buffer;
		_current = default(Rune);
	}

	public SpanRuneEnumerator GetEnumerator()
	{
		return this;
	}

	public bool MoveNext()
	{
		if (_remaining.IsEmpty)
		{
			_current = default(Rune);
			return false;
		}
		int num = Rune.ReadFirstRuneFromUtf16Buffer(_remaining);
		if (num < 0)
		{
			num = Rune.ReplacementChar.Value;
		}
		_current = Rune.UnsafeCreate((uint)num);
		_remaining = _remaining.Slice(_current.Utf16SequenceLength);
		return true;
	}
}
