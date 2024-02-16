using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
[Obsolete("FilterUIHintAttribute has been deprecated and is not supported.")]
public sealed class FilterUIHintAttribute : Attribute
{
	private readonly UIHintAttribute.UIHintImplementation _implementation;

	public string FilterUIHint => _implementation.UIHint;

	public string? PresentationLayer => _implementation.PresentationLayer;

	public IDictionary<string, object?> ControlParameters => _implementation.ControlParameters;

	public FilterUIHintAttribute(string filterUIHint)
		: this(filterUIHint, null, Array.Empty<object>())
	{
	}

	public FilterUIHintAttribute(string filterUIHint, string? presentationLayer)
		: this(filterUIHint, presentationLayer, Array.Empty<object>())
	{
	}

	public FilterUIHintAttribute(string filterUIHint, string? presentationLayer, params object?[] controlParameters)
	{
		_implementation = new UIHintAttribute.UIHintImplementation(filterUIHint, presentationLayer, controlParameters);
	}

	public override int GetHashCode()
	{
		return _implementation.GetHashCode();
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is FilterUIHintAttribute filterUIHintAttribute)
		{
			return _implementation.Equals(filterUIHintAttribute._implementation);
		}
		return false;
	}
}
