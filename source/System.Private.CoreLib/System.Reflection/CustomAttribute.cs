using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System.Reflection;

internal static class CustomAttribute
{
	private static readonly RuntimeType Type_RuntimeType = (RuntimeType)typeof(RuntimeType);

	private static readonly RuntimeType Type_Type = (RuntimeType)typeof(Type);

	internal static bool IsDefined(RuntimeType type, RuntimeType caType, bool inherit)
	{
		if ((object)type.GetElementType() != null)
		{
			return false;
		}
		if (PseudoCustomAttribute.IsDefined(type, caType))
		{
			return true;
		}
		if (IsCustomAttributeDefined(type.GetRuntimeModule(), type.MetadataToken, caType))
		{
			return true;
		}
		if (!inherit)
		{
			return false;
		}
		type = type.BaseType as RuntimeType;
		while ((object)type != null)
		{
			if (IsCustomAttributeDefined(type.GetRuntimeModule(), type.MetadataToken, caType, 0, inherit))
			{
				return true;
			}
			type = type.BaseType as RuntimeType;
		}
		return false;
	}

	internal static bool IsDefined(RuntimeMethodInfo method, RuntimeType caType, bool inherit)
	{
		if (PseudoCustomAttribute.IsDefined(method, caType))
		{
			return true;
		}
		if (IsCustomAttributeDefined(method.GetRuntimeModule(), method.MetadataToken, caType))
		{
			return true;
		}
		if (!inherit)
		{
			return false;
		}
		method = method.GetParentDefinition();
		while ((object)method != null)
		{
			if (IsCustomAttributeDefined(method.GetRuntimeModule(), method.MetadataToken, caType, 0, inherit))
			{
				return true;
			}
			method = method.GetParentDefinition();
		}
		return false;
	}

	internal static bool IsDefined(RuntimeConstructorInfo ctor, RuntimeType caType)
	{
		return IsCustomAttributeDefined(ctor.GetRuntimeModule(), ctor.MetadataToken, caType);
	}

	internal static bool IsDefined(RuntimePropertyInfo property, RuntimeType caType)
	{
		return IsCustomAttributeDefined(property.GetRuntimeModule(), property.MetadataToken, caType);
	}

	internal static bool IsDefined(RuntimeEventInfo e, RuntimeType caType)
	{
		return IsCustomAttributeDefined(e.GetRuntimeModule(), e.MetadataToken, caType);
	}

	internal static bool IsDefined(RuntimeFieldInfo field, RuntimeType caType)
	{
		if (PseudoCustomAttribute.IsDefined(field, caType))
		{
			return true;
		}
		return IsCustomAttributeDefined(field.GetRuntimeModule(), field.MetadataToken, caType);
	}

	internal static bool IsDefined(RuntimeParameterInfo parameter, RuntimeType caType)
	{
		if (PseudoCustomAttribute.IsDefined(parameter, caType))
		{
			return true;
		}
		return IsCustomAttributeDefined(parameter.GetRuntimeModule(), parameter.MetadataToken, caType);
	}

	internal static bool IsDefined(RuntimeAssembly assembly, RuntimeType caType)
	{
		return IsCustomAttributeDefined(assembly.ManifestModule as RuntimeModule, RuntimeAssembly.GetToken(assembly.GetNativeHandle()), caType);
	}

	internal static bool IsDefined(RuntimeModule module, RuntimeType caType)
	{
		return IsCustomAttributeDefined(module, module.MetadataToken, caType);
	}

