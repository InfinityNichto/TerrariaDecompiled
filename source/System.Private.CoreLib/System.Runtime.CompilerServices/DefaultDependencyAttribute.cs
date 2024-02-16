namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class DefaultDependencyAttribute : Attribute
{
	public LoadHint LoadHint { get; }

	public DefaultDependencyAttribute(LoadHint loadHintArgument)
	{
		LoadHint = loadHintArgument;
	}
}
