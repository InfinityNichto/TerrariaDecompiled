namespace System.Reflection;

[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
public sealed class AssemblyDelaySignAttribute : Attribute
{
	public bool DelaySign { get; }

	public AssemblyDelaySignAttribute(bool delaySign)
	{
		DelaySign = delaySign;
	}
}
