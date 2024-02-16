using System.Collections;

namespace System.Xml;

internal sealed class XmlNodeListEnumerator : IEnumerator
{
	private readonly XPathNodeList _list;

	private int _index;

	private bool _valid;

	public object Current
	{
		get
		{
			if (_valid)
			{
				return _list[_index];
			}
			return null;
		}
	}

	public XmlNodeListEnumerator(XPathNodeList list)
	{
		_list = list;
		_index = -1;
		_valid = false;
	}

	public void Reset()
	{
		_index = -1;
	}

	public bool MoveNext()
	{
		_index++;
		int num = _list.ReadUntil(_index + 1);
		if (num - 1 < _index)
		{
			return false;
		}
		_valid = _list[_index] != null;
		return _valid;
	}
}
