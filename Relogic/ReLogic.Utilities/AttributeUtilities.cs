using System;
using System.Linq;
using System.Reflection;

namespace ReLogic.Utilities;

public static class AttributeUtilities
{
	private static class TypeAttributeCache<T, A> where A : Attribute
	{
		public static readonly A Value = typeof(T).GetAttribute<A>();
	}

	public static T GetAttribute<T>(this MethodBase method) where T : Attribute
	{
		return (T)method.GetCustomAttributes(typeof(T), inherit: false).SingleOrDefault();
	}

	public static T GetAttribute<T>(this Enum value) where T : Attribute
	{
		Type type = value.GetType();
		string name = Enum.GetName(type, value);
		return type.GetField(name).GetCustomAttributes(inherit: false).OfType<T>()
			.SingleOrDefault();
	}

	public static A GetCacheableAttribute<T, A>() where A : Attribute
	{
		return TypeAttributeCache<T, A>.Value;
	}

	public static T GetAttribute<T>(this Type type) where T : Attribute
	{
		return type.GetCustomAttributes(inherit: false).OfType<T>().SingleOrDefault();
	}
}
