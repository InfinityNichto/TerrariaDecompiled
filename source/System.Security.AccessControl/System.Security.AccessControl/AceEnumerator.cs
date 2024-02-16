using System.Collections;

namespace System.Security.AccessControl;

public sealed class AceEnumerator : IEnumerator
{
	private int _current;

	private readonly GenericAcl _acl;

	object IEnumerator.Current
	{
		get
		{
			if (_current == -1 || _current >= _acl.Count)
			{
				throw new InvalidOperationException(System.SR.Arg_InvalidOperationException);
			}
			return _acl[_current];
		}
	}

	public GenericAce Current => (GenericAce)((IEnumerator)this).Current;

	internal AceEnumerator(GenericAcl collection)
	{
		if (collection == null)
		{
			throw new ArgumentNullException("collection");
		}
		_acl = collection;
		Reset();
	}

	public bool MoveNext()
	{
		_current++;
		return _current < _acl.Count;
	}

	public void Reset()
	{
		_current = -1;
	}
}
