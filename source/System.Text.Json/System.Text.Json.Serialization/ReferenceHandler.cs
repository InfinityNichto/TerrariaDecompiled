namespace System.Text.Json.Serialization;

public abstract class ReferenceHandler
{
	internal ReferenceHandlingStrategy HandlingStrategy = ReferenceHandlingStrategy.Preserve;

	public static ReferenceHandler Preserve { get; } = new PreserveReferenceHandler();


	public static ReferenceHandler IgnoreCycles { get; } = new IgnoreReferenceHandler();


	public abstract ReferenceResolver CreateResolver();

	internal virtual ReferenceResolver CreateResolver(bool writing)
	{
		return CreateResolver();
	}
}
public sealed class ReferenceHandler<T> : ReferenceHandler where T : ReferenceResolver, new()
{
	public override ReferenceResolver CreateResolver()
	{
		return new T();
	}
}