	internal static object[] GetCustomAttributes(RuntimeType type, RuntimeType caType, bool inherit)
	{
		if ((object)type.GetElementType() != null)
		{
			if (!caType.IsValueType)
			{
				return CreateAttributeArrayHelper(caType, 0);
			}
			return Array.Empty<object>();
		}
		if (type.IsGenericType && !type.IsGenericTypeDefinition)
		{
			type = type.GetGenericTypeDefinition() as RuntimeType;
		}
		RuntimeType.ListBuilder<Attribute> pcas = default(RuntimeType.ListBuilder<Attribute>);
		PseudoCustomAttribute.GetCustomAttributes(type, caType, ref pcas);
		if (!inherit || (caType.IsSealed && !GetAttributeUsage(caType).Inherited))
		{
			object[] customAttributes = GetCustomAttributes(type.GetRuntimeModule(), type.MetadataToken, pcas.Count, caType);
			if (pcas.Count > 0)
			{
				pcas.CopyTo(customAttributes, customAttributes.Length - pcas.Count);
			}
			return customAttributes;
		}
		RuntimeType.ListBuilder<object> attributes = default(RuntimeType.ListBuilder<object>);
		bool mustBeInheritable = false;
		RuntimeType elementType = ((caType.IsValueType || caType.ContainsGenericParameters) ? ((RuntimeType)typeof(object)) : caType);
		for (int i = 0; i < pcas.Count; i++)
		{
			attributes.Add(pcas[i]);
		}
		while (type != (RuntimeType)typeof(object) && type != null)
		{
			AddCustomAttributes(ref attributes, type.GetRuntimeModule(), type.MetadataToken, caType, mustBeInheritable, attributes);
			mustBeInheritable = true;
			type = type.BaseType as RuntimeType;
		}
		object[] array = CreateAttributeArrayHelper(elementType, attributes.Count);
		for (int j = 0; j < attributes.Count; j++)
		{
			array[j] = attributes[j];
		}
		return array;
	}

	internal static object[] GetCustomAttributes(RuntimeMethodInfo method, RuntimeType caType, bool inherit)
	{
		if (method.IsGenericMethod && !method.IsGenericMethodDefinition)
		{
			method = method.GetGenericMethodDefinition() as RuntimeMethodInfo;
		}
		RuntimeType.ListBuilder<Attribute> pcas = default(RuntimeType.ListBuilder<Attribute>);
		PseudoCustomAttribute.GetCustomAttributes(method, caType, ref pcas);
		if (!inherit || (caType.IsSealed && !GetAttributeUsage(caType).Inherited))
		{
			object[] customAttributes = GetCustomAttributes(method.GetRuntimeModule(), method.MetadataToken, pcas.Count, caType);
			if (pcas.Count > 0)
			{
				pcas.CopyTo(customAttributes, customAttributes.Length - pcas.Count);
			}
			return customAttributes;
		}
		RuntimeType.ListBuilder<object> attributes = default(RuntimeType.ListBuilder<object>);
		bool mustBeInheritable = false;
		RuntimeType elementType = ((caType.IsValueType || caType.ContainsGenericParameters) ? ((RuntimeType)typeof(object)) : caType);
		for (int i = 0; i < pcas.Count; i++)
		{
			attributes.Add(pcas[i]);
		}
		while (method != null)
		{
			AddCustomAttributes(ref attributes, method.GetRuntimeModule(), method.MetadataToken, caType, mustBeInheritable, attributes);
			mustBeInheritable = true;
			method = method.GetParentDefinition();
		}
		object[] array = CreateAttributeArrayHelper(elementType, attributes.Count);
		for (int j = 0; j < attributes.Count; j++)
		{
			array[j] = attributes[j];
		}
		return array;
	}

	internal static object[] GetCustomAttributes(RuntimeConstructorInfo ctor, RuntimeType caType)
	{
		return GetCustomAttributes(ctor.GetRuntimeModule(), ctor.MetadataToken, 0, caType);
	}

	internal static object[] GetCustomAttributes(RuntimePropertyInfo property, RuntimeType caType)
	{
		return GetCustomAttributes(property.GetRuntimeModule(), property.MetadataToken, 0, caType);
	}

	internal static object[] GetCustomAttributes(RuntimeEventInfo e, RuntimeType caType)
	{
		return GetCustomAttributes(e.GetRuntimeModule(), e.MetadataToken, 0, caType);
	}

