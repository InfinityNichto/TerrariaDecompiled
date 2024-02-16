namespace System.Xml.Schema;

internal sealed class LocatedActiveAxis : ActiveAxis
{
	private readonly int _column;

	internal bool isMatched;

	internal KeySequence Ks;

	internal int Column => _column;

	internal LocatedActiveAxis(Asttree astfield, KeySequence ks, int column)
		: base(astfield)
	{
		Ks = ks;
		_column = column;
		isMatched = false;
	}

	internal void Reactivate(KeySequence ks)
	{
		Reactivate();
		Ks = ks;
	}
}
