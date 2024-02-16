using System.Collections;

namespace System.Xml.Schema;

internal sealed class Positions
{
	private readonly ArrayList _positions = new ArrayList();

	public Position this[int pos] => (Position)_positions[pos];

	public int Count => _positions.Count;

	public int Add(int symbol, object particle)
	{
		return _positions.Add(new Position(symbol, particle));
	}
}
