using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection;

internal static class PseudoCustomAttribute
{
	private static readonly HashSet<RuntimeType> s_pca = CreatePseudoCustomAttributeHashSet();

	private static HashSet<RuntimeType> CreatePseudoCustomAttributeHashSet()
	{
		Type[] array = new Type[11]
		{
			typeof(FieldOffsetAttribute),
			typeof(SerializableAttribute),
			typeof(MarshalAsAttribute),
			typeof(ComImportAttribute),
			typeof(NonSerializedAttribute),
			typeof(InAttribute),
			typeof(OutAttribute),
			typeof(OptionalAttribute),
			typeof(DllImportAttribute),
			typeof(PreserveSigAttribute),
			typeof(TypeForwardedToAttribute)
		};
		HashSet<RuntimeType> hashSet = new HashSet<RuntimeType>(array.Length);
		Type[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			RuntimeType item = (RuntimeType)array2[i];
			hashSet.Add(item);
		}
		return hashSet;
	}

	internal static void GetCustomAttributes(RuntimeType type, RuntimeType caType, ref RuntimeType.ListBuilder<Attribute> pcas)
	{
		bool flag = caType == typeof(object) || caType == typeof(Attribute);
		if (flag || s_pca.Contains(caType))
		{
			if ((flag || caType == typeof(SerializableAttribute)) && (type.Attributes & TypeAttributes.Serializable) != 0)
			{
				pcas.Add(new SerializableAttribute());
			}
			if ((flag || caType == typeof(ComImportAttribute)) && (type.Attributes & TypeAttributes.Import) != 0)
			{
				pcas.Add(new ComImportAttribute());
			}
		}
	}

	internal static bool IsDefined(RuntimeType type, RuntimeType caType)
	{
		bool flag = caType == typeof(object) || caType == typeof(Attribute);
		if (!flag && !s_pca.Contains(caType))
		{
			return false;
		}
		if ((flag || caType == typeof(SerializableAttribute)) && (type.Attributes & TypeAttributes.Serializable) != 0)
		{
			return true;
		}
		if ((flag || caType == typeof(ComImportAttribute)) && (type.Attributes & TypeAttributes.Import) != 0)
		{
			return true;
		}
		return false;
	}

	internal static void GetCustomAttributes(RuntimeMethodInfo method, RuntimeType caType, ref RuntimeType.ListBuilder<Attribute> pcas)
	{
		bool flag = caType == typeof(object) || caType == typeof(Attribute);
		if (!flag && !s_pca.Contains(caType))
		{
			return;
		}
		if (flag || caType == typeof(DllImportAttribute))
		{
			Attribute dllImportCustomAttribute = GetDllImportCustomAttribute(method);
			if (dllImportCustomAttribute != null)
			{
				pcas.Add(dllImportCustomAttribute);
			}
		}
		if ((flag || caType == typeof(PreserveSigAttribute)) && (method.GetMethodImplementationFlags() & MethodImplAttributes.PreserveSig) != 0)
		{
			pcas.Add(new PreserveSigAttribute());
		}
	}

	internal static bool IsDefined(RuntimeMethodInfo method, RuntimeType caType)
	{
		bool flag = caType == typeof(object) || caType == typeof(Attribute);
		if (!flag && !s_pca.Contains(caType))
		{
			return false;
		}
		if ((flag || caType == typeof(DllImportAttribute)) && (method.Attributes & MethodAttributes.PinvokeImpl) != 0)
		{
			return true;
		}
		if ((flag || caType == typeof(PreserveSigAttribute)) && (method.GetMethodImplementationFlags() & MethodImplAttributes.PreserveSig) != 0)
		{
			return true;
		}
		return false;
	}

	internal static void GetCustomAttributes(RuntimeParameterInfo parameter, RuntimeType caType, ref RuntimeType.ListBuilder<Attribute> pcas)
	{
		bool flag = caType == typeof(object) || caType == typeof(Attribute);
		if (!flag && !s_pca.Contains(caType))
		{
			return;
		}
		if ((flag || caType == typeof(InAttribute)) && parameter.IsIn)
		{
			pcas.Add(new InAttribute());
		}
		if ((flag || caType == typeof(OutAttribute)) && parameter.IsOut)
		{
			pcas.Add(new OutAttribute());
		}
		if ((flag || caType == typeof(OptionalAttribute)) && parameter.IsOptional)
		{
			pcas.Add(new OptionalAttribute());
		}
		if (flag || caType == typeof(MarshalAsAttribute))
		{
			Attribute marshalAsCustomAttribute = GetMarshalAsCustomAttribute(parameter);
			if (marshalAsCustomAttribute != null)
			{
				pcas.Add(marshalAsCustomAttribute);
			}
		}
	}

