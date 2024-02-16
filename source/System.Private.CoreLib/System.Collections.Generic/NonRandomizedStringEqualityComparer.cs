using System.Runtime.Serialization;

namespace System.Collections.Generic;

[Serializable]
public class NonRandomizedStringEqualityComparer : IEqualityComparer<string?>, IInternalStringEqualityComparer, ISerializable
{
	private sealed class OrdinalComparer : NonRandomizedStringEqualityComparer
	{
		internal OrdinalComparer(IEqualityComparer<string> wrappedComparer)
			: base(wrappedComparer)
		{
		}

		public override bool Equals(string x, string y)
		{
			return string.Equals(x, y);
		}

		public override int GetHashCode(string obj)
		{
			return obj.GetNonRandomizedHashCode();
		}
	}

	private sealed class OrdinalIgnoreCaseComparer : NonRandomizedStringEqualityComparer
	{
		internal OrdinalIgnoreCaseComparer(IEqualityComparer<string> wrappedComparer)
			: base(wrappedComparer)
		{
		}

		public override bool Equals(string x, string y)
		{
			return string.EqualsOrdinalIgnoreCase(x, y);
		}

		public override int GetHashCode(string obj)
		{
			return obj.GetNonRandomizedHashCodeOrdinalIgnoreCase();
		}

		internal override RandomizedStringEqualityComparer GetRandomizedEqualityComparer()
		{
			return RandomizedStringEqualityComparer.Create(_underlyingComparer, ignoreCase: true);
		}
	}

	private static readonly NonRandomizedStringEqualityComparer WrappedAroundDefaultComparer = new OrdinalComparer(EqualityComparer<string>.Default);

	private static readonly NonRandomizedStringEqualityComparer WrappedAroundStringComparerOrdinal = new OrdinalComparer(StringComparer.Ordinal);

	private static readonly NonRandomizedStringEqualityComparer WrappedAroundStringComparerOrdinalIgnoreCase = new OrdinalIgnoreCaseComparer(StringComparer.OrdinalIgnoreCase);

	private readonly IEqualityComparer<string> _underlyingComparer;

	private NonRandomizedStringEqualityComparer(IEqualityComparer<string> underlyingComparer)
	{
		_underlyingComparer = underlyingComparer;
	}

	protected NonRandomizedStringEqualityComparer(SerializationInfo information, StreamingContext context)
		: this(EqualityComparer<string>.Default)
	{
	}

	public virtual bool Equals(string? x, string? y)
	{
		return string.Equals(x, y);
	}

	public virtual int GetHashCode(string? obj)
	{
		return obj?.GetNonRandomizedHashCode() ?? 0;
	}

	internal virtual RandomizedStringEqualityComparer GetRandomizedEqualityComparer()
	{
		return RandomizedStringEqualityComparer.Create(_underlyingComparer, ignoreCase: false);
	}

	public virtual IEqualityComparer<string?> GetUnderlyingEqualityComparer()
	{
		return _underlyingComparer;
	}

	void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
	{
		info.SetType(typeof(GenericEqualityComparer<string>));
	}

	public static IEqualityComparer<string>? GetStringComparer(object? comparer)
	{
		if (comparer == null)
		{
			return WrappedAroundDefaultComparer;
		}
		if (comparer == StringComparer.Ordinal)
		{
			return WrappedAroundStringComparerOrdinal;
		}
		if (comparer == StringComparer.OrdinalIgnoreCase)
		{
			return WrappedAroundStringComparerOrdinalIgnoreCase;
		}
		return null;
	}
}
