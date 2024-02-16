namespace System.Xml.Schema;

internal sealed class UpaException : Exception
{
	private readonly object _particle1;

	private readonly object _particle2;

	public object Particle1 => _particle1;

	public object Particle2 => _particle2;

	public UpaException(object particle1, object particle2)
	{
		_particle1 = particle1;
		_particle2 = particle2;
	}
}