	internal static object[] GetCustomAttributes(RuntimeFieldInfo field, RuntimeType caType)
	{
		RuntimeType.ListBuilder<Attribute> pcas = default(RuntimeType.ListBuilder<Attribute>);
		PseudoCustomAttribute.GetCustomAttributes(field, caType, ref pcas);
		object[] customAttributes = GetCustomAttributes(field.GetRuntimeModule(), field.MetadataToken, pcas.Count, caType);
		if (pcas.Count > 0)
		{
			pcas.CopyTo(customAttributes, customAttributes.Length - pcas.Count);
		}
		return customAttributes;
	}

	internal static object[] GetCustomAttributes(RuntimeParameterInfo parameter, RuntimeType caType)
	{
		RuntimeType.ListBuilder<Attribute> pcas = default(RuntimeType.ListBuilder<Attribute>);
		PseudoCustomAttribute.GetCustomAttributes(parameter, caType, ref pcas);
		object[] customAttributes = GetCustomAttributes(parameter.GetRuntimeModule(), parameter.MetadataToken, pcas.Count, caType);
		if (pcas.Count > 0)
		{
			pcas.CopyTo(customAttributes, customAttributes.Length - pcas.Count);
		}
		return customAttributes;
	}

	internal static object[] GetCustomAttributes(RuntimeAssembly assembly, RuntimeType caType)
	{
		int token = RuntimeAssembly.GetToken(assembly.GetNativeHandle());
		return GetCustomAttributes(assembly.ManifestModule as RuntimeModule, token, 0, caType);
	}

	internal static object[] GetCustomAttributes(RuntimeModule module, RuntimeType caType)
	{
		return GetCustomAttributes(module, module.MetadataToken, 0, caType);
	}

	private static bool IsCustomAttributeDefined(RuntimeModule decoratedModule, int decoratedMetadataToken, RuntimeType attributeFilterType)
	{
		return IsCustomAttributeDefined(decoratedModule, decoratedMetadataToken, attributeFilterType, 0, mustBeInheritable: false);
	}

