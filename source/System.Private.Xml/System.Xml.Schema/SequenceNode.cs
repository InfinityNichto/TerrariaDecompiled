using System.Collections.Generic;

namespace System.Xml.Schema;

internal sealed class SequenceNode : InteriorNode
{
	private struct SequenceConstructPosContext
	{
		public SequenceNode this_;

		public BitSet firstpos;

		public BitSet lastpos;

		public BitSet lastposLeft;

		public BitSet firstposRight;

		public SequenceConstructPosContext(SequenceNode node, BitSet firstpos, BitSet lastpos)
		{
			this_ = node;
			this.firstpos = firstpos;
			this.lastpos = lastpos;
			lastposLeft = null;
			firstposRight = null;
		}
	}

	public override bool IsNullable
	{
		get
		{
			SequenceNode sequenceNode = this;
			SyntaxTreeNode leftChild;
			do
			{
				if (sequenceNode.RightChild.IsRangeNode && ((LeafRangeNode)sequenceNode.RightChild).Min == 0m)
				{
					return true;
				}
				if (!sequenceNode.RightChild.IsNullable && !sequenceNode.RightChild.IsRangeNode)
				{
					return false;
				}
				leftChild = sequenceNode.LeftChild;
				sequenceNode = leftChild as SequenceNode;
			}
			while (sequenceNode != null);
			return leftChild.IsNullable;
		}
	}

	public override void ConstructPos(BitSet firstpos, BitSet lastpos, BitSet[] followpos)
	{
		Stack<SequenceConstructPosContext> stack = new Stack<SequenceConstructPosContext>();
		SequenceConstructPosContext item = new SequenceConstructPosContext(this, firstpos, lastpos);
		SequenceNode this_;
		while (true)
		{
			this_ = item.this_;
			item.lastposLeft = new BitSet(lastpos.Count);
			if (!(this_.LeftChild is SequenceNode))
			{
				break;
			}
			stack.Push(item);
			item = new SequenceConstructPosContext((SequenceNode)this_.LeftChild, item.firstpos, item.lastposLeft);
		}
		this_.LeftChild.ConstructPos(item.firstpos, item.lastposLeft, followpos);
		while (true)
		{
			item.firstposRight = new BitSet(firstpos.Count);
			this_.RightChild.ConstructPos(item.firstposRight, item.lastpos, followpos);
			if (this_.LeftChild.IsNullable && !this_.RightChild.IsRangeNode)
			{
				item.firstpos.Or(item.firstposRight);
			}
			if (this_.RightChild.IsNullable)
			{
				item.lastpos.Or(item.lastposLeft);
			}
			for (int num = item.lastposLeft.NextSet(-1); num != -1; num = item.lastposLeft.NextSet(num))
			{
				followpos[num].Or(item.firstposRight);
			}
			if (this_.RightChild.IsRangeNode)
			{
				((LeafRangeNode)this_.RightChild).NextIteration = item.firstpos.Clone();
			}
			if (stack.Count != 0)
			{
				item = stack.Pop();
				this_ = item.this_;
				continue;
			}
			break;
		}
	}

	public override void ExpandTree(InteriorNode parent, SymbolsDictionary symbols, Positions positions)
	{
		ExpandTreeNoRecursive(parent, symbols, positions);
	}
}
