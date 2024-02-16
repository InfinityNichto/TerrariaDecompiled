using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Metadata;

internal static class RuntimeTypeMetadataUpdateHandler
{
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Clearing the caches on a Type isn't affected if a Type is trimmed, or has any of its members trimmed.")]
	public static void ClearCache(Type[] types)
	{
		if (RequiresClearingAllTypes(types))
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly in assemblies)
			{
				if (SkipAssembly(assembly))
				{
					continue;
				}
				try
				{
					Type[] types2 = assembly.GetTypes();
					foreach (Type type in types2)
					{
						ClearCache(type);
					}
				}
				catch (ReflectionTypeLoadException)
				{
				}
			}
		}
		else
		{
			foreach (Type type2 in types)
			{
				ClearCache(type2);
			}
		}
	}

	private static bool SkipAssembly(Assembly assembly)
	{
		return typeof(object).Assembly == assembly;
	}

	private static void ClearCache(Type type)
	{
		(type as RuntimeType)?.ClearCache();
	}

	private static bool RequiresClearingAllTypes([NotNullWhen(false)] Type[] types)
	{
		if (types == null)
		{
			return true;
		}
		foreach (Type type in types)
		{
			if (!type.IsSealed)
			{
				return true;
			}
		}
		return false;
	}
}
