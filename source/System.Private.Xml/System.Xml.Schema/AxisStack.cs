using System.Collections;

namespace System.Xml.Schema;

internal sealed class AxisStack
{
	private readonly ArrayList _stack;

	private readonly ForwardAxis _subtree;

	private readonly ActiveAxis _parent;

	internal ForwardAxis Subtree => _subtree;

	internal int Length => _stack.Count;

	public AxisStack(ForwardAxis faxis, ActiveAxis parent)
	{
		_subtree = faxis;
		_stack = new ArrayList();
		_parent = parent;
		if (!faxis.IsDss)
		{
			Push(1);
		}
	}

	internal void Push(int depth)
	{
		AxisElement value = new AxisElement(_subtree.RootNode, depth);
		_stack.Add(value);
	}

	internal void Pop()
	{
		_stack.RemoveAt(Length - 1);
	}

	internal static bool Equal(string thisname, string thisURN, string name, string URN)
	{
		if (thisURN == null)
		{
			if (URN != null && URN.Length != 0)
			{
				return false;
			}
		}
		else if (thisURN.Length != 0 && thisURN != URN)
		{
			return false;
		}
		if (thisname.Length != 0 && thisname != name)
		{
			return false;
		}
		return true;
	}

	internal void MoveToParent(string name, string URN, int depth)
	{
		if (!_subtree.IsSelfAxis)
		{
			for (int i = 0; i < _stack.Count; i++)
			{
				((AxisElement)_stack[i]).MoveToParent(depth, _subtree);
			}
			if (_subtree.IsDss && Equal(_subtree.RootNode.Name, _subtree.RootNode.Urn, name, URN))
			{
				Pop();
			}
		}
	}

	internal bool MoveToChild(string name, string URN, int depth)
	{
		bool result = false;
		if (_subtree.IsDss && Equal(_subtree.RootNode.Name, _subtree.RootNode.Urn, name, URN))
		{
			Push(-1);
		}
		for (int i = 0; i < _stack.Count; i++)
		{
			if (((AxisElement)_stack[i]).MoveToChild(name, URN, depth, _subtree))
			{
				result = true;
			}
		}
		return result;
	}

	internal bool MoveToAttribute(string name, string URN, int depth)
	{
		if (!_subtree.IsAttribute)
		{
			return false;
		}
		if (!Equal(_subtree.TopNode.Name, _subtree.TopNode.Urn, name, URN))
		{
			return false;
		}
		bool result = false;
		if (_subtree.TopNode.Input == null)
		{
			if (!_subtree.IsDss)
			{
				return depth == 1;
			}
			return true;
		}
		for (int i = 0; i < _stack.Count; i++)
		{
			AxisElement axisElement = (AxisElement)_stack[i];
			if (axisElement.isMatch && axisElement.CurNode == _subtree.TopNode.Input)
			{
				result = true;
			}
		}
		return result;
	}
}
