using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System;

internal class Signature
{
	internal RuntimeType[] m_arguments;

	internal RuntimeType m_declaringType;

	internal RuntimeType m_returnTypeORfieldType;

	internal object m_keepalive;

	internal unsafe void* m_sig;

	internal int m_managedCallingConventionAndArgIteratorFlags;

	internal int m_nSizeOfArgStack;

	internal int m_csig;

	internal RuntimeMethodHandleInternal m_pMethod;

	internal CallingConventions CallingConvention => (CallingConventions)(byte)m_managedCallingConventionAndArgIteratorFlags;

	internal RuntimeType[] Arguments => m_arguments;

	internal RuntimeType ReturnType => m_returnTypeORfieldType;

	internal RuntimeType FieldType => m_returnTypeORfieldType;

	[MethodImpl(MethodImplOptions.InternalCall)]
	[MemberNotNull("m_arguments")]
	[MemberNotNull("m_returnTypeORfieldType")]
	private unsafe extern void GetSignature(void* pCorSig, int cCorSig, RuntimeFieldHandleInternal fieldHandle, IRuntimeMethodInfo methodHandle, RuntimeType declaringType);

	public unsafe Signature(IRuntimeMethodInfo method, RuntimeType[] arguments, RuntimeType returnType, CallingConventions callingConvention)
	{
		m_pMethod = method.Value;
		m_arguments = arguments;
		m_returnTypeORfieldType = returnType;
		m_managedCallingConventionAndArgIteratorFlags = (byte)callingConvention;
		GetSignature(null, 0, default(RuntimeFieldHandleInternal), method, null);
	}

	public unsafe Signature(IRuntimeMethodInfo methodHandle, RuntimeType declaringType)
	{
		GetSignature(null, 0, default(RuntimeFieldHandleInternal), methodHandle, declaringType);
	}

	public unsafe Signature(IRuntimeFieldInfo fieldHandle, RuntimeType declaringType)
	{
		GetSignature(null, 0, fieldHandle.Value, null, declaringType);
		GC.KeepAlive(fieldHandle);
	}

	public unsafe Signature(void* pCorSig, int cCorSig, RuntimeType declaringType)
	{
		GetSignature(pCorSig, cCorSig, default(RuntimeFieldHandleInternal), null, declaringType);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool CompareSig(Signature sig1, Signature sig2);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal extern Type[] GetCustomModifiers(int position, bool required);
}
