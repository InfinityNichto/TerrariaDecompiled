using System.Collections;

namespace System.Xml.Schema;

public class XmlSchemaObjectEnumerator : IEnumerator
{
	private readonly IEnumerator _enumerator;

	public XmlSchemaObject Current => (XmlSchemaObject)_enumerator.Current;

	object IEnumerator.Current => _enumerator.Current;

	internal XmlSchemaObjectEnumerator(IEnumerator enumerator)
	{
		_enumerator = enumerator;
	}

	public void Reset()
	{
		_enumerator.Reset();
	}

	public bool MoveNext()
	{
		return _enumerator.MoveNext();
	}

	void IEnumerator.Reset()
	{
		_enumerator.Reset();
	}

	bool IEnumerator.MoveNext()
	{
		return _enumerator.MoveNext();
	}
}
