using System.Collections;
using System.Collections.Generic;

namespace System.Security.Principal;

internal sealed class IdentityReferenceEnumerator : IEnumerator<IdentityReference>, IEnumerator, IDisposable
{
	private int _current;

	private readonly IdentityReferenceCollection _collection;

	object IEnumerator.Current => Current;

	public IdentityReference Current => _collection.Identities[_current];

	internal IdentityReferenceEnumerator(IdentityReferenceCollection collection)
	{
		if (collection == null)
		{
			throw new ArgumentNullException("collection");
		}
		_collection = collection;
		_current = -1;
	}

	public bool MoveNext()
	{
		_current++;
		return _current < _collection.Count;
	}

	public void Reset()
	{
		_current = -1;
	}

	public void Dispose()
	{
	}
}
