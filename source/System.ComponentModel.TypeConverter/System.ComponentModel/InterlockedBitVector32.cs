using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.ComponentModel;

internal struct InterlockedBitVector32
{
	private int _data;

	public bool this[int bit]
	{
		get
		{
			return (Volatile.Read(ref _data) & bit) == bit;
		}
		set
		{
			if (value)
			{
				Interlocked.Or(ref _data, bit);
			}
			else
			{
				Interlocked.And(ref _data, ~bit);
			}
		}
	}

	public void DangerousSet(int bit, bool value)
	{
		_data = (value ? (_data | bit) : (_data & ~bit));
	}

	public static int CreateMask()
	{
		return CreateMask(0);
	}

	public static int CreateMask(int previous)
	{
		if (previous != 0)
		{
			return previous << 1;
		}
		return 1;
	}

	public override bool Equals([NotNullWhen(true)] object o)
	{
		if (o is InterlockedBitVector32 interlockedBitVector)
		{
			return _data == interlockedBitVector._data;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}
}
