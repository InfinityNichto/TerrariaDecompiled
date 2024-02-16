using System.Collections;

namespace System.Xml;

internal sealed class XmlElementListEnumerator : IEnumerator
{
	private readonly XmlElementList _list;

	private XmlNode _curElem;

	private int _changeCount;

	public object Current => _curElem;

	public XmlElementListEnumerator(XmlElementList list)
	{
		_list = list;
		_curElem = null;
		_changeCount = list.ChangeCount;
	}

	public bool MoveNext()
	{
		if (_list.ChangeCount != _changeCount)
		{
			throw new InvalidOperationException(System.SR.Xdom_Enum_ElementList);
		}
		_curElem = _list.GetNextNode(_curElem);
		return _curElem != null;
	}

	public void Reset()
	{
		_curElem = null;
		_changeCount = _list.ChangeCount;
	}
}
