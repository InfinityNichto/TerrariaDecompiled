namespace System.Diagnostics;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class DebuggerBrowsableAttribute : Attribute
{
	public DebuggerBrowsableState State { get; }

	public DebuggerBrowsableAttribute(DebuggerBrowsableState state)
	{
		if (state < DebuggerBrowsableState.Never || state > DebuggerBrowsableState.RootHidden)
		{
			throw new ArgumentOutOfRangeException("state");
		}
		State = state;
	}
}
