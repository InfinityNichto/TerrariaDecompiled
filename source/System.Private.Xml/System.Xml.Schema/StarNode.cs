namespace System.Xml.Schema;

internal sealed class StarNode : InteriorNode
{
	public override bool IsNullable => true;

	public override void ConstructPos(BitSet firstpos, BitSet lastpos, BitSet[] followpos)
	{
		base.LeftChild.ConstructPos(firstpos, lastpos, followpos);
		for (int num = lastpos.NextSet(-1); num != -1; num = lastpos.NextSet(num))
		{
			followpos[num].Or(firstpos);
		}
	}
}