	private static bool IsCustomAttributeDefined(RuntimeModule decoratedModule, int decoratedMetadataToken, RuntimeType attributeFilterType, int attributeCtorToken, bool mustBeInheritable)
	{
		MetadataImport scope = decoratedModule.MetadataImport;
		scope.EnumCustomAttributes(decoratedMetadataToken, out var result);
		if (result.Length == 0)
		{
			return false;
		}
		CustomAttributeRecord customAttributeRecord = default(CustomAttributeRecord);
		if ((object)attributeFilterType != null)
		{
			RuntimeType.ListBuilder<object> derivedAttributes = default(RuntimeType.ListBuilder<object>);
			for (int i = 0; i < result.Length; i++)
			{
				scope.GetCustomAttributeProps(result[i], out customAttributeRecord.tkCtor.Value, out customAttributeRecord.blob);
				if (FilterCustomAttributeRecord(customAttributeRecord.tkCtor, in scope, decoratedModule, decoratedMetadataToken, attributeFilterType, mustBeInheritable, ref derivedAttributes, out var _, out var _, out var _))
				{
					return true;
				}
			}
		}
		else
		{
			for (int j = 0; j < result.Length; j++)
			{
				scope.GetCustomAttributeProps(result[j], out customAttributeRecord.tkCtor.Value, out customAttributeRecord.blob);
				if ((int)customAttributeRecord.tkCtor == attributeCtorToken)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static object[] GetCustomAttributes(RuntimeModule decoratedModule, int decoratedMetadataToken, int pcaCount, RuntimeType attributeFilterType)
	{
		RuntimeType.ListBuilder<object> attributes = default(RuntimeType.ListBuilder<object>);
		AddCustomAttributes(ref attributes, decoratedModule, decoratedMetadataToken, attributeFilterType, mustBeInheritable: false, default(RuntimeType.ListBuilder<object>));
		RuntimeType elementType = (((object)attributeFilterType == null || attributeFilterType.IsValueType || attributeFilterType.ContainsGenericParameters) ? ((RuntimeType)typeof(object)) : attributeFilterType);
		object[] array = CreateAttributeArrayHelper(elementType, attributes.Count + pcaCount);
		for (int i = 0; i < attributes.Count; i++)
		{
			array[i] = attributes[i];
		}
		return array;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2065:UnrecognizedReflectionPattern", Justification = "Linker guarantees presence of all the constructor parameters, property setters and fields which are accessed by any attribute instantiation which is present in the code linker has analyzed.As such the reflection usage in this method will never fail as those methods/fields will be present.")]
	private unsafe static void AddCustomAttributes(ref RuntimeType.ListBuilder<object> attributes, RuntimeModule decoratedModule, int decoratedMetadataToken, RuntimeType attributeFilterType, bool mustBeInheritable, RuntimeType.ListBuilder<object> derivedAttributes)
	{
		CustomAttributeRecord[] customAttributeRecords = RuntimeCustomAttributeData.GetCustomAttributeRecords(decoratedModule, decoratedMetadataToken);
		if ((object)attributeFilterType == null && customAttributeRecords.Length == 0)
		{
			return;
		}
		MetadataImport scope = decoratedModule.MetadataImport;
		for (int i = 0; i < customAttributeRecords.Length; i++)
		{
			ref CustomAttributeRecord reference = ref customAttributeRecords[i];
			IntPtr blob = reference.blob.Signature;
			IntPtr intPtr = (IntPtr)((byte*)(void*)blob + reference.blob.Length);
			if (!FilterCustomAttributeRecord(reference.tkCtor, in scope, decoratedModule, decoratedMetadataToken, attributeFilterType, mustBeInheritable, ref derivedAttributes, out var attributeType, out var ctorWithParameters, out var isVarArg))
			{
				continue;
			}
			RuntimeConstructorInfo.CheckCanCreateInstance(attributeType, isVarArg);
			object obj;
			int namedArgs;
			if (ctorWithParameters != null)
			{
				obj = CreateCaObject(decoratedModule, attributeType, ctorWithParameters, ref blob, intPtr, out namedArgs);
			}
			else
			{
				obj = attributeType.CreateInstanceDefaultCtor(publicOnly: false, wrapExceptions: false);
				if ((int)((byte*)(void*)intPtr - (byte*)(void*)blob) == 0)
				{
					namedArgs = 0;
				}
				else
				{
					int num = Unsafe.ReadUnaligned<int>((void*)blob);
					if ((num & 0xFFFF) != 1)
					{
						throw new CustomAttributeFormatException();
					}
					namedArgs = num >> 16;
					blob = (IntPtr)((byte*)(void*)blob + 4);
				}
			}
			for (int j = 0; j < namedArgs; j++)
			{
				GetPropertyOrFieldData(decoratedModule, ref blob, intPtr, out var name, out var isProperty, out var type, out var value);
				try
				{
					if (isProperty)
					{
						if ((object)type == null && value != null)
						{
							type = (RuntimeType)value.GetType();
							if (type == Type_RuntimeType)
							{
								type = Type_Type;
							}
						}
						PropertyInfo propertyInfo = (((object)type == null) ? attributeType.GetProperty(name) : attributeType.GetProperty(name, type, Type.EmptyTypes));
						if ((object)propertyInfo == null)
						{
							throw new CustomAttributeFormatException(SR.Format(SR.RFLCT_InvalidPropFail, name));
						}
						MethodInfo setMethod = propertyInfo.GetSetMethod(nonPublic: true);
						if (setMethod.IsPublic)
						{
							setMethod.Invoke(obj, BindingFlags.Default, null, new object[1] { value }, null);
						}
					}
					else
					{
						FieldInfo field = attributeType.GetField(name);
						field.SetValue(obj, value, BindingFlags.Default, Type.DefaultBinder, null);
					}
				}
				catch (Exception inner)
				{
					throw new CustomAttributeFormatException(SR.Format(isProperty ? SR.RFLCT_InvalidPropFail : SR.RFLCT_InvalidFieldFail, name), inner);
				}
			}
			if (blob != intPtr)
			{
				throw new CustomAttributeFormatException();
			}
			attributes.Add(obj);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Module.ResolveMethod and Module.ResolveType are marked as RequiresUnreferencedCode because they rely on tokenswhich are not guaranteed to be stable across trimming. So if somebody hardcodes a token it could break.The usage here is not like that as all these tokens come from existing metadata loaded from some ILand so trimming has no effect (the tokens are read AFTER trimming occured).")]
	private static bool FilterCustomAttributeRecord(MetadataToken caCtorToken, in MetadataImport scope, RuntimeModule decoratedModule, MetadataToken decoratedToken, RuntimeType attributeFilterType, bool mustBeInheritable, ref RuntimeType.ListBuilder<object> derivedAttributes, out RuntimeType attributeType, out IRuntimeMethodInfo ctorWithParameters, out bool isVarArg)
	{
		ctorWithParameters = null;
		isVarArg = false;
		attributeType = decoratedModule.ResolveType(scope.GetParentToken(caCtorToken), null, null) as RuntimeType;
		if (!attributeFilterType.IsAssignableFrom(attributeType))
		{
			return false;
		}
		if (!AttributeUsageCheck(attributeType, mustBeInheritable, ref derivedAttributes))
		{
			return false;
		}
		if ((attributeType.Attributes & TypeAttributes.WindowsRuntime) == TypeAttributes.WindowsRuntime)
		{
			return false;
		}
		ConstArray methodSignature = scope.GetMethodSignature(caCtorToken);
		isVarArg = (methodSignature[0] & 5) != 0;
		if (methodSignature[1] != 0)
		{
			if (attributeType.IsGenericType)
			{
				ctorWithParameters = decoratedModule.ResolveMethod(caCtorToken, attributeType.GenericTypeArguments, null).MethodHandle.GetMethodInfo();
			}
			else
			{
				ctorWithParameters = new ModuleHandle(decoratedModule).ResolveMethodHandle(caCtorToken).GetMethodInfo();
			}
		}
		MetadataToken metadataToken = default(MetadataToken);
		if (decoratedToken.IsParamDef)
		{
			metadataToken = new MetadataToken(scope.GetParentToken(decoratedToken));
			metadataToken = new MetadataToken(scope.GetParentToken(metadataToken));
		}
		else if (decoratedToken.IsMethodDef || decoratedToken.IsProperty || decoratedToken.IsEvent || decoratedToken.IsFieldDef)
		{
			metadataToken = new MetadataToken(scope.GetParentToken(decoratedToken));
		}
		else if (decoratedToken.IsTypeDef)
		{
			metadataToken = decoratedToken;
		}
		else if (decoratedToken.IsGenericPar)
		{
			metadataToken = new MetadataToken(scope.GetParentToken(decoratedToken));
			if (metadataToken.IsMethodDef)
			{
				metadataToken = new MetadataToken(scope.GetParentToken(metadataToken));
			}
		}
		RuntimeTypeHandle rth = (metadataToken.IsTypeDef ? decoratedModule.ModuleHandle.ResolveTypeHandle(metadataToken) : default(RuntimeTypeHandle));
		RuntimeTypeHandle rth2 = attributeType.TypeHandle;
		bool result = RuntimeMethodHandle.IsCAVisibleFromDecoratedType(new QCallTypeHandle(ref rth2), (ctorWithParameters != null) ? ctorWithParameters.Value : RuntimeMethodHandleInternal.EmptyHandle, new QCallTypeHandle(ref rth), new QCallModule(ref decoratedModule)) != Interop.BOOL.FALSE;
		GC.KeepAlive(ctorWithParameters);
		return result;
	}

	private static bool AttributeUsageCheck(RuntimeType attributeType, bool mustBeInheritable, ref RuntimeType.ListBuilder<object> derivedAttributes)
	{
		AttributeUsageAttribute attributeUsageAttribute = null;
		if (mustBeInheritable)
		{
			attributeUsageAttribute = GetAttributeUsage(attributeType);
			if (!attributeUsageAttribute.Inherited)
			{
				return false;
			}
		}
		if (derivedAttributes.Count == 0)
		{
			return true;
		}
		for (int i = 0; i < derivedAttributes.Count; i++)
		{
			if (derivedAttributes[i].GetType() == attributeType)
			{
				if (attributeUsageAttribute == null)
				{
					attributeUsageAttribute = GetAttributeUsage(attributeType);
				}
				return attributeUsageAttribute.AllowMultiple;
			}
		}
		return true;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Module.ResolveType is marked as RequiresUnreferencedCode because it relies on tokenswhich are not guaranteed to be stable across trimming. So if somebody hardcodes a token it could break.The usage here is not like that as all these tokens come from existing metadata loaded from some ILand so trimming has no effect (the tokens are read AFTER trimming occured).")]
	internal static AttributeUsageAttribute GetAttributeUsage(RuntimeType decoratedAttribute)
	{
		RuntimeModule runtimeModule = decoratedAttribute.GetRuntimeModule();
		MetadataImport metadataImport = runtimeModule.MetadataImport;
		CustomAttributeRecord[] customAttributeRecords = RuntimeCustomAttributeData.GetCustomAttributeRecords(runtimeModule, decoratedAttribute.MetadataToken);
		AttributeUsageAttribute attributeUsageAttribute = null;
		for (int i = 0; i < customAttributeRecords.Length; i++)
		{
			ref CustomAttributeRecord reference = ref customAttributeRecords[i];
			RuntimeType runtimeType = runtimeModule.ResolveType(metadataImport.GetParentToken(reference.tkCtor), null, null) as RuntimeType;
			if (!(runtimeType != (RuntimeType)typeof(AttributeUsageAttribute)))
			{
				if (attributeUsageAttribute != null)
				{
					throw new FormatException(SR.Format(SR.Format_AttributeUsage, runtimeType));
				}
				ParseAttributeUsageAttribute(reference.blob, out var targets, out var inherited, out var allowMultiple);
				attributeUsageAttribute = new AttributeUsageAttribute(targets, allowMultiple, inherited);
			}
		}
		return attributeUsageAttribute ?? AttributeUsageAttribute.Default;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void _ParseAttributeUsageAttribute(IntPtr pCa, int cCa, out int targets, out bool inherited, out bool allowMultiple);

	private static void ParseAttributeUsageAttribute(ConstArray ca, out AttributeTargets targets, out bool inherited, out bool allowMultiple)
	{
		_ParseAttributeUsageAttribute(ca.Signature, ca.Length, out var targets2, out inherited, out allowMultiple);
		targets = (AttributeTargets)targets2;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern object _CreateCaObject(RuntimeModule pModule, RuntimeType type, IRuntimeMethodInfo pCtor, byte** ppBlob, byte* pEndBlob, int* pcNamedArgs);

	private unsafe static object CreateCaObject(RuntimeModule module, RuntimeType type, IRuntimeMethodInfo ctor, ref IntPtr blob, IntPtr blobEnd, out int namedArgs)
	{
		byte* ptr = (byte*)(void*)blob;
		byte* pEndBlob = (byte*)(void*)blobEnd;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out int num);
		object result = _CreateCaObject(module, type, ctor, &ptr, pEndBlob, &num);
		blob = (IntPtr)ptr;
		namedArgs = num;
		return result;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern void _GetPropertyOrFieldData(RuntimeModule pModule, byte** ppBlobStart, byte* pBlobEnd, out string name, out bool bIsProperty, out RuntimeType type, out object value);

	private unsafe static void GetPropertyOrFieldData(RuntimeModule module, ref IntPtr blobStart, IntPtr blobEnd, out string name, out bool isProperty, out RuntimeType type, out object value)
	{
		byte* ptr = (byte*)(void*)blobStart;
		_GetPropertyOrFieldData(module, &ptr, (byte*)(void*)blobEnd, out name, out isProperty, out type, out value);
		blobStart = (IntPtr)ptr;
	}

	private static object[] CreateAttributeArrayHelper(RuntimeType elementType, int elementCount)
	{
		if (elementCount == 0)
		{
			return elementType.GetEmptyArray();
		}
		return (object[])Array.CreateInstance(elementType, elementCount);
	}
}
