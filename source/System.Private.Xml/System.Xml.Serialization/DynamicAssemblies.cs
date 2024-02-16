using System.Collections;
using System.Reflection;

namespace System.Xml.Serialization;

internal static class DynamicAssemblies
{
	private static readonly Hashtable s_nameToAssemblyMap = new Hashtable();

	private static readonly Hashtable s_assemblyToNameMap = new Hashtable();

	private static readonly Hashtable s_tableIsTypeDynamic = Hashtable.Synchronized(new Hashtable());

	internal static bool IsTypeDynamic(Type type)
	{
		object obj = s_tableIsTypeDynamic[type];
		if (obj == null)
		{
			Assembly assembly = type.Assembly;
			bool flag = assembly.IsDynamic;
			if (!flag)
			{
				if (type.IsArray)
				{
					flag = IsTypeDynamic(type.GetElementType());
				}
				else if (type.IsGenericType)
				{
					Type[] genericArguments = type.GetGenericArguments();
					if (genericArguments != null)
					{
						foreach (Type type2 in genericArguments)
						{
							if (!(type2 == null) && !type2.IsGenericParameter)
							{
								flag = IsTypeDynamic(type2);
								if (flag)
								{
									break;
								}
							}
						}
					}
				}
			}
			obj = (s_tableIsTypeDynamic[type] = flag);
		}
		return (bool)obj;
	}

	internal static bool IsTypeDynamic(Type[] arguments)
	{
		foreach (Type type in arguments)
		{
			if (IsTypeDynamic(type))
			{
				return true;
			}
		}
		return false;
	}

	internal static void Add(Assembly a)
	{
		lock (s_nameToAssemblyMap)
		{
			if (s_assemblyToNameMap[a] == null)
			{
				Assembly assembly = s_nameToAssemblyMap[a.FullName] as Assembly;
				string text = null;
				if (assembly == null)
				{
					text = a.FullName;
				}
				else if (assembly != a)
				{
					text = a.FullName + ", " + s_nameToAssemblyMap.Count;
				}
				if (text != null)
				{
					s_nameToAssemblyMap.Add(text, a);
					s_assemblyToNameMap.Add(a, text);
				}
			}
		}
	}

	internal static Assembly Get(string fullName)
	{
		if (s_nameToAssemblyMap == null)
		{
			return null;
		}
		return (Assembly)s_nameToAssemblyMap[fullName];
	}

	internal static string GetName(Assembly a)
	{
		if (s_assemblyToNameMap == null)
		{
			return null;
		}
		return (string)s_assemblyToNameMap[a];
	}
}
