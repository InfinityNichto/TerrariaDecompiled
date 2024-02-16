using System.Collections;

namespace System.Xml.Schema;

public sealed class XmlSchemaCollectionEnumerator : IEnumerator
{
	private readonly IDictionaryEnumerator _enumerator;

	object? IEnumerator.Current => Current;

	public XmlSchema? Current => ((XmlSchemaCollectionNode)_enumerator.Value)?.Schema;

	internal XmlSchemaCollectionNode? CurrentNode => (XmlSchemaCollectionNode)_enumerator.Value;

	internal XmlSchemaCollectionEnumerator(Hashtable collection)
	{
		_enumerator = collection.GetEnumerator();
	}

	void IEnumerator.Reset()
	{
		_enumerator.Reset();
	}

	bool IEnumerator.MoveNext()
	{
		return _enumerator.MoveNext();
	}

	public bool MoveNext()
	{
		return _enumerator.MoveNext();
	}
}
