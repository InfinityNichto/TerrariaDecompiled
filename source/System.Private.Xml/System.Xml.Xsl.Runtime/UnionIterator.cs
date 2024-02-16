using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct UnionIterator
{
	private enum IteratorState
	{
		InitLeft,
		NeedLeft,
		NeedRight,
		LeftIsCurrent,
		RightIsCurrent
	}

	private XmlQueryRuntime _runtime;

	private XPathNavigator _navCurr;

	private XPathNavigator _navOther;

	private IteratorState _state;

	public XPathNavigator Current => _navCurr;

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
			_navOther = nestedNavigator;
			_state = IteratorState.NeedRight;
			return SetIteratorResult.InitRightIterator;
		case IteratorState.NeedLeft:
			_navCurr = nestedNavigator;
			_state = IteratorState.LeftIsCurrent;
			break;
		case IteratorState.NeedRight:
			_navCurr = nestedNavigator;
			_state = IteratorState.RightIsCurrent;
			break;
		case IteratorState.LeftIsCurrent:
			_state = IteratorState.NeedLeft;
			return SetIteratorResult.NeedLeftNode;
		case IteratorState.RightIsCurrent:
			_state = IteratorState.NeedRight;
			return SetIteratorResult.NeedRightNode;
		}
		if (_navCurr == null)
		{
			if (_navOther == null)
			{
				return SetIteratorResult.NoMoreNodes;
			}
			Swap();
		}
		else if (_navOther != null)
		{
			int num = _runtime.ComparePosition(_navOther, _navCurr);
			if (num == 0)
			{
				if (_state == IteratorState.LeftIsCurrent)
				{
					_state = IteratorState.NeedLeft;
					return SetIteratorResult.NeedLeftNode;
				}
				_state = IteratorState.NeedRight;
				return SetIteratorResult.NeedRightNode;
			}
			if (num < 0)
			{
				Swap();
			}
		}
		return SetIteratorResult.HaveCurrentNode;
	}

	private void Swap()
	{
		XPathNavigator navCurr = _navCurr;
		_navCurr = _navOther;
		_navOther = navCurr;
		if (_state == IteratorState.LeftIsCurrent)
		{
			_state = IteratorState.RightIsCurrent;
		}
		else
		{
			_state = IteratorState.LeftIsCurrent;
		}
	}
}
