using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Reflection;

internal class RuntimeModule : Module
{
	private RuntimeType m_runtimeType;

	private RuntimeAssembly m_runtimeAssembly;

	private IntPtr m_pRefClass;

	private IntPtr m_pData;

	private IntPtr m_pGlobals;

	private IntPtr m_pFields;

	public override int MDStreamVersion => ModuleHandle.GetMDStreamVersion(this);

	internal RuntimeType RuntimeType => m_runtimeType ?? (m_runtimeType = ModuleHandle.GetModuleType(this));

	internal MetadataImport MetadataImport => ModuleHandle.GetMetadataImport(this);

	[RequiresAssemblyFiles("Returns <Unknown> for modules with no file path")]
	public override string FullyQualifiedName => GetFullyQualifiedName();

	public override Guid ModuleVersionId
	{
		get
		{
			MetadataImport.GetScopeProps(out var mvid);
			return mvid;
		}
	}

	public override int MetadataToken => ModuleHandle.GetToken(this);

	public override string ScopeName
	{
		get
		{
			string s = null;
			RuntimeModule module = this;
			GetScopeName(new QCallModule(ref module), new StringHandleOnStack(ref s));
			return s;
		}
	}

	[RequiresAssemblyFiles("Returns <Unknown> for modules with no file path")]
	public override string Name
	{
		get
		{
			string fullyQualifiedName = GetFullyQualifiedName();
			int num = fullyQualifiedName.LastIndexOf(Path.DirectorySeparatorChar);
			if (num == -1)
			{
				return fullyQualifiedName;
			}
			return fullyQualifiedName.Substring(num + 1);
		}
	}

	public override Assembly Assembly => GetRuntimeAssembly();

