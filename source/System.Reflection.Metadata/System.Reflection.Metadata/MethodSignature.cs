using System.Collections.Immutable;

namespace System.Reflection.Metadata;

public readonly struct MethodSignature<TType>
{
	public SignatureHeader Header { get; }

	public TType ReturnType { get; }

	public int RequiredParameterCount { get; }

	public int GenericParameterCount { get; }

	public ImmutableArray<TType> ParameterTypes { get; }

	public MethodSignature(SignatureHeader header, TType returnType, int requiredParameterCount, int genericParameterCount, ImmutableArray<TType> parameterTypes)
	{
		Header = header;
		ReturnType = returnType;
		GenericParameterCount = genericParameterCount;
		RequiredParameterCount = requiredParameterCount;
		ParameterTypes = parameterTypes;
	}
}
