namespace ReLogic.Utilities;

public struct SlotId
{
	public static readonly SlotId Invalid = new SlotId(65535u);

	private const uint KEY_INC = 65536u;

	private const uint INDEX_MASK = 65535u;

	private const uint ACTIVE_MASK = 2147483648u;

	private const uint KEY_MASK = 2147418112u;

	public readonly uint Value;

	public bool IsValid => (Value & 0xFFFF) != 65535;

	internal bool IsActive
	{
		get
		{
			if ((Value & 0x80000000u) != 0)
			{
				return IsValid;
			}
			return false;
		}
	}

	internal uint Index => Value & 0xFFFFu;

	internal uint Key => Value & 0x7FFF0000u;

	internal SlotId ToInactive(uint freeHead)
	{
		return new SlotId(Key | freeHead);
	}

	internal SlotId ToActive(uint index)
	{
		uint num = 0x7FFF0000u & (Key + 65536);
		return new SlotId(0x80000000u | num | index);
	}

	public SlotId(uint value)
	{
		Value = value;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is SlotId))
		{
			return false;
		}
		return ((SlotId)obj).Value == Value;
	}

	public override int GetHashCode()
	{
		return Value.GetHashCode();
	}

	public static bool operator ==(SlotId lhs, SlotId rhs)
	{
		return lhs.Value == rhs.Value;
	}

	public static bool operator !=(SlotId lhs, SlotId rhs)
	{
		return lhs.Value != rhs.Value;
	}

	public float ToFloat()
	{
		return ReinterpretCast.UIntAsFloat(Value);
	}

	public static SlotId FromFloat(float value)
	{
		return new SlotId(ReinterpretCast.FloatAsUInt(value));
	}
}
