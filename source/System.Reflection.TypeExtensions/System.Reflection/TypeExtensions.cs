using System.Diagnostics.CodeAnalysis;

namespace System.Reflection;

public static class TypeExtensions
{
	public static ConstructorInfo? GetConstructor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] this Type type, Type[] types)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetConstructor(types);
	}

	public static ConstructorInfo[] GetConstructors([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] this Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetConstructors();
	}

	public static ConstructorInfo[] GetConstructors([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] this Type type, BindingFlags bindingAttr)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetConstructors(bindingAttr);
	}

	public static MemberInfo[] GetDefaultMembers([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicEvents)] this Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetDefaultMembers();
	}

	public static EventInfo? GetEvent([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)] this Type type, string name)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetEvent(name);
	}

	public static EventInfo? GetEvent([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)] this Type type, string name, BindingFlags bindingAttr)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetEvent(name, bindingAttr);
	}

	public static EventInfo[] GetEvents([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)] this Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetEvents();
	}

	public static EventInfo[] GetEvents([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)] this Type type, BindingFlags bindingAttr)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetEvents(bindingAttr);
	}

	public static FieldInfo? GetField([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] this Type type, string name)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetField(name);
	}

	public static FieldInfo? GetField([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] this Type type, string name, BindingFlags bindingAttr)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetField(name, bindingAttr);
	}

	public static FieldInfo[] GetFields([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] this Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetFields();
	}

	public static FieldInfo[] GetFields([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] this Type type, BindingFlags bindingAttr)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetFields(bindingAttr);
	}

	public static Type[] GetGenericArguments(this Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetGenericArguments();
	}

	public static Type[] GetInterfaces([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetInterfaces();
	}

	public static MemberInfo[] GetMember([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicEvents)] this Type type, string name)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetMember(name);
	}

	public static MemberInfo[] GetMember([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] this Type type, string name, BindingFlags bindingAttr)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetMember(name, bindingAttr);
	}

	public static MemberInfo[] GetMembers([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicEvents)] this Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetMembers();
	}

	public static MemberInfo[] GetMembers([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] this Type type, BindingFlags bindingAttr)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetMembers(bindingAttr);
	}

	public static MethodInfo? GetMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] this Type type, string name)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetMethod(name);
	}

	public static MethodInfo? GetMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] this Type type, string name, BindingFlags bindingAttr)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetMethod(name, bindingAttr);
	}

	public static MethodInfo? GetMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] this Type type, string name, Type[] types)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetMethod(name, types);
	}

	public static MethodInfo[] GetMethods([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] this Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetMethods();
	}

	public static MethodInfo[] GetMethods([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] this Type type, BindingFlags bindingAttr)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetMethods(bindingAttr);
	}

	public static Type? GetNestedType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)] this Type type, string name, BindingFlags bindingAttr)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetNestedType(name, bindingAttr);
	}

	public static Type[] GetNestedTypes([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)] this Type type, BindingFlags bindingAttr)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetNestedTypes(bindingAttr);
	}

	public static PropertyInfo[] GetProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] this Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetProperties();
	}

	public static PropertyInfo[] GetProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] this Type type, BindingFlags bindingAttr)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetProperties(bindingAttr);
	}

	public static PropertyInfo? GetProperty([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] this Type type, string name)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetProperty(name);
	}

	public static PropertyInfo? GetProperty([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] this Type type, string name, BindingFlags bindingAttr)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetProperty(name, bindingAttr);
	}

	public static PropertyInfo? GetProperty([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] this Type type, string name, Type? returnType)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetProperty(name, returnType);
	}

	public static PropertyInfo? GetProperty([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] this Type type, string name, Type? returnType, Type[] types)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.GetProperty(name, returnType, types);
	}

	public static bool IsAssignableFrom(this Type type, [NotNullWhen(true)] Type? c)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.IsAssignableFrom(c);
	}

	public static bool IsInstanceOfType(this Type type, [NotNullWhen(true)] object? o)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		return type.IsInstanceOfType(o);
	}
}
