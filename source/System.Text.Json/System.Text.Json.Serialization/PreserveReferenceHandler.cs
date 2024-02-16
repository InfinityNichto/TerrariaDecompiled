namespace System.Text.Json.Serialization;

internal sealed class PreserveReferenceHandler : ReferenceHandler
{
	public override ReferenceResolver CreateResolver()
	{
		throw new InvalidOperationException();
	}

	internal override ReferenceResolver CreateResolver(bool writing)
	{
		return new PreserveReferenceResolver(writing);
	}
}
