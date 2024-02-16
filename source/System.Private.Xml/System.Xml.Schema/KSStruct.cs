namespace System.Xml.Schema;

internal sealed class KSStruct
{
	public int depth;

	public KeySequence ks;

	public LocatedActiveAxis[] fields;

	public KSStruct(KeySequence ks, int dim)
	{
		this.ks = ks;
		fields = new LocatedActiveAxis[dim];
	}
}