	internal static bool IsDefined(RuntimeParameterInfo parameter, RuntimeType caType)
	{
		bool flag = caType == typeof(object) || caType == typeof(Attribute);
		if (!flag && !s_pca.Contains(caType))
		{
			return false;
		}
		if ((flag || caType == typeof(InAttribute)) && parameter.IsIn)
		{
			return true;
		}
		if ((flag || caType == typeof(OutAttribute)) && parameter.IsOut)
		{
			return true;
		}
		if ((flag || caType == typeof(OptionalAttribute)) && parameter.IsOptional)
		{
			return true;
		}
		if ((flag || caType == typeof(MarshalAsAttribute)) && GetMarshalAsCustomAttribute(parameter) != null)
		{
			return true;
		}
		return false;
	}

	internal static void GetCustomAttributes(RuntimeFieldInfo field, RuntimeType caType, ref RuntimeType.ListBuilder<Attribute> pcas)
	{
		bool flag = caType == typeof(object) || caType == typeof(Attribute);
		if (!flag && !s_pca.Contains(caType))
		{
			return;
		}
		if (flag || caType == typeof(MarshalAsAttribute))
		{
			Attribute marshalAsCustomAttribute = GetMarshalAsCustomAttribute(field);
			if (marshalAsCustomAttribute != null)
			{
				pcas.Add(marshalAsCustomAttribute);
			}
		}
		if (flag || caType == typeof(FieldOffsetAttribute))
		{
			Attribute marshalAsCustomAttribute = GetFieldOffsetCustomAttribute(field);
			if (marshalAsCustomAttribute != null)
			{
				pcas.Add(marshalAsCustomAttribute);
			}
		}
		if ((flag || caType == typeof(NonSerializedAttribute)) && (field.Attributes & FieldAttributes.NotSerialized) != 0)
		{
			pcas.Add(new NonSerializedAttribute());
		}
	}

	internal static bool IsDefined(RuntimeFieldInfo field, RuntimeType caType)
	{
		bool flag = caType == typeof(object) || caType == typeof(Attribute);
		if (!flag && !s_pca.Contains(caType))
		{
			return false;
		}
		if ((flag || caType == typeof(MarshalAsAttribute)) && GetMarshalAsCustomAttribute(field) != null)
		{
			return true;
		}
		if ((flag || caType == typeof(FieldOffsetAttribute)) && GetFieldOffsetCustomAttribute(field) != null)
		{
			return true;
		}
		if ((flag || caType == typeof(NonSerializedAttribute)) && (field.Attributes & FieldAttributes.NotSerialized) != 0)
		{
			return true;
		}
		return false;
	}

	private static DllImportAttribute GetDllImportCustomAttribute(RuntimeMethodInfo method)
	{
		if ((method.Attributes & MethodAttributes.PinvokeImpl) == 0)
		{
			return null;
		}
		MetadataImport metadataImport = ModuleHandle.GetMetadataImport(method.Module.ModuleHandle.GetRuntimeModule());
		int metadataToken = method.MetadataToken;
		metadataImport.GetPInvokeMap(metadataToken, out var attributes, out var importName, out var importDll);
		CharSet charSet = CharSet.None;
		switch (attributes & PInvokeAttributes.CharSetMask)
		{
		case PInvokeAttributes.CharSetNotSpec:
			charSet = CharSet.None;
			break;
		case PInvokeAttributes.CharSetAnsi:
			charSet = CharSet.Ansi;
			break;
		case PInvokeAttributes.CharSetUnicode:
			charSet = CharSet.Unicode;
			break;
		case PInvokeAttributes.CharSetMask:
			charSet = CharSet.Auto;
			break;
		}
		CallingConvention callingConvention = CallingConvention.Cdecl;
		switch (attributes & PInvokeAttributes.CallConvMask)
		{
		case PInvokeAttributes.CallConvWinapi:
			callingConvention = CallingConvention.Winapi;
			break;
		case PInvokeAttributes.CallConvCdecl:
			callingConvention = CallingConvention.Cdecl;
			break;
		case PInvokeAttributes.CallConvStdcall:
			callingConvention = CallingConvention.StdCall;
			break;
		case PInvokeAttributes.CallConvThiscall:
			callingConvention = CallingConvention.ThisCall;
			break;
		case PInvokeAttributes.CallConvFastcall:
			callingConvention = CallingConvention.FastCall;
			break;
		}
		DllImportAttribute dllImportAttribute = new DllImportAttribute(importDll);
		dllImportAttribute.EntryPoint = importName;
		dllImportAttribute.CharSet = charSet;
		dllImportAttribute.SetLastError = (attributes & PInvokeAttributes.SupportsLastError) != 0;
		dllImportAttribute.ExactSpelling = (attributes & PInvokeAttributes.NoMangle) != 0;
		dllImportAttribute.PreserveSig = (method.GetMethodImplementationFlags() & MethodImplAttributes.PreserveSig) != 0;
		dllImportAttribute.CallingConvention = callingConvention;
		dllImportAttribute.BestFitMapping = (attributes & PInvokeAttributes.BestFitMask) == PInvokeAttributes.BestFitEnabled;
		dllImportAttribute.ThrowOnUnmappableChar = (attributes & PInvokeAttributes.ThrowOnUnmappableCharMask) == PInvokeAttributes.ThrowOnUnmappableCharEnabled;
		return dllImportAttribute;
	}

