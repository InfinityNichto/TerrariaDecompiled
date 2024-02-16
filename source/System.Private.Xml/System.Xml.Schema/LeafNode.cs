namespace System.Xml.Schema;

internal class LeafNode : SyntaxTreeNode
{
	private int _pos;

	public int Pos
	{
		get
		{
			return _pos;
		}
		set
		{
			_pos = value;
		}
	}

	public override bool IsNullable => false;

	public LeafNode(int pos)
	{
		_pos = pos;
	}

	public override void ExpandTree(InteriorNode parent, SymbolsDictionary symbols, Positions positions)
	{
	}

	public override void ConstructPos(BitSet firstpos, BitSet lastpos, BitSet[] followpos)
	{
		firstpos.Set(_pos);
		lastpos.Set(_pos);
	}
}
