using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit;

public class ModuleBuilder : Module
{
	private Dictionary<string, Type> _typeBuilderDict;

	internal ModuleBuilderData _moduleData;

	internal InternalModuleBuilder _internalModuleBuilder;

	private readonly AssemblyBuilder _assemblyBuilder;

	internal AssemblyBuilder ContainingAssemblyBuilder => _assemblyBuilder;

	internal object SyncRoot => ContainingAssemblyBuilder.SyncRoot;

	internal InternalModuleBuilder InternalModule => _internalModuleBuilder;

	[RequiresAssemblyFiles("Returns <Unknown> for modules with no file path")]
	public override string FullyQualifiedName => _moduleData._moduleName;

	public override int MDStreamVersion => InternalModule.MDStreamVersion;

	public override Guid ModuleVersionId => InternalModule.ModuleVersionId;

	public override int MetadataToken => InternalModule.MetadataToken;

	public override string ScopeName => InternalModule.ScopeName;

	[RequiresAssemblyFiles("Returns <Unknown> for modules with no file path")]
	public override string Name => InternalModule.Name;

	public override Assembly Assembly => _assemblyBuilder;

	internal static string UnmangleTypeName(string typeName)
	{
		int startIndex = typeName.Length - 1;
		while (true)
		{
			startIndex = typeName.LastIndexOf('+', startIndex);
			if (startIndex == -1)
			{
				break;
			}
			bool flag = true;
			int num = startIndex;
			while (typeName[--num] == '\\')
			{
				flag = !flag;
			}
			if (flag)
			{
				break;
			}
			startIndex = num;
		}
		return typeName.Substring(startIndex + 1);
	}

	internal ModuleBuilder(AssemblyBuilder assemblyBuilder, InternalModuleBuilder internalModuleBuilder)
	{
		_internalModuleBuilder = internalModuleBuilder;
		_assemblyBuilder = assemblyBuilder;
	}

	internal void AddType(string name, Type type)
	{
		_typeBuilderDict.Add(name, type);
	}

	internal void CheckTypeNameConflict(string strTypeName, Type enclosingType)
	{
		if (_typeBuilderDict.TryGetValue(strTypeName, out var value) && (object)value.DeclaringType == enclosingType)
		{
			throw new ArgumentException(SR.Argument_DuplicateTypeName);
		}
	}

