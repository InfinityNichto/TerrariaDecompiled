namespace System.Reflection;

internal abstract class SignatureHasElementType : SignatureType
{
	private readonly SignatureType _elementType;

	public sealed override bool IsTypeDefinition => false;

	public sealed override bool IsGenericTypeDefinition => false;

	public sealed override bool IsByRefLike => false;

	public abstract override bool IsSZArray { get; }

	public abstract override bool IsVariableBoundArray { get; }

	public sealed override bool IsConstructedGenericType => false;

	public sealed override bool IsGenericParameter => false;

	public sealed override bool IsGenericTypeParameter => false;

	public sealed override bool IsGenericMethodParameter => false;

	public sealed override bool ContainsGenericParameters => _elementType.ContainsGenericParameters;

	internal sealed override SignatureType ElementType => _elementType;

	public sealed override Type[] GenericTypeArguments => Type.EmptyTypes;

	public sealed override int GenericParameterPosition
	{
		get
		{
			throw new InvalidOperationException(SR.Arg_NotGenericParameter);
		}
	}

	public sealed override string Name => _elementType.Name + Suffix;

	public sealed override string Namespace => _elementType.Namespace;

	protected abstract string Suffix { get; }

	protected SignatureHasElementType(SignatureType elementType)
	{
		_elementType = elementType;
	}

	protected sealed override bool HasElementTypeImpl()
	{
		return true;
	}

	protected abstract override bool IsArrayImpl();

	protected abstract override bool IsByRefImpl();

	protected abstract override bool IsPointerImpl();

	public abstract override int GetArrayRank();

	public sealed override Type GetGenericTypeDefinition()
	{
		throw new InvalidOperationException(SR.InvalidOperation_NotGenericType);
	}

	public sealed override Type[] GetGenericArguments()
	{
		return Type.EmptyTypes;
	}

	public sealed override string ToString()
	{
		return _elementType.ToString() + Suffix;
	}
}