	private static MarshalAsAttribute GetMarshalAsCustomAttribute(RuntimeParameterInfo parameter)
	{
		return GetMarshalAsCustomAttribute(parameter.MetadataToken, parameter.GetRuntimeModule());
	}

	private static MarshalAsAttribute GetMarshalAsCustomAttribute(RuntimeFieldInfo field)
	{
		return GetMarshalAsCustomAttribute(field.MetadataToken, field.GetRuntimeModule());
	}

	private static MarshalAsAttribute GetMarshalAsCustomAttribute(int token, RuntimeModule scope)
	{
		ConstArray fieldMarshal = ModuleHandle.GetMetadataImport(scope).GetFieldMarshal(token);
		if (fieldMarshal.Length == 0)
		{
			return null;
		}
		MetadataImport.GetMarshalAs(fieldMarshal, out var unmanagedType, out var safeArraySubType, out var safeArrayUserDefinedSubType, out var arraySubType, out var sizeParamIndex, out var sizeConst, out var marshalType, out var marshalCookie, out var iidParamIndex);
		RuntimeType safeArrayUserDefinedSubType2 = (string.IsNullOrEmpty(safeArrayUserDefinedSubType) ? null : RuntimeTypeHandle.GetTypeByNameUsingCARules(safeArrayUserDefinedSubType, scope));
		RuntimeType marshalTypeRef = null;
		try
		{
			marshalTypeRef = ((marshalType == null) ? null : RuntimeTypeHandle.GetTypeByNameUsingCARules(marshalType, scope));
		}
		catch (TypeLoadException)
		{
		}
		MarshalAsAttribute marshalAsAttribute = new MarshalAsAttribute(unmanagedType);
		marshalAsAttribute.SafeArraySubType = safeArraySubType;
		marshalAsAttribute.SafeArrayUserDefinedSubType = safeArrayUserDefinedSubType2;
		marshalAsAttribute.IidParameterIndex = iidParamIndex;
		marshalAsAttribute.ArraySubType = arraySubType;
		marshalAsAttribute.SizeParamIndex = (short)sizeParamIndex;
		marshalAsAttribute.SizeConst = sizeConst;
		marshalAsAttribute.MarshalType = marshalType;
		marshalAsAttribute.MarshalTypeRef = marshalTypeRef;
		marshalAsAttribute.MarshalCookie = marshalCookie;
		return marshalAsAttribute;
	}

	private static FieldOffsetAttribute GetFieldOffsetCustomAttribute(RuntimeFieldInfo field)
	{
		if ((object)field.DeclaringType != null && field.GetRuntimeModule().MetadataImport.GetFieldOffset(field.DeclaringType.MetadataToken, field.MetadataToken, out var offset))
		{
			return new FieldOffsetAttribute(offset);
		}
		return null;
	}

	internal static StructLayoutAttribute GetStructLayoutCustomAttribute(RuntimeType type)
	{
		if (type.IsInterface || type.HasElementType || type.IsGenericParameter)
		{
			return null;
		}
		LayoutKind layoutKind = LayoutKind.Auto;
		switch (type.Attributes & TypeAttributes.LayoutMask)
		{
		case TypeAttributes.ExplicitLayout:
			layoutKind = LayoutKind.Explicit;
			break;
		case TypeAttributes.NotPublic:
			layoutKind = LayoutKind.Auto;
			break;
		case TypeAttributes.SequentialLayout:
			layoutKind = LayoutKind.Sequential;
			break;
		}
		CharSet charSet = CharSet.None;
		switch (type.Attributes & TypeAttributes.StringFormatMask)
		{
		case TypeAttributes.NotPublic:
			charSet = CharSet.Ansi;
			break;
		case TypeAttributes.AutoClass:
			charSet = CharSet.Auto;
			break;
		case TypeAttributes.UnicodeClass:
			charSet = CharSet.Unicode;
			break;
		}
		type.GetRuntimeModule().MetadataImport.GetClassLayout(type.MetadataToken, out var packSize, out var classSize);
		if (packSize == 0)
		{
			packSize = 8;
		}
		StructLayoutAttribute structLayoutAttribute = new StructLayoutAttribute(layoutKind);
		structLayoutAttribute.Pack = packSize;
		structLayoutAttribute.Size = classSize;
		structLayoutAttribute.CharSet = charSet;
		return structLayoutAttribute;
	}
}
