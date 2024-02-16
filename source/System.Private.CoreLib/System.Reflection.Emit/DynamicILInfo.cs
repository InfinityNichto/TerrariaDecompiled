namespace System.Reflection.Emit;

public class DynamicILInfo
{
	private DynamicMethod m_method;

	private DynamicScope m_scope;

	private byte[] m_exceptions;

	private byte[] m_code;

	private byte[] m_localSignature;

	private int m_maxStackSize;

	private int m_methodSignature;

	internal byte[] LocalSignature => m_localSignature ?? (m_localSignature = SignatureHelper.GetLocalVarSigHelper().InternalGetSignatureArray());

	internal byte[] Exceptions => m_exceptions;

	internal byte[] Code => m_code;

	internal int MaxStackSize => m_maxStackSize;

	public DynamicMethod DynamicMethod => m_method;

	internal DynamicScope DynamicScope => m_scope;

	internal DynamicILInfo(DynamicMethod method, byte[] methodSignature)
	{
		m_scope = new DynamicScope();
		m_method = method;
		m_methodSignature = m_scope.GetTokenFor(methodSignature);
		m_exceptions = Array.Empty<byte>();
		m_code = Array.Empty<byte>();
		m_localSignature = Array.Empty<byte>();
	}

	internal void GetCallableMethod(RuntimeModule module, DynamicMethod dm)
	{
		dm.m_methodHandle = ModuleHandle.GetDynamicMethod(dm, module, m_method.Name, (byte[])m_scope[m_methodSignature], new DynamicResolver(this));
	}

	public void SetCode(byte[]? code, int maxStackSize)
	{
		m_code = ((code != null) ? ((byte[])code.Clone()) : Array.Empty<byte>());
		m_maxStackSize = maxStackSize;
	}

	[CLSCompliant(false)]
	public unsafe void SetCode(byte* code, int codeSize, int maxStackSize)
	{
		if (codeSize < 0)
		{
			throw new ArgumentOutOfRangeException("codeSize", SR.ArgumentOutOfRange_GenericPositive);
		}
		if (codeSize > 0 && code == null)
		{
			throw new ArgumentNullException("code");
		}
		m_code = new Span<byte>(code, codeSize).ToArray();
		m_maxStackSize = maxStackSize;
	}

	public void SetExceptions(byte[]? exceptions)
	{
		m_exceptions = ((exceptions != null) ? ((byte[])exceptions.Clone()) : Array.Empty<byte>());
	}

	[CLSCompliant(false)]
	public unsafe void SetExceptions(byte* exceptions, int exceptionsSize)
	{
		if (exceptionsSize < 0)
		{
			throw new ArgumentOutOfRangeException("exceptionsSize", SR.ArgumentOutOfRange_GenericPositive);
		}
		if (exceptionsSize > 0 && exceptions == null)
		{
			throw new ArgumentNullException("exceptions");
		}
		m_exceptions = new Span<byte>(exceptions, exceptionsSize).ToArray();
	}

	public void SetLocalSignature(byte[]? localSignature)
	{
		m_localSignature = ((localSignature != null) ? ((byte[])localSignature.Clone()) : Array.Empty<byte>());
	}

	[CLSCompliant(false)]
	public unsafe void SetLocalSignature(byte* localSignature, int signatureSize)
	{
		if (signatureSize < 0)
		{
			throw new ArgumentOutOfRangeException("signatureSize", SR.ArgumentOutOfRange_GenericPositive);
		}
		if (signatureSize > 0 && localSignature == null)
		{
			throw new ArgumentNullException("localSignature");
		}
		m_localSignature = new Span<byte>(localSignature, signatureSize).ToArray();
	}

	public int GetTokenFor(RuntimeMethodHandle method)
	{
		return DynamicScope.GetTokenFor(method);
	}

	public int GetTokenFor(DynamicMethod method)
	{
		return DynamicScope.GetTokenFor(method);
	}

	public int GetTokenFor(RuntimeMethodHandle method, RuntimeTypeHandle contextType)
	{
		return DynamicScope.GetTokenFor(method, contextType);
	}

	public int GetTokenFor(RuntimeFieldHandle field)
	{
		return DynamicScope.GetTokenFor(field);
	}

	public int GetTokenFor(RuntimeFieldHandle field, RuntimeTypeHandle contextType)
	{
		return DynamicScope.GetTokenFor(field, contextType);
	}

	public int GetTokenFor(RuntimeTypeHandle type)
	{
		return DynamicScope.GetTokenFor(type);
	}

	public int GetTokenFor(string literal)
	{
		return DynamicScope.GetTokenFor(literal);
	}

	public int GetTokenFor(byte[] signature)
	{
		return DynamicScope.GetTokenFor(signature);
	}
}
