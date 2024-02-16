using Internal.Runtime.CompilerServices;

namespace System.Collections.Generic;

internal abstract class RandomizedStringEqualityComparer : EqualityComparer<string>, IInternalStringEqualityComparer, IEqualityComparer<string>
{
	private struct MarvinSeed
	{
		internal uint p0;

		internal uint p1;
	}

	private sealed class OrdinalComparer : RandomizedStringEqualityComparer
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
			if (obj == null)
			{
				return 0;
			}
			return Marvin.ComputeHash32(ref Unsafe.As<char, byte>(ref obj.GetRawStringData()), (uint)(obj.Length * 2), _seed.p0, _seed.p1);
		}
	}

	private sealed class OrdinalIgnoreCaseComparer : RandomizedStringEqualityComparer
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
			if (obj == null)
			{
				return 0;
			}
			return Marvin.ComputeHash32OrdinalIgnoreCase(ref obj.GetRawStringData(), obj.Length, _seed.p0, _seed.p1);
		}
	}

	private readonly MarvinSeed _seed;

	private readonly IEqualityComparer<string> _underlyingComparer;

	private unsafe RandomizedStringEqualityComparer(IEqualityComparer<string> underlyingComparer)
	{
		_underlyingComparer = underlyingComparer;
		fixed (MarvinSeed* buffer = &_seed)
		{
			Interop.GetRandomBytes((byte*)buffer, sizeof(MarvinSeed));
		}
	}

	internal static RandomizedStringEqualityComparer Create(IEqualityComparer<string> underlyingComparer, bool ignoreCase)
	{
		if (!ignoreCase)
		{
			return new OrdinalComparer(underlyingComparer);
		}
		return new OrdinalIgnoreCaseComparer(underlyingComparer);
	}

	public IEqualityComparer<string> GetUnderlyingEqualityComparer()
	{
		return _underlyingComparer;
	}
}
