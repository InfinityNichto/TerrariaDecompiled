using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Reflection;

public abstract class Module : ICustomAttributeProvider, ISerializable
{
	public static readonly TypeFilter FilterTypeName = (Type m, object c) => FilterTypeNameImpl(m, c, StringComparison.Ordinal);

	public static readonly TypeFilter FilterTypeNameIgnoreCase = (Type m, object c) => FilterTypeNameImpl(m, c, StringComparison.OrdinalIgnoreCase);

	public virtual Assembly Assembly
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	[RequiresAssemblyFiles("Returns <Unknown> for modules with no file path")]
	public virtual string FullyQualifiedName
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	[RequiresAssemblyFiles("Returns <Unknown> for modules with no file path")]
	public virtual string Name
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public virtual int MDStreamVersion
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public virtual Guid ModuleVersionId
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public virtual string ScopeName
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public ModuleHandle ModuleHandle => GetModuleHandleImpl();

	public virtual IEnumerable<CustomAttributeData> CustomAttributes => GetCustomAttributesData();

	public virtual int MetadataToken
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	protected virtual ModuleHandle GetModuleHandleImpl()
	{
		return ModuleHandle.EmptyHandle;
	}

	public virtual void GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine)
	{
		throw NotImplemented.ByDesign;
	}

	public virtual bool IsResource()
	{
		throw NotImplemented.ByDesign;
	}

	public virtual bool IsDefined(Type attributeType, bool inherit)
	{
		throw NotImplemented.ByDesign;
	}

	public virtual IList<CustomAttributeData> GetCustomAttributesData()
	{
		throw NotImplemented.ByDesign;
	}

	public virtual object[] GetCustomAttributes(bool inherit)
	{
		throw NotImplemented.ByDesign;
	}

	public virtual object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		throw NotImplemented.ByDesign;
	}

	[RequiresUnreferencedCode("Methods might be removed")]
	public MethodInfo? GetMethod(string name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		return GetMethodImpl(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, null, CallingConventions.Any, null, null);
	}

	[RequiresUnreferencedCode("Methods might be removed")]
	public MethodInfo? GetMethod(string name, Type[] types)
	{
		return GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, null, CallingConventions.Any, types, null);
	}

	[RequiresUnreferencedCode("Methods might be removed")]
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

	[RequiresUnreferencedCode("Methods might be removed")]
	protected virtual MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
	{
		throw NotImplemented.ByDesign;
	}

	[RequiresUnreferencedCode("Methods might be removed")]
	public MethodInfo[] GetMethods()
	{
		return GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
	}

	[RequiresUnreferencedCode("Methods might be removed")]
	public virtual MethodInfo[] GetMethods(BindingFlags bindingFlags)
	{
		throw NotImplemented.ByDesign;
	}

	[RequiresUnreferencedCode("Fields might be removed")]
	public FieldInfo? GetField(string name)
	{
		return GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
	}

	[RequiresUnreferencedCode("Fields might be removed")]
	public virtual FieldInfo? GetField(string name, BindingFlags bindingAttr)
	{
		throw NotImplemented.ByDesign;
	}

	[RequiresUnreferencedCode("Fields might be removed")]
	public FieldInfo[] GetFields()
	{
		return GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
	}

	[RequiresUnreferencedCode("Fields might be removed")]
	public virtual FieldInfo[] GetFields(BindingFlags bindingFlags)
	{
		throw NotImplemented.ByDesign;
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public virtual Type[] GetTypes()
	{
		throw NotImplemented.ByDesign;
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public virtual Type? GetType(string className)
	{
		return GetType(className, throwOnError: false, ignoreCase: false);
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public virtual Type? GetType(string className, bool ignoreCase)
	{
		return GetType(className, throwOnError: false, ignoreCase);
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public virtual Type? GetType(string className, bool throwOnError, bool ignoreCase)
	{
		throw NotImplemented.ByDesign;
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public virtual Type[] FindTypes(TypeFilter? filter, object? filterCriteria)
	{
		Type[] types = GetTypes();
		int num = 0;
		for (int i = 0; i < types.Length; i++)
		{
			if (filter != null && !filter(types[i], filterCriteria))
			{
				types[i] = null;
			}
			else
			{
				num++;
			}
		}
		if (num == types.Length)
		{
			return types;
		}
		Type[] array = new Type[num];
		num = 0;
		for (int j = 0; j < types.Length; j++)
		{
			if (types[j] != null)
			{
				array[num++] = types[j];
			}
		}
		return array;
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public FieldInfo? ResolveField(int metadataToken)
	{
		return ResolveField(metadataToken, null, null);
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public virtual FieldInfo? ResolveField(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
	{
		throw NotImplemented.ByDesign;
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public MemberInfo? ResolveMember(int metadataToken)
	{
		return ResolveMember(metadataToken, null, null);
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public virtual MemberInfo? ResolveMember(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
	{
		throw NotImplemented.ByDesign;
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public MethodBase? ResolveMethod(int metadataToken)
	{
		return ResolveMethod(metadataToken, null, null);
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public virtual MethodBase? ResolveMethod(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
	{
		throw NotImplemented.ByDesign;
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public virtual byte[] ResolveSignature(int metadataToken)
	{
		throw NotImplemented.ByDesign;
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public virtual string ResolveString(int metadataToken)
	{
		throw NotImplemented.ByDesign;
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public Type ResolveType(int metadataToken)
	{
		return ResolveType(metadataToken, null, null);
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public virtual Type ResolveType(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
	{
		throw NotImplemented.ByDesign;
	}

	public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw NotImplemented.ByDesign;
	}

	public override bool Equals(object? o)
	{
		return base.Equals(o);
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Module? left, Module? right)
	{
		if ((object)right == null)
		{
			if ((object)left != null)
			{
				return false;
			}
			return true;
		}
		if ((object)left == right)
		{
			return true;
		}
		return left?.Equals(right) ?? false;
	}

	public static bool operator !=(Module? left, Module? right)
	{
		return !(left == right);
	}

	public override string ToString()
	{
		return ScopeName;
	}

	private static bool FilterTypeNameImpl(Type cls, object filterCriteria, StringComparison comparison)
	{
		if (!(filterCriteria is string text))
		{
			throw new InvalidFilterCriteriaException(SR.InvalidFilterCriteriaException_CritString);
		}
		if (text.Length > 0 && text[^1] == '*')
		{
			ReadOnlySpan<char> value = text.AsSpan(0, text.Length - 1);
			return cls.Name.AsSpan().StartsWith(value, comparison);
		}
		return cls.Name.Equals(text, comparison);
	}
}
