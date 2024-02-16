using System.Dynamic;
using System.Reflection;

namespace System.Runtime.CompilerServices;

public static class CallSiteHelpers
{
	private static readonly Type s_knownNonDynamicMethodType = typeof(object).GetMethod("ToString").GetType();

	public static bool IsInternalFrame(MethodBase mb)
	{
		if (mb.Name == "CallSite.Target" && mb.GetType() != s_knownNonDynamicMethodType)
		{
			return true;
		}
		if (mb.DeclaringType == typeof(UpdateDelegates))
		{
			return true;
		}
		return false;
	}
}
