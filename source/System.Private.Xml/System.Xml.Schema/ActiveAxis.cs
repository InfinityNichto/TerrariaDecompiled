using System.Collections;

namespace System.Xml.Schema;

internal class ActiveAxis
{
	private int _currentDepth;

	private bool _isActive;

	private readonly Asttree _axisTree;

	private readonly ArrayList _axisStack;

	public int CurrentDepth => _currentDepth;

	internal void Reactivate()
	{
		_isActive = true;
		_currentDepth = -1;
	}

	internal ActiveAxis(Asttree axisTree)
	{
		_axisTree = axisTree;
		_currentDepth = -1;
		_axisStack = new ArrayList(axisTree.SubtreeArray.Count);
		for (int i = 0; i < axisTree.SubtreeArray.Count; i++)
		{
			AxisStack value = new AxisStack((ForwardAxis)axisTree.SubtreeArray[i], this);
			_axisStack.Add(value);
		}
		_isActive = true;
	}

	public bool MoveToStartElement(string localname, string URN)
	{
		if (!_isActive)
		{
			return false;
		}
		_currentDepth++;
		bool result = false;
		for (int i = 0; i < _axisStack.Count; i++)
		{
			AxisStack axisStack = (AxisStack)_axisStack[i];
			if (axisStack.Subtree.IsSelfAxis)
			{
				if (axisStack.Subtree.IsDss || CurrentDepth == 0)
				{
					result = true;
				}
			}
			else if (CurrentDepth != 0 && axisStack.MoveToChild(localname, URN, _currentDepth))
			{
				result = true;
			}
		}
		return result;
	}

	public virtual bool EndElement(string localname, string URN)
	{
		if (_currentDepth == 0)
		{
			_isActive = false;
			_currentDepth--;
		}
		if (!_isActive)
		{
			return false;
		}
		for (int i = 0; i < _axisStack.Count; i++)
		{
			((AxisStack)_axisStack[i]).MoveToParent(localname, URN, _currentDepth);
		}
		_currentDepth--;
		return false;
	}

	public bool MoveToAttribute(string localname, string URN)
	{
		if (!_isActive)
		{
			return false;
		}
		bool result = false;
		for (int i = 0; i < _axisStack.Count; i++)
		{
			if (((AxisStack)_axisStack[i]).MoveToAttribute(localname, URN, _currentDepth + 1))
			{
				result = true;
			}
		}
		return result;
	}
}
