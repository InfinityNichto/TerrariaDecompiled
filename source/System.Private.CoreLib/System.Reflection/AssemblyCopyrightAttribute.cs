namespace System.Reflection;

[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
public sealed class AssemblyCopyrightAttribute : Attribute
{
	public string Copyright { get; }

	public AssemblyCopyrightAttribute(string copyright)
	{
		Copyright = copyright;
	}
}