	private static Type GetType(string strFormat, Type baseType)
	{
		if (string.IsNullOrEmpty(strFormat))
		{
			return baseType;
		}
		return SymbolType.FormCompoundType(strFormat, baseType, 0);
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern int GetTypeRef(QCallModule module, string strFullName, QCallModule refedModule, string strRefedModuleFileName, int tkResolution);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern int GetMemberRef(QCallModule module, QCallModule refedModule, int tr, int defToken);

	private int GetMemberRef(Module refedModule, int tr, int defToken)
	{
		ModuleBuilder module = this;
		RuntimeModule module2 = GetRuntimeModuleFromModule(refedModule);
		return GetMemberRef(new QCallModule(ref module), new QCallModule(ref module2), tr, defToken);
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern int GetMemberRefFromSignature(QCallModule module, int tr, string methodName, byte[] signature, int length);

	private int GetMemberRefFromSignature(int tr, string methodName, byte[] signature, int length)
	{
		ModuleBuilder module = this;
		return GetMemberRefFromSignature(new QCallModule(ref module), tr, methodName, signature, length);
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern int GetMemberRefOfMethodInfo(QCallModule module, int tr, RuntimeMethodHandleInternal method);

	private int GetMemberRefOfMethodInfo(int tr, RuntimeMethodInfo method)
	{
		ModuleBuilder module = this;
		int memberRefOfMethodInfo = GetMemberRefOfMethodInfo(new QCallModule(ref module), tr, ((IRuntimeMethodInfo)method).Value);
		GC.KeepAlive(method);
		return memberRefOfMethodInfo;
	}

	private int GetMemberRefOfMethodInfo(int tr, RuntimeConstructorInfo method)
	{
		ModuleBuilder module = this;
		int memberRefOfMethodInfo = GetMemberRefOfMethodInfo(new QCallModule(ref module), tr, ((IRuntimeMethodInfo)method).Value);
		GC.KeepAlive(method);
		return memberRefOfMethodInfo;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern int GetMemberRefOfFieldInfo(QCallModule module, int tkType, QCallTypeHandle declaringType, int tkField);

	private int GetMemberRefOfFieldInfo(int tkType, RuntimeTypeHandle declaringType, RuntimeFieldInfo runtimeField)
	{
		ModuleBuilder module = this;
		return GetMemberRefOfFieldInfo(new QCallModule(ref module), tkType, new QCallTypeHandle(ref declaringType), runtimeField.MetadataToken);
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern int GetTokenFromTypeSpec(QCallModule pModule, byte[] signature, int length);

	private int GetTokenFromTypeSpec(byte[] signature, int length)
	{
		ModuleBuilder module = this;
		return GetTokenFromTypeSpec(new QCallModule(ref module), signature, length);
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern int GetArrayMethodToken(QCallModule module, int tkTypeSpec, string methodName, byte[] signature, int sigLength);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern int GetStringConstant(QCallModule module, string str, int length);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern void SetFieldRVAContent(QCallModule module, int fdToken, byte[] data, int length);

	internal virtual Type FindTypeBuilderWithName(string strTypeName, bool ignoreCase)
	{
		Type value;
		if (ignoreCase)
		{
			foreach (string key in _typeBuilderDict.Keys)
			{
				if (string.Equals(key, strTypeName, StringComparison.OrdinalIgnoreCase))
				{
					return _typeBuilderDict[key];
				}
			}
		}
		else if (_typeBuilderDict.TryGetValue(strTypeName, out value))
		{
			return value;
		}
		return null;
	}

	private int GetTypeRefNested(Type type, Module refedModule, string strRefedModuleFileName)
	{
		Type declaringType = type.DeclaringType;
		int tkResolution = 0;
		string text = type.FullName;
		if (declaringType != null)
		{
			tkResolution = GetTypeRefNested(declaringType, refedModule, strRefedModuleFileName);
			text = UnmangleTypeName(text);
		}
		ModuleBuilder module = this;
		RuntimeModule module2 = GetRuntimeModuleFromModule(refedModule);
		return GetTypeRef(new QCallModule(ref module), text, new QCallModule(ref module2), strRefedModuleFileName, tkResolution);
	}

	internal int InternalGetConstructorToken(ConstructorInfo con, bool usingRef)
	{
		if (con == null)
		{
			throw new ArgumentNullException("con");
		}
		int typeTokenInternal;
		if (con is ConstructorBuilder constructorBuilder)
		{
			if (!usingRef && constructorBuilder.Module.Equals(this))
			{
				return constructorBuilder.MetadataToken;
			}
			typeTokenInternal = GetTypeTokenInternal(con.ReflectedType);
			return GetMemberRef(con.ReflectedType.Module, typeTokenInternal, constructorBuilder.MetadataToken);
		}
		if (con is ConstructorOnTypeBuilderInstantiation constructorOnTypeBuilderInstantiation)
		{
			if (usingRef)
			{
				throw new InvalidOperationException();
			}
			typeTokenInternal = GetTypeTokenInternal(con.DeclaringType);
			return GetMemberRef(con.DeclaringType.Module, typeTokenInternal, constructorOnTypeBuilderInstantiation.MetadataToken);
		}
		if (con is RuntimeConstructorInfo method && !con.ReflectedType.IsArray)
		{
			typeTokenInternal = GetTypeTokenInternal(con.ReflectedType);
			return GetMemberRefOfMethodInfo(typeTokenInternal, method);
		}
		ParameterInfo[] parameters = con.GetParameters();
		if (parameters == null)
		{
			throw new ArgumentException(SR.Argument_InvalidConstructorInfo);
		}
		Type[] array = new Type[parameters.Length];
		Type[][] array2 = new Type[parameters.Length][];
		Type[][] array3 = new Type[parameters.Length][];
		for (int i = 0; i < parameters.Length; i++)
		{
			if (parameters[i] == null)
			{
				throw new ArgumentException(SR.Argument_InvalidConstructorInfo);
			}
			array[i] = parameters[i].ParameterType;
			array2[i] = parameters[i].GetRequiredCustomModifiers();
			array3[i] = parameters[i].GetOptionalCustomModifiers();
		}
		typeTokenInternal = GetTypeTokenInternal(con.ReflectedType);
		SignatureHelper methodSigHelper = SignatureHelper.GetMethodSigHelper(this, con.CallingConvention, null, null, null, array, array2, array3);
		int length;
		byte[] signature = methodSigHelper.InternalGetSignature(out length);
		return GetMemberRefFromSignature(typeTokenInternal, con.Name, signature, length);
	}

	internal void Init(string strModuleName)
	{
		_moduleData = new ModuleBuilderData(this, strModuleName);
		_typeBuilderDict = new Dictionary<string, Type>();
	}

	protected override ModuleHandle GetModuleHandleImpl()
	{
		return new ModuleHandle(InternalModule);
	}

	private static RuntimeModule GetRuntimeModuleFromModule(Module m)
	{
		ModuleBuilder moduleBuilder = m as ModuleBuilder;
		if (moduleBuilder != null)
		{
			return moduleBuilder.InternalModule;
		}
		return m as RuntimeModule;
	}

	private int GetMemberRefToken(MethodBase method, Type[] optionalParameterTypes)
	{
		int cGenericParameters = 0;
		if (method.IsGenericMethod)
		{
			if (!method.IsGenericMethodDefinition)
			{
				throw new InvalidOperationException();
			}
			cGenericParameters = method.GetGenericArguments().Length;
		}
		if (optionalParameterTypes != null && (method.CallingConvention & CallingConventions.VarArgs) == 0)
		{
			throw new InvalidOperationException(SR.InvalidOperation_NotAVarArgCallingConvention);
		}
		MethodInfo methodInfo = method as MethodInfo;
		SignatureHelper memberRefSignature;
		if (method.DeclaringType.IsGenericType)
		{
			MethodBase genericMethodBaseDefinition = GetGenericMethodBaseDefinition(method);
			memberRefSignature = GetMemberRefSignature(genericMethodBaseDefinition, cGenericParameters);
		}
		else
		{
			memberRefSignature = GetMemberRefSignature(method, cGenericParameters);
		}
		if (optionalParameterTypes != null && optionalParameterTypes.Length != 0)
		{
			memberRefSignature.AddSentinel();
			memberRefSignature.AddArguments(optionalParameterTypes, null, null);
		}
		int length;
		byte[] signature = memberRefSignature.InternalGetSignature(out length);
		int tr;
		if (!method.DeclaringType.IsGenericType)
		{
			tr = ((!method.Module.Equals(this)) ? GetTypeToken(method.DeclaringType) : ((!(methodInfo != null)) ? GetConstructorToken(method as ConstructorInfo) : GetMethodToken(methodInfo)));
		}
		else
		{
			int length2;
			byte[] signature2 = SignatureHelper.GetTypeSigToken(this, method.DeclaringType).InternalGetSignature(out length2);
			tr = GetTokenFromTypeSpec(signature2, length2);
		}
		return GetMemberRefFromSignature(tr, method.Name, signature, length);
	}

	internal SignatureHelper GetMemberRefSignature(CallingConventions call, Type returnType, Type[] parameterTypes, Type[][] requiredCustomModifiers, Type[][] optionalCustomModifiers, Type[] optionalParameterTypes, int cGenericParameters)
	{
		SignatureHelper methodSigHelper = SignatureHelper.GetMethodSigHelper(this, call, cGenericParameters, returnType, null, null, parameterTypes, requiredCustomModifiers, optionalCustomModifiers);
		if (optionalParameterTypes != null && optionalParameterTypes.Length != 0)
		{
			methodSigHelper.AddSentinel();
			methodSigHelper.AddArguments(optionalParameterTypes, null, null);
		}
		return methodSigHelper;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Module.ResolveMethod is marked as RequiresUnreferencedCode because it relies on tokens which are not guaranteed to be stable across trimming. So if somebody hardcodes a token it could break. The usage here is not like that as all these tokens come from existing metadata loaded from some IL and so trimming has no effect (the tokens are read AFTER trimming occured).")]
	private static MethodBase GetGenericMethodBaseDefinition(MethodBase methodBase)
	{
		MethodInfo methodInfo = methodBase as MethodInfo;
		if (methodBase is MethodOnTypeBuilderInstantiation methodOnTypeBuilderInstantiation)
		{
			return methodOnTypeBuilderInstantiation.m_method;
		}
		if (methodBase is ConstructorOnTypeBuilderInstantiation constructorOnTypeBuilderInstantiation)
		{
			return constructorOnTypeBuilderInstantiation.m_ctor;
		}
		if (methodBase is MethodBuilder || methodBase is ConstructorBuilder)
		{
			return methodBase;
		}
		if (methodBase.IsGenericMethod)
		{
			MethodBase genericMethodDefinition = methodInfo.GetGenericMethodDefinition();
			return genericMethodDefinition.Module.ResolveMethod(methodBase.MetadataToken, genericMethodDefinition.DeclaringType?.GetGenericArguments(), genericMethodDefinition.GetGenericArguments());
		}
		return methodBase.Module.ResolveMethod(methodBase.MetadataToken, methodBase.DeclaringType?.GetGenericArguments(), null);
	}

	internal SignatureHelper GetMemberRefSignature(MethodBase method, int cGenericParameters)
	{
		MethodBase methodBase = method;
		if (!(methodBase is MethodBuilder methodBuilder))
		{
			if (!(methodBase is ConstructorBuilder constructorBuilder))
			{
				if (!(methodBase is MethodOnTypeBuilderInstantiation methodOnTypeBuilderInstantiation))
				{
					if (methodBase is ConstructorOnTypeBuilderInstantiation constructorOnTypeBuilderInstantiation)
					{
						if (constructorOnTypeBuilderInstantiation.m_ctor is ConstructorBuilder constructorBuilder2)
						{
							return constructorBuilder2.GetMethodSignature();
						}
						ConstructorOnTypeBuilderInstantiation constructorOnTypeBuilderInstantiation2 = constructorOnTypeBuilderInstantiation;
						method = constructorOnTypeBuilderInstantiation2.m_ctor;
					}
				}
				else
				{
					if (methodOnTypeBuilderInstantiation.m_method is MethodBuilder methodBuilder2)
					{
						return methodBuilder2.GetMethodSignature();
					}
					MethodOnTypeBuilderInstantiation methodOnTypeBuilderInstantiation2 = methodOnTypeBuilderInstantiation;
					method = methodOnTypeBuilderInstantiation2.m_method;
				}
				ParameterInfo[] parametersNoCopy = method.GetParametersNoCopy();
				Type[] array = new Type[parametersNoCopy.Length];
				Type[][] array2 = new Type[array.Length][];
				Type[][] array3 = new Type[array.Length][];
				for (int i = 0; i < parametersNoCopy.Length; i++)
				{
					array[i] = parametersNoCopy[i].ParameterType;
					array2[i] = parametersNoCopy[i].GetRequiredCustomModifiers();
					array3[i] = parametersNoCopy[i].GetOptionalCustomModifiers();
				}
				ParameterInfo parameterInfo = ((method is MethodInfo methodInfo) ? methodInfo.ReturnParameter : null);
				return SignatureHelper.GetMethodSigHelper(this, method.CallingConvention, cGenericParameters, parameterInfo?.ParameterType, parameterInfo?.GetRequiredCustomModifiers(), parameterInfo?.GetOptionalCustomModifiers(), array, array2, array3);
			}
			return constructorBuilder.GetMethodSignature();
		}
		return methodBuilder.GetMethodSignature();
	}

	public override bool Equals(object? obj)
	{
		return InternalModule.Equals(obj);
	}

	public override int GetHashCode()
	{
		return InternalModule.GetHashCode();
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return InternalModule.GetCustomAttributes(inherit);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		return InternalModule.GetCustomAttributes(attributeType, inherit);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		return InternalModule.IsDefined(attributeType, inherit);
	}

	public override IList<CustomAttributeData> GetCustomAttributesData()
	{
		return InternalModule.GetCustomAttributesData();
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public override Type[] GetTypes()
	{
		lock (SyncRoot)
		{
			return GetTypesNoLock();
		}
	}

	internal Type[] GetTypesNoLock()
	{
		Type[] array = new Type[_typeBuilderDict.Count];
		int num = 0;
		foreach (Type value in _typeBuilderDict.Values)
		{
			EnumBuilder enumBuilder = value as EnumBuilder;
			TypeBuilder typeBuilder = ((!(enumBuilder != null)) ? ((TypeBuilder)value) : enumBuilder.m_typeBuilder);
			if (typeBuilder.IsCreated())
			{
				array[num++] = typeBuilder.UnderlyingSystemType;
			}
			else
			{
				array[num++] = value;
			}
		}
		return array;
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public override Type? GetType(string className)
	{
		return GetType(className, throwOnError: false, ignoreCase: false);
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public override Type? GetType(string className, bool ignoreCase)
	{
		return GetType(className, throwOnError: false, ignoreCase);
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public override Type? GetType(string className, bool throwOnError, bool ignoreCase)
	{
		lock (SyncRoot)
		{
			return GetTypeNoLock(className, throwOnError, ignoreCase);
		}
	}

	[RequiresUnreferencedCode("Types might be removed")]
	private Type GetTypeNoLock(string className, bool throwOnError, bool ignoreCase)
	{
		Type type = InternalModule.GetType(className, throwOnError, ignoreCase);
		if (type != null)
		{
			return type;
		}
		string text = null;
		string text2 = null;
		int num = 0;
		while (num <= className.Length)
		{
			int num2 = className.AsSpan(num).IndexOfAny('[', '*', '&');
			if (num2 == -1)
			{
				text = className;
				text2 = null;
				break;
			}
			num2 += num;
			int num3 = 0;
			int num4 = num2 - 1;
			while (num4 >= 0 && className[num4] == '\\')
			{
				num3++;
				num4--;
			}
			if (num3 % 2 == 1)
			{
				num = num2 + 1;
				continue;
			}
			text = className.Substring(0, num2);
			text2 = className.Substring(num2);
			break;
		}
		if (text == null)
		{
			text = className;
			text2 = null;
		}
		text = text.Replace("\\\\", "\\").Replace("\\[", "[").Replace("\\*", "*")
			.Replace("\\&", "&");
		if (text2 != null)
		{
			type = InternalModule.GetType(text, throwOnError: false, ignoreCase);
		}
		if (type == null)
		{
			type = FindTypeBuilderWithName(text, ignoreCase);
			if (type == null && Assembly is AssemblyBuilder)
			{
				List<ModuleBuilder> moduleBuilderList = ContainingAssemblyBuilder._assemblyData._moduleBuilderList;
				int count = moduleBuilderList.Count;
				for (int i = 0; i < count; i++)
				{
					if (!(type == null))
					{
						break;
					}
					ModuleBuilder moduleBuilder = moduleBuilderList[i];
					type = moduleBuilder.FindTypeBuilderWithName(text, ignoreCase);
				}
			}
			if (type == null)
			{
				return null;
			}
		}
		if (text2 == null)
		{
			return type;
		}
		return GetType(text2, type);
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public override byte[] ResolveSignature(int metadataToken)
	{
		return InternalModule.ResolveSignature(metadataToken);
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public override MethodBase? ResolveMethod(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
	{
		return InternalModule.ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments);
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public override FieldInfo? ResolveField(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
	{
		return InternalModule.ResolveField(metadataToken, genericTypeArguments, genericMethodArguments);
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public override Type ResolveType(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
	{
		return InternalModule.ResolveType(metadataToken, genericTypeArguments, genericMethodArguments);
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public override MemberInfo? ResolveMember(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
	{
		return InternalModule.ResolveMember(metadataToken, genericTypeArguments, genericMethodArguments);
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public override string ResolveString(int metadataToken)
	{
		return InternalModule.ResolveString(metadataToken);
	}

	public override void GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine)
	{
		InternalModule.GetPEKind(out peKind, out machine);
	}

	public override bool IsResource()
	{
		return InternalModule.IsResource();
	}

	[RequiresUnreferencedCode("Fields might be removed")]
	public override FieldInfo[] GetFields(BindingFlags bindingFlags)
	{
		return InternalModule.GetFields(bindingFlags);
	}

	[RequiresUnreferencedCode("Fields might be removed")]
	public override FieldInfo? GetField(string name, BindingFlags bindingAttr)
	{
		return InternalModule.GetField(name, bindingAttr);
	}

	[RequiresUnreferencedCode("Methods might be removed")]
	public override MethodInfo[] GetMethods(BindingFlags bindingFlags)
	{
		return InternalModule.GetMethods(bindingFlags);
	}

	[RequiresUnreferencedCode("Methods might be removed")]
	protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
	{
		return InternalModule.GetMethodInternal(name, bindingAttr, binder, callConvention, types, modifiers);
	}

	public TypeBuilder DefineType(string name)
	{
		lock (SyncRoot)
		{
			return DefineTypeNoLock(name, TypeAttributes.NotPublic, null, null, PackingSize.Unspecified, 0);
		}
	}

	public TypeBuilder DefineType(string name, TypeAttributes attr)
	{
		lock (SyncRoot)
		{
			return DefineTypeNoLock(name, attr, null, null, PackingSize.Unspecified, 0);
		}
	}

	public TypeBuilder DefineType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent)
	{
		lock (SyncRoot)
		{
			AssemblyBuilder.CheckContext(parent);
			return DefineTypeNoLock(name, attr, parent, null, PackingSize.Unspecified, 0);
		}
	}

	public TypeBuilder DefineType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, int typesize)
	{
		lock (SyncRoot)
		{
			return DefineTypeNoLock(name, attr, parent, null, PackingSize.Unspecified, typesize);
		}
	}

	public TypeBuilder DefineType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, PackingSize packingSize, int typesize)
	{
		lock (SyncRoot)
		{
			return DefineTypeNoLock(name, attr, parent, null, packingSize, typesize);
		}
	}

	public TypeBuilder DefineType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, Type[]? interfaces)
	{
		lock (SyncRoot)
		{
			return DefineTypeNoLock(name, attr, parent, interfaces, PackingSize.Unspecified, 0);
		}
	}

	private TypeBuilder DefineTypeNoLock(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type parent, Type[] interfaces, PackingSize packingSize, int typesize)
	{
		return new TypeBuilder(name, attr, parent, interfaces, this, packingSize, typesize, null);
	}

	public TypeBuilder DefineType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, PackingSize packsize)
	{
		lock (SyncRoot)
		{
			return DefineTypeNoLock(name, attr, parent, packsize);
		}
	}

	private TypeBuilder DefineTypeNoLock(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type parent, PackingSize packsize)
	{
		return new TypeBuilder(name, attr, parent, null, this, packsize, 0, null);
	}

	public EnumBuilder DefineEnum(string name, TypeAttributes visibility, Type underlyingType)
	{
		AssemblyBuilder.CheckContext(underlyingType);
		lock (SyncRoot)
		{
			EnumBuilder enumBuilder = DefineEnumNoLock(name, visibility, underlyingType);
			_typeBuilderDict[name] = enumBuilder;
			return enumBuilder;
		}
	}

	private EnumBuilder DefineEnumNoLock(string name, TypeAttributes visibility, Type underlyingType)
	{
		return new EnumBuilder(name, underlyingType, visibility, this);
	}

	[RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
	public MethodBuilder DefinePInvokeMethod(string name, string dllName, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
	{
		return DefinePInvokeMethod(name, dllName, name, attributes, callingConvention, returnType, parameterTypes, nativeCallConv, nativeCharSet);
	}

	[RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
	public MethodBuilder DefinePInvokeMethod(string name, string dllName, string entryName, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
	{
		lock (SyncRoot)
		{
			if ((attributes & MethodAttributes.Static) == 0)
			{
				throw new ArgumentException(SR.Argument_GlobalFunctionHasToBeStatic);
			}
			AssemblyBuilder.CheckContext(returnType);
			AssemblyBuilder.CheckContext(parameterTypes);
			return _moduleData._globalTypeBuilder.DefinePInvokeMethod(name, dllName, entryName, attributes, callingConvention, returnType, parameterTypes, nativeCallConv, nativeCharSet);
		}
	}

	public MethodBuilder DefineGlobalMethod(string name, MethodAttributes attributes, Type? returnType, Type[]? parameterTypes)
	{
		return DefineGlobalMethod(name, attributes, CallingConventions.Standard, returnType, parameterTypes);
	}

	public MethodBuilder DefineGlobalMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes)
	{
		return DefineGlobalMethod(name, attributes, callingConvention, returnType, null, null, parameterTypes, null, null);
	}

	public MethodBuilder DefineGlobalMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? requiredReturnTypeCustomModifiers, Type[]? optionalReturnTypeCustomModifiers, Type[]? parameterTypes, Type[][]? requiredParameterTypeCustomModifiers, Type[][]? optionalParameterTypeCustomModifiers)
	{
		lock (SyncRoot)
		{
			return DefineGlobalMethodNoLock(name, attributes, callingConvention, returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers, parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
		}
	}

	private MethodBuilder DefineGlobalMethodNoLock(string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] requiredReturnTypeCustomModifiers, Type[] optionalReturnTypeCustomModifiers, Type[] parameterTypes, Type[][] requiredParameterTypeCustomModifiers, Type[][] optionalParameterTypeCustomModifiers)
	{
		if (_moduleData._hasGlobalBeenCreated)
		{
			throw new InvalidOperationException(SR.InvalidOperation_GlobalsHaveBeenCreated);
		}
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyName, "name");
		}
		if ((attributes & MethodAttributes.Static) == 0)
		{
			throw new ArgumentException(SR.Argument_GlobalFunctionHasToBeStatic);
		}
		AssemblyBuilder.CheckContext(returnType);
		AssemblyBuilder.CheckContext(requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers, parameterTypes);
		AssemblyBuilder.CheckContext(requiredParameterTypeCustomModifiers);
		AssemblyBuilder.CheckContext(optionalParameterTypeCustomModifiers);
		return _moduleData._globalTypeBuilder.DefineMethod(name, attributes, callingConvention, returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers, parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
	}

	public void CreateGlobalFunctions()
	{
		lock (SyncRoot)
		{
			CreateGlobalFunctionsNoLock();
		}
	}

	private void CreateGlobalFunctionsNoLock()
	{
		if (_moduleData._hasGlobalBeenCreated)
		{
			throw new InvalidOperationException(SR.InvalidOperation_NotADebugModule);
		}
		_moduleData._globalTypeBuilder.CreateType();
		_moduleData._hasGlobalBeenCreated = true;
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
		if (_moduleData._hasGlobalBeenCreated)
		{
			throw new InvalidOperationException(SR.InvalidOperation_GlobalsHaveBeenCreated);
		}
		return _moduleData._globalTypeBuilder.DefineInitializedData(name, data, attributes);
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
		if (_moduleData._hasGlobalBeenCreated)
		{
			throw new InvalidOperationException(SR.InvalidOperation_GlobalsHaveBeenCreated);
		}
		return _moduleData._globalTypeBuilder.DefineUninitializedData(name, size, attributes);
	}

	internal int GetTypeTokenInternal(Type type)
	{
		return GetTypeTokenInternal(type, getGenericDefinition: false);
	}

	private int GetTypeTokenInternal(Type type, bool getGenericDefinition)
	{
		lock (SyncRoot)
		{
			return GetTypeTokenWorkerNoLock(type, getGenericDefinition);
		}
	}

	internal int GetTypeToken(Type type)
	{
		return GetTypeTokenInternal(type, getGenericDefinition: true);
	}

	private int GetTypeTokenWorkerNoLock(Type type, bool getGenericDefinition)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		AssemblyBuilder.CheckContext(type);
		if (type.IsByRef)
		{
			throw new ArgumentException(SR.Argument_CannotGetTypeTokenForByRef);
		}
		if ((type.IsGenericType && (!type.IsGenericTypeDefinition || !getGenericDefinition)) || type.IsGenericParameter || type.IsArray || type.IsPointer)
		{
			int length;
			byte[] signature = SignatureHelper.GetTypeSigToken(this, type).InternalGetSignature(out length);
			return GetTokenFromTypeSpec(signature, length);
		}
		Module module = type.Module;
		if (module.Equals(this))
		{
			TypeBuilder typeBuilder = null;
			EnumBuilder enumBuilder = type as EnumBuilder;
			typeBuilder = ((enumBuilder != null) ? enumBuilder.m_typeBuilder : (type as TypeBuilder));
			if (typeBuilder != null)
			{
				return typeBuilder.TypeToken;
			}
			if (type is GenericTypeParameterBuilder genericTypeParameterBuilder)
			{
				return genericTypeParameterBuilder.MetadataToken;
			}
			return GetTypeRefNested(type, this, string.Empty);
		}
		ModuleBuilder moduleBuilder = module as ModuleBuilder;
		string strRefedModuleFileName = string.Empty;
		if (module.Assembly.Equals(Assembly))
		{
			if (moduleBuilder == null)
			{
				moduleBuilder = ContainingAssemblyBuilder.GetModuleBuilder((InternalModuleBuilder)module);
			}
			strRefedModuleFileName = moduleBuilder._moduleData._moduleName;
		}
		return GetTypeRefNested(type, module, strRefedModuleFileName);
	}

	internal int GetMethodToken(MethodInfo method)
	{
		lock (SyncRoot)
		{
			return GetMethodTokenNoLock(method, getGenericTypeDefinition: false);
		}
	}

	private int GetMethodTokenNoLock(MethodInfo method, bool getGenericTypeDefinition)
	{
		if (method == null)
		{
			throw new ArgumentNullException("method");
		}
		int num = 0;
		int tr;
		if (method is MethodBuilder { MetadataToken: var metadataToken })
		{
			if (method.Module.Equals(this))
			{
				return metadataToken;
			}
			if (method.DeclaringType == null)
			{
				throw new InvalidOperationException(SR.InvalidOperation_CannotImportGlobalFromDifferentModule);
			}
			tr = (getGenericTypeDefinition ? GetTypeToken(method.DeclaringType) : GetTypeTokenInternal(method.DeclaringType));
			return GetMemberRef(method.DeclaringType.Module, tr, metadataToken);
		}
		if (method is MethodOnTypeBuilderInstantiation)
		{
			return GetMemberRefToken(method, null);
		}
		if (method is SymbolMethod symbolMethod)
		{
			if (symbolMethod.GetModule() == this)
			{
				return symbolMethod.MetadataToken;
			}
			return symbolMethod.GetToken(this);
		}
		Type declaringType = method.DeclaringType;
		if (declaringType == null)
		{
			throw new InvalidOperationException(SR.InvalidOperation_CannotImportGlobalFromDifferentModule);
		}
		if (declaringType.IsArray)
		{
			ParameterInfo[] parameters = method.GetParameters();
			Type[] array = new Type[parameters.Length];
			for (int i = 0; i < parameters.Length; i++)
			{
				array[i] = parameters[i].ParameterType;
			}
			return GetArrayMethodToken(declaringType, method.Name, method.CallingConvention, method.ReturnType, array);
		}
		if (method is RuntimeMethodInfo method2)
		{
			tr = (getGenericTypeDefinition ? GetTypeToken(declaringType) : GetTypeTokenInternal(declaringType));
			return GetMemberRefOfMethodInfo(tr, method2);
		}
		ParameterInfo[] parameters2 = method.GetParameters();
		Type[] array2 = new Type[parameters2.Length];
		Type[][] array3 = new Type[array2.Length][];
		Type[][] array4 = new Type[array2.Length][];
		for (int j = 0; j < parameters2.Length; j++)
		{
			array2[j] = parameters2[j].ParameterType;
			array3[j] = parameters2[j].GetRequiredCustomModifiers();
			array4[j] = parameters2[j].GetOptionalCustomModifiers();
		}
		tr = (getGenericTypeDefinition ? GetTypeToken(declaringType) : GetTypeTokenInternal(declaringType));
		SignatureHelper methodSigHelper;
		try
		{
			methodSigHelper = SignatureHelper.GetMethodSigHelper(this, method.CallingConvention, method.ReturnType, method.ReturnParameter.GetRequiredCustomModifiers(), method.ReturnParameter.GetOptionalCustomModifiers(), array2, array3, array4);
		}
		catch (NotImplementedException)
		{
			methodSigHelper = SignatureHelper.GetMethodSigHelper(this, method.ReturnType, array2);
		}
		int length;
		byte[] signature = methodSigHelper.InternalGetSignature(out length);
		return GetMemberRefFromSignature(tr, method.Name, signature, length);
	}

	internal int GetMethodTokenInternal(MethodBase method, Type[] optionalParameterTypes, bool useMethodDef)
	{
		MethodInfo methodInfo = method as MethodInfo;
		if (method.IsGenericMethod)
		{
			MethodInfo methodInfo2 = methodInfo;
			bool isGenericMethodDefinition = methodInfo.IsGenericMethodDefinition;
			if (!isGenericMethodDefinition)
			{
				methodInfo2 = methodInfo.GetGenericMethodDefinition();
			}
			int num = ((Equals(methodInfo2.Module) && (!(methodInfo2.DeclaringType != null) || !methodInfo2.DeclaringType.IsGenericType)) ? GetMethodToken(methodInfo2) : GetMemberRefToken(methodInfo2, null));
			if (isGenericMethodDefinition && useMethodDef)
			{
				return num;
			}
			int length;
			byte[] signature = SignatureHelper.GetMethodSpecSigHelper(this, methodInfo.GetGenericArguments()).InternalGetSignature(out length);
			ModuleBuilder module = this;
			return TypeBuilder.DefineMethodSpec(new QCallModule(ref module), num, signature, length);
		}
		if ((method.CallingConvention & CallingConventions.VarArgs) == 0 && (method.DeclaringType == null || !method.DeclaringType.IsGenericType))
		{
			if (methodInfo != null)
			{
				return GetMethodToken(methodInfo);
			}
			return GetConstructorToken(method as ConstructorInfo);
		}
		return GetMemberRefToken(method, optionalParameterTypes);
	}

	internal int GetArrayMethodToken(Type arrayClass, string methodName, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
	{
		lock (SyncRoot)
		{
			return GetArrayMethodTokenNoLock(arrayClass, methodName, callingConvention, returnType, parameterTypes);
		}
	}

	private int GetArrayMethodTokenNoLock(Type arrayClass, string methodName, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
	{
		if (arrayClass == null)
		{
			throw new ArgumentNullException("arrayClass");
		}
		if (methodName == null)
		{
			throw new ArgumentNullException("methodName");
		}
		if (methodName.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyName, "methodName");
		}
		if (!arrayClass.IsArray)
		{
			throw new ArgumentException(SR.Argument_HasToBeArrayClass);
		}
		AssemblyBuilder.CheckContext(returnType, arrayClass);
		AssemblyBuilder.CheckContext(parameterTypes);
		SignatureHelper methodSigHelper = SignatureHelper.GetMethodSigHelper(this, callingConvention, returnType, null, null, parameterTypes, null, null);
		int length;
		byte[] signature = methodSigHelper.InternalGetSignature(out length);
		int typeTokenInternal = GetTypeTokenInternal(arrayClass);
		ModuleBuilder module = this;
		return GetArrayMethodToken(new QCallModule(ref module), typeTokenInternal, methodName, signature, length);
	}

	public MethodInfo GetArrayMethod(Type arrayClass, string methodName, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes)
	{
		AssemblyBuilder.CheckContext(returnType, arrayClass);
		AssemblyBuilder.CheckContext(parameterTypes);
		int arrayMethodToken = GetArrayMethodToken(arrayClass, methodName, callingConvention, returnType, parameterTypes);
		return new SymbolMethod(this, arrayMethodToken, arrayClass, methodName, callingConvention, returnType, parameterTypes);
	}

	internal int GetConstructorToken(ConstructorInfo con)
	{
		return InternalGetConstructorToken(con, usingRef: false);
	}

	internal int GetFieldToken(FieldInfo field)
	{
		lock (SyncRoot)
		{
			return GetFieldTokenNoLock(field);
		}
	}

	private int GetFieldTokenNoLock(FieldInfo field)
	{
		if (field == null)
		{
			throw new ArgumentNullException("field");
		}
		int num = 0;
		int tokenFromTypeSpec;
		if (field is FieldBuilder fieldBuilder)
		{
			if (field.DeclaringType != null && field.DeclaringType.IsGenericType)
			{
				int length;
				byte[] signature = SignatureHelper.GetTypeSigToken(this, field.DeclaringType).InternalGetSignature(out length);
				tokenFromTypeSpec = GetTokenFromTypeSpec(signature, length);
				return GetMemberRef(this, tokenFromTypeSpec, fieldBuilder.MetadataToken);
			}
			if (fieldBuilder.Module.Equals(this))
			{
				return fieldBuilder.MetadataToken;
			}
			if (field.DeclaringType == null)
			{
				throw new InvalidOperationException(SR.InvalidOperation_CannotImportGlobalFromDifferentModule);
			}
			tokenFromTypeSpec = GetTypeTokenInternal(field.DeclaringType);
			return GetMemberRef(field.ReflectedType.Module, tokenFromTypeSpec, fieldBuilder.MetadataToken);
		}
		if (field is RuntimeFieldInfo runtimeField)
		{
			if (field.DeclaringType == null)
			{
				throw new InvalidOperationException(SR.InvalidOperation_CannotImportGlobalFromDifferentModule);
			}
			if (field.DeclaringType != null && field.DeclaringType.IsGenericType)
			{
				int length2;
				byte[] signature2 = SignatureHelper.GetTypeSigToken(this, field.DeclaringType).InternalGetSignature(out length2);
				tokenFromTypeSpec = GetTokenFromTypeSpec(signature2, length2);
				return GetMemberRefOfFieldInfo(tokenFromTypeSpec, field.DeclaringType.GetTypeHandleInternal(), runtimeField);
			}
			tokenFromTypeSpec = GetTypeTokenInternal(field.DeclaringType);
			return GetMemberRefOfFieldInfo(tokenFromTypeSpec, field.DeclaringType.GetTypeHandleInternal(), runtimeField);
		}
		if (field is FieldOnTypeBuilderInstantiation { FieldInfo: var fieldInfo } fieldOnTypeBuilderInstantiation)
		{
			int length3;
			byte[] signature3 = SignatureHelper.GetTypeSigToken(this, field.DeclaringType).InternalGetSignature(out length3);
			tokenFromTypeSpec = GetTokenFromTypeSpec(signature3, length3);
			return GetMemberRef(fieldInfo.ReflectedType.Module, tokenFromTypeSpec, fieldOnTypeBuilderInstantiation.MetadataToken);
		}
		tokenFromTypeSpec = GetTypeTokenInternal(field.ReflectedType);
		SignatureHelper fieldSigHelper = SignatureHelper.GetFieldSigHelper(this);
		fieldSigHelper.AddArgument(field.FieldType, field.GetRequiredCustomModifiers(), field.GetOptionalCustomModifiers());
		int length4;
		byte[] signature4 = fieldSigHelper.InternalGetSignature(out length4);
		return GetMemberRefFromSignature(tokenFromTypeSpec, field.Name, signature4, length4);
	}

	internal int GetStringConstant(string str)
	{
		if (str == null)
		{
			throw new ArgumentNullException("str");
		}
		ModuleBuilder module = this;
		return GetStringConstant(new QCallModule(ref module), str, str.Length);
	}

	internal int GetSignatureToken(SignatureHelper sigHelper)
	{
		if (sigHelper == null)
		{
			throw new ArgumentNullException("sigHelper");
		}
		int length;
		byte[] signature = sigHelper.InternalGetSignature(out length);
		ModuleBuilder module = this;
		return TypeBuilder.GetTokenFromSig(new QCallModule(ref module), signature, length);
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
		TypeBuilder.DefineCustomAttribute(this, 1, GetConstructorToken(con), binaryAttribute);
	}

	public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
	{
		if (customBuilder == null)
		{
			throw new ArgumentNullException("customBuilder");
		}
		customBuilder.CreateCustomAttribute(this, 1);
	}
}
