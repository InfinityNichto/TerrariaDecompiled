namespace System.Xml.Schema;

internal abstract class SyntaxTreeNode
{
	public abstract bool IsNullable { get; }

	public virtual bool IsRangeNode => false;

	public abstract void ExpandTree(InteriorNode parent, SymbolsDictionary symbols, Positions positions);

	public abstract void ConstructPos(BitSet firstpos, BitSet lastpos, BitSet[] followpos);
}
