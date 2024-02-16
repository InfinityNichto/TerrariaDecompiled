using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Reflection;

public class TypeDelegator : TypeInfo
{
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	protected Type typeImpl;

	public override Guid GUID => typeImpl.GUID;

	public override int MetadataToken => typeImpl.MetadataToken;

	public override Module Module => typeImpl.Module;

	public override Assembly Assembly => typeImpl.Assembly;

	public override RuntimeTypeHandle TypeHandle => typeImpl.TypeHandle;

	public override string Name => typeImpl.Name;

	public override string? FullName => typeImpl.FullName;

	public override string? Namespace => typeImpl.Namespace;

	public override string? AssemblyQualifiedName => typeImpl.AssemblyQualifiedName;

	public override Type? BaseType => typeImpl.BaseType;

	public override bool IsTypeDefinition => typeImpl.IsTypeDefinition;

	public override bool IsSZArray => typeImpl.IsSZArray;

	public override bool IsVariableBoundArray => typeImpl.IsVariableBoundArray;

	public override bool IsGenericTypeParameter => typeImpl.IsGenericTypeParameter;

	public override bool IsGenericMethodParameter => typeImpl.IsGenericMethodParameter;

	public override bool IsByRefLike => typeImpl.IsByRefLike;

	public override bool IsConstructedGenericType => typeImpl.IsConstructedGenericType;

	public override bool IsCollectible => typeImpl.IsCollectible;

	public override Type UnderlyingSystemType => typeImpl.UnderlyingSystemType;

	public override bool IsAssignableFrom([NotNullWhen(true)] TypeInfo? typeInfo)
	{
		if (typeInfo == null)
		{
			return false;
		}
		return IsAssignableFrom(typeInfo.AsType());
	}

	protected TypeDelegator()
	{
	}

	public TypeDelegator([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type delegatingType)
	{
		if ((object)delegatingType == null)
		{
			throw new ArgumentNullException("delegatingType");
		}
		typeImpl = delegatingType;
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	public override object? InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters)
	{
		return typeImpl.InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
	protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers)
	{
		return typeImpl.GetConstructor(bindingAttr, binder, callConvention, types, modifiers);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
	public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
	{
		return typeImpl.GetConstructors(bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
	{
		if (types == null)
		{
			return typeImpl.GetMethod(name, bindingAttr);
		}
		return typeImpl.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
	{
		return typeImpl.GetMethods(bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
	public override FieldInfo? GetField(string name, BindingFlags bindingAttr)
	{
		return typeImpl.GetField(name, bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
	public override FieldInfo[] GetFields(BindingFlags bindingAttr)
	{
		return typeImpl.GetFields(bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	public override Type? GetInterface(string name, bool ignoreCase)
	{
		return typeImpl.GetInterface(name, ignoreCase);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	public override Type[] GetInterfaces()
	{
		return typeImpl.GetInterfaces();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override EventInfo? GetEvent(string name, BindingFlags bindingAttr)
	{
		return typeImpl.GetEvent(name, bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
	public override EventInfo[] GetEvents()
	{
		return typeImpl.GetEvents();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	protected override PropertyInfo? GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers)
	{
		if (returnType == null && types == null)
		{
			return typeImpl.GetProperty(name, bindingAttr);
		}
		return typeImpl.GetProperty(name, bindingAttr, binder, returnType, types, modifiers);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
	{
		return typeImpl.GetProperties(bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override EventInfo[] GetEvents(BindingFlags bindingAttr)
	{
		return typeImpl.GetEvents(bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
	public override Type[] GetNestedTypes(BindingFlags bindingAttr)
	{
		return typeImpl.GetNestedTypes(bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
	public override Type? GetNestedType(string name, BindingFlags bindingAttr)
	{
		return typeImpl.GetNestedType(name, bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
	{
		return typeImpl.GetMember(name, type, bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
	{
		return typeImpl.GetMembers(bindingAttr);
	}

	public override MemberInfo GetMemberWithSameMetadataDefinitionAs(MemberInfo member)
	{
		return typeImpl.GetMemberWithSameMetadataDefinitionAs(member);
	}

	protected override TypeAttributes GetAttributeFlagsImpl()
	{
		return typeImpl.Attributes;
	}

	protected override bool IsArrayImpl()
	{
		return typeImpl.IsArray;
	}

	protected override bool IsPrimitiveImpl()
	{
		return typeImpl.IsPrimitive;
	}

	protected override bool IsByRefImpl()
	{
		return typeImpl.IsByRef;
	}

	protected override bool IsPointerImpl()
	{
		return typeImpl.IsPointer;
	}

	protected override bool IsValueTypeImpl()
	{
		return typeImpl.IsValueType;
	}

	protected override bool IsCOMObjectImpl()
	{
		return typeImpl.IsCOMObject;
	}

	public override Type? GetElementType()
	{
		return typeImpl.GetElementType();
	}

	protected override bool HasElementTypeImpl()
	{
		return typeImpl.HasElementType;
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return typeImpl.GetCustomAttributes(inherit);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		return typeImpl.GetCustomAttributes(attributeType, inherit);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		return typeImpl.IsDefined(attributeType, inherit);
	}

	public override InterfaceMapping GetInterfaceMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType)
	{
		return typeImpl.GetInterfaceMap(interfaceType);
	}
}
