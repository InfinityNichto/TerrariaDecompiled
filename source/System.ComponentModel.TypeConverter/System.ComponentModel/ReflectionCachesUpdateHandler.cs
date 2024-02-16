using System.Reflection;

namespace System.ComponentModel;

internal static class ReflectionCachesUpdateHandler
{
	public static void ClearCache(Type[] types)
	{
		ReflectTypeDescriptionProvider.ClearReflectionCaches();
		if (types != null)
		{
			foreach (Type type in types)
			{
				TypeDescriptor.Refresh(type);
			}
			return;
		}
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			TypeDescriptor.Refresh(assembly);
		}
	}
}
