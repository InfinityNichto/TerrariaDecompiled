using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace System;

public abstract class Type : MemberInfo, IReflect
{
	private static volatile Binder s_defaultBinder;

	public static readonly char Delimiter = '.';

	public static readonly Type[] EmptyTypes = Array.Empty<Type>();

	public static readonly object Missing = System.Reflection.Missing.Value;

	public static readonly MemberFilter FilterAttribute = FilterAttributeImpl;

	public static readonly MemberFilter FilterName = (MemberInfo m, object c) => FilterNameImpl(m, c, StringComparison.Ordinal);

	public static readonly MemberFilter FilterNameIgnoreCase = (MemberInfo m, object c) => FilterNameImpl(m, c, StringComparison.OrdinalIgnoreCase);

	public bool IsInterface
	{
		get
		{
			if (this is RuntimeType type)
			{
				return RuntimeTypeHandle.IsInterface(type);
			}
			return (GetAttributeFlagsImpl() & TypeAttributes.ClassSemanticsMask) == TypeAttributes.ClassSemanticsMask;
		}
	}

	public override MemberTypes MemberType => MemberTypes.TypeInfo;

	public abstract string? Namespace { get; }

	public abstract string? AssemblyQualifiedName { get; }

	public abstract string? FullName { get; }

	public abstract Assembly Assembly { get; }

	public new abstract Module Module { get; }

	public bool IsNested => DeclaringType != null;

	public override Type? DeclaringType => null;

	public virtual MethodBase? DeclaringMethod => null;

	public override Type? ReflectedType => null;

	public abstract Type UnderlyingSystemType { get; }

	public virtual bool IsTypeDefinition
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public bool IsArray => IsArrayImpl();

	public bool IsByRef => IsByRefImpl();

	public bool IsPointer => IsPointerImpl();

	public virtual bool IsConstructedGenericType
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public virtual bool IsGenericParameter => false;

	public virtual bool IsGenericTypeParameter
	{
		get
		{
			if (IsGenericParameter)
			{
				return (object)DeclaringMethod == null;
			}
			return false;
		}
	}

	public virtual bool IsGenericMethodParameter
	{
		get
		{
			if (IsGenericParameter)
			{
				return DeclaringMethod != null;
			}
			return false;
		}
	}

	public virtual bool IsGenericType => false;

	public virtual bool IsGenericTypeDefinition => false;

	public virtual bool IsSZArray
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public virtual bool IsVariableBoundArray
	{
		get
		{
			if (IsArray)
			{
				return !IsSZArray;
			}
			return false;
		}
	}

	public virtual bool IsByRefLike
	{
		get
		{
			throw new NotSupportedException(SR.NotSupported_SubclassOverride);
		}
	}

	public bool HasElementType => HasElementTypeImpl();

	public virtual Type[] GenericTypeArguments
	{
		get
		{
			if (!IsGenericType || IsGenericTypeDefinition)
			{
				return EmptyTypes;
			}
			return GetGenericArguments();
		}
	}

	public virtual int GenericParameterPosition
	{
		get
		{
			throw new InvalidOperationException(SR.Arg_NotGenericParameter);
		}
	}

	public virtual GenericParameterAttributes GenericParameterAttributes
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	public TypeAttributes Attributes => GetAttributeFlagsImpl();

	public bool IsAbstract => (GetAttributeFlagsImpl() & TypeAttributes.Abstract) != 0;

	public bool IsImport => (GetAttributeFlagsImpl() & TypeAttributes.Import) != 0;

	public bool IsSealed => (GetAttributeFlagsImpl() & TypeAttributes.Sealed) != 0;

	public bool IsSpecialName => (GetAttributeFlagsImpl() & TypeAttributes.SpecialName) != 0;

	public bool IsClass
	{
		get
		{
			if ((GetAttributeFlagsImpl() & TypeAttributes.ClassSemanticsMask) == 0)
			{
				return !IsValueType;
			}
			return false;
		}
	}

