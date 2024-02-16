namespace System.Reflection;

internal abstract class SignatureGenericParameterType : SignatureType
{
	private readonly int _position;

	public sealed override bool IsTypeDefinition => false;

	public sealed override bool IsGenericTypeDefinition => false;

	public sealed override bool IsByRefLike => false;

	public sealed override bool IsSZArray => false;

	public sealed override bool IsVariableBoundArray => false;

	public sealed override bool IsConstructedGenericType => false;

	public sealed override bool IsGenericParameter => true;

	public abstract override bool IsGenericMethodParameter { get; }

	public sealed override bool ContainsGenericParameters => true;

	internal sealed override SignatureType ElementType => null;

	public sealed override Type[] GenericTypeArguments => Type.EmptyTypes;

	public sealed override int GenericParameterPosition => _position;

	public abstract override string Name { get; }

	public sealed override string Namespace => null;

	protected SignatureGenericParameterType(int position)
	{
		_position = position;
	}

	protected sealed override bool HasElementTypeImpl()
	{
		return false;
	}

	protected sealed override bool IsArrayImpl()
	{
		return false;
	}

	protected sealed override bool IsByRefImpl()
	{
		return false;
	}

	protected sealed override bool IsPointerImpl()
	{
		return false;
	}

	public sealed override int GetArrayRank()
	{
		throw new ArgumentException(SR.Argument_HasToBeArrayClass);
	}

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
		return Name;
	}
}
