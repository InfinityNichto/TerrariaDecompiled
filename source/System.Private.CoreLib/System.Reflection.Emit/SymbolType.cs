using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Reflection.Emit;

internal sealed class SymbolType : TypeInfo
{
	internal TypeKind m_typeKind;

	internal Type m_baseType;

	internal int m_cRank;

	internal int[] m_iaLowerBound;

	internal int[] m_iaUpperBound;

	private string m_format;

	private bool m_isSzArray = true;

	public override bool IsTypeDefinition => false;

	public override bool IsSZArray
	{
		get
		{
			if (m_cRank <= 1)
			{
				return m_isSzArray;
			}
			return false;
		}
	}

	public override Guid GUID
	{
		get
		{
			throw new NotSupportedException(SR.NotSupported_NonReflectedType);
		}
	}

	public override Module Module
	{
		get
		{
			Type baseType = m_baseType;
			while (baseType is SymbolType)
			{
				baseType = ((SymbolType)baseType).m_baseType;
			}
			return baseType.Module;
		}
	}

	public override Assembly Assembly
	{
		get
		{
			Type baseType = m_baseType;
			while (baseType is SymbolType)
			{
				baseType = ((SymbolType)baseType).m_baseType;
			}
			return baseType.Assembly;
		}
	}

	public override RuntimeTypeHandle TypeHandle
	{
		get
		{
			throw new NotSupportedException(SR.NotSupported_NonReflectedType);
		}
	}

	public override string Name
	{
		get
		{
			string text = m_format;
			Type baseType = m_baseType;
			while (baseType is SymbolType)
			{
				text = ((SymbolType)baseType).m_format + text;
				baseType = ((SymbolType)baseType).m_baseType;
			}
			return baseType.Name + text;
		}
	}

	public override string FullName => TypeNameBuilder.ToString(this, TypeNameBuilder.Format.FullName);

	public override string AssemblyQualifiedName => TypeNameBuilder.ToString(this, TypeNameBuilder.Format.AssemblyQualifiedName);

	public override string Namespace => m_baseType.Namespace;

	public override Type BaseType => typeof(Array);

	public override bool IsConstructedGenericType => false;

	public override Type UnderlyingSystemType => this;

	public override bool IsAssignableFrom([NotNullWhen(true)] TypeInfo typeInfo)
	{
		if (typeInfo == null)
		{
			return false;
		}
		return IsAssignableFrom(typeInfo.AsType());
	}

	internal static Type FormCompoundType(string format, Type baseType, int curIndex)
	{
		if (format == null || curIndex == format.Length)
		{
			return baseType;
		}
		if (format[curIndex] == '&')
		{
			SymbolType symbolType = new SymbolType(TypeKind.IsByRef);
			symbolType.SetFormat(format, curIndex, 1);
			curIndex++;
			if (curIndex != format.Length)
			{
				throw new ArgumentException(SR.Argument_BadSigFormat);
			}
			symbolType.SetElementType(baseType);
			return symbolType;
		}
		if (format[curIndex] == '[')
		{
			SymbolType symbolType = new SymbolType(TypeKind.IsArray);
			int num = curIndex;
			curIndex++;
			int num2 = 0;
			int num3 = -1;
			while (format[curIndex] != ']')
			{
				if (format[curIndex] == '*')
				{
					symbolType.m_isSzArray = false;
					curIndex++;
				}
				if ((format[curIndex] >= '0' && format[curIndex] <= '9') || format[curIndex] == '-')
				{
					bool flag = false;
					if (format[curIndex] == '-')
					{
						flag = true;
						curIndex++;
					}
					while (format[curIndex] >= '0' && format[curIndex] <= '9')
					{
						num2 *= 10;
						num2 += format[curIndex] - 48;
						curIndex++;
					}
					if (flag)
					{
						num2 = -num2;
					}
					num3 = num2 - 1;
				}
				if (format[curIndex] == '.')
				{
					curIndex++;
					if (format[curIndex] != '.')
					{
						throw new ArgumentException(SR.Argument_BadSigFormat);
					}
					curIndex++;
					if ((format[curIndex] >= '0' && format[curIndex] <= '9') || format[curIndex] == '-')
					{
						bool flag2 = false;
						num3 = 0;
						if (format[curIndex] == '-')
						{
							flag2 = true;
							curIndex++;
						}
						while (format[curIndex] >= '0' && format[curIndex] <= '9')
						{
							num3 *= 10;
							num3 += format[curIndex] - 48;
							curIndex++;
						}
						if (flag2)
						{
							num3 = -num3;
						}
						if (num3 < num2)
						{
							throw new ArgumentException(SR.Argument_BadSigFormat);
						}
					}
				}
				if (format[curIndex] == ',')
				{
					curIndex++;
					symbolType.SetBounds(num2, num3);
					num2 = 0;
					num3 = -1;
				}
				else if (format[curIndex] != ']')
				{
					throw new ArgumentException(SR.Argument_BadSigFormat);
				}
			}
			symbolType.SetBounds(num2, num3);
			curIndex++;
			symbolType.SetFormat(format, num, curIndex - num);
			symbolType.SetElementType(baseType);
			return FormCompoundType(format, symbolType, curIndex);
		}
		if (format[curIndex] == '*')
		{
			SymbolType symbolType = new SymbolType(TypeKind.IsPointer);
			symbolType.SetFormat(format, curIndex, 1);
			curIndex++;
			symbolType.SetElementType(baseType);
			return FormCompoundType(format, symbolType, curIndex);
		}
		return null;
	}

