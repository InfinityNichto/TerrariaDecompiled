namespace System.Xml.Schema;

internal sealed class ChoiceNode : InteriorNode
{
	public override bool IsNullable
	{
		get
		{
			ChoiceNode choiceNode = this;
			SyntaxTreeNode leftChild;
			do
			{
				if (choiceNode.RightChild.IsNullable)
				{
					return true;
				}
				leftChild = choiceNode.LeftChild;
				choiceNode = leftChild as ChoiceNode;
			}
			while (choiceNode != null);
			return leftChild.IsNullable;
		}
	}

	private static void ConstructChildPos(SyntaxTreeNode child, BitSet firstpos, BitSet lastpos, BitSet[] followpos)
	{
		BitSet bitSet = new BitSet(firstpos.Count);
		BitSet bitSet2 = new BitSet(lastpos.Count);
		child.ConstructPos(bitSet, bitSet2, followpos);
		firstpos.Or(bitSet);
		lastpos.Or(bitSet2);
	}

	public override void ConstructPos(BitSet firstpos, BitSet lastpos, BitSet[] followpos)
	{
		BitSet bitSet = new BitSet(firstpos.Count);
		BitSet bitSet2 = new BitSet(lastpos.Count);
		ChoiceNode choiceNode = this;
		SyntaxTreeNode leftChild;
		do
		{
			ConstructChildPos(choiceNode.RightChild, bitSet, bitSet2, followpos);
			leftChild = choiceNode.LeftChild;
			choiceNode = leftChild as ChoiceNode;
		}
		while (choiceNode != null);
		leftChild.ConstructPos(firstpos, lastpos, followpos);
		firstpos.Or(bitSet);
		lastpos.Or(bitSet2);
	}

	public override void ExpandTree(InteriorNode parent, SymbolsDictionary symbols, Positions positions)
	{
		ExpandTreeNoRecursive(parent, symbols, positions);
	}
}
