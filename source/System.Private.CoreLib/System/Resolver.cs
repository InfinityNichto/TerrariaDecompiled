using System.Reflection;

namespace System;

internal abstract class Resolver
{
	internal struct CORINFO_EH_CLAUSE
	{
		internal int Flags;

		internal int TryOffset;

		internal int TryLength;

		internal int HandlerOffset;

		internal int HandlerLength;

		internal int ClassTokenOrFilterOffset;
	}

	internal abstract RuntimeType GetJitContext(out int securityControlFlags);

	internal abstract byte[] GetCodeInfo(out int stackSize, out int initLocals, out int EHCount);

	internal abstract byte[] GetLocalsSignature();

	internal unsafe abstract void GetEHInfo(int EHNumber, void* exception);

	internal abstract byte[] GetRawEHInfo();

	internal abstract string GetStringLiteral(int token);

	internal abstract void ResolveToken(int token, out IntPtr typeHandle, out IntPtr methodHandle, out IntPtr fieldHandle);

	internal abstract byte[] ResolveSignature(int token, int fromMethod);

	internal abstract MethodInfo GetDynamicMethod();
}
