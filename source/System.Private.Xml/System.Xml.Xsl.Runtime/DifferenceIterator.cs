using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct DifferenceIterator
{
	private enum IteratorState
	{
		InitLeft,
		NeedLeft,
		NeedRight,
		NeedLeftAndRight,
		HaveCurrent
	}

	private XmlQueryRuntime _runtime;

	private XPathNavigator _navLeft;

	private XPathNavigator _navRight;

	private IteratorState _state;

	public XPathNavigator Current => _navLeft;

	public void Create(XmlQueryRuntime runtime)
	{
		_runtime = runtime;
		_state = IteratorState.InitLeft;
	}

	public SetIteratorResult MoveNext(XPathNavigator nestedNavigator)
	{
		switch (_state)
		{
		case IteratorState.InitLeft:
			_navLeft = nestedNavigator;
			_state = IteratorState.NeedRight;
			return SetIteratorResult.InitRightIterator;
		case IteratorState.NeedLeft:
			_navLeft = nestedNavigator;
			break;
		case IteratorState.NeedRight:
			_navRight = nestedNavigator;
			break;
		case IteratorState.NeedLeftAndRight:
			_navLeft = nestedNavigator;
			_state = IteratorState.NeedRight;
			return SetIteratorResult.NeedRightNode;
		case IteratorState.HaveCurrent:
			_state = IteratorState.NeedLeft;
			return SetIteratorResult.NeedLeftNode;
		}
		if (_navLeft == null)
		{
			return SetIteratorResult.NoMoreNodes;
		}
		if (_navRight != null)
		{
			int num = _runtime.ComparePosition(_navLeft, _navRight);
			if (num == 0)
			{
				_state = IteratorState.NeedLeftAndRight;
				return SetIteratorResult.NeedLeftNode;
			}
			if (num > 0)
			{
				_state = IteratorState.NeedRight;
				return SetIteratorResult.NeedRightNode;
			}
		}
		_state = IteratorState.HaveCurrent;
		return SetIteratorResult.HaveCurrentNode;
	}
}
