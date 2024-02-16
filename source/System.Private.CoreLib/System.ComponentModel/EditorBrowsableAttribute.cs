using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate)]
public sealed class EditorBrowsableAttribute : Attribute
{
	public EditorBrowsableState State { get; }

	public EditorBrowsableAttribute(EditorBrowsableState state)
	{
		State = state;
	}

	public EditorBrowsableAttribute()
		: this(EditorBrowsableState.Always)
	{
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj == this)
		{
			return true;
		}
		if (obj is EditorBrowsableAttribute editorBrowsableAttribute)
		{
			return editorBrowsableAttribute.State == State;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}
}