	internal RuntimeModule()
	{
		throw new NotSupportedException();
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetType(QCallModule module, string className, bool throwOnError, bool ignoreCase, ObjectHandleOnStack type, ObjectHandleOnStack keepAlive);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetScopeName(QCallModule module, StringHandleOnStack retString);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetFullyQualifiedName(QCallModule module, StringHandleOnStack retString);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern RuntimeType[] GetTypes(RuntimeModule module);

	internal RuntimeType[] GetDefinedTypes()
	{
		return GetTypes(this);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern bool IsResource(RuntimeModule module);

	private static RuntimeTypeHandle[] ConvertToTypeHandleArray(Type[] genericArguments)
	{
		if (genericArguments == null)
		{
			return null;
		}
		int num = genericArguments.Length;
		RuntimeTypeHandle[] array = new RuntimeTypeHandle[num];
		for (int i = 0; i < num; i++)
		{
			Type type = genericArguments[i];
			if (type == null)
			{
				throw new ArgumentException(SR.Argument_InvalidGenericInstArray);
			}
			type = type.UnderlyingSystemType;
			if (type == null)
			{
				throw new ArgumentException(SR.Argument_InvalidGenericInstArray);
			}
			if (!(type is RuntimeType))
			{
				throw new ArgumentException(SR.Argument_InvalidGenericInstArray);
			}
			array[i] = type.GetTypeHandleInternal();
		}
		return array;
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public override byte[] ResolveSignature(int metadataToken)
	{
		MetadataToken metadataToken2 = new MetadataToken(metadataToken);
		if (!MetadataImport.IsValidToken(metadataToken2))
		{
			throw new ArgumentOutOfRangeException("metadataToken", SR.Format(SR.Argument_InvalidToken, metadataToken2, this));
		}
		if (!metadataToken2.IsMemberRef && !metadataToken2.IsMethodDef && !metadataToken2.IsTypeSpec && !metadataToken2.IsSignature && !metadataToken2.IsFieldDef)
		{
			throw new ArgumentException(SR.Format(SR.Argument_InvalidToken, metadataToken2, this), "metadataToken");
		}
		ConstArray constArray = ((!metadataToken2.IsMemberRef) ? MetadataImport.GetSignatureFromToken(metadataToken) : MetadataImport.GetMemberRefProps(metadataToken));
		byte[] array = new byte[constArray.Length];
		for (int i = 0; i < constArray.Length; i++)
		{
			array[i] = constArray[i];
		}
		return array;
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public unsafe override MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
	{
		try
		{
			MetadataToken metadataToken2 = new MetadataToken(metadataToken);
			if (!metadataToken2.IsMethodDef && !metadataToken2.IsMethodSpec)
			{
				if (!metadataToken2.IsMemberRef)
				{
					throw new ArgumentException(SR.Format(SR.Argument_ResolveMethod, metadataToken2, this), "metadataToken");
				}
				if (*(byte*)(void*)MetadataImport.GetMemberRefProps(metadataToken2).Signature == 6)
				{
					throw new ArgumentException(SR.Format(SR.Argument_ResolveMethod, metadataToken2, this), "metadataToken");
				}
			}
			RuntimeTypeHandle[] typeInstantiationContext = null;
			RuntimeTypeHandle[] methodInstantiationContext = null;
			if (genericTypeArguments != null && genericTypeArguments.Length != 0)
			{
				typeInstantiationContext = ConvertToTypeHandleArray(genericTypeArguments);
			}
			if (genericMethodArguments != null && genericMethodArguments.Length != 0)
			{
				methodInstantiationContext = ConvertToTypeHandleArray(genericMethodArguments);
			}
			IRuntimeMethodInfo methodInfo = new ModuleHandle(this).ResolveMethodHandle(metadataToken2, typeInstantiationContext, methodInstantiationContext).GetMethodInfo();
			Type type = RuntimeMethodHandle.GetDeclaringType(methodInfo);
			if (type.IsGenericType || type.IsArray)
			{
				MetadataToken metadataToken3 = new MetadataToken(MetadataImport.GetParentToken(metadataToken2));
				if (metadataToken2.IsMethodSpec)
				{
					metadataToken3 = new MetadataToken(MetadataImport.GetParentToken(metadataToken3));
				}
				type = ResolveType(metadataToken3, genericTypeArguments, genericMethodArguments);
			}
			return RuntimeType.GetMethodBase(type as RuntimeType, methodInfo);
		}
		catch (BadImageFormatException innerException)
		{
			throw new ArgumentException(SR.Argument_BadImageFormatExceptionResolve, innerException);
		}
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	private FieldInfo ResolveLiteralField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
	{
		MetadataToken metadataToken2 = new MetadataToken(metadataToken);
		if (!MetadataImport.IsValidToken(metadataToken2) || !metadataToken2.IsFieldDef)
		{
			throw new ArgumentOutOfRangeException("metadataToken", SR.Format(SR.Argument_InvalidToken, metadataToken2, this));
		}
		string name = MetadataImport.GetName(metadataToken2).ToString();
		int parentToken = MetadataImport.GetParentToken(metadataToken2);
		Type type = ResolveType(parentToken, genericTypeArguments, genericMethodArguments);
		type.GetFields();
		try
		{
			return type.GetField(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		}
		catch
		{
			throw new ArgumentException(SR.Format(SR.Argument_ResolveField, metadataToken2, this), "metadataToken");
		}
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public unsafe override FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
	{
		try
		{
			MetadataToken metadataToken2 = new MetadataToken(metadataToken);
			if (!MetadataImport.IsValidToken(metadataToken2))
			{
				throw new ArgumentOutOfRangeException("metadataToken", SR.Format(SR.Argument_InvalidToken, metadataToken2, this));
			}
			RuntimeTypeHandle[] typeInstantiationContext = null;
			RuntimeTypeHandle[] methodInstantiationContext = null;
			if (genericTypeArguments != null && genericTypeArguments.Length != 0)
			{
				typeInstantiationContext = ConvertToTypeHandleArray(genericTypeArguments);
			}
			if (genericMethodArguments != null && genericMethodArguments.Length != 0)
			{
				methodInstantiationContext = ConvertToTypeHandleArray(genericMethodArguments);
			}
			ModuleHandle moduleHandle = new ModuleHandle(this);
			if (!metadataToken2.IsFieldDef)
			{
				if (!metadataToken2.IsMemberRef)
				{
					throw new ArgumentException(SR.Format(SR.Argument_ResolveField, metadataToken2, this), "metadataToken");
				}
				if (*(byte*)(void*)MetadataImport.GetMemberRefProps(metadataToken2).Signature != 6)
				{
					throw new ArgumentException(SR.Format(SR.Argument_ResolveField, metadataToken2, this), "metadataToken");
				}
			}
			IRuntimeFieldInfo runtimeFieldInfo = moduleHandle.ResolveFieldHandle(metadataToken, typeInstantiationContext, methodInstantiationContext).GetRuntimeFieldInfo();
			RuntimeType runtimeType = RuntimeFieldHandle.GetApproxDeclaringType(runtimeFieldInfo.Value);
			if (runtimeType.IsGenericType || runtimeType.IsArray)
			{
				int parentToken = ModuleHandle.GetMetadataImport(this).GetParentToken(metadataToken);
				runtimeType = (RuntimeType)ResolveType(parentToken, genericTypeArguments, genericMethodArguments);
			}
			return RuntimeType.GetFieldInfo(runtimeType, runtimeFieldInfo);
		}
		catch (MissingFieldException)
		{
			return ResolveLiteralField(metadataToken, genericTypeArguments, genericMethodArguments);
		}
		catch (BadImageFormatException innerException)
		{
			throw new ArgumentException(SR.Argument_BadImageFormatExceptionResolve, innerException);
		}
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public override Type ResolveType(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
	{
		try
		{
			MetadataToken metadataToken2 = new MetadataToken(metadataToken);
			if (metadataToken2.IsGlobalTypeDefToken)
			{
				throw new ArgumentException(SR.Format(SR.Argument_ResolveModuleType, metadataToken2), "metadataToken");
			}
			if (!metadataToken2.IsTypeDef && !metadataToken2.IsTypeSpec && !metadataToken2.IsTypeRef)
			{
				throw new ArgumentException(SR.Format(SR.Argument_ResolveType, metadataToken2, this), "metadataToken");
			}
			RuntimeTypeHandle[] typeInstantiationContext = null;
			RuntimeTypeHandle[] methodInstantiationContext = null;
			if (genericTypeArguments != null && genericTypeArguments.Length != 0)
			{
				typeInstantiationContext = ConvertToTypeHandleArray(genericTypeArguments);
			}
			if (genericMethodArguments != null && genericMethodArguments.Length != 0)
			{
				methodInstantiationContext = ConvertToTypeHandleArray(genericMethodArguments);
			}
			return GetModuleHandleImpl().ResolveTypeHandle(metadataToken, typeInstantiationContext, methodInstantiationContext).GetRuntimeType();
		}
		catch (BadImageFormatException innerException)
		{
			throw new ArgumentException(SR.Argument_BadImageFormatExceptionResolve, innerException);
		}
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public unsafe override MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
	{
		MetadataToken metadataToken2 = new MetadataToken(metadataToken);
		if (metadataToken2.IsProperty)
		{
			throw new ArgumentException(SR.InvalidOperation_PropertyInfoNotAvailable);
		}
		if (metadataToken2.IsEvent)
		{
			throw new ArgumentException(SR.InvalidOperation_EventInfoNotAvailable);
		}
		if (metadataToken2.IsMethodSpec || metadataToken2.IsMethodDef)
		{
			return ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments);
		}
		if (metadataToken2.IsFieldDef)
		{
			return ResolveField(metadataToken, genericTypeArguments, genericMethodArguments);
		}
		if (metadataToken2.IsTypeRef || metadataToken2.IsTypeDef || metadataToken2.IsTypeSpec)
		{
			return ResolveType(metadataToken, genericTypeArguments, genericMethodArguments);
		}
		if (metadataToken2.IsMemberRef)
		{
			if (!MetadataImport.IsValidToken(metadataToken2))
			{
				throw new ArgumentOutOfRangeException("metadataToken", SR.Format(SR.Argument_InvalidToken, metadataToken2, this));
			}
			if (*(byte*)(void*)MetadataImport.GetMemberRefProps(metadataToken2).Signature == 6)
			{
				return ResolveField(metadataToken2, genericTypeArguments, genericMethodArguments);
			}
			return ResolveMethod(metadataToken2, genericTypeArguments, genericMethodArguments);
		}
		throw new ArgumentException(SR.Format(SR.Argument_ResolveMember, metadataToken2, this), "metadataToken");
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public override string ResolveString(int metadataToken)
	{
		MetadataToken metadataToken2 = new MetadataToken(metadataToken);
		if (!metadataToken2.IsString)
		{
			throw new ArgumentException(SR.Format(SR.Argument_ResolveString, metadataToken, this));
		}
		if (!MetadataImport.IsValidToken(metadataToken2))
		{
			throw new ArgumentOutOfRangeException("metadataToken", SR.Format(SR.Argument_InvalidToken, metadataToken2, this));
		}
		string userString = MetadataImport.GetUserString(metadataToken);
		if (userString == null)
		{
			throw new ArgumentException(SR.Format(SR.Argument_ResolveString, metadataToken, this));
		}
		return userString;
	}

	public override void GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine)
	{
		ModuleHandle.GetPEKind(this, out peKind, out machine);
	}

	[RequiresUnreferencedCode("Methods might be removed because Module methods can't currently be annotated for dynamic access.")]
	protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
	{
		return GetMethodInternal(name, bindingAttr, binder, callConvention, types, modifiers);
	}

	[RequiresUnreferencedCode("Methods might be removed because Module methods can't currently be annotated for dynamic access.")]
	internal MethodInfo GetMethodInternal(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
	{
		if (RuntimeType == null)
		{
			return null;
		}
		if (types == null)
		{
			return RuntimeType.GetMethod(name, bindingAttr);
		}
		return RuntimeType.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return CustomAttribute.GetCustomAttributes(this, typeof(object) as RuntimeType);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		if (attributeType == null)
		{
			throw new ArgumentNullException("attributeType");
		}
		RuntimeType runtimeType = attributeType.UnderlyingSystemType as RuntimeType;
		if (runtimeType == null)
		{
			throw new ArgumentException(SR.Arg_MustBeType, "attributeType");
		}
		return CustomAttribute.GetCustomAttributes(this, runtimeType);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		if (attributeType == null)
		{
			throw new ArgumentNullException("attributeType");
		}
		RuntimeType runtimeType = attributeType.UnderlyingSystemType as RuntimeType;
		if (runtimeType == null)
		{
			throw new ArgumentException(SR.Arg_MustBeType, "attributeType");
		}
		return CustomAttribute.IsDefined(this, runtimeType);
	}

	public override IList<CustomAttributeData> GetCustomAttributesData()
	{
		return RuntimeCustomAttributeData.GetCustomAttributesInternal(this);
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public override Type GetType(string className, bool throwOnError, bool ignoreCase)
	{
		if (className == null)
		{
			throw new ArgumentNullException("className");
		}
		RuntimeType o = null;
		object o2 = null;
		RuntimeModule module = this;
		GetType(new QCallModule(ref module), className, throwOnError, ignoreCase, ObjectHandleOnStack.Create(ref o), ObjectHandleOnStack.Create(ref o2));
		GC.KeepAlive(o2);
		return o;
	}

	[RequiresAssemblyFiles("Returns <Unknown> for modules with no file path")]
	internal string GetFullyQualifiedName()
	{
		string s = null;
		RuntimeModule module = this;
		GetFullyQualifiedName(new QCallModule(ref module), new StringHandleOnStack(ref s));
		return s;
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public override Type[] GetTypes()
	{
		return GetTypes(this);
	}

	public override bool IsResource()
	{
		return IsResource(this);
	}

	[RequiresUnreferencedCode("Fields might be removed")]
	public override FieldInfo[] GetFields(BindingFlags bindingFlags)
	{
		if (RuntimeType == null)
		{
			return Array.Empty<FieldInfo>();
		}
		return RuntimeType.GetFields(bindingFlags);
	}

	[RequiresUnreferencedCode("Fields might be removed")]
	public override FieldInfo GetField(string name, BindingFlags bindingAttr)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (RuntimeType == null)
		{
			return null;
		}
		return RuntimeType.GetField(name, bindingAttr);
	}

	[RequiresUnreferencedCode("Methods might be removed")]
	public override MethodInfo[] GetMethods(BindingFlags bindingFlags)
	{
		if (RuntimeType == null)
		{
			return Array.Empty<MethodInfo>();
		}
		return RuntimeType.GetMethods(bindingFlags);
	}

	internal RuntimeAssembly GetRuntimeAssembly()
	{
		return m_runtimeAssembly;
	}

	protected override ModuleHandle GetModuleHandleImpl()
	{
		return new ModuleHandle(this);
	}

	internal IntPtr GetUnderlyingNativeHandle()
	{
		return m_pData;
	}
}
