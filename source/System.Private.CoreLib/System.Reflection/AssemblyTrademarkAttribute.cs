namespace System.Reflection;

[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
public sealed class AssemblyTrademarkAttribute : Attribute
{
	public string Trademark { get; }

	public AssemblyTrademarkAttribute(string trademark)
	{
		Trademark = trademark;
	}
}
