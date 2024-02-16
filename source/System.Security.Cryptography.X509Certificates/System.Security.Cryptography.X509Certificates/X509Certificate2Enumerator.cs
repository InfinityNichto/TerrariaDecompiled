using System.Collections;
using System.Collections.Generic;

namespace System.Security.Cryptography.X509Certificates;

public sealed class X509Certificate2Enumerator : IEnumerator, IEnumerator<X509Certificate2>, IDisposable
{
	private readonly IEnumerator _enumerator;

	public X509Certificate2 Current => (X509Certificate2)_enumerator.Current;

	object IEnumerator.Current => Current;

	internal X509Certificate2Enumerator(X509Certificate2Collection collection)
	{
		_enumerator = ((IEnumerable)collection).GetEnumerator();
	}

	public bool MoveNext()
	{
		return _enumerator.MoveNext();
	}

	bool IEnumerator.MoveNext()
	{
		return MoveNext();
	}

	public void Reset()
	{
		_enumerator.Reset();
	}

	void IEnumerator.Reset()
	{
		Reset();
	}

	void IDisposable.Dispose()
	{
	}
}
