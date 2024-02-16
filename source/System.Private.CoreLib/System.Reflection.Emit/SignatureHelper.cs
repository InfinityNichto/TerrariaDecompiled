using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Reflection.Emit;

public sealed class SignatureHelper
{
	private byte[] m_signature;

	private int m_currSig;

	private int m_sizeLoc;

	private ModuleBuilder m_module;

	private bool m_sigDone;

	private int m_argCount;

	internal int ArgumentCount => m_argCount;

	public static SignatureHelper GetMethodSigHelper(Module? mod, Type? returnType, Type[]? parameterTypes)
	{
		return GetMethodSigHelper(mod, CallingConventions.Standard, returnType, null, null, parameterTypes, null, null);
	}

	public static SignatureHelper GetMethodSigHelper(Module? mod, CallingConventions callingConvention, Type? returnType)
	{
		return GetMethodSigHelper(mod, callingConvention, returnType, null, null, null, null, null);
	}

	internal static SignatureHelper GetMethodSpecSigHelper(Module scope, Type[] inst)
	{
		SignatureHelper signatureHelper = new SignatureHelper(scope, MdSigCallingConvention.GenericInst);
		signatureHelper.AddData(inst.Length);
		foreach (Type clsArgument in inst)
		{
			signatureHelper.AddArgument(clsArgument);
		}
		return signatureHelper;
	}

