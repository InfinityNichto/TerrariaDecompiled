namespace System.Text.Json.Serialization;

internal sealed class IgnoreReferenceHandler : ReferenceHandler
{
	public IgnoreReferenceHandler()
	{
		HandlingStrategy = ReferenceHandlingStrategy.IgnoreCycles;
	}

	public override ReferenceResolver CreateResolver()
	{
		return new IgnoreReferenceResolver();
	}
}
