namespace System.Reflection.Emit;

internal sealed class DynamicResolver : Resolver
{
	private sealed class DestroyScout
	{
		internal RuntimeMethodHandleInternal m_methodHandle;

		~DestroyScout()
		{
			if (!m_methodHandle.IsNullHandle())
			{
				if (RuntimeMethodHandle.GetResolver(m_methodHandle) != null)
				{
					GC.ReRegisterForFinalize(this);
				}
				else
				{
					RuntimeMethodHandle.Destroy(m_methodHandle);
				}
			}
		}
	}

	[Flags]
	internal enum SecurityControlFlags
	{
		Default = 0,
		SkipVisibilityChecks = 1,
		RestrictedSkipVisibilityChecks = 2,
		HasCreationContext = 4,
		CanSkipCSEvaluation = 8
	}

	private __ExceptionInfo[] m_exceptions;

	private byte[] m_exceptionHeader;

	private DynamicMethod m_method;

	private byte[] m_code;

	private byte[] m_localSignature;

	private int m_stackSize;

	private DynamicScope m_scope;

	internal DynamicResolver(DynamicILGenerator ilGenerator)
	{
		m_stackSize = ilGenerator.GetMaxStackSize();
		m_exceptions = ilGenerator.GetExceptions();
		m_code = ilGenerator.BakeByteArray();
		m_localSignature = ilGenerator.m_localSignature.InternalGetSignatureArray();
		m_scope = ilGenerator.m_scope;
		m_method = (DynamicMethod)ilGenerator.m_methodBuilder;
		m_method.m_resolver = this;
	}

	internal DynamicResolver(DynamicILInfo dynamicILInfo)
	{
		m_stackSize = dynamicILInfo.MaxStackSize;
		m_code = dynamicILInfo.Code;
		m_localSignature = dynamicILInfo.LocalSignature;
		m_exceptionHeader = dynamicILInfo.Exceptions;
		m_scope = dynamicILInfo.DynamicScope;
		m_method = dynamicILInfo.DynamicMethod;
		m_method.m_resolver = this;
	}

	~DynamicResolver()
	{
		DynamicMethod method = m_method;
		if (!(method == null) && method.m_methodHandle != null)
		{
			DestroyScout destroyScout;
			try
			{
				destroyScout = new DestroyScout();
			}
			catch
			{
				GC.ReRegisterForFinalize(this);
				return;
			}
			destroyScout.m_methodHandle = method.m_methodHandle.Value;
		}
	}

	internal override RuntimeType GetJitContext(out int securityControlFlags)
	{
		SecurityControlFlags securityControlFlags2 = SecurityControlFlags.Default;
		if (m_method.m_restrictedSkipVisibility)
		{
			securityControlFlags2 |= SecurityControlFlags.RestrictedSkipVisibilityChecks;
		}
		else if (m_method.m_skipVisibility)
		{
			securityControlFlags2 |= SecurityControlFlags.SkipVisibilityChecks;
		}
		RuntimeType typeOwner = m_method.m_typeOwner;
		securityControlFlags = (int)securityControlFlags2;
		return typeOwner;
	}

	private static int CalculateNumberOfExceptions(__ExceptionInfo[] excp)
	{
		int num = 0;
		if (excp == null)
		{
			return 0;
		}
		for (int i = 0; i < excp.Length; i++)
		{
			num += excp[i].GetNumberOfCatches();
		}
		return num;
	}

	internal override byte[] GetCodeInfo(out int stackSize, out int initLocals, out int EHCount)
	{
		stackSize = m_stackSize;
		if (m_exceptionHeader != null && m_exceptionHeader.Length != 0)
		{
			if (m_exceptionHeader.Length < 4)
			{
				throw new FormatException();
			}
			byte b = m_exceptionHeader[0];
			if ((b & 0x40u) != 0)
			{
				int num = m_exceptionHeader[3] << 16;
				num |= m_exceptionHeader[2] << 8;
				num |= m_exceptionHeader[1];
				EHCount = (num - 4) / 24;
			}
			else
			{
				EHCount = (m_exceptionHeader[1] - 2) / 12;
			}
		}
		else
		{
			EHCount = CalculateNumberOfExceptions(m_exceptions);
		}
		initLocals = (m_method.InitLocals ? 1 : 0);
		return m_code;
	}

