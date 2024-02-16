namespace System.Runtime.Serialization;

internal sealed class SurrogateKey
{
	internal readonly Type _type;

	internal readonly StreamingContext _context;

	internal SurrogateKey(Type type, StreamingContext context)
	{
		_type = type;
		_context = context;
	}

	public override int GetHashCode()
	{
		return _type.GetHashCode();
	}
}
