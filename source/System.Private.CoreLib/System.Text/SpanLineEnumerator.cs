namespace System.Text;

public ref struct SpanLineEnumerator
{
	private ReadOnlySpan<char> _remaining;

	private ReadOnlySpan<char> _current;

	private bool _isEnumeratorActive;

	public ReadOnlySpan<char> Current => _current;

	internal SpanLineEnumerator(ReadOnlySpan<char> buffer)
	{
		_remaining = buffer;
		_current = default(ReadOnlySpan<char>);
		_isEnumeratorActive = true;
	}

	public SpanLineEnumerator GetEnumerator()
	{
		return this;
	}

	public bool MoveNext()
	{
		if (!_isEnumeratorActive)
		{
			return false;
		}
		int stride;
		int num = string.IndexOfNewlineChar(_remaining, out stride);
		if (num >= 0)
		{
			_current = _remaining.Slice(0, num);
			_remaining = _remaining.Slice(num + stride);
		}
		else
		{
			_current = _remaining;
			_remaining = default(ReadOnlySpan<char>);
			_isEnumeratorActive = false;
		}
		return true;
	}
}
