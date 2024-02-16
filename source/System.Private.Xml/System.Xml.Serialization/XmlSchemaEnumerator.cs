using System.Collections;
using System.Collections.Generic;
using System.Xml.Schema;

namespace System.Xml.Serialization;

public class XmlSchemaEnumerator : IEnumerator<XmlSchema>, IEnumerator, IDisposable
{
	private readonly XmlSchemas _list;

	private int _idx;

	private readonly int _end;

	public XmlSchema Current => _list[_idx];

	object IEnumerator.Current => _list[_idx];

	public XmlSchemaEnumerator(XmlSchemas list)
	{
		_list = list;
		_idx = -1;
		_end = list.Count - 1;
	}

	public void Dispose()
	{
	}

	public bool MoveNext()
	{
		if (_idx >= _end)
		{
			return false;
		}
		_idx++;
		return true;
	}

	void IEnumerator.Reset()
	{
		_idx = -1;
	}
}
