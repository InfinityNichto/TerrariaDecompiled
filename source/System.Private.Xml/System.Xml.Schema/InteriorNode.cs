using System.Collections.Generic;

namespace System.Xml.Schema;

internal abstract class InteriorNode : SyntaxTreeNode
{
	private SyntaxTreeNode _leftChild;

	private SyntaxTreeNode _rightChild;

	public SyntaxTreeNode LeftChild
	{
		get
		{
			return _leftChild;
		}
		set
		{
			_leftChild = value;
		}
	}

	public SyntaxTreeNode RightChild
	{
		get
		{
			return _rightChild;
		}
		set
		{
			_rightChild = value;
		}
	}

	protected void ExpandTreeNoRecursive(InteriorNode parent, SymbolsDictionary symbols, Positions positions)
	{
		Stack<InteriorNode> stack = new Stack<InteriorNode>();
		InteriorNode interiorNode = this;
		while (interiorNode._leftChild is ChoiceNode || interiorNode._leftChild is SequenceNode)
		{
			stack.Push(interiorNode);
			interiorNode = (InteriorNode)interiorNode._leftChild;
		}
		interiorNode._leftChild.ExpandTree(interiorNode, symbols, positions);
		while (true)
		{
			if (interiorNode._rightChild != null)
			{
				interiorNode._rightChild.ExpandTree(interiorNode, symbols, positions);
			}
			if (stack.Count != 0)
			{
				interiorNode = stack.Pop();
				continue;
			}
			break;
		}
	}

	public override void ExpandTree(InteriorNode parent, SymbolsDictionary symbols, Positions positions)
	{
		_leftChild.ExpandTree(this, symbols, positions);
		if (_rightChild != null)
		{
			_rightChild.ExpandTree(this, symbols, positions);
		}
	}
}