	public bool IsNestedAssembly => (GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedAssembly;

	public bool IsNestedFamANDAssem => (GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamANDAssem;

	public bool IsNestedFamily => (GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamily;

	public bool IsNestedFamORAssem => (GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.VisibilityMask;

	public bool IsNestedPrivate => (GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPrivate;

	public bool IsNestedPublic => (GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPublic;

	public bool IsNotPublic => (GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == 0;

	public bool IsPublic => (GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.Public;

	public bool IsAutoLayout => (GetAttributeFlagsImpl() & TypeAttributes.LayoutMask) == 0;

	public bool IsExplicitLayout => (GetAttributeFlagsImpl() & TypeAttributes.LayoutMask) == TypeAttributes.ExplicitLayout;

	public bool IsLayoutSequential => (GetAttributeFlagsImpl() & TypeAttributes.LayoutMask) == TypeAttributes.SequentialLayout;

	public bool IsAnsiClass => (GetAttributeFlagsImpl() & TypeAttributes.StringFormatMask) == 0;

	public bool IsAutoClass => (GetAttributeFlagsImpl() & TypeAttributes.StringFormatMask) == TypeAttributes.AutoClass;

	public bool IsUnicodeClass => (GetAttributeFlagsImpl() & TypeAttributes.StringFormatMask) == TypeAttributes.UnicodeClass;

	public bool IsCOMObject => IsCOMObjectImpl();

	public bool IsContextful => IsContextfulImpl();

	public virtual bool IsEnum => IsSubclassOf(typeof(Enum));

	public bool IsMarshalByRef => IsMarshalByRefImpl();

	public bool IsPrimitive => IsPrimitiveImpl();

	public bool IsValueType
	{
		[Intrinsic]
		get
		{
			return IsValueTypeImpl();
		}
	}

	public virtual bool IsSignatureType => false;

	public virtual bool IsSecurityCritical
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public virtual bool IsSecuritySafeCritical
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public virtual bool IsSecurityTransparent
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public virtual StructLayoutAttribute? StructLayoutAttribute
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	public ConstructorInfo? TypeInitializer
	{
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
		get
		{
			return GetConstructorImpl(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, CallingConventions.Any, EmptyTypes, null);
		}
	}

	public virtual RuntimeTypeHandle TypeHandle
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	public abstract Guid GUID { get; }

	public abstract Type? BaseType { get; }

	public static Binder DefaultBinder
	{
		get
		{
			if (s_defaultBinder == null)
			{
				DefaultBinder value = new DefaultBinder();
				Interlocked.CompareExchange(ref s_defaultBinder, value, null);
			}
			return s_defaultBinder;
		}
	}

	public virtual bool IsSerializable
	{
		get
		{
			if ((GetAttributeFlagsImpl() & TypeAttributes.Serializable) != 0)
			{
				return true;
			}
			Type type = UnderlyingSystemType;
			if (type.IsRuntimeImplemented())
			{
				do
				{
					if (type == typeof(Delegate) || type == typeof(Enum))
					{
						return true;
					}
					type = type.BaseType;
				}
				while (type != null);
			}
			return false;
		}
	}

	public virtual bool ContainsGenericParameters
	{
		get
		{
			if (HasElementType)
			{
				return GetRootElementType().ContainsGenericParameters;
			}
			if (IsGenericParameter)
			{
				return true;
			}
			if (!IsGenericType)
			{
				return false;
			}
			Type[] genericArguments = GetGenericArguments();
			for (int i = 0; i < genericArguments.Length; i++)
			{
				if (genericArguments[i].ContainsGenericParameters)
				{
					return true;
				}
			}
			return false;
		}
	}

	public bool IsVisible
	{
		get
		{
			if (this is RuntimeType type)
			{
				return RuntimeTypeHandle.IsVisible(type);
			}
			if (IsGenericParameter)
			{
				return true;
			}
			if (HasElementType)
			{
				return GetElementType().IsVisible;
			}
			Type type2 = this;
			while (type2.IsNested)
			{
				if (!type2.IsNestedPublic)
				{
					return false;
				}
				type2 = type2.DeclaringType;
			}
			if (!type2.IsPublic)
			{
				return false;
			}
			if (IsGenericType && !IsGenericTypeDefinition)
			{
				Type[] genericArguments = GetGenericArguments();
				foreach (Type type3 in genericArguments)
				{
					if (!type3.IsVisible)
					{
						return false;
					}
				}
			}
			return true;
		}
	}

	[RequiresUnreferencedCode("The type might be removed")]
	public static Type? GetType(string typeName, bool throwOnError, bool ignoreCase)
	{
		StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
		return RuntimeType.GetType(typeName, throwOnError, ignoreCase, ref stackMark);
	}

	[RequiresUnreferencedCode("The type might be removed")]
	public static Type? GetType(string typeName, bool throwOnError)
	{
		StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
		return RuntimeType.GetType(typeName, throwOnError, ignoreCase: false, ref stackMark);
	}

	[RequiresUnreferencedCode("The type might be removed")]
	public static Type? GetType(string typeName)
	{
		StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
		return RuntimeType.GetType(typeName, throwOnError: false, ignoreCase: false, ref stackMark);
	}

	[RequiresUnreferencedCode("The type might be removed")]
	public static Type? GetType(string typeName, Func<AssemblyName, Assembly?>? assemblyResolver, Func<Assembly?, string, bool, Type?>? typeResolver)
	{
		StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
		return TypeNameParser.GetType(typeName, assemblyResolver, typeResolver, throwOnError: false, ignoreCase: false, ref stackMark);
	}

	[RequiresUnreferencedCode("The type might be removed")]
	public static Type? GetType(string typeName, Func<AssemblyName, Assembly?>? assemblyResolver, Func<Assembly?, string, bool, Type?>? typeResolver, bool throwOnError)
	{
		StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
		return TypeNameParser.GetType(typeName, assemblyResolver, typeResolver, throwOnError, ignoreCase: false, ref stackMark);
	}

	[RequiresUnreferencedCode("The type might be removed")]
	public static Type? GetType(string typeName, Func<AssemblyName, Assembly?>? assemblyResolver, Func<Assembly?, string, bool, Type?>? typeResolver, bool throwOnError, bool ignoreCase)
	{
		StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
		return TypeNameParser.GetType(typeName, assemblyResolver, typeResolver, throwOnError, ignoreCase, ref stackMark);
	}

	internal virtual RuntimeTypeHandle GetTypeHandleInternal()
	{
		return TypeHandle;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern RuntimeType GetTypeFromHandleUnsafe(IntPtr handle);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern Type GetTypeFromHandle(RuntimeTypeHandle handle);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern bool operator ==(Type? left, Type? right);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern bool operator !=(Type? left, Type? right);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal bool IsRuntimeImplemented()
	{
		return this is RuntimeType;
	}

	public new Type GetType()
	{
		return base.GetType();
	}

	protected abstract bool IsArrayImpl();

	protected abstract bool IsByRefImpl();

	protected abstract bool IsPointerImpl();

	protected abstract bool HasElementTypeImpl();

	public abstract Type? GetElementType();

	public virtual int GetArrayRank()
	{
		throw new NotSupportedException(SR.NotSupported_SubclassOverride);
	}

	public virtual Type GetGenericTypeDefinition()
	{
		throw new NotSupportedException(SR.NotSupported_SubclassOverride);
	}

	public virtual Type[] GetGenericArguments()
	{
		throw new NotSupportedException(SR.NotSupported_SubclassOverride);
	}

	public virtual Type[] GetGenericParameterConstraints()
	{
		if (!IsGenericParameter)
		{
			throw new InvalidOperationException(SR.Arg_NotGenericParameter);
		}
		throw new InvalidOperationException();
	}

	protected abstract TypeAttributes GetAttributeFlagsImpl();

	protected abstract bool IsCOMObjectImpl();

	protected virtual bool IsContextfulImpl()
	{
		return false;
	}

	protected virtual bool IsMarshalByRefImpl()
	{
		return false;
	}

	protected abstract bool IsPrimitiveImpl();

	[Intrinsic]
	public bool IsAssignableTo([NotNullWhen(true)] Type? targetType)
	{
		return targetType?.IsAssignableFrom(this) ?? false;
	}

	protected virtual bool IsValueTypeImpl()
	{
		return IsSubclassOf(typeof(ValueType));
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	public ConstructorInfo? GetConstructor(Type[] types)
	{
		return GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, types, null);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
	public ConstructorInfo? GetConstructor(BindingFlags bindingAttr, Type[] types)
	{
		return GetConstructor(bindingAttr, null, types, null);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
	public ConstructorInfo? GetConstructor(BindingFlags bindingAttr, Binder? binder, Type[] types, ParameterModifier[]? modifiers)
	{
		return GetConstructor(bindingAttr, binder, CallingConventions.Any, types, modifiers);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
	public ConstructorInfo? GetConstructor(BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers)
	{
		if (types == null)
		{
			throw new ArgumentNullException("types");
		}
		for (int i = 0; i < types.Length; i++)
		{
			if (types[i] == null)
			{
				throw new ArgumentNullException("types");
			}
		}
		return GetConstructorImpl(bindingAttr, binder, callConvention, types, modifiers);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
	protected abstract ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers);

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	public ConstructorInfo[] GetConstructors()
	{
		return GetConstructors(BindingFlags.Instance | BindingFlags.Public);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
	public abstract ConstructorInfo[] GetConstructors(BindingFlags bindingAttr);

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
	public EventInfo? GetEvent(string name)
	{
		return GetEvent(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public abstract EventInfo? GetEvent(string name, BindingFlags bindingAttr);

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
	public virtual EventInfo[] GetEvents()
	{
		return GetEvents(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public abstract EventInfo[] GetEvents(BindingFlags bindingAttr);

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
	public FieldInfo? GetField(string name)
	{
		return GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
	public abstract FieldInfo? GetField(string name, BindingFlags bindingAttr);

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
	public FieldInfo[] GetFields()
	{
		return GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
	public abstract FieldInfo[] GetFields(BindingFlags bindingAttr);

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicEvents)]
	public MemberInfo[] GetMember(string name)
	{
		return GetMember(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public virtual MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
	{
		return GetMember(name, MemberTypes.All, bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public virtual MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_SubclassOverride);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicEvents)]
	public MemberInfo[] GetMembers()
	{
		return GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2085:UnrecognizedReflectionPattern", Justification = "This is finding the MemberInfo with the same MetadataToken as specified MemberInfo. If the specified MemberInfo exists and wasn't trimmed, then the current Type's MemberInfo couldn't have been trimmed.")]
	public virtual MemberInfo GetMemberWithSameMetadataDefinitionAs(MemberInfo member)
	{
		if ((object)member == null)
		{
			throw new ArgumentNullException("member");
		}
		MemberInfo[] members = GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (MemberInfo memberInfo in members)
		{
			if (memberInfo.HasSameMetadataDefinitionAs(member))
			{
				return memberInfo;
			}
		}
		throw CreateGetMemberWithSameMetadataDefinitionAsNotFoundException(member);
	}

	private protected static ArgumentException CreateGetMemberWithSameMetadataDefinitionAsNotFoundException(MemberInfo member)
	{
		return new ArgumentException(SR.Format(SR.Arg_MemberInfoNotFound, member.Name), "member");
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public abstract MemberInfo[] GetMembers(BindingFlags bindingAttr);

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
	public MethodInfo? GetMethod(string name)
	{
		return GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	public MethodInfo? GetMethod(string name, BindingFlags bindingAttr)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		return GetMethodImpl(name, bindingAttr, null, CallingConventions.Any, null, null);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	public MethodInfo? GetMethod(string name, BindingFlags bindingAttr, Type[] types)
	{
		return GetMethod(name, bindingAttr, null, types, null);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
	public MethodInfo? GetMethod(string name, Type[] types)
	{
		return GetMethod(name, types, null);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
	public MethodInfo? GetMethod(string name, Type[] types, ParameterModifier[]? modifiers)
	{
		return GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, null, types, modifiers);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	public MethodInfo? GetMethod(string name, BindingFlags bindingAttr, Binder? binder, Type[] types, ParameterModifier[]? modifiers)
	{
		return GetMethod(name, bindingAttr, binder, CallingConventions.Any, types, modifiers);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	public MethodInfo? GetMethod(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (types == null)
		{
			throw new ArgumentNullException("types");
		}
		for (int i = 0; i < types.Length; i++)
		{
			if (types[i] == null)
			{
				throw new ArgumentNullException("types");
			}
		}
		return GetMethodImpl(name, bindingAttr, binder, callConvention, types, modifiers);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	protected abstract MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers);

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
	public MethodInfo? GetMethod(string name, int genericParameterCount, Type[] types)
	{
		return GetMethod(name, genericParameterCount, types, null);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
	public MethodInfo? GetMethod(string name, int genericParameterCount, Type[] types, ParameterModifier[]? modifiers)
	{
		return GetMethod(name, genericParameterCount, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, null, types, modifiers);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	public MethodInfo? GetMethod(string name, int genericParameterCount, BindingFlags bindingAttr, Binder? binder, Type[] types, ParameterModifier[]? modifiers)
	{
		return GetMethod(name, genericParameterCount, bindingAttr, binder, CallingConventions.Any, types, modifiers);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	public MethodInfo? GetMethod(string name, int genericParameterCount, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (genericParameterCount < 0)
		{
			throw new ArgumentException(SR.ArgumentOutOfRange_NeedNonNegNum, "genericParameterCount");
		}
		if (types == null)
		{
			throw new ArgumentNullException("types");
		}
		for (int i = 0; i < types.Length; i++)
		{
			if (types[i] == null)
			{
				throw new ArgumentNullException("types");
			}
		}
		return GetMethodImpl(name, genericParameterCount, bindingAttr, binder, callConvention, types, modifiers);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	protected virtual MethodInfo? GetMethodImpl(string name, int genericParameterCount, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
	public MethodInfo[] GetMethods()
	{
		return GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	public abstract MethodInfo[] GetMethods(BindingFlags bindingAttr);

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes)]
	public Type? GetNestedType(string name)
	{
		return GetNestedType(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
	public abstract Type? GetNestedType(string name, BindingFlags bindingAttr);

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes)]
	public Type[] GetNestedTypes()
	{
		return GetNestedTypes(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
	public abstract Type[] GetNestedTypes(BindingFlags bindingAttr);

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
	public PropertyInfo? GetProperty(string name)
	{
		return GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	public PropertyInfo? GetProperty(string name, BindingFlags bindingAttr)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		return GetPropertyImpl(name, bindingAttr, null, null, null, null);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2085:UnrecognizedReflectionPattern", Justification = "Linker doesn't recognize GetPropertyImpl(BindingFlags.Public) but this is what the body is doing")]
	public PropertyInfo? GetProperty(string name, Type? returnType)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		return GetPropertyImpl(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, null, returnType, null, null);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
	public PropertyInfo? GetProperty(string name, Type[] types)
	{
		return GetProperty(name, null, types);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
	public PropertyInfo? GetProperty(string name, Type? returnType, Type[] types)
	{
		return GetProperty(name, returnType, types, null);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
	public PropertyInfo? GetProperty(string name, Type? returnType, Type[] types, ParameterModifier[]? modifiers)
	{
		return GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, null, returnType, types, modifiers);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	public PropertyInfo? GetProperty(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[] types, ParameterModifier[]? modifiers)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (types == null)
		{
			throw new ArgumentNullException("types");
		}
		return GetPropertyImpl(name, bindingAttr, binder, returnType, types, modifiers);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	protected abstract PropertyInfo? GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers);

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
	public PropertyInfo[] GetProperties()
	{
		return GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	public abstract PropertyInfo[] GetProperties(BindingFlags bindingAttr);

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicEvents)]
	public virtual MemberInfo[] GetDefaultMembers()
	{
		throw NotImplemented.ByDesign;
	}

	public static RuntimeTypeHandle GetTypeHandle(object o)
	{
		if (o == null)
		{
			throw new ArgumentNullException(null, SR.Arg_InvalidHandle);
		}
		Type type = o.GetType();
		return type.TypeHandle;
	}

	public static Type[] GetTypeArray(object[] args)
	{
		if (args == null)
		{
			throw new ArgumentNullException("args");
		}
		Type[] array = new Type[args.Length];
		for (int i = 0; i < array.Length; i++)
		{
			if (args[i] == null)
			{
				throw new ArgumentException(SR.ArgumentNull_ArrayValue, "args");
			}
			array[i] = args[i].GetType();
		}
		return array;
	}

	public static TypeCode GetTypeCode(Type? type)
	{
		return type?.GetTypeCodeImpl() ?? TypeCode.Empty;
	}

	protected virtual TypeCode GetTypeCodeImpl()
	{
		Type underlyingSystemType = UnderlyingSystemType;
		if ((object)this != underlyingSystemType && (object)underlyingSystemType != null)
		{
			return GetTypeCode(underlyingSystemType);
		}
		return TypeCode.Object;
	}

	[SupportedOSPlatform("windows")]
	public static Type? GetTypeFromCLSID(Guid clsid)
	{
		return GetTypeFromCLSID(clsid, null, throwOnError: false);
	}

	[SupportedOSPlatform("windows")]
	public static Type? GetTypeFromCLSID(Guid clsid, bool throwOnError)
	{
		return GetTypeFromCLSID(clsid, null, throwOnError);
	}

	[SupportedOSPlatform("windows")]
	public static Type? GetTypeFromCLSID(Guid clsid, string? server)
	{
		return GetTypeFromCLSID(clsid, server, throwOnError: false);
	}

	[SupportedOSPlatform("windows")]
	public static Type? GetTypeFromCLSID(Guid clsid, string? server, bool throwOnError)
	{
		return Marshal.GetTypeFromCLSID(clsid, server, throwOnError);
	}

	[SupportedOSPlatform("windows")]
	public static Type? GetTypeFromProgID(string progID)
	{
		return GetTypeFromProgID(progID, null, throwOnError: false);
	}

	[SupportedOSPlatform("windows")]
	public static Type? GetTypeFromProgID(string progID, bool throwOnError)
	{
		return GetTypeFromProgID(progID, null, throwOnError);
	}

	[SupportedOSPlatform("windows")]
	public static Type? GetTypeFromProgID(string progID, string? server)
	{
		return GetTypeFromProgID(progID, server, throwOnError: false);
	}

	[SupportedOSPlatform("windows")]
	public static Type? GetTypeFromProgID(string progID, string? server, bool throwOnError)
	{
		return Marshal.GetTypeFromProgID(progID, server, throwOnError);
	}

	[DebuggerHidden]
	[DebuggerStepThrough]
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	public object? InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object?[]? args)
	{
		return InvokeMember(name, invokeAttr, binder, target, args, null, null, null);
	}

	[DebuggerHidden]
	[DebuggerStepThrough]
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	public object? InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object?[]? args, CultureInfo? culture)
	{
		return InvokeMember(name, invokeAttr, binder, target, args, null, culture, null);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	public abstract object? InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters);

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	public Type? GetInterface(string name)
	{
		return GetInterface(name, ignoreCase: false);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	public abstract Type? GetInterface(string name, bool ignoreCase);

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	public abstract Type[] GetInterfaces();

	public virtual InterfaceMapping GetInterfaceMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType)
	{
		throw new NotSupportedException(SR.NotSupported_SubclassOverride);
	}

	public virtual bool IsInstanceOfType([NotNullWhen(true)] object? o)
	{
		if (o != null)
		{
			return IsAssignableFrom(o.GetType());
		}
		return false;
	}

	public virtual bool IsEquivalentTo([NotNullWhen(true)] Type? other)
	{
		return this == other;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2085:UnrecognizedReflectionPattern", Justification = "The single instance field on enum types is never trimmed")]
	public virtual Type GetEnumUnderlyingType()
	{
		if (!IsEnum)
		{
			throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");
		}
		FieldInfo[] fields = GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (fields == null || fields.Length != 1)
		{
			throw new ArgumentException(SR.Argument_InvalidEnum, "enumType");
		}
		return fields[0].FieldType;
	}

	public virtual Array GetEnumValues()
	{
		if (!IsEnum)
		{
			throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");
		}
		throw NotImplemented.ByDesign;
	}

	public virtual Type MakeArrayType()
	{
		throw new NotSupportedException();
	}

	public virtual Type MakeArrayType(int rank)
	{
		throw new NotSupportedException();
	}

	public virtual Type MakeByRefType()
	{
		throw new NotSupportedException();
	}

	[RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
	public virtual Type MakeGenericType(params Type[] typeArguments)
	{
		throw new NotSupportedException(SR.NotSupported_SubclassOverride);
	}

	public virtual Type MakePointerType()
	{
		throw new NotSupportedException();
	}

	public static Type MakeGenericSignatureType(Type genericTypeDefinition, params Type[] typeArguments)
	{
		return new SignatureConstructedGenericType(genericTypeDefinition, typeArguments);
	}

	public static Type MakeGenericMethodParameter(int position)
	{
		if (position < 0)
		{
			throw new ArgumentException(SR.ArgumentOutOfRange_NeedNonNegNum, "position");
		}
		return new SignatureGenericMethodParameterType(position);
	}

	internal string FormatTypeName()
	{
		Type rootElementType = GetRootElementType();
		if (rootElementType.IsPrimitive || rootElementType.IsNested || rootElementType == typeof(void) || rootElementType == typeof(TypedReference))
		{
			return Name;
		}
		return ToString();
	}

	public override string ToString()
	{
		return "Type: " + Name;
	}

	public override bool Equals(object? o)
	{
		if (o != null)
		{
			return Equals(o as Type);
		}
		return false;
	}

	public override int GetHashCode()
	{
		Type underlyingSystemType = UnderlyingSystemType;
		if ((object)underlyingSystemType != this)
		{
			return underlyingSystemType.GetHashCode();
		}
		return base.GetHashCode();
	}

	public virtual bool Equals(Type? o)
	{
		if (!(o == null))
		{
			return (object)UnderlyingSystemType == o.UnderlyingSystemType;
		}
		return false;
	}

	[Obsolete("ReflectionOnly loading is not supported and throws PlatformNotSupportedException.", DiagnosticId = "SYSLIB0018", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static Type? ReflectionOnlyGetType(string typeName, bool throwIfNotFound, bool ignoreCase)
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_ReflectionOnly);
	}

	public virtual bool IsEnumDefined(object value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (!IsEnum)
		{
			throw new ArgumentException(SR.Arg_MustBeEnum, "value");
		}
		Type type = value.GetType();
		if (type.IsEnum)
		{
			if (!type.IsEquivalentTo(this))
			{
				throw new ArgumentException(SR.Format(SR.Arg_EnumAndObjectMustBeSameType, type, this));
			}
			type = type.GetEnumUnderlyingType();
		}
		if (type == typeof(string))
		{
			string[] enumNames = GetEnumNames();
			object[] array = enumNames;
			if (Array.IndexOf(array, value) >= 0)
			{
				return true;
			}
			return false;
		}
		if (IsIntegerType(type))
		{
			Type enumUnderlyingType = GetEnumUnderlyingType();
			if (enumUnderlyingType.GetTypeCodeImpl() != type.GetTypeCodeImpl())
			{
				throw new ArgumentException(SR.Format(SR.Arg_EnumUnderlyingTypeAndObjectMustBeSameType, type, enumUnderlyingType));
			}
			Array enumRawConstantValues = GetEnumRawConstantValues();
			return BinarySearch(enumRawConstantValues, value) >= 0;
		}
		throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
	}

	public virtual string? GetEnumName(object value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (!IsEnum)
		{
			throw new ArgumentException(SR.Arg_MustBeEnum, "value");
		}
		Type type = value.GetType();
		if (!type.IsEnum && !IsIntegerType(type))
		{
			throw new ArgumentException(SR.Arg_MustBeEnumBaseTypeOrEnum, "value");
		}
		Array enumRawConstantValues = GetEnumRawConstantValues();
		int num = BinarySearch(enumRawConstantValues, value);
		if (num >= 0)
		{
			string[] enumNames = GetEnumNames();
			return enumNames[num];
		}
		return null;
	}

	public virtual string[] GetEnumNames()
	{
		if (!IsEnum)
		{
			throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");
		}
		GetEnumData(out var enumNames, out var _);
		return enumNames;
	}

	private Array GetEnumRawConstantValues()
	{
		GetEnumData(out var _, out var enumValues);
		return enumValues;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2085:UnrecognizedReflectionPattern", Justification = "Literal fields on enums can never be trimmed")]
	private void GetEnumData(out string[] enumNames, out Array enumValues)
	{
		FieldInfo[] fields = GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		object[] array = new object[fields.Length];
		string[] array2 = new string[fields.Length];
		for (int i = 0; i < fields.Length; i++)
		{
			array2[i] = fields[i].Name;
			array[i] = fields[i].GetRawConstantValue();
		}
		Comparer @default = Comparer.Default;
		for (int j = 1; j < array.Length; j++)
		{
			int num = j;
			string text = array2[j];
			object obj = array[j];
			bool flag = false;
			while (@default.Compare(array[num - 1], obj) > 0)
			{
				array2[num] = array2[num - 1];
				array[num] = array[num - 1];
				num--;
				flag = true;
				if (num == 0)
				{
					break;
				}
			}
			if (flag)
			{
				array2[num] = text;
				array[num] = obj;
			}
		}
		enumNames = array2;
		enumValues = array;
	}

	private static int BinarySearch(Array array, object value)
	{
		ulong[] array2 = new ulong[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array2[i] = Enum.ToUInt64(array.GetValue(i));
		}
		ulong value2 = Enum.ToUInt64(value);
		return Array.BinarySearch(array2, value2);
	}

	internal static bool IsIntegerType(Type t)
	{
		if (!(t == typeof(int)) && !(t == typeof(short)) && !(t == typeof(ushort)) && !(t == typeof(byte)) && !(t == typeof(sbyte)) && !(t == typeof(uint)) && !(t == typeof(long)) && !(t == typeof(ulong)) && !(t == typeof(char)))
		{
			return t == typeof(bool);
		}
		return true;
	}

	internal Type GetRootElementType()
	{
		Type type = this;
		while (type.HasElementType)
		{
			type = type.GetElementType();
		}
		return type;
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	public virtual Type[] FindInterfaces(TypeFilter filter, object? filterCriteria)
	{
		if (filter == null)
		{
			throw new ArgumentNullException("filter");
		}
		Type[] interfaces = GetInterfaces();
		int num = 0;
		for (int i = 0; i < interfaces.Length; i++)
		{
			if (!filter(interfaces[i], filterCriteria))
			{
				interfaces[i] = null;
			}
			else
			{
				num++;
			}
		}
		if (num == interfaces.Length)
		{
			return interfaces;
		}
		Type[] array = new Type[num];
		num = 0;
		foreach (Type type in interfaces)
		{
			if ((object)type != null)
			{
				array[num++] = type;
			}
		}
		return array;
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	public virtual MemberInfo[] FindMembers(MemberTypes memberType, BindingFlags bindingAttr, MemberFilter? filter, object? filterCriteria)
	{
		MethodInfo[] array = null;
		ConstructorInfo[] array2 = null;
		FieldInfo[] array3 = null;
		PropertyInfo[] array4 = null;
		EventInfo[] array5 = null;
		Type[] array6 = null;
		int num = 0;
		if ((memberType & MemberTypes.Method) != 0)
		{
			array = GetMethods(bindingAttr);
			if (filter != null)
			{
				for (int i = 0; i < array.Length; i++)
				{
					if (!filter(array[i], filterCriteria))
					{
						array[i] = null;
					}
					else
					{
						num++;
					}
				}
			}
			else
			{
				num += array.Length;
			}
		}
		if ((memberType & MemberTypes.Constructor) != 0)
		{
			array2 = GetConstructors(bindingAttr);
			if (filter != null)
			{
				for (int i = 0; i < array2.Length; i++)
				{
					if (!filter(array2[i], filterCriteria))
					{
						array2[i] = null;
					}
					else
					{
						num++;
					}
				}
			}
			else
			{
				num += array2.Length;
			}
		}
		if ((memberType & MemberTypes.Field) != 0)
		{
			array3 = GetFields(bindingAttr);
			if (filter != null)
			{
				for (int i = 0; i < array3.Length; i++)
				{
					if (!filter(array3[i], filterCriteria))
					{
						array3[i] = null;
					}
					else
					{
						num++;
					}
				}
			}
			else
			{
				num += array3.Length;
			}
		}
		if ((memberType & MemberTypes.Property) != 0)
		{
			array4 = GetProperties(bindingAttr);
			if (filter != null)
			{
				for (int i = 0; i < array4.Length; i++)
				{
					if (!filter(array4[i], filterCriteria))
					{
						array4[i] = null;
					}
					else
					{
						num++;
					}
				}
			}
			else
			{
				num += array4.Length;
			}
		}
		if ((memberType & MemberTypes.Event) != 0)
		{
			array5 = GetEvents(bindingAttr);
			if (filter != null)
			{
				for (int i = 0; i < array5.Length; i++)
				{
					if (!filter(array5[i], filterCriteria))
					{
						array5[i] = null;
					}
					else
					{
						num++;
					}
				}
			}
			else
			{
				num += array5.Length;
			}
		}
		if ((memberType & MemberTypes.NestedType) != 0)
		{
			array6 = GetNestedTypes(bindingAttr);
			if (filter != null)
			{
				for (int i = 0; i < array6.Length; i++)
				{
					if (!filter(array6[i], filterCriteria))
					{
						array6[i] = null;
					}
					else
					{
						num++;
					}
				}
			}
			else
			{
				num += array6.Length;
			}
		}
		MemberInfo[] array7 = new MemberInfo[num];
		num = 0;
		if (array != null)
		{
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] != null)
				{
					array7[num++] = array[i];
				}
			}
		}
		if (array2 != null)
		{
			foreach (ConstructorInfo constructorInfo in array2)
			{
				if ((object)constructorInfo != null)
				{
					array7[num++] = constructorInfo;
				}
			}
		}
		if (array3 != null)
		{
			foreach (FieldInfo fieldInfo in array3)
			{
				if ((object)fieldInfo != null)
				{
					array7[num++] = fieldInfo;
				}
			}
		}
		if (array4 != null)
		{
			foreach (PropertyInfo propertyInfo in array4)
			{
				if ((object)propertyInfo != null)
				{
					array7[num++] = propertyInfo;
				}
			}
		}
		if (array5 != null)
		{
			foreach (EventInfo eventInfo in array5)
			{
				if ((object)eventInfo != null)
				{
					array7[num++] = eventInfo;
				}
			}
		}
		if (array6 != null)
		{
			foreach (Type type in array6)
			{
				if ((object)type != null)
				{
					array7[num++] = type;
				}
			}
		}
		return array7;
	}

	public virtual bool IsSubclassOf(Type c)
	{
		Type type = this;
		if (type == c)
		{
			return false;
		}
		while (type != null)
		{
			if (type == c)
			{
				return true;
			}
			type = type.BaseType;
		}
		return false;
	}

	[Intrinsic]
	public virtual bool IsAssignableFrom([NotNullWhen(true)] Type? c)
	{
		if (c == null)
		{
			return false;
		}
		if (this == c)
		{
			return true;
		}
		Type underlyingSystemType = UnderlyingSystemType;
		if ((object)underlyingSystemType != null && underlyingSystemType.IsRuntimeImplemented())
		{
			return underlyingSystemType.IsAssignableFrom(c);
		}
		if (c.IsSubclassOf(this))
		{
			return true;
		}
		if (IsInterface)
		{
			return c.ImplementInterface(this);
		}
		if (IsGenericParameter)
		{
			Type[] genericParameterConstraints = GetGenericParameterConstraints();
			for (int i = 0; i < genericParameterConstraints.Length; i++)
			{
				if (!genericParameterConstraints[i].IsAssignableFrom(c))
				{
					return false;
				}
			}
			return true;
		}
		return false;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2085:UnrecognizedReflectionPattern", Justification = "The GetInterfaces technically requires all interfaces to be preservedBut this method only compares the result against the passed in ifaceType.So if ifaceType exists, then trimming should have kept it implemented on any type.")]
	internal bool ImplementInterface(Type ifaceType)
	{
		Type type = this;
		while (type != null)
		{
			Type[] interfaces = type.GetInterfaces();
			if (interfaces != null)
			{
				for (int i = 0; i < interfaces.Length; i++)
				{
					if (interfaces[i] == ifaceType || (interfaces[i] != null && interfaces[i].ImplementInterface(ifaceType)))
					{
						return true;
					}
				}
			}
			type = type.BaseType;
		}
		return false;
	}

	private static bool FilterAttributeImpl(MemberInfo m, object filterCriteria)
	{
		if (filterCriteria == null)
		{
			throw new InvalidFilterCriteriaException(SR.InvalidFilterCriteriaException_CritInt);
		}
		switch (m.MemberType)
		{
		case MemberTypes.Constructor:
		case MemberTypes.Method:
		{
			MethodAttributes methodAttributes;
			try
			{
				int num2 = (int)filterCriteria;
				methodAttributes = (MethodAttributes)num2;
			}
			catch
			{
				throw new InvalidFilterCriteriaException(SR.InvalidFilterCriteriaException_CritInt);
			}
			MethodAttributes methodAttributes2 = ((m.MemberType != MemberTypes.Method) ? ((ConstructorInfo)m).Attributes : ((MethodInfo)m).Attributes);
			if ((methodAttributes & MethodAttributes.MemberAccessMask) != 0 && (methodAttributes2 & MethodAttributes.MemberAccessMask) != (methodAttributes & MethodAttributes.MemberAccessMask))
			{
				return false;
			}
			if ((methodAttributes & MethodAttributes.Static) != 0 && (methodAttributes2 & MethodAttributes.Static) == 0)
			{
				return false;
			}
			if ((methodAttributes & MethodAttributes.Final) != 0 && (methodAttributes2 & MethodAttributes.Final) == 0)
			{
				return false;
			}
			if ((methodAttributes & MethodAttributes.Virtual) != 0 && (methodAttributes2 & MethodAttributes.Virtual) == 0)
			{
				return false;
			}
			if ((methodAttributes & MethodAttributes.Abstract) != 0 && (methodAttributes2 & MethodAttributes.Abstract) == 0)
			{
				return false;
			}
			if ((methodAttributes & MethodAttributes.SpecialName) != 0 && (methodAttributes2 & MethodAttributes.SpecialName) == 0)
			{
				return false;
			}
			return true;
		}
		case MemberTypes.Field:
		{
			FieldAttributes fieldAttributes;
			try
			{
				int num = (int)filterCriteria;
				fieldAttributes = (FieldAttributes)num;
			}
			catch
			{
				throw new InvalidFilterCriteriaException(SR.InvalidFilterCriteriaException_CritInt);
			}
			FieldAttributes attributes = ((FieldInfo)m).Attributes;
			if ((fieldAttributes & FieldAttributes.FieldAccessMask) != 0 && (attributes & FieldAttributes.FieldAccessMask) != (fieldAttributes & FieldAttributes.FieldAccessMask))
			{
				return false;
			}
			if ((fieldAttributes & FieldAttributes.Static) != 0 && (attributes & FieldAttributes.Static) == 0)
			{
				return false;
			}
			if ((fieldAttributes & FieldAttributes.InitOnly) != 0 && (attributes & FieldAttributes.InitOnly) == 0)
			{
				return false;
			}
			if ((fieldAttributes & FieldAttributes.Literal) != 0 && (attributes & FieldAttributes.Literal) == 0)
			{
				return false;
			}
			if ((fieldAttributes & FieldAttributes.NotSerialized) != 0 && (attributes & FieldAttributes.NotSerialized) == 0)
			{
				return false;
			}
			if ((fieldAttributes & FieldAttributes.PinvokeImpl) != 0 && (attributes & FieldAttributes.PinvokeImpl) == 0)
			{
				return false;
			}
			return true;
		}
		default:
			return false;
		}
	}

	private static bool FilterNameImpl(MemberInfo m, object filterCriteria, StringComparison comparison)
	{
		if (!(filterCriteria is string text))
		{
			throw new InvalidFilterCriteriaException(SR.InvalidFilterCriteriaException_CritString);
		}
		ReadOnlySpan<char> readOnlySpan = text.AsSpan().Trim();
		ReadOnlySpan<char> span = m.Name;
		if (m.MemberType == MemberTypes.NestedType)
		{
			span = span.Slice(span.LastIndexOf('+') + 1);
		}
		if (readOnlySpan.Length > 0 && readOnlySpan[readOnlySpan.Length - 1] == '*')
		{
			readOnlySpan = readOnlySpan.Slice(0, readOnlySpan.Length - 1);
			return span.StartsWith(readOnlySpan, comparison);
		}
		return MemoryExtensions.Equals(span, readOnlySpan, comparison);
	}
}
