namespace System.Reflection;

public static class PropertyInfoExtensions
{
	public static MethodInfo[] GetAccessors(this PropertyInfo property)
	{
		ArgumentNullException.ThrowIfNull(property, "property");
		return property.GetAccessors();
	}

	public static MethodInfo[] GetAccessors(this PropertyInfo property, bool nonPublic)
	{
		ArgumentNullException.ThrowIfNull(property, "property");
		return property.GetAccessors(nonPublic);
	}

	public static MethodInfo? GetGetMethod(this PropertyInfo property)
	{
		ArgumentNullException.ThrowIfNull(property, "property");
		return property.GetGetMethod();
	}

	public static MethodInfo? GetGetMethod(this PropertyInfo property, bool nonPublic)
	{
		ArgumentNullException.ThrowIfNull(property, "property");
		return property.GetGetMethod(nonPublic);
	}

	public static MethodInfo? GetSetMethod(this PropertyInfo property)
	{
		ArgumentNullException.ThrowIfNull(property, "property");
		return property.GetSetMethod();
	}

	public static MethodInfo? GetSetMethod(this PropertyInfo property, bool nonPublic)
	{
		ArgumentNullException.ThrowIfNull(property, "property");
		return property.GetSetMethod(nonPublic);
	}
}
