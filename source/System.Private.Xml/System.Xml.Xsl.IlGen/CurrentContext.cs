using System.Reflection;
using System.Reflection.Emit;

namespace System.Xml.Xsl.IlGen;

internal sealed class CurrentContext
{
	public readonly LocalBuilder Local;

	public readonly MethodInfo CurrentMethod;

	public CurrentContext(LocalBuilder local, MethodInfo currentMethod)
	{
		Local = local;
		CurrentMethod = currentMethod;
	}
}
