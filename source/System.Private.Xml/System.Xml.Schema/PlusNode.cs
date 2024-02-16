namespace System.Xml.Schema;

internal sealed class PlusNode : InteriorNode
{
	public override bool IsNullable => base.LeftChild.IsNullable;

	public override void ConstructPos(BitSet firstpos, BitSet lastpos, BitSet[] followpos)
	{
		base.LeftChild.ConstructPos(firstpos, lastpos, followpos);
		for (int num = lastpos.NextSet(-1); num != -1; num = lastpos.NextSet(num))
		{
			followpos[num].Or(firstpos);
		}
	}
}
