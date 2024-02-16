using System.Collections;
using System.Collections.Generic;

namespace System.Security.Cryptography.X509Certificates;

public sealed class X509ExtensionEnumerator : IEnumerator, IEnumerator<X509Extension>, IDisposable
{
	private readonly X509ExtensionCollection _extensions;

	private int _current;

	public X509Extension Current => _extensions[_current];

	object IEnumerator.Current => Current;

	internal X509ExtensionEnumerator(X509ExtensionCollection extensions)
	{
		_extensions = extensions;
		_current = -1;
	}

	public bool MoveNext()
	{
		if (_current == _extensions.Count - 1)
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

	void IDisposable.Dispose()
	{
	}
}
