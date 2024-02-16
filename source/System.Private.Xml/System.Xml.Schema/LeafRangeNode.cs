namespace System.Xml.Schema;

internal sealed class LeafRangeNode : LeafNode
{
	private decimal _min;

	private readonly decimal _max;

	private BitSet _nextIteration;

	public decimal Max => _max;

	public decimal Min => _min;

	public BitSet NextIteration
	{
		get
		{
			return _nextIteration;
		}
		set
		{
			_nextIteration = value;
		}
	}

	public override bool IsRangeNode => true;

	public LeafRangeNode(decimal min, decimal max)
		: this(-1, min, max)
	{
	}

	public LeafRangeNode(int pos, decimal min, decimal max)
		: base(pos)
	{
		_min = min;
		_max = max;
	}

	public override void ExpandTree(InteriorNode parent, SymbolsDictionary symbols, Positions positions)
	{
		if (parent.LeftChild.IsNullable)
		{
			_min = default(decimal);
		}
	}
}