	internal override byte[] GetLocalsSignature()
	{
		return m_localSignature;
	}

	internal override byte[] GetRawEHInfo()
	{
		return m_exceptionHeader;
	}

	internal unsafe override void GetEHInfo(int excNumber, void* exc)
	{
		for (int i = 0; i < m_exceptions.Length; i++)
		{
			int numberOfCatches = m_exceptions[i].GetNumberOfCatches();
			if (excNumber < numberOfCatches)
			{
				((CORINFO_EH_CLAUSE*)exc)->Flags = m_exceptions[i].GetExceptionTypes()[excNumber];
				((CORINFO_EH_CLAUSE*)exc)->TryOffset = m_exceptions[i].GetStartAddress();
				if ((((CORINFO_EH_CLAUSE*)exc)->Flags & 2) != 2)
				{
					((CORINFO_EH_CLAUSE*)exc)->TryLength = m_exceptions[i].GetEndAddress() - ((CORINFO_EH_CLAUSE*)exc)->TryOffset;
				}
				else
				{
					((CORINFO_EH_CLAUSE*)exc)->TryLength = m_exceptions[i].GetFinallyEndAddress() - ((CORINFO_EH_CLAUSE*)exc)->TryOffset;
				}
				((CORINFO_EH_CLAUSE*)exc)->HandlerOffset = m_exceptions[i].GetCatchAddresses()[excNumber];
				((CORINFO_EH_CLAUSE*)exc)->HandlerLength = m_exceptions[i].GetCatchEndAddresses()[excNumber] - ((CORINFO_EH_CLAUSE*)exc)->HandlerOffset;
				((CORINFO_EH_CLAUSE*)exc)->ClassTokenOrFilterOffset = m_exceptions[i].GetFilterAddresses()[excNumber];
				break;
			}
			excNumber -= numberOfCatches;
		}
	}

	internal override string GetStringLiteral(int token)
	{
		return m_scope.GetString(token);
	}

	internal override void ResolveToken(int token, out IntPtr typeHandle, out IntPtr methodHandle, out IntPtr fieldHandle)
	{
		typeHandle = default(IntPtr);
		methodHandle = default(IntPtr);
		fieldHandle = default(IntPtr);
		object obj = m_scope[token];
		if (obj == null)
		{
			throw new InvalidProgramException();
		}
		if (obj is RuntimeTypeHandle)
		{
			typeHandle = ((RuntimeTypeHandle)obj).Value;
			return;
		}
		if (obj is RuntimeMethodHandle)
		{
			methodHandle = ((RuntimeMethodHandle)obj).Value;
			return;
		}
		if (obj is RuntimeFieldHandle)
		{
			fieldHandle = ((RuntimeFieldHandle)obj).Value;
			return;
		}
		DynamicMethod dynamicMethod = obj as DynamicMethod;
		if (dynamicMethod != null)
		{
			methodHandle = dynamicMethod.GetMethodDescriptor().Value;
		}
		else if (obj is GenericMethodInfo genericMethodInfo)
		{
			methodHandle = genericMethodInfo.m_methodHandle.Value;
			typeHandle = genericMethodInfo.m_context.Value;
		}
		else if (obj is GenericFieldInfo genericFieldInfo)
		{
			fieldHandle = genericFieldInfo.m_fieldHandle.Value;
			typeHandle = genericFieldInfo.m_context.Value;
		}
		else if (obj is VarArgMethod varArgMethod)
		{
			if (varArgMethod.m_dynamicMethod == null)
			{
				methodHandle = varArgMethod.m_method.MethodHandle.Value;
				typeHandle = varArgMethod.m_method.GetDeclaringTypeInternal().GetTypeHandleInternal().Value;
			}
			else
			{
				methodHandle = varArgMethod.m_dynamicMethod.GetMethodDescriptor().Value;
			}
		}
	}

	internal override byte[] ResolveSignature(int token, int fromMethod)
	{
		return m_scope.ResolveSignature(token, fromMethod);
	}

	internal override MethodInfo GetDynamicMethod()
	{
		return m_method.GetMethodInfo();
	}
}
