using System.Collections;
using System.Collections.Generic;

namespace System.Net;

internal sealed class ListenerPrefixEnumerator : IEnumerator<string>, IEnumerator, IDisposable
{
	private readonly IEnumerator _enumerator;

	public string Current => (string)_enumerator.Current;

	object IEnumerator.Current => _enumerator.Current;

	internal ListenerPrefixEnumerator(IEnumerator enumerator)
	{
		_enumerator = enumerator;
	}

	public bool MoveNext()
	{
		return _enumerator.MoveNext();
	}

	public void Dispose()
	{
	}

	void IEnumerator.Reset()
	{
		_enumerator.Reset();
	}
}
