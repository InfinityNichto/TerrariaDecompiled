using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal sealed class SortKey
{
	private readonly int _numKeys;

	private readonly object[] _keys;

	private readonly int _originalPosition;

	private readonly XPathNavigator _node;

	public object this[int index]
	{
		get
		{
			return _keys[index];
		}
		set
		{
			_keys[index] = value;
		}
	}

	public int NumKeys => _numKeys;

	public int OriginalPosition => _originalPosition;

	public XPathNavigator Node => _node;

	public SortKey(int numKeys, int originalPosition, XPathNavigator node)
	{
		_numKeys = numKeys;
		_keys = new object[numKeys];
		_originalPosition = originalPosition;
		_node = node;
	}
}
