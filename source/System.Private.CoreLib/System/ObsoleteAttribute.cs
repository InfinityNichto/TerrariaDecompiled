namespace System;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate, Inherited = false)]
public sealed class ObsoleteAttribute : Attribute
{
	public string? Message { get; }

	public bool IsError { get; }

	public string? DiagnosticId { get; set; }

	public string? UrlFormat { get; set; }

	public ObsoleteAttribute()
	{
	}

	public ObsoleteAttribute(string? message)
	{
		Message = message;
	}

	public ObsoleteAttribute(string? message, bool error)
	{
		Message = message;
		IsError = error;
	}
}
