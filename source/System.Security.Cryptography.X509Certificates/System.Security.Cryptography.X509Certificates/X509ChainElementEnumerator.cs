using System.Collections;
using System.Collections.Generic;

namespace System.Security.Cryptography.X509Certificates;

public sealed class X509ChainElementEnumerator : IEnumerator, IEnumerator<X509ChainElement>, IDisposable
{
	private readonly X509ChainElementCollection _chainElements;

	private int _current;

	public X509ChainElement Current => _chainElements[_current];

	object IEnumerator.Current => Current;

	internal X509ChainElementEnumerator(X509ChainElementCollection chainElements)
	{
		_chainElements = chainElements;
		_current = -1;
	}

	void IDisposable.Dispose()
	{
	}

	public bool MoveNext()
	{
		if (_current == _chainElements.Count - 1)
		{
			return false;
		}
		_current++;
		return true;
	}

	public void Reset()
	{
		_current = -1;
	}
}