	internal SymbolType(TypeKind typeKind)
	{
		m_typeKind = typeKind;
		m_iaLowerBound = new int[4];
		m_iaUpperBound = new int[4];
	}

	internal void SetElementType(Type baseType)
	{
		if ((object)baseType == null)
		{
			throw new ArgumentNullException("baseType");
		}
		m_baseType = baseType;
	}

	private void SetBounds(int lower, int upper)
	{
		if (lower != 0 || upper != -1)
		{
			m_isSzArray = false;
		}
		if (m_iaLowerBound.Length <= m_cRank)
		{
			int[] array = new int[m_cRank * 2];
			Array.Copy(m_iaLowerBound, array, m_cRank);
			m_iaLowerBound = array;
			Array.Copy(m_iaUpperBound, array, m_cRank);
			m_iaUpperBound = array;
		}
		m_iaLowerBound[m_cRank] = lower;
		m_iaUpperBound[m_cRank] = upper;
		m_cRank++;
	}

	internal void SetFormat(string format, int curIndex, int length)
	{
		m_format = format.Substring(curIndex, length);
	}

	public override Type MakePointerType()
	{
		return FormCompoundType(m_format + "*", m_baseType, 0);
	}

	public override Type MakeByRefType()
	{
		return FormCompoundType(m_format + "&", m_baseType, 0);
	}

	public override Type MakeArrayType()
	{
		return FormCompoundType(m_format + "[]", m_baseType, 0);
	}

	public override Type MakeArrayType(int rank)
	{
		string rankString = TypeInfo.GetRankString(rank);
		return FormCompoundType(m_format + rankString, m_baseType, 0) as SymbolType;
	}

	public override int GetArrayRank()
	{
		if (!base.IsArray)
		{
			throw new NotSupportedException(SR.NotSupported_SubclassOverride);
		}
		return m_cRank;
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	public override string ToString()
	{
		return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.ToString);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
	protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
	public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
	public override FieldInfo GetField(string name, BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
	public override FieldInfo[] GetFields(BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2063:UnrecognizedReflectionPattern", Justification = "Linker doesn't recognize always throwing method. https://github.com/mono/linker/issues/2025")]
	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	public override Type GetInterface(string name, bool ignoreCase)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	public override Type[] GetInterfaces()
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
	public override EventInfo[] GetEvents()
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
	public override Type[] GetNestedTypes(BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
	public override Type GetNestedType(string name, BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	public override InterfaceMapping GetInterfaceMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override EventInfo[] GetEvents(BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	protected override TypeAttributes GetAttributeFlagsImpl()
	{
		Type baseType = m_baseType;
		while (baseType is SymbolType)
		{
			baseType = ((SymbolType)baseType).m_baseType;
		}
		return baseType.Attributes;
	}

	protected override bool IsArrayImpl()
	{
		return m_typeKind == TypeKind.IsArray;
	}

	protected override bool IsPointerImpl()
	{
		return m_typeKind == TypeKind.IsPointer;
	}

	protected override bool IsByRefImpl()
	{
		return m_typeKind == TypeKind.IsByRef;
	}

	protected override bool IsPrimitiveImpl()
	{
		return false;
	}

	protected override bool IsValueTypeImpl()
	{
		return false;
	}

	protected override bool IsCOMObjectImpl()
	{
		return false;
	}

	public override Type GetElementType()
	{
		return m_baseType;
	}

	protected override bool HasElementTypeImpl()
	{
		return m_baseType != null;
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		throw new NotSupportedException(SR.NotSupported_NonReflectedType);
	}
}
