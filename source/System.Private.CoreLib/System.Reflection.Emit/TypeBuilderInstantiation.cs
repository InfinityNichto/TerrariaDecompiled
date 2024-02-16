using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Reflection.Emit;

internal sealed class TypeBuilderInstantiation : TypeInfo
{
	private Type m_type;

	private Type[] m_inst;

	private string m_strFullQualName;

	internal Hashtable m_hashtable;

	public override Type DeclaringType => m_type.DeclaringType;

	public override Type ReflectedType => m_type.ReflectedType;

	public override string Name => m_type.Name;

	public override Module Module => m_type.Module;

	public override Guid GUID
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	public override Assembly Assembly => m_type.Assembly;

	public override RuntimeTypeHandle TypeHandle
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	public override string FullName => m_strFullQualName ?? (m_strFullQualName = TypeNameBuilder.ToString(this, TypeNameBuilder.Format.FullName));

	public override string Namespace => m_type.Namespace;

	public override string AssemblyQualifiedName => TypeNameBuilder.ToString(this, TypeNameBuilder.Format.AssemblyQualifiedName);

	public override Type BaseType
	{
		get
		{
			Type baseType = m_type.BaseType;
			if (baseType == null)
			{
				return null;
			}
			TypeBuilderInstantiation typeBuilderInstantiation = baseType as TypeBuilderInstantiation;
			if (typeBuilderInstantiation == null)
			{
				return baseType;
			}
			return typeBuilderInstantiation.Substitute(GetGenericArguments());
		}
	}

	public override bool IsTypeDefinition => false;

	public override bool IsSZArray => false;

	public override Type UnderlyingSystemType => this;

	public override bool IsGenericTypeDefinition => false;

	public override bool IsGenericType => true;

	public override bool IsConstructedGenericType => true;

	public override bool IsGenericParameter => false;

	public override int GenericParameterPosition
	{
		get
		{
			throw new InvalidOperationException();
		}
	}

	public override bool ContainsGenericParameters
	{
		get
		{
			for (int i = 0; i < m_inst.Length; i++)
			{
				if (m_inst[i].ContainsGenericParameters)
				{
					return true;
				}
			}
			return false;
		}
	}

	public override MethodBase DeclaringMethod => null;

	public override bool IsAssignableFrom([NotNullWhen(true)] TypeInfo typeInfo)
	{
		if (typeInfo == null)
		{
			return false;
		}
		return IsAssignableFrom(typeInfo.AsType());
	}

	internal static Type MakeGenericType(Type type, Type[] typeArguments)
	{
		if (!type.IsGenericTypeDefinition)
		{
			throw new InvalidOperationException();
		}
		if (typeArguments == null)
		{
			throw new ArgumentNullException("typeArguments");
		}
		foreach (Type type2 in typeArguments)
		{
			if (type2 == null)
			{
				throw new ArgumentNullException("typeArguments");
			}
		}
		return new TypeBuilderInstantiation(type, typeArguments);
	}

	private TypeBuilderInstantiation(Type type, Type[] inst)
	{
		m_type = type;
		m_inst = inst;
		m_hashtable = new Hashtable();
	}

	public override string ToString()
	{
		return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.ToString);
	}

	public override Type MakePointerType()
	{
		return SymbolType.FormCompoundType("*", this, 0);
	}

	public override Type MakeByRefType()
	{
		return SymbolType.FormCompoundType("&", this, 0);
	}

	public override Type MakeArrayType()
	{
		return SymbolType.FormCompoundType("[]", this, 0);
	}

	public override Type MakeArrayType(int rank)
	{
		if (rank <= 0)
		{
			throw new IndexOutOfRangeException();
		}
		string format = ((rank == 1) ? "[]" : ("[" + new string(',', rank - 1) + "]"));
		return SymbolType.FormCompoundType(format, this, 0);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
	{
		throw new NotSupportedException();
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:UnrecognizedReflectionPattern", Justification = "The entire TypeBuilderInstantiation is serving the MakeGenericType implementation. Currently this is not supported by linker. Once it is supported the outercall (Type.MakeGenericType)will validate that the types fullfill the necessary requirements of annotations on type parameters.As such the actual internals of the implementation are not interesting.")]
	private Type Substitute(Type[] substitutes)
	{
		Type[] genericArguments = GetGenericArguments();
		Type[] array = new Type[genericArguments.Length];
		for (int i = 0; i < array.Length; i++)
		{
			Type type = genericArguments[i];
			if (type is TypeBuilderInstantiation typeBuilderInstantiation)
			{
				array[i] = typeBuilderInstantiation.Substitute(substitutes);
			}
			else if (type is GenericTypeParameterBuilder)
			{
				array[i] = substitutes[type.GenericParameterPosition];
			}
			else
			{
				array[i] = type;
			}
		}
		return GetGenericTypeDefinition().MakeGenericType(array);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
	protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
	public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
	public override FieldInfo GetField(string name, BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
	public override FieldInfo[] GetFields(BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2063:UnrecognizedReflectionPattern", Justification = "Linker doesn't recognize always throwing method. https://github.com/mono/linker/issues/2025")]
	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	public override Type GetInterface(string name, bool ignoreCase)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	public override Type[] GetInterfaces()
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
	public override EventInfo[] GetEvents()
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
	public override Type[] GetNestedTypes(BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
	public override Type GetNestedType(string name, BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	public override InterfaceMapping GetInterfaceMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override EventInfo[] GetEvents(BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	protected override TypeAttributes GetAttributeFlagsImpl()
	{
		return m_type.Attributes;
	}

	protected override bool IsArrayImpl()
	{
		return false;
	}

	protected override bool IsByRefImpl()
	{
		return false;
	}

	protected override bool IsPointerImpl()
	{
		return false;
	}

	protected override bool IsPrimitiveImpl()
	{
		return false;
	}

	protected override bool IsCOMObjectImpl()
	{
		return false;
	}

	public override Type GetElementType()
	{
		throw new NotSupportedException();
	}

	protected override bool HasElementTypeImpl()
	{
		return false;
	}

	public override Type[] GetGenericArguments()
	{
		return m_inst;
	}

	protected override bool IsValueTypeImpl()
	{
		return m_type.IsValueType;
	}

	public override Type GetGenericTypeDefinition()
	{
		return m_type;
	}

	[RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
	public override Type MakeGenericType(params Type[] inst)
	{
		throw new InvalidOperationException(SR.Format(SR.Arg_NotGenericTypeDefinition, this));
	}

	public override bool IsAssignableFrom([NotNullWhen(true)] Type c)
	{
		throw new NotSupportedException();
	}

	public override bool IsSubclassOf(Type c)
	{
		throw new NotSupportedException();
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		throw new NotSupportedException();
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		throw new NotSupportedException();
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		throw new NotSupportedException();
	}
}
