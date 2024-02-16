using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection;

internal readonly struct MetadataImport
{
	private readonly IntPtr m_metadataImport2;

	private readonly object m_keepalive;

	internal static readonly MetadataImport EmptyImport = new MetadataImport((IntPtr)0, null);

	public override int GetHashCode()
	{
		return ValueType.GetHashCodeOfPtr(m_metadataImport2);
	}

	public override bool Equals(object obj)
	{
		if (!(obj is MetadataImport))
		{
			return false;
		}
		return Equals((MetadataImport)obj);
	}

	private bool Equals(MetadataImport import)
	{
		return import.m_metadataImport2 == m_metadataImport2;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void _GetMarshalAs(IntPtr pNativeType, int cNativeType, out int unmanagedType, out int safeArraySubType, out string safeArrayUserDefinedSubType, out int arraySubType, out int sizeParamIndex, out int sizeConst, out string marshalType, out string marshalCookie, out int iidParamIndex);

	internal static void GetMarshalAs(ConstArray nativeType, out UnmanagedType unmanagedType, out VarEnum safeArraySubType, out string safeArrayUserDefinedSubType, out UnmanagedType arraySubType, out int sizeParamIndex, out int sizeConst, out string marshalType, out string marshalCookie, out int iidParamIndex)
	{
		_GetMarshalAs(nativeType.Signature, nativeType.Length, out var unmanagedType2, out var safeArraySubType2, out safeArrayUserDefinedSubType, out var arraySubType2, out sizeParamIndex, out sizeConst, out marshalType, out marshalCookie, out iidParamIndex);
		unmanagedType = (UnmanagedType)unmanagedType2;
		safeArraySubType = (VarEnum)safeArraySubType2;
		arraySubType = (UnmanagedType)arraySubType2;
	}

	internal static void ThrowError(int hResult)
	{
		throw new MetadataException(hResult);
	}

	internal MetadataImport(IntPtr metadataImport2, object keepalive)
	{
		m_metadataImport2 = metadataImport2;
		m_keepalive = keepalive;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void _Enum(IntPtr scope, int type, int parent, out MetadataEnumResult result);

	public void Enum(MetadataTokenType type, int parent, out MetadataEnumResult result)
	{
		_Enum(m_metadataImport2, (int)type, parent, out result);
	}

	public void EnumNestedTypes(int mdTypeDef, out MetadataEnumResult result)
	{
		Enum(MetadataTokenType.TypeDef, mdTypeDef, out result);
	}

	public void EnumCustomAttributes(int mdToken, out MetadataEnumResult result)
	{
		Enum(MetadataTokenType.CustomAttribute, mdToken, out result);
	}

	public void EnumParams(int mdMethodDef, out MetadataEnumResult result)
	{
		Enum(MetadataTokenType.ParamDef, mdMethodDef, out result);
	}

	public void EnumFields(int mdTypeDef, out MetadataEnumResult result)
	{
		Enum(MetadataTokenType.FieldDef, mdTypeDef, out result);
	}

	public void EnumProperties(int mdTypeDef, out MetadataEnumResult result)
	{
		Enum(MetadataTokenType.Property, mdTypeDef, out result);
	}

	public void EnumEvents(int mdTypeDef, out MetadataEnumResult result)
	{
		Enum(MetadataTokenType.Event, mdTypeDef, out result);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern string _GetDefaultValue(IntPtr scope, int mdToken, out long value, out int length, out int corElementType);

	public string GetDefaultValue(int mdToken, out long value, out int length, out CorElementType corElementType)
	{
		int corElementType2;
		string result = _GetDefaultValue(m_metadataImport2, mdToken, out value, out length, out corElementType2);
		corElementType = (CorElementType)corElementType2;
		return result;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern void _GetUserString(IntPtr scope, int mdToken, void** name, out int length);

	public unsafe string GetUserString(int mdToken)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out void* ptr);
		_GetUserString(m_metadataImport2, mdToken, &ptr, out var length);
		if (ptr == null)
		{
			return null;
		}
		return new string((char*)ptr, 0, length);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern void _GetName(IntPtr scope, int mdToken, void** name);

	public unsafe MdUtf8String GetName(int mdToken)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out void* pStringHeap);
		_GetName(m_metadataImport2, mdToken, &pStringHeap);
		return new MdUtf8String(pStringHeap);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern void _GetNamespace(IntPtr scope, int mdToken, void** namesp);

	public unsafe MdUtf8String GetNamespace(int mdToken)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out void* pStringHeap);
		_GetNamespace(m_metadataImport2, mdToken, &pStringHeap);
		return new MdUtf8String(pStringHeap);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern void _GetEventProps(IntPtr scope, int mdToken, void** name, out int eventAttributes);

	public unsafe void GetEventProps(int mdToken, out void* name, out EventAttributes eventAttributes)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out void* ptr);
		_GetEventProps(m_metadataImport2, mdToken, &ptr, out var eventAttributes2);
		name = ptr;
		eventAttributes = (EventAttributes)eventAttributes2;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void _GetFieldDefProps(IntPtr scope, int mdToken, out int fieldAttributes);

	public void GetFieldDefProps(int mdToken, out FieldAttributes fieldAttributes)
	{
		_GetFieldDefProps(m_metadataImport2, mdToken, out var fieldAttributes2);
		fieldAttributes = (FieldAttributes)fieldAttributes2;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern void _GetPropertyProps(IntPtr scope, int mdToken, void** name, out int propertyAttributes, out ConstArray signature);

	public unsafe void GetPropertyProps(int mdToken, out void* name, out PropertyAttributes propertyAttributes, out ConstArray signature)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out void* ptr);
		_GetPropertyProps(m_metadataImport2, mdToken, &ptr, out var propertyAttributes2, out signature);
		name = ptr;
		propertyAttributes = (PropertyAttributes)propertyAttributes2;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void _GetParentToken(IntPtr scope, int mdToken, out int tkParent);

	public int GetParentToken(int tkToken)
	{
		_GetParentToken(m_metadataImport2, tkToken, out var tkParent);
		return tkParent;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void _GetParamDefProps(IntPtr scope, int parameterToken, out int sequence, out int attributes);

	public void GetParamDefProps(int parameterToken, out int sequence, out ParameterAttributes attributes)
	{
		_GetParamDefProps(m_metadataImport2, parameterToken, out sequence, out var attributes2);
		attributes = (ParameterAttributes)attributes2;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void _GetGenericParamProps(IntPtr scope, int genericParameter, out int flags);

	public void GetGenericParamProps(int genericParameter, out GenericParameterAttributes attributes)
	{
		_GetGenericParamProps(m_metadataImport2, genericParameter, out var flags);
		attributes = (GenericParameterAttributes)flags;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void _GetScopeProps(IntPtr scope, out Guid mvid);

	public void GetScopeProps(out Guid mvid)
	{
		_GetScopeProps(m_metadataImport2, out mvid);
	}

	public ConstArray GetMethodSignature(MetadataToken token)
	{
		if (token.IsMemberRef)
		{
			return GetMemberRefProps(token);
		}
		return GetSigOfMethodDef(token);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void _GetSigOfMethodDef(IntPtr scope, int methodToken, ref ConstArray signature);

	public ConstArray GetSigOfMethodDef(int methodToken)
	{
		ConstArray signature = default(ConstArray);
		_GetSigOfMethodDef(m_metadataImport2, methodToken, ref signature);
		return signature;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void _GetSignatureFromToken(IntPtr scope, int methodToken, ref ConstArray signature);

	public ConstArray GetSignatureFromToken(int token)
	{
		ConstArray signature = default(ConstArray);
		_GetSignatureFromToken(m_metadataImport2, token, ref signature);
		return signature;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void _GetMemberRefProps(IntPtr scope, int memberTokenRef, out ConstArray signature);

	public ConstArray GetMemberRefProps(int memberTokenRef)
	{
		_GetMemberRefProps(m_metadataImport2, memberTokenRef, out var signature);
		return signature;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void _GetCustomAttributeProps(IntPtr scope, int customAttributeToken, out int constructorToken, out ConstArray signature);

	public void GetCustomAttributeProps(int customAttributeToken, out int constructorToken, out ConstArray signature)
	{
		_GetCustomAttributeProps(m_metadataImport2, customAttributeToken, out constructorToken, out signature);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void _GetClassLayout(IntPtr scope, int typeTokenDef, out int packSize, out int classSize);

	public void GetClassLayout(int typeTokenDef, out int packSize, out int classSize)
	{
		_GetClassLayout(m_metadataImport2, typeTokenDef, out packSize, out classSize);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern bool _GetFieldOffset(IntPtr scope, int typeTokenDef, int fieldTokenDef, out int offset);

	public bool GetFieldOffset(int typeTokenDef, int fieldTokenDef, out int offset)
	{
		return _GetFieldOffset(m_metadataImport2, typeTokenDef, fieldTokenDef, out offset);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void _GetSigOfFieldDef(IntPtr scope, int fieldToken, ref ConstArray fieldMarshal);

	public ConstArray GetSigOfFieldDef(int fieldToken)
	{
		ConstArray fieldMarshal = default(ConstArray);
		_GetSigOfFieldDef(m_metadataImport2, fieldToken, ref fieldMarshal);
		return fieldMarshal;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void _GetFieldMarshal(IntPtr scope, int fieldToken, ref ConstArray fieldMarshal);

	public ConstArray GetFieldMarshal(int fieldToken)
	{
		ConstArray fieldMarshal = default(ConstArray);
		_GetFieldMarshal(m_metadataImport2, fieldToken, ref fieldMarshal);
		return fieldMarshal;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern void _GetPInvokeMap(IntPtr scope, int token, out int attributes, void** importName, void** importDll);

	public unsafe void GetPInvokeMap(int token, out PInvokeAttributes attributes, out string importName, out string importDll)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out void* pStringHeap);
		System.Runtime.CompilerServices.Unsafe.SkipInit(out void* pStringHeap2);
		_GetPInvokeMap(m_metadataImport2, token, out var attributes2, &pStringHeap, &pStringHeap2);
		importName = new MdUtf8String(pStringHeap).ToString();
		importDll = new MdUtf8String(pStringHeap2).ToString();
		attributes = (PInvokeAttributes)attributes2;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern bool _IsValidToken(IntPtr scope, int token);

	public bool IsValidToken(int token)
	{
		return _IsValidToken(m_metadataImport2, token);
	}
}
