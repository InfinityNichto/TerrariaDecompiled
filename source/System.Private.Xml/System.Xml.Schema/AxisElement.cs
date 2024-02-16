namespace System.Xml.Schema;

internal sealed class AxisElement
{
	internal DoubleLinkAxis curNode;

	internal int rootDepth;

	internal int curDepth;

	internal bool isMatch;

	internal DoubleLinkAxis CurNode => curNode;

	internal AxisElement(DoubleLinkAxis node, int depth)
	{
		curNode = node;
		rootDepth = (curDepth = depth);
		isMatch = false;
	}

	internal void SetDepth(int depth)
	{
		rootDepth = (curDepth = depth);
	}

	internal void MoveToParent(int depth, ForwardAxis parent)
	{
		if (depth == curDepth - 1)
		{
			if (curNode.Input == parent.RootNode && parent.IsDss)
			{
				curNode = parent.RootNode;
				rootDepth = (curDepth = -1);
			}
			else if (curNode.Input != null)
			{
				curNode = (DoubleLinkAxis)curNode.Input;
				curDepth--;
			}
		}
		else if (depth == curDepth && isMatch)
		{
			isMatch = false;
		}
	}

	internal bool MoveToChild(string name, string URN, int depth, ForwardAxis parent)
	{
		if (Asttree.IsAttribute(curNode))
		{
			return false;
		}
		if (isMatch)
		{
			isMatch = false;
		}
		if (!AxisStack.Equal(curNode.Name, curNode.Urn, name, URN))
		{
			return false;
		}
		if (curDepth == -1)
		{
			SetDepth(depth);
		}
		else if (depth > curDepth)
		{
			return false;
		}
		if (curNode == parent.TopNode)
		{
			isMatch = true;
			return true;
		}
		DoubleLinkAxis ast = (DoubleLinkAxis)curNode.Next;
		if (Asttree.IsAttribute(ast))
		{
			isMatch = true;
			return false;
		}
		curNode = ast;
		curDepth++;
		return false;
	}
}
