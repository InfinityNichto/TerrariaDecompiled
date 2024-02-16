using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Text.Json.Serialization;

internal struct ReferenceEqualsWrapper : IEquatable<ReferenceEqualsWrapper>
{
	private object _object;

	public ReferenceEqualsWrapper(object obj)
	{
		_object = obj;
	}

	public override bool Equals([NotNullWhen(true)] object obj)
	{
		if (obj is ReferenceEqualsWrapper obj2)
		{
			return Equals(obj2);
		}
		return false;
	}

	public bool Equals(ReferenceEqualsWrapper obj)
	{
		return _object == obj._object;
	}

	public override int GetHashCode()
	{
		return RuntimeHelpers.GetHashCode(_object);
	}
}