	internal static SignatureHelper GetMethodSigHelper(Module scope, CallingConventions callingConvention, Type returnType, Type[] requiredReturnTypeCustomModifiers, Type[] optionalReturnTypeCustomModifiers, Type[] parameterTypes, Type[][] requiredParameterTypeCustomModifiers, Type[][] optionalParameterTypeCustomModifiers)
	{
		return GetMethodSigHelper(scope, callingConvention, 0, returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers, parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
	}

	internal static SignatureHelper GetMethodSigHelper(Module scope, CallingConventions callingConvention, int cGenericParam, Type returnType, Type[] requiredReturnTypeCustomModifiers, Type[] optionalReturnTypeCustomModifiers, Type[] parameterTypes, Type[][] requiredParameterTypeCustomModifiers, Type[][] optionalParameterTypeCustomModifiers)
	{
		if (returnType == null)
		{
			returnType = typeof(void);
		}
		MdSigCallingConvention mdSigCallingConvention = MdSigCallingConvention.Default;
		if ((callingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
		{
			mdSigCallingConvention = MdSigCallingConvention.Vararg;
		}
		if (cGenericParam > 0)
		{
			mdSigCallingConvention |= MdSigCallingConvention.Generic;
		}
		if ((callingConvention & CallingConventions.HasThis) == CallingConventions.HasThis)
		{
			mdSigCallingConvention |= MdSigCallingConvention.HasThis;
		}
		SignatureHelper signatureHelper = new SignatureHelper(scope, mdSigCallingConvention, cGenericParam, returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers);
		signatureHelper.AddArguments(parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
		return signatureHelper;
	}

	internal static SignatureHelper GetMethodSigHelper(Module mod, CallingConvention unmanagedCallConv, Type returnType)
	{
		if ((object)returnType == null)
		{
			returnType = typeof(void);
		}
		MdSigCallingConvention callingConvention;
		switch (unmanagedCallConv)
		{
		case CallingConvention.Cdecl:
			callingConvention = MdSigCallingConvention.C;
			break;
		case CallingConvention.Winapi:
		case CallingConvention.StdCall:
			callingConvention = MdSigCallingConvention.StdCall;
			break;
		case CallingConvention.ThisCall:
			callingConvention = MdSigCallingConvention.ThisCall;
			break;
		case CallingConvention.FastCall:
			callingConvention = MdSigCallingConvention.FastCall;
			break;
		default:
			throw new ArgumentException(SR.Argument_UnknownUnmanagedCallConv, "unmanagedCallConv");
		}
		return new SignatureHelper(mod, callingConvention, returnType, null, null);
	}

	public static SignatureHelper GetLocalVarSigHelper()
	{
		return GetLocalVarSigHelper(null);
	}

	public static SignatureHelper GetMethodSigHelper(CallingConventions callingConvention, Type? returnType)
	{
		return GetMethodSigHelper(null, callingConvention, returnType);
	}

	internal static SignatureHelper GetMethodSigHelper(CallingConvention unmanagedCallingConvention, Type returnType)
	{
		return GetMethodSigHelper(null, unmanagedCallingConvention, returnType);
	}

	public static SignatureHelper GetLocalVarSigHelper(Module? mod)
	{
		return new SignatureHelper(mod, MdSigCallingConvention.LocalSig);
	}

	public static SignatureHelper GetFieldSigHelper(Module? mod)
	{
		return new SignatureHelper(mod, MdSigCallingConvention.Field);
	}

	public static SignatureHelper GetPropertySigHelper(Module? mod, Type? returnType, Type[]? parameterTypes)
	{
		return GetPropertySigHelper(mod, returnType, null, null, parameterTypes, null, null);
	}

	public static SignatureHelper GetPropertySigHelper(Module? mod, Type? returnType, Type[]? requiredReturnTypeCustomModifiers, Type[]? optionalReturnTypeCustomModifiers, Type[]? parameterTypes, Type[][]? requiredParameterTypeCustomModifiers, Type[][]? optionalParameterTypeCustomModifiers)
	{
		return GetPropertySigHelper(mod, (CallingConventions)0, returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers, parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
	}

	public static SignatureHelper GetPropertySigHelper(Module? mod, CallingConventions callingConvention, Type? returnType, Type[]? requiredReturnTypeCustomModifiers, Type[]? optionalReturnTypeCustomModifiers, Type[]? parameterTypes, Type[][]? requiredParameterTypeCustomModifiers, Type[][]? optionalParameterTypeCustomModifiers)
	{
		if (returnType == null)
		{
			returnType = typeof(void);
		}
		MdSigCallingConvention mdSigCallingConvention = MdSigCallingConvention.Property;
		if ((callingConvention & CallingConventions.HasThis) == CallingConventions.HasThis)
		{
			mdSigCallingConvention |= MdSigCallingConvention.HasThis;
		}
		SignatureHelper signatureHelper = new SignatureHelper(mod, mdSigCallingConvention, returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers);
		signatureHelper.AddArguments(parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
		return signatureHelper;
	}

	internal static SignatureHelper GetTypeSigToken(Module module, Type type)
	{
		if (module == null)
		{
			throw new ArgumentNullException("module");
		}
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		return new SignatureHelper(module, type);
	}

	private SignatureHelper(Module mod, MdSigCallingConvention callingConvention)
	{
		Init(mod, callingConvention);
	}

	private SignatureHelper(Module mod, MdSigCallingConvention callingConvention, int cGenericParameters, Type returnType, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers)
	{
		Init(mod, callingConvention, cGenericParameters);
		if (callingConvention == MdSigCallingConvention.Field)
		{
			throw new ArgumentException(SR.Argument_BadFieldSig);
		}
		AddOneArgTypeHelper(returnType, requiredCustomModifiers, optionalCustomModifiers);
	}

	private SignatureHelper(Module mod, MdSigCallingConvention callingConvention, Type returnType, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers)
		: this(mod, callingConvention, 0, returnType, requiredCustomModifiers, optionalCustomModifiers)
	{
	}

	private SignatureHelper(Module mod, Type type)
	{
		Init(mod);
		AddOneArgTypeHelper(type);
	}

	[MemberNotNull("m_signature")]
	private void Init(Module mod)
	{
		m_signature = new byte[32];
		m_currSig = 0;
		m_module = mod as ModuleBuilder;
		m_argCount = 0;
		m_sigDone = false;
		m_sizeLoc = -1;
		if (m_module == null && mod != null)
		{
			throw new ArgumentException(SR.NotSupported_MustBeModuleBuilder);
		}
	}

	[MemberNotNull("m_signature")]
	private void Init(Module mod, MdSigCallingConvention callingConvention)
	{
		Init(mod, callingConvention, 0);
	}

	[MemberNotNull("m_signature")]
	private void Init(Module mod, MdSigCallingConvention callingConvention, int cGenericParam)
	{
		Init(mod);
		AddData((int)callingConvention);
		if (callingConvention == MdSigCallingConvention.Field || callingConvention == MdSigCallingConvention.GenericInst)
		{
			m_sizeLoc = -1;
			return;
		}
		if (cGenericParam > 0)
		{
			AddData(cGenericParam);
		}
		m_sizeLoc = m_currSig++;
	}

	private void AddOneArgTypeHelper(Type argument, bool pinned)
	{
		if (pinned)
		{
			AddElementType(CorElementType.ELEMENT_TYPE_PINNED);
		}
		AddOneArgTypeHelper(argument);
	}

	private void AddOneArgTypeHelper(Type clsArgument, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers)
	{
		if (optionalCustomModifiers != null)
		{
			foreach (Type type in optionalCustomModifiers)
			{
				if (type == null)
				{
					throw new ArgumentNullException("optionalCustomModifiers");
				}
				if (type.HasElementType)
				{
					throw new ArgumentException(SR.Argument_ArraysInvalid, "optionalCustomModifiers");
				}
				if (type.ContainsGenericParameters)
				{
					throw new ArgumentException(SR.Argument_GenericsInvalid, "optionalCustomModifiers");
				}
				AddElementType(CorElementType.ELEMENT_TYPE_CMOD_OPT);
				int typeToken = m_module.GetTypeToken(type);
				AddToken(typeToken);
			}
		}
		if (requiredCustomModifiers != null)
		{
			foreach (Type type2 in requiredCustomModifiers)
			{
				if (type2 == null)
				{
					throw new ArgumentNullException("requiredCustomModifiers");
				}
				if (type2.HasElementType)
				{
					throw new ArgumentException(SR.Argument_ArraysInvalid, "requiredCustomModifiers");
				}
				if (type2.ContainsGenericParameters)
				{
					throw new ArgumentException(SR.Argument_GenericsInvalid, "requiredCustomModifiers");
				}
				AddElementType(CorElementType.ELEMENT_TYPE_CMOD_REQD);
				int typeToken2 = m_module.GetTypeToken(type2);
				AddToken(typeToken2);
			}
		}
		AddOneArgTypeHelper(clsArgument);
	}

	private void AddOneArgTypeHelper(Type clsArgument)
	{
		AddOneArgTypeHelperWorker(clsArgument, lastWasGenericInst: false);
	}

	private void AddOneArgTypeHelperWorker(Type clsArgument, bool lastWasGenericInst)
	{
		if (clsArgument.IsGenericParameter)
		{
			if (clsArgument.DeclaringMethod != null)
			{
				AddElementType(CorElementType.ELEMENT_TYPE_MVAR);
			}
			else
			{
				AddElementType(CorElementType.ELEMENT_TYPE_VAR);
			}
			AddData(clsArgument.GenericParameterPosition);
			return;
		}
		if (clsArgument.IsGenericType && (!clsArgument.IsGenericTypeDefinition || !lastWasGenericInst))
		{
			AddElementType(CorElementType.ELEMENT_TYPE_GENERICINST);
			AddOneArgTypeHelperWorker(clsArgument.GetGenericTypeDefinition(), lastWasGenericInst: true);
			Type[] genericArguments = clsArgument.GetGenericArguments();
			AddData(genericArguments.Length);
			Type[] array = genericArguments;
			foreach (Type clsArgument2 in array)
			{
				AddOneArgTypeHelper(clsArgument2);
			}
			return;
		}
		if (clsArgument is TypeBuilder)
		{
			TypeBuilder typeBuilder = (TypeBuilder)clsArgument;
			int clsToken = ((!typeBuilder.Module.Equals(m_module)) ? m_module.GetTypeToken(clsArgument) : typeBuilder.TypeToken);
			if (clsArgument.IsValueType)
			{
				InternalAddTypeToken(clsToken, CorElementType.ELEMENT_TYPE_VALUETYPE);
			}
			else
			{
				InternalAddTypeToken(clsToken, CorElementType.ELEMENT_TYPE_CLASS);
			}
			return;
		}
		if (clsArgument is EnumBuilder)
		{
			TypeBuilder typeBuilder2 = ((EnumBuilder)clsArgument).m_typeBuilder;
			int clsToken2 = ((!typeBuilder2.Module.Equals(m_module)) ? m_module.GetTypeToken(clsArgument) : typeBuilder2.TypeToken);
			if (clsArgument.IsValueType)
			{
				InternalAddTypeToken(clsToken2, CorElementType.ELEMENT_TYPE_VALUETYPE);
			}
			else
			{
				InternalAddTypeToken(clsToken2, CorElementType.ELEMENT_TYPE_CLASS);
			}
			return;
		}
		if (clsArgument.IsByRef)
		{
			AddElementType(CorElementType.ELEMENT_TYPE_BYREF);
			clsArgument = clsArgument.GetElementType();
			AddOneArgTypeHelper(clsArgument);
			return;
		}
		if (clsArgument.IsPointer)
		{
			AddElementType(CorElementType.ELEMENT_TYPE_PTR);
			AddOneArgTypeHelper(clsArgument.GetElementType());
			return;
		}
		if (clsArgument.IsArray)
		{
			if (clsArgument.IsSZArray)
			{
				AddElementType(CorElementType.ELEMENT_TYPE_SZARRAY);
				AddOneArgTypeHelper(clsArgument.GetElementType());
				return;
			}
			AddElementType(CorElementType.ELEMENT_TYPE_ARRAY);
			AddOneArgTypeHelper(clsArgument.GetElementType());
			int arrayRank = clsArgument.GetArrayRank();
			AddData(arrayRank);
			AddData(0);
			AddData(arrayRank);
			for (int j = 0; j < arrayRank; j++)
			{
				AddData(0);
			}
			return;
		}
		CorElementType corElementType = CorElementType.ELEMENT_TYPE_MAX;
		if (clsArgument is RuntimeType)
		{
			corElementType = RuntimeTypeHandle.GetCorElementType((RuntimeType)clsArgument);
			if (corElementType == CorElementType.ELEMENT_TYPE_CLASS)
			{
				if (clsArgument == typeof(object))
				{
					corElementType = CorElementType.ELEMENT_TYPE_OBJECT;
				}
				else if (clsArgument == typeof(string))
				{
					corElementType = CorElementType.ELEMENT_TYPE_STRING;
				}
			}
		}
		if (IsSimpleType(corElementType))
		{
			AddElementType(corElementType);
		}
		else if (m_module == null)
		{
			InternalAddRuntimeType(clsArgument);
		}
		else if (clsArgument.IsValueType)
		{
			InternalAddTypeToken(m_module.GetTypeToken(clsArgument), CorElementType.ELEMENT_TYPE_VALUETYPE);
		}
		else
		{
			InternalAddTypeToken(m_module.GetTypeToken(clsArgument), CorElementType.ELEMENT_TYPE_CLASS);
		}
	}

	private void AddData(int data)
	{
		if (m_currSig + 4 > m_signature.Length)
		{
			m_signature = ExpandArray(m_signature);
		}
		if (data <= 127)
		{
			m_signature[m_currSig++] = (byte)data;
			return;
		}
		if (data <= 16383)
		{
			BinaryPrimitives.WriteInt16BigEndian(m_signature.AsSpan(m_currSig), (short)(data | 0x8000));
			m_currSig += 2;
			return;
		}
		if (data <= 536870911)
		{
			BinaryPrimitives.WriteInt32BigEndian(m_signature.AsSpan(m_currSig), (int)(data | 0xC0000000u));
			m_currSig += 4;
			return;
		}
		throw new ArgumentException(SR.Argument_LargeInteger);
	}

	private void AddElementType(CorElementType cvt)
	{
		if (m_currSig + 1 > m_signature.Length)
		{
			m_signature = ExpandArray(m_signature);
		}
		m_signature[m_currSig++] = (byte)cvt;
	}

	private void AddToken(int token)
	{
		int num = token & 0xFFFFFF;
		MetadataTokenType metadataTokenType = (MetadataTokenType)(token & -16777216);
		if (num > 67108863)
		{
			throw new ArgumentException(SR.Argument_LargeInteger);
		}
		num <<= 2;
		switch (metadataTokenType)
		{
		case MetadataTokenType.TypeRef:
			num |= 1;
			break;
		case MetadataTokenType.TypeSpec:
			num |= 2;
			break;
		}
		AddData(num);
	}

	private void InternalAddTypeToken(int clsToken, CorElementType CorType)
	{
		AddElementType(CorType);
		AddToken(clsToken);
	}

	private unsafe void InternalAddRuntimeType(Type type)
	{
		AddElementType(CorElementType.ELEMENT_TYPE_INTERNAL);
		IntPtr value = type.GetTypeHandleInternal().Value;
		if (m_currSig + sizeof(void*) > m_signature.Length)
		{
			m_signature = ExpandArray(m_signature);
		}
		byte* ptr = (byte*)(&value);
		for (int i = 0; i < sizeof(void*); i++)
		{
			m_signature[m_currSig++] = ptr[i];
		}
	}

	private static byte[] ExpandArray(byte[] inArray)
	{
		return ExpandArray(inArray, inArray.Length * 2);
	}

	private static byte[] ExpandArray(byte[] inArray, int requiredLength)
	{
		if (requiredLength < inArray.Length)
		{
			requiredLength = inArray.Length * 2;
		}
		byte[] array = new byte[requiredLength];
		Buffer.BlockCopy(inArray, 0, array, 0, inArray.Length);
		return array;
	}

	private void IncrementArgCounts()
	{
		if (m_sizeLoc != -1)
		{
			m_argCount++;
		}
	}

	private void SetNumberOfSignatureElements(bool forceCopy)
	{
		int currSig = m_currSig;
		if (m_sizeLoc != -1)
		{
			if (m_argCount < 128 && !forceCopy)
			{
				m_signature[m_sizeLoc] = (byte)m_argCount;
				return;
			}
			int num = ((m_argCount < 128) ? 1 : ((m_argCount >= 16384) ? 4 : 2));
			byte[] array = new byte[m_currSig + num - 1];
			array[0] = m_signature[0];
			Buffer.BlockCopy(m_signature, m_sizeLoc + 1, array, m_sizeLoc + num, currSig - (m_sizeLoc + 1));
			m_signature = array;
			m_currSig = m_sizeLoc;
			AddData(m_argCount);
			m_currSig = currSig + (num - 1);
		}
	}

	internal static bool IsSimpleType(CorElementType type)
	{
		if ((int)type <= 14)
		{
			return true;
		}
		if (type == CorElementType.ELEMENT_TYPE_TYPEDBYREF || type == CorElementType.ELEMENT_TYPE_I || type == CorElementType.ELEMENT_TYPE_U || type == CorElementType.ELEMENT_TYPE_OBJECT)
		{
			return true;
		}
		return false;
	}

	internal byte[] InternalGetSignature(out int length)
	{
		if (!m_sigDone)
		{
			m_sigDone = true;
			SetNumberOfSignatureElements(forceCopy: false);
		}
		length = m_currSig;
		return m_signature;
	}

	internal byte[] InternalGetSignatureArray()
	{
		int argCount = m_argCount;
		int currSig = m_currSig;
		int num = currSig;
		num = ((argCount < 127) ? (num + 1) : ((argCount >= 16383) ? (num + 4) : (num + 2)));
		byte[] array = new byte[num];
		int dstOffset = 0;
		array[dstOffset++] = m_signature[0];
		if (argCount <= 127)
		{
			array[dstOffset++] = (byte)((uint)argCount & 0xFFu);
		}
		else if (argCount <= 16383)
		{
			array[dstOffset++] = (byte)((uint)(argCount >> 8) | 0x80u);
			array[dstOffset++] = (byte)((uint)argCount & 0xFFu);
		}
		else
		{
			if (argCount > 536870911)
			{
				throw new ArgumentException(SR.Argument_LargeInteger);
			}
			array[dstOffset++] = (byte)((uint)(argCount >> 24) | 0xC0u);
			array[dstOffset++] = (byte)((uint)(argCount >> 16) & 0xFFu);
			array[dstOffset++] = (byte)((uint)(argCount >> 8) & 0xFFu);
			array[dstOffset++] = (byte)((uint)argCount & 0xFFu);
		}
		Buffer.BlockCopy(m_signature, 2, array, dstOffset, currSig - 2);
		array[num - 1] = 0;
		return array;
	}

	public void AddArgument(Type clsArgument)
	{
		AddArgument(clsArgument, null, null);
	}

	public void AddArgument(Type argument, bool pinned)
	{
		if (argument == null)
		{
			throw new ArgumentNullException("argument");
		}
		IncrementArgCounts();
		AddOneArgTypeHelper(argument, pinned);
	}

	public void AddArguments(Type[]? arguments, Type[][]? requiredCustomModifiers, Type[][]? optionalCustomModifiers)
	{
		if (requiredCustomModifiers != null && (arguments == null || requiredCustomModifiers.Length != arguments.Length))
		{
			throw new ArgumentException(SR.Format(SR.Argument_MismatchedArrays, "requiredCustomModifiers", "arguments"));
		}
		if (optionalCustomModifiers != null && (arguments == null || optionalCustomModifiers.Length != arguments.Length))
		{
			throw new ArgumentException(SR.Format(SR.Argument_MismatchedArrays, "optionalCustomModifiers", "arguments"));
		}
		if (arguments != null)
		{
			for (int i = 0; i < arguments.Length; i++)
			{
				AddArgument(arguments[i], (requiredCustomModifiers != null) ? requiredCustomModifiers[i] : null, (optionalCustomModifiers != null) ? optionalCustomModifiers[i] : null);
			}
		}
	}

	public void AddArgument(Type argument, Type[]? requiredCustomModifiers, Type[]? optionalCustomModifiers)
	{
		if (m_sigDone)
		{
			throw new ArgumentException(SR.Argument_SigIsFinalized);
		}
		if (argument == null)
		{
			throw new ArgumentNullException("argument");
		}
		IncrementArgCounts();
		AddOneArgTypeHelper(argument, requiredCustomModifiers, optionalCustomModifiers);
	}

	public void AddSentinel()
	{
		AddElementType(CorElementType.ELEMENT_TYPE_SENTINEL);
	}

	public override bool Equals(object? obj)
	{
		if (!(obj is SignatureHelper))
		{
			return false;
		}
		SignatureHelper signatureHelper = (SignatureHelper)obj;
		if (!signatureHelper.m_module.Equals(m_module) || signatureHelper.m_currSig != m_currSig || signatureHelper.m_sizeLoc != m_sizeLoc || signatureHelper.m_sigDone != m_sigDone)
		{
			return false;
		}
		for (int i = 0; i < m_currSig; i++)
		{
			if (m_signature[i] != signatureHelper.m_signature[i])
			{
				return false;
			}
		}
		return true;
	}

	public override int GetHashCode()
	{
		int num = m_module.GetHashCode() + m_currSig + m_sizeLoc;
		if (m_sigDone)
		{
			num++;
		}
		for (int i = 0; i < m_currSig; i++)
		{
			num += m_signature[i].GetHashCode();
		}
		return num;
	}

	public byte[] GetSignature()
	{
		return GetSignature(appendEndOfSig: false);
	}

	internal byte[] GetSignature(bool appendEndOfSig)
	{
		if (!m_sigDone)
		{
			if (appendEndOfSig)
			{
				AddElementType(CorElementType.ELEMENT_TYPE_END);
			}
			SetNumberOfSignatureElements(forceCopy: true);
			m_sigDone = true;
		}
		if (m_signature.Length > m_currSig)
		{
			byte[] array = new byte[m_currSig];
			Array.Copy(m_signature, array, m_currSig);
			m_signature = array;
		}
		return m_signature;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("Length: ").Append(m_currSig).AppendLine();
		if (m_sizeLoc != -1)
		{
			stringBuilder.Append("Arguments: ").Append(m_signature[m_sizeLoc]).AppendLine();
		}
		else
		{
			stringBuilder.AppendLine("Field Signature");
		}
		stringBuilder.AppendLine("Signature: ");
		for (int i = 0; i <= m_currSig; i++)
		{
			stringBuilder.Append(m_signature[i]).Append("  ");
		}
		stringBuilder.AppendLine();
		return stringBuilder.ToString();
	}
}
