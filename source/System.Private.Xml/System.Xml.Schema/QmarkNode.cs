namespace System.Xml.Schema;

internal sealed class QmarkNode : InteriorNode
{
	public override bool IsNullable => true;

	public override void ConstructPos(BitSet firstpos, BitSet lastpos, BitSet[] followpos)
	{
		base.LeftChild.ConstructPos(firstpos, lastpos, followpos);
	}
}
