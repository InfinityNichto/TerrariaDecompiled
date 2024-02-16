using System.Collections.Generic;

namespace System.Reflection.Emit;

internal sealed class DynamicScope
{
	internal readonly List<object> m_tokens = new List<object> { null };

	internal object this[int token]
	{
		get
		{
			token &= 0xFFFFFF;
			if (token < 0 || token > m_tokens.Count)
			{
				return null;
			}
			return m_tokens[token];
		}
	}

	internal int GetTokenFor(VarArgMethod varArgMethod)
	{
		m_tokens.Add(varArgMethod);
		return (m_tokens.Count - 1) | 0xA000000;
	}

	internal string GetString(int token)
	{
		return this[token] as string;
	}

	internal byte[] ResolveSignature(int token, int fromMethod)
	{
		if (fromMethod == 0)
		{
			return (byte[])this[token];
		}
		if (!(this[token] is VarArgMethod varArgMethod))
		{
			return null;
		}
		return varArgMethod.m_signature.GetSignature(appendEndOfSig: true);
	}

	public int GetTokenFor(RuntimeMethodHandle method)
	{
		IRuntimeMethodInfo methodInfo = method.GetMethodInfo();
		if (methodInfo != null)
		{
			RuntimeMethodHandleInternal value = methodInfo.Value;
			if (!RuntimeMethodHandle.IsDynamicMethod(value))
			{
				RuntimeType declaringType = RuntimeMethodHandle.GetDeclaringType(value);
				if (declaringType != null && RuntimeTypeHandle.HasInstantiation(declaringType))
				{
					MethodBase methodBase = RuntimeType.GetMethodBase(methodInfo);
					Type genericTypeDefinition = methodBase.DeclaringType.GetGenericTypeDefinition();
					throw new ArgumentException(SR.Format(SR.Argument_MethodDeclaringTypeGenericLcg, methodBase, genericTypeDefinition));
				}
			}
		}
		m_tokens.Add(method);
		return (m_tokens.Count - 1) | 0x6000000;
	}

	public int GetTokenFor(RuntimeMethodHandle method, RuntimeTypeHandle typeContext)
	{
		m_tokens.Add(new GenericMethodInfo(method, typeContext));
		return (m_tokens.Count - 1) | 0x6000000;
	}

	public int GetTokenFor(DynamicMethod method)
	{
		m_tokens.Add(method);
		return (m_tokens.Count - 1) | 0x6000000;
	}

	public int GetTokenFor(RuntimeFieldHandle field)
	{
		m_tokens.Add(field);
		return (m_tokens.Count - 1) | 0x4000000;
	}

	public int GetTokenFor(RuntimeFieldHandle field, RuntimeTypeHandle typeContext)
	{
		m_tokens.Add(new GenericFieldInfo(field, typeContext));
		return (m_tokens.Count - 1) | 0x4000000;
	}

	public int GetTokenFor(RuntimeTypeHandle type)
	{
		m_tokens.Add(type);
		return (m_tokens.Count - 1) | 0x2000000;
	}

	public int GetTokenFor(string literal)
	{
		m_tokens.Add(literal);
		return (m_tokens.Count - 1) | 0x70000000;
	}

	public int GetTokenFor(byte[] signature)
	{
		m_tokens.Add(signature);
		return (m_tokens.Count - 1) | 0x11000000;
	}
}
