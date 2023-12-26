using System.Runtime.InteropServices;

namespace ReLogic.Utilities;

public static class ReinterpretCast
{
	[StructLayout(LayoutKind.Explicit)]
	private struct IntFloat
	{
		[FieldOffset(0)]
		public readonly int IntValue;

		[FieldOffset(0)]
		public readonly float FloatValue;

		public IntFloat(int value)
		{
			FloatValue = 0f;
			IntValue = value;
		}

		public IntFloat(float value)
		{
			IntValue = 0;
			FloatValue = value;
		}
	}

	[StructLayout(LayoutKind.Explicit)]
	private struct UIntFloat
	{
		[FieldOffset(0)]
		public readonly uint UIntValue;

		[FieldOffset(0)]
		public readonly float FloatValue;

		public UIntFloat(uint value)
		{
			FloatValue = 0f;
			UIntValue = value;
		}

		public UIntFloat(float value)
		{
			UIntValue = 0u;
			FloatValue = value;
		}
	}

	public static float UIntAsFloat(uint value)
	{
		return new UIntFloat(value).FloatValue;
	}

	public static float IntAsFloat(int value)
	{
		return new IntFloat(value).FloatValue;
	}

	public static uint FloatAsUInt(float value)
	{
		return new UIntFloat(value).UIntValue;
	}

	public static int FloatAsInt(float value)
	{
		return new IntFloat(value).IntValue;
	}
}
