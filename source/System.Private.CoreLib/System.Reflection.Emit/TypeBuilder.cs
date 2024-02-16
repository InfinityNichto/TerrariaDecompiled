using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit;

public sealed class TypeBuilder : TypeInfo
{
	private sealed class CustAttr
	{
		private readonly ConstructorInfo m_con;

		private readonly byte[] m_binaryAttribute;

		private readonly CustomAttributeBuilder m_customBuilder;

		public CustAttr(ConstructorInfo con, byte[] binaryAttribute)
		{
			if ((object)con == null)
			{
				throw new ArgumentNullException("con");
			}
			if (binaryAttribute == null)
			{
				throw new ArgumentNullException("binaryAttribute");
			}
			m_con = con;
			m_binaryAttribute = binaryAttribute;
		}

		public CustAttr(CustomAttributeBuilder customBuilder)
		{
			if (customBuilder == null)
			{
				throw new ArgumentNullException("customBuilder");
			}
			m_customBuilder = customBuilder;
		}

		public void Bake(ModuleBuilder module, int token)
		{
			if (m_customBuilder == null)
			{
				DefineCustomAttribute(module, token, module.GetConstructorToken(m_con), m_binaryAttribute);
			}
			else
			{
				m_customBuilder.CreateCustomAttribute(module, token);
			}
		}
	}

	public const int UnspecifiedTypeSize = 0;

	private List<CustAttr> m_ca;

	private int m_tdType;

	private readonly ModuleBuilder m_module;

	private readonly string m_strName;

	private readonly string m_strNameSpace;

	private string m_strFullQualName;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	private Type m_typeParent;

	private List<Type> m_typeInterfaces;

	private readonly TypeAttributes m_iAttr;

	private GenericParameterAttributes m_genParamAttributes;

	internal List<MethodBuilder> m_listMethods;

	internal int m_lastTokenizedMethod;

	private int m_constructorCount;

	private readonly int m_iTypeSize;

	private readonly PackingSize m_iPackingSize;

	private readonly TypeBuilder m_DeclaringType;

	private Type m_enumUnderlyingType;

	internal bool m_isHiddenGlobalType;

	private bool m_hasBeenCreated;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	private RuntimeType m_bakedRuntimeType;

	private readonly int m_genParamPos;

	private GenericTypeParameterBuilder[] m_inst;

	private readonly bool m_bIsGenParam;

	private readonly MethodBuilder m_declMeth;

	private readonly TypeBuilder m_genTypeDef;

	internal object SyncRoot => m_module.SyncRoot;

	internal RuntimeType BakedRuntimeType => m_bakedRuntimeType;

	public override Type? DeclaringType => m_DeclaringType;

	public override Type? ReflectedType => m_DeclaringType;

	public override string Name => m_strName;

	public override Module Module => GetModuleBuilder();

	public override bool IsByRefLike => false;

	public override int MetadataToken => m_tdType;

	public override Guid GUID
	{
		get
		{
			if (!IsCreated())
			{
				throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
			}
			return m_bakedRuntimeType.GUID;
		}
	}

	public override Assembly Assembly => m_module.Assembly;

	public override RuntimeTypeHandle TypeHandle
	{
		get
		{
			throw new NotSupportedException(SR.NotSupported_DynamicModule);
		}
	}

	public override string? FullName => m_strFullQualName ?? (m_strFullQualName = TypeNameBuilder.ToString(this, TypeNameBuilder.Format.FullName));

	public override string? Namespace => m_strNameSpace;

	public override string? AssemblyQualifiedName => TypeNameBuilder.ToString(this, TypeNameBuilder.Format.AssemblyQualifiedName);

	public override Type? BaseType => m_typeParent;

	public override bool IsTypeDefinition => true;

	public override bool IsSZArray => false;

	public override bool IsSecurityCritical => true;

	public override bool IsSecuritySafeCritical => false;

	public override bool IsSecurityTransparent => false;

	public override Type UnderlyingSystemType
	{
		get
		{
			if (m_bakedRuntimeType != null)
			{
				return m_bakedRuntimeType;
			}
			if (IsEnum)
			{
				if (m_enumUnderlyingType == null)
				{
					throw new InvalidOperationException(SR.InvalidOperation_NoUnderlyingTypeOnEnum);
				}
				return m_enumUnderlyingType;
			}
			return this;
		}
	}

	public override GenericParameterAttributes GenericParameterAttributes => m_genParamAttributes;

	public override bool IsGenericTypeDefinition => IsGenericType;

	public override bool IsGenericType => m_inst != null;

	public override bool IsGenericParameter => m_bIsGenParam;

	public override bool IsConstructedGenericType => false;

	public override int GenericParameterPosition => m_genParamPos;

	public override MethodBase? DeclaringMethod => m_declMeth;

	public int Size => m_iTypeSize;

	public PackingSize PackingSize => m_iPackingSize;

	internal int TypeToken
	{
		get
		{
			if (IsGenericParameter)
			{
				ThrowIfCreated();
			}
			return m_tdType;
		}
	}

