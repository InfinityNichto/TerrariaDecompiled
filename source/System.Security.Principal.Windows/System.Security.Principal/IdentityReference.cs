namespace System.Security.Principal;

public abstract class IdentityReference
{
	public abstract string Value { get; }

	internal IdentityReference()
	{
	}

	public abstract bool IsValidTargetType(Type targetType);

	public abstract IdentityReference Translate(Type targetType);

	public abstract override bool Equals(object? o);

	public abstract override int GetHashCode();

	public abstract override string ToString();

	public static bool operator ==(IdentityReference? left, IdentityReference? right)
	{
		if ((object)left == right)
		{
			return true;
		}
		if ((object)left == null || (object)right == null)
		{
			return false;
		}
		return left.Equals(right);
	}

	public static bool operator !=(IdentityReference? left, IdentityReference? right)
	{
		return !(left == right);
	}
}