	public override bool IsAssignableFrom([NotNullWhen(true)] TypeInfo? typeInfo)
	{
		if (typeInfo == null)
		{
			return false;
		}
		return IsAssignableFrom(typeInfo.AsType());
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:UnrecognizedReflectionPattern", Justification = "MakeGenericType is only called on a TypeBuilder which is not subject to trimming")]
	public static MethodInfo GetMethod(Type type, MethodInfo method)
	{
		if (!(type is TypeBuilder) && !(type is TypeBuilderInstantiation))
		{
			throw new ArgumentException(SR.Argument_MustBeTypeBuilder);
		}
		if (method.IsGenericMethod && !method.IsGenericMethodDefinition)
		{
			throw new ArgumentException(SR.Argument_NeedGenericMethodDefinition, "method");
		}
		if (method.DeclaringType == null || !method.DeclaringType.IsGenericTypeDefinition)
		{
			throw new ArgumentException(SR.Argument_MethodNeedGenericDeclaringType, "method");
		}
		if (type.GetGenericTypeDefinition() != method.DeclaringType)
		{
			throw new ArgumentException(SR.Argument_InvalidMethodDeclaringType, "type");
		}
		if (type.IsGenericTypeDefinition)
		{
			type = type.MakeGenericType(type.GetGenericArguments());
		}
		if (!(type is TypeBuilderInstantiation))
		{
			throw new ArgumentException(SR.Argument_NeedNonGenericType, "type");
		}
		return MethodOnTypeBuilderInstantiation.GetMethod(method, type as TypeBuilderInstantiation);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:UnrecognizedReflectionPattern", Justification = "MakeGenericType is only called on a TypeBuilder which is not subject to trimming")]
	public static ConstructorInfo GetConstructor(Type type, ConstructorInfo constructor)
	{
		if (!(type is TypeBuilder) && !(type is TypeBuilderInstantiation))
		{
			throw new ArgumentException(SR.Argument_MustBeTypeBuilder);
		}
		if (!constructor.DeclaringType.IsGenericTypeDefinition)
		{
			throw new ArgumentException(SR.Argument_ConstructorNeedGenericDeclaringType, "constructor");
		}
		if (!(type is TypeBuilderInstantiation))
		{
			throw new ArgumentException(SR.Argument_NeedNonGenericType, "type");
		}
		if (type is TypeBuilder && type.IsGenericTypeDefinition)
		{
			type = type.MakeGenericType(type.GetGenericArguments());
		}
		if (type.GetGenericTypeDefinition() != constructor.DeclaringType)
		{
			throw new ArgumentException(SR.Argument_InvalidConstructorDeclaringType, "type");
		}
		return ConstructorOnTypeBuilderInstantiation.GetConstructor(constructor, type as TypeBuilderInstantiation);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:UnrecognizedReflectionPattern", Justification = "MakeGenericType is only called on a TypeBuilder which is not subject to trimming")]
	public static FieldInfo GetField(Type type, FieldInfo field)
	{
		if (!(type is TypeBuilder) && !(type is TypeBuilderInstantiation))
		{
			throw new ArgumentException(SR.Argument_MustBeTypeBuilder);
		}
		if (!field.DeclaringType.IsGenericTypeDefinition)
		{
			throw new ArgumentException(SR.Argument_FieldNeedGenericDeclaringType, "field");
		}
		if (!(type is TypeBuilderInstantiation))
		{
			throw new ArgumentException(SR.Argument_NeedNonGenericType, "type");
		}
		if (type is TypeBuilder && type.IsGenericTypeDefinition)
		{
			type = type.MakeGenericType(type.GetGenericArguments());
		}
		if (type.GetGenericTypeDefinition() != field.DeclaringType)
		{
			throw new ArgumentException(SR.Argument_InvalidFieldDeclaringType, "type");
		}
		return FieldOnTypeBuilderInstantiation.GetField(field, type as TypeBuilderInstantiation);
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void SetParentType(QCallModule module, int tdTypeDef, int tkParent);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void AddInterfaceImpl(QCallModule module, int tdTypeDef, int tkInterface);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern int DefineMethod(QCallModule module, int tkParent, string name, byte[] signature, int sigLength, MethodAttributes attributes);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern int DefineMethodSpec(QCallModule module, int tkParent, byte[] signature, int sigLength);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern int DefineField(QCallModule module, int tkParent, string name, byte[] signature, int sigLength, FieldAttributes attributes);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void SetMethodIL(QCallModule module, int tk, bool isInitLocals, byte[] body, int bodyLength, byte[] LocalSig, int sigLength, int maxStackSize, ExceptionHandler[] exceptions, int numExceptions, int[] tokenFixups, int numTokenFixups);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void DefineCustomAttribute(QCallModule module, int tkAssociate, int tkConstructor, byte[] attr, int attrLength);

	internal static void DefineCustomAttribute(ModuleBuilder module, int tkAssociate, int tkConstructor, byte[] attr)
	{
		byte[] array = null;
		if (attr != null)
		{
			array = new byte[attr.Length];
			Buffer.BlockCopy(attr, 0, array, 0, attr.Length);
		}
		DefineCustomAttribute(new QCallModule(ref module), tkAssociate, tkConstructor, array, (array != null) ? array.Length : 0);
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern int DefineProperty(QCallModule module, int tkParent, string name, PropertyAttributes attributes, byte[] signature, int sigLength);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern int DefineEvent(QCallModule module, int tkParent, string name, EventAttributes attributes, int tkEventType);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern void DefineMethodSemantics(QCallModule module, int tkAssociation, MethodSemanticsAttributes semantics, int tkMethod);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern void DefineMethodImpl(QCallModule module, int tkType, int tkBody, int tkDecl);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern void SetMethodImpl(QCallModule module, int tkMethod, MethodImplAttributes MethodImplAttributes);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern int SetParamInfo(QCallModule module, int tkMethod, int iSequence, ParameterAttributes iParamAttributes, string strParamName);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern int GetTokenFromSig(QCallModule module, byte[] signature, int sigLength);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern void SetFieldLayoutOffset(QCallModule module, int fdToken, int iOffset);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern void SetClassLayout(QCallModule module, int tk, PackingSize iPackingSize, int iTypeSize);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private unsafe static extern void SetConstantValue(QCallModule module, int tk, int corType, void* pValue);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void SetPInvokeData(QCallModule module, string DllName, string name, int token, int linkFlags);

	internal static bool IsTypeEqual(Type t1, Type t2)
	{
		if (t1 == t2)
		{
			return true;
		}
		TypeBuilder typeBuilder = null;
		TypeBuilder typeBuilder2 = null;
		Type type;
		if (t1 is TypeBuilder)
		{
			typeBuilder = (TypeBuilder)t1;
			type = typeBuilder.m_bakedRuntimeType;
		}
		else
		{
			type = t1;
		}
		Type type2;
		if (t2 is TypeBuilder)
		{
			typeBuilder2 = (TypeBuilder)t2;
			type2 = typeBuilder2.m_bakedRuntimeType;
		}
		else
		{
			type2 = t2;
		}
		if (typeBuilder != null && typeBuilder2 != null && (object)typeBuilder == typeBuilder2)
		{
			return true;
		}
		if (type != null && type2 != null && type == type2)
		{
			return true;
		}
		return false;
	}

	internal unsafe static void SetConstantValue(ModuleBuilder module, int tk, Type destType, object value)
	{
		if (value != null)
		{
			Type type = value.GetType();
			if (destType.IsByRef)
			{
				destType = destType.GetElementType();
			}
			destType = Nullable.GetUnderlyingType(destType) ?? destType;
			if (destType.IsEnum)
			{
				Type type2;
				if (destType is EnumBuilder enumBuilder)
				{
					type2 = enumBuilder.GetEnumUnderlyingType();
					if (type != enumBuilder.m_typeBuilder.m_bakedRuntimeType && type != type2)
					{
						throw new ArgumentException(SR.Argument_ConstantDoesntMatch);
					}
				}
				else if (destType is TypeBuilder typeBuilder)
				{
					type2 = typeBuilder.m_enumUnderlyingType;
					if (type2 == null || (type != typeBuilder.UnderlyingSystemType && type != type2))
					{
						throw new ArgumentException(SR.Argument_ConstantDoesntMatch);
					}
				}
				else
				{
					type2 = Enum.GetUnderlyingType(destType);
					if (type != destType && type != type2)
					{
						throw new ArgumentException(SR.Argument_ConstantDoesntMatch);
					}
				}
				type = type2;
			}
			else if (!destType.IsAssignableFrom(type))
			{
				throw new ArgumentException(SR.Argument_ConstantDoesntMatch);
			}
			CorElementType corElementType = RuntimeTypeHandle.GetCorElementType((RuntimeType)type);
			if (corElementType - 2 <= CorElementType.ELEMENT_TYPE_U8)
			{
				fixed (byte* pValue = &value.GetRawData())
				{
					SetConstantValue(new QCallModule(ref module), tk, (int)corElementType, pValue);
				}
				return;
			}
			if (type == typeof(string))
			{
				fixed (char* pValue2 = (string)value)
				{
					SetConstantValue(new QCallModule(ref module), tk, 14, pValue2);
				}
				return;
			}
			if (!(type == typeof(DateTime)))
			{
				throw new ArgumentException(SR.Format(SR.Argument_ConstantNotSupported, type));
			}
			long ticks = ((DateTime)value).Ticks;
			SetConstantValue(new QCallModule(ref module), tk, 10, &ticks);
		}
		else
		{
			SetConstantValue(new QCallModule(ref module), tk, 18, null);
		}
	}

	internal TypeBuilder(ModuleBuilder module)
	{
		m_tdType = 33554432;
		m_isHiddenGlobalType = true;
		m_module = module;
		m_listMethods = new List<MethodBuilder>();
		m_lastTokenizedMethod = -1;
	}

	internal TypeBuilder(string szName, int genParamPos, MethodBuilder declMeth)
	{
		m_strName = szName;
		m_genParamPos = genParamPos;
		m_bIsGenParam = true;
		m_typeInterfaces = new List<Type>();
		m_declMeth = declMeth;
		m_DeclaringType = m_declMeth.GetTypeBuilder();
		m_module = declMeth.GetModuleBuilder();
	}

	private TypeBuilder(string szName, int genParamPos, TypeBuilder declType)
	{
		m_strName = szName;
		m_genParamPos = genParamPos;
		m_bIsGenParam = true;
		m_typeInterfaces = new List<Type>();
		m_DeclaringType = declType;
		m_module = declType.GetModuleBuilder();
	}

	internal TypeBuilder(string fullname, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type parent, Type[] interfaces, ModuleBuilder module, PackingSize iPackingSize, int iTypeSize, TypeBuilder enclosingType)
	{
		if (fullname == null)
		{
			throw new ArgumentNullException("fullname");
		}
		if (fullname.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyName, "fullname");
		}
		if (fullname[0] == '\0')
		{
			throw new ArgumentException(SR.Argument_IllegalName, "fullname");
		}
		if (fullname.Length > 1023)
		{
			throw new ArgumentException(SR.Argument_TypeNameTooLong, "fullname");
		}
		m_module = module;
		m_DeclaringType = enclosingType;
		AssemblyBuilder containingAssemblyBuilder = m_module.ContainingAssemblyBuilder;
		containingAssemblyBuilder._assemblyData.CheckTypeNameConflict(fullname, enclosingType);
		if (enclosingType != null && ((attr & TypeAttributes.VisibilityMask) == TypeAttributes.Public || (attr & TypeAttributes.VisibilityMask) == 0))
		{
			throw new ArgumentException(SR.Argument_BadNestedTypeFlags, "attr");
		}
		int[] array = null;
		if (interfaces != null)
		{
			for (int i = 0; i < interfaces.Length; i++)
			{
				if (interfaces[i] == null)
				{
					throw new ArgumentNullException("interfaces");
				}
			}
			array = new int[interfaces.Length + 1];
			for (int i = 0; i < interfaces.Length; i++)
			{
				array[i] = m_module.GetTypeTokenInternal(interfaces[i]);
			}
		}
		int num = fullname.LastIndexOf('.');
		if (num == -1 || num == 0)
		{
			m_strNameSpace = string.Empty;
			m_strName = fullname;
		}
		else
		{
			m_strNameSpace = fullname.Substring(0, num);
			m_strName = fullname.Substring(num + 1);
		}
		VerifyTypeAttributes(attr);
		m_iAttr = attr;
		SetParent(parent);
		m_listMethods = new List<MethodBuilder>();
		m_lastTokenizedMethod = -1;
		SetInterfaces(interfaces);
		int tkParent = 0;
		if (m_typeParent != null)
		{
			tkParent = m_module.GetTypeTokenInternal(m_typeParent);
		}
		int tkEnclosingType = 0;
		if (enclosingType != null)
		{
			tkEnclosingType = enclosingType.m_tdType;
		}
		m_tdType = DefineType(new QCallModule(ref module), fullname, tkParent, m_iAttr, tkEnclosingType, array);
		m_iPackingSize = iPackingSize;
		m_iTypeSize = iTypeSize;
		if (m_iPackingSize != 0 || m_iTypeSize != 0)
		{
			SetClassLayout(new QCallModule(ref module), m_tdType, m_iPackingSize, m_iTypeSize);
		}
		m_module.AddType(FullName, this);
	}

	private FieldBuilder DefineDataHelper(string name, byte[] data, int size, FieldAttributes attributes)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyName, "name");
		}
		if (size <= 0 || size >= 4128768)
		{
			throw new ArgumentException(SR.Argument_BadSizeForData);
		}
		ThrowIfCreated();
		string text = "$ArrayType$" + size;
		Type type = m_module.FindTypeBuilderWithName(text, ignoreCase: false);
		TypeBuilder typeBuilder = type as TypeBuilder;
		if (typeBuilder == null)
		{
			TypeAttributes attr = TypeAttributes.Public | TypeAttributes.ExplicitLayout | TypeAttributes.Sealed;
			typeBuilder = m_module.DefineType(text, attr, typeof(ValueType), PackingSize.Size1, size);
			typeBuilder.CreateType();
		}
		FieldBuilder fieldBuilder = DefineField(name, typeBuilder, attributes | FieldAttributes.Static);
		fieldBuilder.SetData(data, size);
		return fieldBuilder;
	}

	private void VerifyTypeAttributes(TypeAttributes attr)
	{
		if (DeclaringType == null)
		{
			if ((attr & TypeAttributes.VisibilityMask) != 0 && (attr & TypeAttributes.VisibilityMask) != TypeAttributes.Public)
			{
				throw new ArgumentException(SR.Argument_BadTypeAttrNestedVisibilityOnNonNestedType);
			}
		}
		else if ((attr & TypeAttributes.VisibilityMask) == 0 || (attr & TypeAttributes.VisibilityMask) == TypeAttributes.Public)
		{
			throw new ArgumentException(SR.Argument_BadTypeAttrNonNestedVisibilityNestedType);
		}
		if ((attr & TypeAttributes.LayoutMask) != 0 && (attr & TypeAttributes.LayoutMask) != TypeAttributes.SequentialLayout && (attr & TypeAttributes.LayoutMask) != TypeAttributes.ExplicitLayout)
		{
			throw new ArgumentException(SR.Argument_BadTypeAttrInvalidLayout);
		}
		if ((attr & TypeAttributes.ReservedMask) != 0)
		{
			throw new ArgumentException(SR.Argument_BadTypeAttrReservedBitsSet);
		}
	}

	public bool IsCreated()
	{
		return m_hasBeenCreated;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern int DefineType(QCallModule module, string fullname, int tkParent, TypeAttributes attributes, int tkEnclosingType, int[] interfaceTokens);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern int DefineGenericParam(QCallModule module, string name, int tkParent, GenericParameterAttributes attributes, int position, int[] constraints);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void TermCreateClass(QCallModule module, int tk, ObjectHandleOnStack type);

	internal void ThrowIfCreated()
	{
		if (IsCreated())
		{
			throw new InvalidOperationException(SR.InvalidOperation_TypeHasBeenCreated);
		}
	}

	internal ModuleBuilder GetModuleBuilder()
	{
		return m_module;
	}

	internal void SetGenParamAttributes(GenericParameterAttributes genericParameterAttributes)
	{
		m_genParamAttributes = genericParameterAttributes;
	}

	internal void SetGenParamCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
	{
		CustAttr genParamCustomAttributeNoLock = new CustAttr(con, binaryAttribute);
		lock (SyncRoot)
		{
			SetGenParamCustomAttributeNoLock(genParamCustomAttributeNoLock);
		}
	}

	internal void SetGenParamCustomAttribute(CustomAttributeBuilder customBuilder)
	{
		CustAttr genParamCustomAttributeNoLock = new CustAttr(customBuilder);
		lock (SyncRoot)
		{
			SetGenParamCustomAttributeNoLock(genParamCustomAttributeNoLock);
		}
	}

	private void SetGenParamCustomAttributeNoLock(CustAttr ca)
	{
		if (m_ca == null)
		{
			m_ca = new List<CustAttr>();
		}
		m_ca.Add(ca);
	}

	public override string ToString()
	{
		return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.ToString);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	public override object? InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
	protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetConstructor(bindingAttr, binder, callConvention, types, modifiers);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
	public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetConstructors(bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		if (types == null)
		{
			return m_bakedRuntimeType.GetMethod(name, bindingAttr);
		}
		return m_bakedRuntimeType.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetMethods(bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
	public override FieldInfo? GetField(string name, BindingFlags bindingAttr)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetField(name, bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
	public override FieldInfo[] GetFields(BindingFlags bindingAttr)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetFields(bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	public override Type? GetInterface(string name, bool ignoreCase)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetInterface(name, ignoreCase);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	public override Type[] GetInterfaces()
	{
		if (m_bakedRuntimeType != null)
		{
			return m_bakedRuntimeType.GetInterfaces();
		}
		if (m_typeInterfaces == null)
		{
			return Type.EmptyTypes;
		}
		return m_typeInterfaces.ToArray();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override EventInfo? GetEvent(string name, BindingFlags bindingAttr)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetEvent(name, bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
	public override EventInfo[] GetEvents()
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetEvents();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetProperties(bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
	public override Type[] GetNestedTypes(BindingFlags bindingAttr)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetNestedTypes(bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
	public override Type? GetNestedType(string name, BindingFlags bindingAttr)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetNestedType(name, bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetMember(name, type, bindingAttr);
	}

	public override InterfaceMapping GetInterfaceMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetInterfaceMap(interfaceType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override EventInfo[] GetEvents(BindingFlags bindingAttr)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetEvents(bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetMembers(bindingAttr);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern", Justification = "The GetInterfaces technically requires all interfaces to be preservedBut in this case it acts only on TypeBuilder which is never trimmed (as it's runtime created).")]
	public override bool IsAssignableFrom([NotNullWhen(true)] Type? c)
	{
		if (IsTypeEqual(c, this))
		{
			return true;
		}
		TypeBuilder typeBuilder = c as TypeBuilder;
		Type type = ((!(typeBuilder != null)) ? c : typeBuilder.m_bakedRuntimeType);
		if (type != null && type is RuntimeType)
		{
			if (m_bakedRuntimeType == null)
			{
				return false;
			}
			return m_bakedRuntimeType.IsAssignableFrom(type);
		}
		if (typeBuilder == null)
		{
			return false;
		}
		if (typeBuilder.IsSubclassOf(this))
		{
			return true;
		}
		if (!base.IsInterface)
		{
			return false;
		}
		Type[] interfaces = typeBuilder.GetInterfaces();
		for (int i = 0; i < interfaces.Length; i++)
		{
			if (IsTypeEqual(interfaces[i], this))
			{
				return true;
			}
			if (interfaces[i].IsSubclassOf(this))
			{
				return true;
			}
		}
		return false;
	}

	protected override TypeAttributes GetAttributeFlagsImpl()
	{
		return m_iAttr;
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
		if ((GetAttributeFlagsImpl() & TypeAttributes.Import) == 0)
		{
			return false;
		}
		return true;
	}

	public override Type GetElementType()
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
	}

	protected override bool HasElementTypeImpl()
	{
		return false;
	}

	public override bool IsSubclassOf(Type c)
	{
		Type type = this;
		if (IsTypeEqual(type, c))
		{
			return false;
		}
		type = type.BaseType;
		while (type != null)
		{
			if (IsTypeEqual(type, c))
			{
				return true;
			}
			type = type.BaseType;
		}
		return false;
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
		string rankString = TypeInfo.GetRankString(rank);
		return SymbolType.FormCompoundType(rankString, this, 0);
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return CustomAttribute.GetCustomAttributes(m_bakedRuntimeType, typeof(object) as RuntimeType, inherit);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		if (attributeType == null)
		{
			throw new ArgumentNullException("attributeType");
		}
		RuntimeType runtimeType = attributeType.UnderlyingSystemType as RuntimeType;
		if (runtimeType == null)
		{
			throw new ArgumentException(SR.Arg_MustBeType, "attributeType");
		}
		return CustomAttribute.GetCustomAttributes(m_bakedRuntimeType, runtimeType, inherit);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		if (attributeType == null)
		{
			throw new ArgumentNullException("attributeType");
		}
		RuntimeType runtimeType = attributeType.UnderlyingSystemType as RuntimeType;
		if (runtimeType == null)
		{
			throw new ArgumentException(SR.Arg_MustBeType, "attributeType");
		}
		return CustomAttribute.IsDefined(m_bakedRuntimeType, runtimeType, inherit);
	}

	internal void SetInterfaces(params Type[] interfaces)
	{
		ThrowIfCreated();
		m_typeInterfaces = new List<Type>();
		if (interfaces != null)
		{
			m_typeInterfaces.AddRange(interfaces);
		}
	}

	public GenericTypeParameterBuilder[] DefineGenericParameters(params string[] names)
	{
		if (names == null)
		{
			throw new ArgumentNullException("names");
		}
		if (names.Length == 0)
		{
			throw new ArgumentException(SR.Arg_EmptyArray, "names");
		}
		for (int i = 0; i < names.Length; i++)
		{
			if (names[i] == null)
			{
				throw new ArgumentNullException("names");
			}
		}
		if (m_inst != null)
		{
			throw new InvalidOperationException();
		}
		m_inst = new GenericTypeParameterBuilder[names.Length];
		for (int j = 0; j < names.Length; j++)
		{
			m_inst[j] = new GenericTypeParameterBuilder(new TypeBuilder(names[j], j, this));
		}
		return m_inst;
	}

	[RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
	public override Type MakeGenericType(params Type[] typeArguments)
	{
		AssemblyBuilder.CheckContext(typeArguments);
		return TypeBuilderInstantiation.MakeGenericType(this, typeArguments);
	}

	public override Type[] GetGenericArguments()
	{
		Type[] inst = m_inst;
		return inst ?? Type.EmptyTypes;
	}

	public override Type GetGenericTypeDefinition()
	{
		if (IsGenericTypeDefinition)
		{
			return this;
		}
		if (m_genTypeDef == null)
		{
			throw new InvalidOperationException();
		}
		return m_genTypeDef;
	}

	public void DefineMethodOverride(MethodInfo methodInfoBody, MethodInfo methodInfoDeclaration)
	{
		lock (SyncRoot)
		{
			DefineMethodOverrideNoLock(methodInfoBody, methodInfoDeclaration);
		}
	}

	private void DefineMethodOverrideNoLock(MethodInfo methodInfoBody, MethodInfo methodInfoDeclaration)
	{
		if (methodInfoBody == null)
		{
			throw new ArgumentNullException("methodInfoBody");
		}
		if (methodInfoDeclaration == null)
		{
			throw new ArgumentNullException("methodInfoDeclaration");
		}
		ThrowIfCreated();
		if ((object)methodInfoBody.DeclaringType != this)
		{
			throw new ArgumentException(SR.ArgumentException_BadMethodImplBody);
		}
		int methodToken = m_module.GetMethodToken(methodInfoBody);
		int methodToken2 = m_module.GetMethodToken(methodInfoDeclaration);
		ModuleBuilder module = m_module;
		DefineMethodImpl(new QCallModule(ref module), m_tdType, methodToken, methodToken2);
	}

	public MethodBuilder DefineMethod(string name, MethodAttributes attributes, Type? returnType, Type[]? parameterTypes)
	{
		return DefineMethod(name, attributes, CallingConventions.Standard, returnType, parameterTypes);
	}

	public MethodBuilder DefineMethod(string name, MethodAttributes attributes)
	{
		return DefineMethod(name, attributes, CallingConventions.Standard, null, null);
	}

	public MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention)
	{
		return DefineMethod(name, attributes, callingConvention, null, null);
	}

	public MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes)
	{
		return DefineMethod(name, attributes, callingConvention, returnType, null, null, parameterTypes, null, null);
	}

	public MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
	{
		lock (SyncRoot)
		{
			return DefineMethodNoLock(name, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2082:UnrecognizedReflectionPattern", Justification = "Reflection.Emit is not subject to trimming")]
	private MethodBuilder DefineMethodNoLock(string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyName, "name");
		}
		AssemblyBuilder.CheckContext(returnType);
		AssemblyBuilder.CheckContext(returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes);
		AssemblyBuilder.CheckContext(parameterTypeRequiredCustomModifiers);
		AssemblyBuilder.CheckContext(parameterTypeOptionalCustomModifiers);
		if (parameterTypes != null)
		{
			if (parameterTypeOptionalCustomModifiers != null && parameterTypeOptionalCustomModifiers.Length != parameterTypes.Length)
			{
				throw new ArgumentException(SR.Format(SR.Argument_MismatchedArrays, "parameterTypeOptionalCustomModifiers", "parameterTypes"));
			}
			if (parameterTypeRequiredCustomModifiers != null && parameterTypeRequiredCustomModifiers.Length != parameterTypes.Length)
			{
				throw new ArgumentException(SR.Format(SR.Argument_MismatchedArrays, "parameterTypeRequiredCustomModifiers", "parameterTypes"));
			}
		}
		ThrowIfCreated();
		MethodBuilder methodBuilder = new MethodBuilder(name, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers, m_module, this);
		if (!m_isHiddenGlobalType && (methodBuilder.Attributes & MethodAttributes.SpecialName) != 0 && methodBuilder.Name.Equals(ConstructorInfo.ConstructorName))
		{
			m_constructorCount++;
		}
		m_listMethods.Add(methodBuilder);
		return methodBuilder;
	}

	[RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
	public MethodBuilder DefinePInvokeMethod(string name, string dllName, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
	{
		return DefinePInvokeMethodHelper(name, dllName, name, attributes, callingConvention, returnType, null, null, parameterTypes, null, null, nativeCallConv, nativeCharSet);
	}

	[RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
	public MethodBuilder DefinePInvokeMethod(string name, string dllName, string entryName, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
	{
		return DefinePInvokeMethodHelper(name, dllName, entryName, attributes, callingConvention, returnType, null, null, parameterTypes, null, null, nativeCallConv, nativeCharSet);
	}

	[RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
	public MethodBuilder DefinePInvokeMethod(string name, string dllName, string entryName, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers, CallingConvention nativeCallConv, CharSet nativeCharSet)
	{
		return DefinePInvokeMethodHelper(name, dllName, entryName, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers, nativeCallConv, nativeCharSet);
	}

	[RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
	private MethodBuilder DefinePInvokeMethodHelper(string name, string dllName, string importName, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers, CallingConvention nativeCallConv, CharSet nativeCharSet)
	{
		AssemblyBuilder.CheckContext(returnType);
		AssemblyBuilder.CheckContext(returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes);
		AssemblyBuilder.CheckContext(parameterTypeRequiredCustomModifiers);
		AssemblyBuilder.CheckContext(parameterTypeOptionalCustomModifiers);
		lock (SyncRoot)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (name.Length == 0)
			{
				throw new ArgumentException(SR.Argument_EmptyName, "name");
			}
			if (dllName == null)
			{
				throw new ArgumentNullException("dllName");
			}
			if (dllName.Length == 0)
			{
				throw new ArgumentException(SR.Argument_EmptyName, "dllName");
			}
			if (importName == null)
			{
				throw new ArgumentNullException("importName");
			}
			if (importName.Length == 0)
			{
				throw new ArgumentException(SR.Argument_EmptyName, "importName");
			}
			if ((attributes & MethodAttributes.Abstract) != 0)
			{
				throw new ArgumentException(SR.Argument_BadPInvokeMethod);
			}
			if ((m_iAttr & TypeAttributes.ClassSemanticsMask) == TypeAttributes.ClassSemanticsMask)
			{
				throw new ArgumentException(SR.Argument_BadPInvokeOnInterface);
			}
			ThrowIfCreated();
			attributes |= MethodAttributes.PinvokeImpl;
			MethodBuilder methodBuilder = new MethodBuilder(name, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers, m_module, this);
			methodBuilder.GetMethodSignature().InternalGetSignature(out var _);
			if (m_listMethods.Contains(methodBuilder))
			{
				throw new ArgumentException(SR.Argument_MethodRedefined);
			}
			m_listMethods.Add(methodBuilder);
			int metadataToken = methodBuilder.MetadataToken;
			int num = 0;
			switch (nativeCallConv)
			{
			case CallingConvention.Winapi:
				num = 256;
				break;
			case CallingConvention.Cdecl:
				num = 512;
				break;
			case CallingConvention.StdCall:
				num = 768;
				break;
			case CallingConvention.ThisCall:
				num = 1024;
				break;
			case CallingConvention.FastCall:
				num = 1280;
				break;
			}
			switch (nativeCharSet)
			{
			case CharSet.None:
				num |= 0;
				break;
			case CharSet.Ansi:
				num |= 2;
				break;
			case CharSet.Unicode:
				num |= 4;
				break;
			case CharSet.Auto:
				num |= 6;
				break;
			}
			ModuleBuilder module = m_module;
			SetPInvokeData(new QCallModule(ref module), dllName, importName, metadataToken, num);
			methodBuilder.SetToken(metadataToken);
			return methodBuilder;
		}
	}

	public ConstructorBuilder DefineTypeInitializer()
	{
		lock (SyncRoot)
		{
			return DefineTypeInitializerNoLock();
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2082:UnrecognizedReflectionPattern", Justification = "Reflection.Emit is not subject to trimming")]
	private ConstructorBuilder DefineTypeInitializerNoLock()
	{
		ThrowIfCreated();
		return new ConstructorBuilder(ConstructorInfo.TypeConstructorName, MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName, CallingConventions.Standard, null, m_module, this);
	}

	public ConstructorBuilder DefineDefaultConstructor(MethodAttributes attributes)
	{
		if ((m_iAttr & TypeAttributes.ClassSemanticsMask) == TypeAttributes.ClassSemanticsMask)
		{
			throw new InvalidOperationException(SR.InvalidOperation_ConstructorNotAllowedOnInterface);
		}
		lock (SyncRoot)
		{
			return DefineDefaultConstructorNoLock(attributes);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:UnrecognizedReflectionPattern", Justification = "MakeGenericType is only called on a TypeBuilderInstantiation which is not subject to trimming")]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern", Justification = "GetConstructor is only called on a TypeBuilderInstantiation which is not subject to trimming")]
	private ConstructorBuilder DefineDefaultConstructorNoLock(MethodAttributes attributes)
	{
		ConstructorInfo constructorInfo = null;
		if (m_typeParent is TypeBuilderInstantiation)
		{
			Type type = m_typeParent.GetGenericTypeDefinition();
			if (type is TypeBuilder)
			{
				type = ((TypeBuilder)type).m_bakedRuntimeType;
			}
			if (type == null)
			{
				throw new NotSupportedException(SR.NotSupported_DynamicModule);
			}
			Type type2 = type.MakeGenericType(m_typeParent.GetGenericArguments());
			constructorInfo = ((!(type2 is TypeBuilderInstantiation)) ? type2.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) : GetConstructor(type2, type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null)));
		}
		if (constructorInfo == null)
		{
			constructorInfo = m_typeParent.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
		}
		if (constructorInfo == null)
		{
			throw new NotSupportedException(SR.NotSupported_NoParentDefaultConstructor);
		}
		ConstructorBuilder constructorBuilder = DefineConstructor(attributes, CallingConventions.Standard, null);
		m_constructorCount++;
		ILGenerator iLGenerator = constructorBuilder.GetILGenerator();
		iLGenerator.Emit(OpCodes.Ldarg_0);
		iLGenerator.Emit(OpCodes.Call, constructorInfo);
		iLGenerator.Emit(OpCodes.Ret);
		constructorBuilder.m_isDefaultConstructor = true;
		return constructorBuilder;
	}

	public ConstructorBuilder DefineConstructor(MethodAttributes attributes, CallingConventions callingConvention, Type[]? parameterTypes)
	{
		return DefineConstructor(attributes, callingConvention, parameterTypes, null, null);
	}

	public ConstructorBuilder DefineConstructor(MethodAttributes attributes, CallingConventions callingConvention, Type[]? parameterTypes, Type[][]? requiredCustomModifiers, Type[][]? optionalCustomModifiers)
	{
		if ((m_iAttr & TypeAttributes.ClassSemanticsMask) == TypeAttributes.ClassSemanticsMask && (attributes & MethodAttributes.Static) != MethodAttributes.Static)
		{
			throw new InvalidOperationException(SR.InvalidOperation_ConstructorNotAllowedOnInterface);
		}
		lock (SyncRoot)
		{
			return DefineConstructorNoLock(attributes, callingConvention, parameterTypes, requiredCustomModifiers, optionalCustomModifiers);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2082:UnrecognizedReflectionPattern", Justification = "Reflection.Emit is not subject to trimming")]
	private ConstructorBuilder DefineConstructorNoLock(MethodAttributes attributes, CallingConventions callingConvention, Type[] parameterTypes, Type[][] requiredCustomModifiers, Type[][] optionalCustomModifiers)
	{
		AssemblyBuilder.CheckContext(parameterTypes);
		AssemblyBuilder.CheckContext(requiredCustomModifiers);
		AssemblyBuilder.CheckContext(optionalCustomModifiers);
		ThrowIfCreated();
		string name = (((attributes & MethodAttributes.Static) != 0) ? ConstructorInfo.TypeConstructorName : ConstructorInfo.ConstructorName);
		attributes |= MethodAttributes.SpecialName;
		ConstructorBuilder result = new ConstructorBuilder(name, attributes, callingConvention, parameterTypes, requiredCustomModifiers, optionalCustomModifiers, m_module, this);
		m_constructorCount++;
		return result;
	}

	public TypeBuilder DefineNestedType(string name)
	{
		lock (SyncRoot)
		{
			return DefineNestedTypeNoLock(name, TypeAttributes.NestedPrivate, null, null, PackingSize.Unspecified, 0);
		}
	}

	public TypeBuilder DefineNestedType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, Type[]? interfaces)
	{
		lock (SyncRoot)
		{
			AssemblyBuilder.CheckContext(parent);
			AssemblyBuilder.CheckContext(interfaces);
			return DefineNestedTypeNoLock(name, attr, parent, interfaces, PackingSize.Unspecified, 0);
		}
	}

	public TypeBuilder DefineNestedType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent)
	{
		lock (SyncRoot)
		{
			return DefineNestedTypeNoLock(name, attr, parent, null, PackingSize.Unspecified, 0);
		}
	}

	public TypeBuilder DefineNestedType(string name, TypeAttributes attr)
	{
		lock (SyncRoot)
		{
			return DefineNestedTypeNoLock(name, attr, null, null, PackingSize.Unspecified, 0);
		}
	}

	public TypeBuilder DefineNestedType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, int typeSize)
	{
		lock (SyncRoot)
		{
			return DefineNestedTypeNoLock(name, attr, parent, null, PackingSize.Unspecified, typeSize);
		}
	}

	public TypeBuilder DefineNestedType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, PackingSize packSize)
	{
		lock (SyncRoot)
		{
			return DefineNestedTypeNoLock(name, attr, parent, null, packSize, 0);
		}
	}

	public TypeBuilder DefineNestedType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, PackingSize packSize, int typeSize)
	{
		lock (SyncRoot)
		{
			return DefineNestedTypeNoLock(name, attr, parent, null, packSize, typeSize);
		}
	}

	private TypeBuilder DefineNestedTypeNoLock(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type parent, Type[] interfaces, PackingSize packSize, int typeSize)
	{
		return new TypeBuilder(name, attr, parent, interfaces, m_module, packSize, typeSize, this);
	}

	public FieldBuilder DefineField(string fieldName, Type type, FieldAttributes attributes)
	{
		return DefineField(fieldName, type, null, null, attributes);
	}

	public FieldBuilder DefineField(string fieldName, Type type, Type[]? requiredCustomModifiers, Type[]? optionalCustomModifiers, FieldAttributes attributes)
	{
		lock (SyncRoot)
		{
			return DefineFieldNoLock(fieldName, type, requiredCustomModifiers, optionalCustomModifiers, attributes);
		}
	}

	private FieldBuilder DefineFieldNoLock(string fieldName, Type type, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers, FieldAttributes attributes)
	{
		ThrowIfCreated();
		AssemblyBuilder.CheckContext(type);
		AssemblyBuilder.CheckContext(requiredCustomModifiers);
		if (m_enumUnderlyingType == null && IsEnum && (attributes & FieldAttributes.Static) == 0)
		{
			m_enumUnderlyingType = type;
		}
		return new FieldBuilder(this, fieldName, type, requiredCustomModifiers, optionalCustomModifiers, attributes);
	}

	public FieldBuilder DefineInitializedData(string name, byte[] data, FieldAttributes attributes)
	{
		lock (SyncRoot)
		{
			return DefineInitializedDataNoLock(name, data, attributes);
		}
	}

	private FieldBuilder DefineInitializedDataNoLock(string name, byte[] data, FieldAttributes attributes)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		return DefineDataHelper(name, data, data.Length, attributes);
	}

	public FieldBuilder DefineUninitializedData(string name, int size, FieldAttributes attributes)
	{
		lock (SyncRoot)
		{
			return DefineUninitializedDataNoLock(name, size, attributes);
		}
	}

	private FieldBuilder DefineUninitializedDataNoLock(string name, int size, FieldAttributes attributes)
	{
		return DefineDataHelper(name, null, size, attributes);
	}

	public PropertyBuilder DefineProperty(string name, PropertyAttributes attributes, Type returnType, Type[]? parameterTypes)
	{
		return DefineProperty(name, attributes, returnType, null, null, parameterTypes, null, null);
	}

	public PropertyBuilder DefineProperty(string name, PropertyAttributes attributes, CallingConventions callingConvention, Type returnType, Type[]? parameterTypes)
	{
		return DefineProperty(name, attributes, callingConvention, returnType, null, null, parameterTypes, null, null);
	}

	public PropertyBuilder DefineProperty(string name, PropertyAttributes attributes, Type returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
	{
		return DefineProperty(name, attributes, (CallingConventions)0, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);
	}

	public PropertyBuilder DefineProperty(string name, PropertyAttributes attributes, CallingConventions callingConvention, Type returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
	{
		lock (SyncRoot)
		{
			return DefinePropertyNoLock(name, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);
		}
	}

	private PropertyBuilder DefinePropertyNoLock(string name, PropertyAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyName, "name");
		}
		AssemblyBuilder.CheckContext(returnType);
		AssemblyBuilder.CheckContext(returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes);
		AssemblyBuilder.CheckContext(parameterTypeRequiredCustomModifiers);
		AssemblyBuilder.CheckContext(parameterTypeOptionalCustomModifiers);
		ThrowIfCreated();
		SignatureHelper propertySigHelper = SignatureHelper.GetPropertySigHelper(m_module, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);
		int length;
		byte[] signature = propertySigHelper.InternalGetSignature(out length);
		ModuleBuilder module = m_module;
		int prToken = DefineProperty(new QCallModule(ref module), m_tdType, name, attributes, signature, length);
		return new PropertyBuilder(m_module, name, propertySigHelper, attributes, returnType, prToken, this);
	}

	public EventBuilder DefineEvent(string name, EventAttributes attributes, Type eventtype)
	{
		lock (SyncRoot)
		{
			return DefineEventNoLock(name, attributes, eventtype);
		}
	}

	private EventBuilder DefineEventNoLock(string name, EventAttributes attributes, Type eventtype)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyName, "name");
		}
		if (name[0] == '\0')
		{
			throw new ArgumentException(SR.Argument_IllegalName, "name");
		}
		AssemblyBuilder.CheckContext(eventtype);
		ThrowIfCreated();
		int typeTokenInternal = m_module.GetTypeTokenInternal(eventtype);
		ModuleBuilder module = m_module;
		int evToken = DefineEvent(new QCallModule(ref module), m_tdType, name, attributes, typeTokenInternal);
		return new EventBuilder(m_module, name, attributes, this, evToken);
	}

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	public TypeInfo? CreateTypeInfo()
	{
		lock (SyncRoot)
		{
			return CreateTypeNoLock();
		}
	}

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	public Type? CreateType()
	{
		lock (SyncRoot)
		{
			return CreateTypeNoLock();
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2083:UnrecognizedReflectionPattern", Justification = "Reflection.Emit is not subject to trimming")]
	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	private TypeInfo CreateTypeNoLock()
	{
		if (IsCreated())
		{
			return m_bakedRuntimeType;
		}
		if (m_typeInterfaces == null)
		{
			m_typeInterfaces = new List<Type>();
		}
		int[] array = new int[m_typeInterfaces.Count];
		for (int i = 0; i < m_typeInterfaces.Count; i++)
		{
			array[i] = m_module.GetTypeTokenInternal(m_typeInterfaces[i]);
		}
		int num = 0;
		if (m_typeParent != null)
		{
			num = m_module.GetTypeTokenInternal(m_typeParent);
		}
		ModuleBuilder module = m_module;
		if (IsGenericParameter)
		{
			int[] array2;
			if (m_typeParent != null)
			{
				array2 = new int[m_typeInterfaces.Count + 2];
				array2[^2] = num;
			}
			else
			{
				array2 = new int[m_typeInterfaces.Count + 1];
			}
			for (int j = 0; j < m_typeInterfaces.Count; j++)
			{
				array2[j] = m_module.GetTypeTokenInternal(m_typeInterfaces[j]);
			}
			int tkParent = ((m_declMeth == null) ? m_DeclaringType.m_tdType : m_declMeth.MetadataToken);
			m_tdType = DefineGenericParam(new QCallModule(ref module), m_strName, tkParent, m_genParamAttributes, m_genParamPos, array2);
			if (m_ca != null)
			{
				foreach (CustAttr item in m_ca)
				{
					item.Bake(m_module, MetadataToken);
				}
			}
			m_hasBeenCreated = true;
			return this;
		}
		if (((uint)m_tdType & 0xFFFFFFu) != 0 && ((uint)num & 0xFFFFFFu) != 0)
		{
			SetParentType(new QCallModule(ref module), m_tdType, num);
		}
		if (m_inst != null)
		{
			GenericTypeParameterBuilder[] inst = m_inst;
			foreach (GenericTypeParameterBuilder genericTypeParameterBuilder in inst)
			{
				genericTypeParameterBuilder.m_type.CreateType();
			}
		}
		if (!m_isHiddenGlobalType && m_constructorCount == 0 && (m_iAttr & TypeAttributes.ClassSemanticsMask) == 0 && !base.IsValueType && (m_iAttr & (TypeAttributes.Abstract | TypeAttributes.Sealed)) != (TypeAttributes.Abstract | TypeAttributes.Sealed))
		{
			DefineDefaultConstructor(MethodAttributes.Public);
		}
		int count = m_listMethods.Count;
		for (int l = 0; l < count; l++)
		{
			MethodBuilder methodBuilder = m_listMethods[l];
			if (methodBuilder.IsGenericMethodDefinition)
			{
				int metadataToken = methodBuilder.MetadataToken;
			}
			MethodAttributes attributes = methodBuilder.Attributes;
			if ((methodBuilder.GetMethodImplementationFlags() & (MethodImplAttributes)135) != 0 || (attributes & MethodAttributes.PinvokeImpl) != 0)
			{
				continue;
			}
			int signatureLength;
			byte[] localSignature = methodBuilder.GetLocalSignature(out signatureLength);
			if ((attributes & MethodAttributes.Abstract) != 0 && (m_iAttr & TypeAttributes.Abstract) == 0)
			{
				throw new InvalidOperationException(SR.InvalidOperation_BadTypeAttributesNotAbstract);
			}
			byte[] body = methodBuilder.GetBody();
			if ((attributes & MethodAttributes.Abstract) != 0)
			{
				if (body != null)
				{
					throw new InvalidOperationException(SR.Format(SR.InvalidOperation_BadMethodBody, methodBuilder.Name));
				}
			}
			else if (body == null || body.Length == 0)
			{
				if (methodBuilder.m_ilGenerator != null)
				{
					methodBuilder.CreateMethodBodyHelper(methodBuilder.GetILGenerator());
				}
				body = methodBuilder.GetBody();
				if ((body == null || body.Length == 0) && !methodBuilder.m_canBeRuntimeImpl)
				{
					throw new InvalidOperationException(SR.Format(SR.InvalidOperation_BadEmptyMethodBody, methodBuilder.Name));
				}
			}
			int maxStack = methodBuilder.GetMaxStack();
			ExceptionHandler[] exceptionHandlers = methodBuilder.GetExceptionHandlers();
			int[] tokenFixups = methodBuilder.GetTokenFixups();
			SetMethodIL(new QCallModule(ref module), methodBuilder.MetadataToken, methodBuilder.InitLocals, body, (body != null) ? body.Length : 0, localSignature, signatureLength, maxStack, exceptionHandlers, (exceptionHandlers != null) ? exceptionHandlers.Length : 0, tokenFixups, (tokenFixups != null) ? tokenFixups.Length : 0);
			if (m_module.ContainingAssemblyBuilder._assemblyData._access == AssemblyBuilderAccess.Run)
			{
				methodBuilder.ReleaseBakedStructures();
			}
		}
		m_hasBeenCreated = true;
		RuntimeType o = null;
		TermCreateClass(new QCallModule(ref module), m_tdType, ObjectHandleOnStack.Create(ref o));
		if (!m_isHiddenGlobalType)
		{
			m_bakedRuntimeType = o;
			if (m_DeclaringType != null && m_DeclaringType.m_bakedRuntimeType != null)
			{
				m_DeclaringType.m_bakedRuntimeType.InvalidateCachedNestedType();
			}
			return o;
		}
		return null;
	}

	public void SetParent([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent)
	{
		ThrowIfCreated();
		if (parent != null)
		{
			AssemblyBuilder.CheckContext(parent);
			if (parent.IsInterface)
			{
				throw new ArgumentException(SR.Argument_CannotSetParentToInterface);
			}
			m_typeParent = parent;
		}
		else if ((m_iAttr & TypeAttributes.ClassSemanticsMask) != TypeAttributes.ClassSemanticsMask)
		{
			m_typeParent = typeof(object);
		}
		else
		{
			if ((m_iAttr & TypeAttributes.Abstract) == 0)
			{
				throw new InvalidOperationException(SR.InvalidOperation_BadInterfaceNotAbstract);
			}
			m_typeParent = null;
		}
	}

	public void AddInterfaceImplementation([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type interfaceType)
	{
		if (interfaceType == null)
		{
			throw new ArgumentNullException("interfaceType");
		}
		AssemblyBuilder.CheckContext(interfaceType);
		ThrowIfCreated();
		int typeTokenInternal = m_module.GetTypeTokenInternal(interfaceType);
		ModuleBuilder module = m_module;
		AddInterfaceImpl(new QCallModule(ref module), m_tdType, typeTokenInternal);
		m_typeInterfaces.Add(interfaceType);
	}

	public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
	{
		if (con == null)
		{
			throw new ArgumentNullException("con");
		}
		if (binaryAttribute == null)
		{
			throw new ArgumentNullException("binaryAttribute");
		}
		DefineCustomAttribute(m_module, m_tdType, m_module.GetConstructorToken(con), binaryAttribute);
	}

	public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
	{
		if (customBuilder == null)
		{
			throw new ArgumentNullException("customBuilder");
		}
		customBuilder.CreateCustomAttribute(m_module, m_tdType);
	}
}
