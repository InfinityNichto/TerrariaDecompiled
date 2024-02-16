using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Runtime.CompilerServices;

namespace System;

internal static class Number
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	internal ref struct BigInteger
	{
		private static readonly uint[] s_Pow10UInt32Table = new uint[10] { 1u, 10u, 100u, 1000u, 10000u, 100000u, 1000000u, 10000000u, 100000000u, 1000000000u };

		private static readonly int[] s_Pow10BigNumTableIndices = new int[8] { 0, 2, 5, 10, 18, 33, 61, 116 };

		private static readonly uint[] s_Pow10BigNumTable = new uint[233]
		{
			1u, 100000000u, 2u, 1874919424u, 2328306u, 4u, 0u, 2242703233u, 762134875u, 1262u,
			7u, 0u, 0u, 3211403009u, 1849224548u, 3668416493u, 3913284084u, 1593091u, 14u, 0u,
			0u, 0u, 0u, 781532673u, 64985353u, 253049085u, 594863151u, 3553621484u, 3288652808u, 3167596762u,
			2788392729u, 3911132675u, 590u, 27u, 0u, 0u, 0u, 0u, 0u, 0u,
			0u, 0u, 2553183233u, 3201533787u, 3638140786u, 303378311u, 1809731782u, 3477761648u, 3583367183u, 649228654u,
			2915460784u, 487929380u, 1011012442u, 1677677582u, 3428152256u, 1710878487u, 1438394610u, 2161952759u, 4100910556u, 1608314830u,
			349175u, 54u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u,
			0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 4234999809u, 2012377703u,
			2408924892u, 1570150255u, 3090844311u, 3273530073u, 1187251475u, 2498123591u, 3364452033u, 1148564857u, 687371067u, 2854068671u,
			1883165473u, 505794538u, 2988060450u, 3159489326u, 2531348317u, 3215191468u, 849106862u, 3892080979u, 3288073877u, 2242451748u,
			4183778142u, 2995818208u, 2477501924u, 325481258u, 2487842652u, 1774082830u, 1933815724u, 2962865281u, 1168579910u, 2724829000u,
			2360374019u, 2315984659u, 2360052375u, 3251779801u, 1664357844u, 28u, 107u, 0u, 0u, 0u,
			0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u,
			0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u,
			0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 689565697u,
			4116392818u, 1853628763u, 516071302u, 2568769159u, 365238920u, 336250165u, 1283268122u, 3425490969u, 248595470u, 2305176814u,
			2111925499u, 507770399u, 2681111421u, 589114268u, 591287751u, 1708941527u, 4098957707u, 475844916u, 3378731398u, 2452339615u,
			2817037361u, 2678008327u, 1656645978u, 2383430340u, 73103988u, 448667107u, 2329420453u, 3124020241u, 3625235717u, 3208634035u,
			2412059158u, 2981664444u, 4117622508u, 838560765u, 3069470027u, 270153238u, 1802868219u, 3692709886u, 2161737865u, 2159912357u,
			2585798786u, 837488486u, 4237238160u, 2540319504u, 3798629246u, 3748148874u, 1021550776u, 2386715342u, 1973637538u, 1823520457u,
			1146713475u, 833971519u, 3277251466u, 905620390u, 26278816u, 2680483154u, 2294040859u, 373297482u, 5996609u, 4109575006u,
			512575049u, 917036550u, 1942311753u, 2816916778u, 3248920332u, 1192784020u, 3537586671u, 2456567643u, 2925660628u, 759380297u,
			888447942u, 3559939476u, 3654687237u, 805u, 0u, 0u, 0u, 0u, 0u, 0u,
			0u, 0u, 0u
		};

		private int _length;

		private unsafe fixed uint _blocks[115];

		public unsafe static void Add(ref BigInteger lhs, ref BigInteger rhs, out BigInteger result)
		{
			ref BigInteger reference = ref lhs._length < rhs._length ? ref rhs : ref lhs;
			ref BigInteger reference2 = ref lhs._length < rhs._length ? ref lhs : ref rhs;
			int length = reference._length;
			int length2 = reference2._length;
			result._length = length;
			ulong num = 0uL;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			while (num3 < length2)
			{
				ulong num5 = num + reference._blocks[num2] + reference2._blocks[num3];
				num = num5 >> 32;
				result._blocks[num4] = (uint)num5;
				num2++;
				num3++;
				num4++;
			}
			while (num2 < length)
			{
				ulong num6 = num + reference._blocks[num2];
				num = num6 >> 32;
				result._blocks[num4] = (uint)num6;
				num2++;
				num4++;
			}
			if (num != 0L)
			{
				result._blocks[num4] = 1u;
				result._length++;
			}
		}

		public unsafe static int Compare(ref BigInteger lhs, ref BigInteger rhs)
		{
			int length = lhs._length;
			int length2 = rhs._length;
			int num = length - length2;
			if (num != 0)
			{
				return num;
			}
			if (length == 0)
			{
				return 0;
			}
			for (int num2 = length - 1; num2 >= 0; num2--)
			{
				long num3 = (long)lhs._blocks[num2] - (long)rhs._blocks[num2];
				if (num3 != 0L)
				{
					if (num3 <= 0)
					{
						return -1;
					}
					return 1;
				}
			}
			return 0;
		}

		public static uint CountSignificantBits(uint value)
		{
			return (uint)(32 - BitOperations.LeadingZeroCount(value));
		}

		public static uint CountSignificantBits(ulong value)
		{
			return (uint)(64 - BitOperations.LeadingZeroCount(value));
		}

		public unsafe static uint CountSignificantBits(ref BigInteger value)
		{
			if (value.IsZero())
			{
				return 0u;
			}
			uint num = (uint)(value._length - 1);
			return num * 32 + CountSignificantBits(value._blocks[num]);
		}

		public unsafe static void DivRem(ref BigInteger lhs, ref BigInteger rhs, out BigInteger quo, out BigInteger rem)
		{
			if (lhs.IsZero())
			{
				SetZero(out quo);
				SetZero(out rem);
				return;
			}
			int length = lhs._length;
			int length2 = rhs._length;
			if (length == 1 && length2 == 1)
			{
				var (value, value2) = Math.DivRem(lhs._blocks[0], rhs._blocks[0]);
				SetUInt32(out quo, value);
				SetUInt32(out rem, value2);
				return;
			}
			if (length2 == 1)
			{
				int num = length;
				ulong right = rhs._blocks[0];
				ulong num2 = 0uL;
				for (int num3 = num - 1; num3 >= 0; num3--)
				{
					ulong left = (num2 << 32) | lhs._blocks[num3];
					ulong num4;
					(num4, num2) = Math.DivRem(left, right);
					if (num4 == 0L && num3 == num - 1)
					{
						num--;
					}
					else
					{
						quo._blocks[num3] = (uint)num4;
					}
				}
				quo._length = num;
				SetUInt32(out rem, (uint)num2);
				return;
			}
			if (length2 > length)
			{
				SetZero(out quo);
				SetValue(out rem, ref lhs);
				return;
			}
			int num5 = length - length2 + 1;
			SetValue(out rem, ref lhs);
			int num6 = length;
			uint num7 = rhs._blocks[length2 - 1];
			uint num8 = rhs._blocks[length2 - 2];
			int num9 = BitOperations.LeadingZeroCount(num7);
			int num10 = 32 - num9;
			if (num9 > 0)
			{
				num7 = (num7 << num9) | (num8 >> num10);
				num8 <<= num9;
				if (length2 > 2)
				{
					num8 |= rhs._blocks[length2 - 3] >> num10;
				}
			}
			for (int num11 = length; num11 >= length2; num11--)
			{
				int num12 = num11 - length2;
				uint num13 = ((num11 < length) ? rem._blocks[num11] : 0u);
				ulong num14 = ((ulong)num13 << 32) | rem._blocks[num11 - 1];
				uint num15 = ((num11 > 1) ? rem._blocks[num11 - 2] : 0u);
				if (num9 > 0)
				{
					num14 = (num14 << num9) | (num15 >> num10);
					num15 <<= num9;
					if (num11 > 2)
					{
						num15 |= rem._blocks[num11 - 3] >> num10;
					}
				}
				ulong num16 = num14 / num7;
				if (num16 > uint.MaxValue)
				{
					num16 = 4294967295uL;
				}
				while (DivideGuessTooBig(num16, num14, num15, num7, num8))
				{
					num16--;
				}
				if (num16 != 0)
				{
					uint num17 = SubtractDivisor(ref rem, num12, ref rhs, num16);
					if (num17 != num13)
					{
						num17 = AddDivisor(ref rem, num12, ref rhs);
						num16--;
					}
				}
				if (num5 != 0)
				{
					if (num16 == 0L && num12 == num5 - 1)
					{
						num5--;
					}
					else
					{
						quo._blocks[num12] = (uint)num16;
					}
				}
				if (num11 < num6)
				{
					num6--;
				}
			}
			quo._length = num5;
			for (int num18 = num6 - 1; num18 >= 0; num18--)
			{
				if (rem._blocks[num18] == 0)
				{
					num6--;
				}
			}
			rem._length = num6;
		}

		public unsafe static uint HeuristicDivide(ref BigInteger dividend, ref BigInteger divisor)
		{
			int num = divisor._length;
			if (dividend._length < num)
			{
				return 0u;
			}
			int num2 = num - 1;
			uint num3 = dividend._blocks[num2] / (divisor._blocks[num2] + 1);
			if (num3 != 0)
			{
				int num4 = 0;
				ulong num5 = 0uL;
				ulong num6 = 0uL;
				do
				{
					ulong num7 = (ulong)((long)divisor._blocks[num4] * (long)num3) + num6;
					num6 = num7 >> 32;
					ulong num8 = (ulong)((long)dividend._blocks[num4] - (long)(uint)num7) - num5;
					num5 = (num8 >> 32) & 1;
					dividend._blocks[num4] = (uint)num8;
					num4++;
				}
				while (num4 < num);
				while (num > 0 && dividend._blocks[num - 1] == 0)
				{
					num--;
				}
				dividend._length = num;
			}
			if (Compare(ref dividend, ref divisor) >= 0)
			{
				num3++;
				int num9 = 0;
				ulong num10 = 0uL;
				do
				{
					ulong num11 = (ulong)((long)dividend._blocks[num9] - (long)divisor._blocks[num9]) - num10;
					num10 = (num11 >> 32) & 1;
					dividend._blocks[num9] = (uint)num11;
					num9++;
				}
				while (num9 < num);
				while (num > 0 && dividend._blocks[num - 1] == 0)
				{
					num--;
				}
				dividend._length = num;
			}
			return num3;
		}

		public unsafe static void Multiply(ref BigInteger lhs, uint value, out BigInteger result)
		{
			if (lhs._length <= 1)
			{
				SetUInt64(out result, (ulong)lhs.ToUInt32() * (ulong)value);
				return;
			}
			switch (value)
			{
			case 0u:
				SetZero(out result);
				return;
			case 1u:
				SetValue(out result, ref lhs);
				return;
			}
			int length = lhs._length;
			int i = 0;
			uint num = 0u;
			for (; i < length; i++)
			{
				ulong num2 = (ulong)((long)lhs._blocks[i] * (long)value + num);
				result._blocks[i] = (uint)num2;
				num = (uint)(num2 >> 32);
			}
			if (num != 0)
			{
				result._blocks[i] = num;
				result._length = length + 1;
			}
			else
			{
				result._length = length;
			}
		}

		public unsafe static void Multiply(ref BigInteger lhs, ref BigInteger rhs, out BigInteger result)
		{
			if (lhs._length <= 1)
			{
				Multiply(ref rhs, lhs.ToUInt32(), out result);
				return;
			}
			if (rhs._length <= 1)
			{
				Multiply(ref lhs, rhs.ToUInt32(), out result);
				return;
			}
			ref BigInteger reference = ref lhs;
			int length = lhs._length;
			ref BigInteger reference2 = ref rhs;
			int length2 = rhs._length;
			if (length < length2)
			{
				reference = ref rhs;
				length = rhs._length;
				reference2 = ref lhs;
				length2 = lhs._length;
			}
			int num = (result._length = length2 + length);
			result.Clear((uint)num);
			int num2 = 0;
			int num3 = 0;
			while (num2 < length2)
			{
				if (reference2._blocks[num2] != 0)
				{
					int num4 = 0;
					int num5 = num3;
					ulong num6 = 0uL;
					do
					{
						ulong num7 = (ulong)(result._blocks[num5] + (long)reference2._blocks[num2] * (long)reference._blocks[num4]) + num6;
						num6 = num7 >> 32;
						result._blocks[num5] = (uint)num7;
						num5++;
						num4++;
					}
					while (num4 < length);
					result._blocks[num5] = (uint)num6;
				}
				num2++;
				num3++;
			}
			if (num > 0 && result._blocks[num - 1] == 0)
			{
				result._length--;
			}
		}

		public unsafe static void Pow2(uint exponent, out BigInteger result)
		{
			uint remainder;
			uint num = DivRem32(exponent, out remainder);
			result._length = (int)(num + 1);
			if (num != 0)
			{
				result.Clear(num);
			}
			result._blocks[num] = (uint)(1 << (int)remainder);
		}

		public unsafe static void Pow10(uint exponent, out BigInteger result)
		{
			SetUInt32(out var result2, s_Pow10UInt32Table[exponent & 7]);
			ref BigInteger reference = ref result2;
			SetZero(out var result3);
			ref BigInteger reference2 = ref result3;
			exponent >>= 3;
			uint num = 0u;
			while (exponent != 0)
			{
				if ((exponent & (true ? 1u : 0u)) != 0)
				{
					fixed (uint* ptr = &s_Pow10BigNumTable[s_Pow10BigNumTableIndices[num]])
					{
						Multiply(ref reference, ref *(BigInteger*)ptr, out reference2);
					}
					ref BigInteger reference3 = ref reference2;
					reference2 = ref reference;
					reference = ref reference3;
				}
				num++;
				exponent >>= 1;
			}
			SetValue(out result, ref reference);
		}

		private unsafe static uint AddDivisor(ref BigInteger lhs, int lhsStartIndex, ref BigInteger rhs)
		{
			int length = lhs._length;
			int length2 = rhs._length;
			ulong num = 0uL;
			for (int i = 0; i < length2; i++)
			{
				ref uint reference = ref lhs._blocks[lhsStartIndex + i];
				ulong num2 = reference + num + rhs._blocks[i];
				reference = (uint)num2;
				num = num2 >> 32;
			}
			return (uint)num;
		}

		private static bool DivideGuessTooBig(ulong q, ulong valHi, uint valLo, uint divHi, uint divLo)
		{
			ulong num = divHi * q;
			ulong num2 = divLo * q;
			num += num2 >> 32;
			num2 &= 0xFFFFFFFFu;
			if (num < valHi)
			{
				return false;
			}
			if (num > valHi)
			{
				return true;
			}
			if (num2 < valLo)
			{
				return false;
			}
			if (num2 > valLo)
			{
				return true;
			}
			return false;
		}

		private unsafe static uint SubtractDivisor(ref BigInteger lhs, int lhsStartIndex, ref BigInteger rhs, ulong q)
		{
			int num = lhs._length - lhsStartIndex;
			int length = rhs._length;
			ulong num2 = 0uL;
			for (int i = 0; i < length; i++)
			{
				num2 += rhs._blocks[i] * q;
				uint num3 = (uint)num2;
				num2 >>= 32;
				ref uint reference = ref lhs._blocks[lhsStartIndex + i];
				if (reference < num3)
				{
					num2++;
				}
				reference -= num3;
			}
			return (uint)num2;
		}

		public unsafe void Add(uint value)
		{
			int length = _length;
			if (length == 0)
			{
				SetUInt32(out this, value);
				return;
			}
			_blocks[0] += value;
			if (_blocks[0] >= value)
			{
				return;
			}
			for (int i = 1; i < length; i++)
			{
				ref uint reference = ref _blocks[i];
				reference++;
				if (_blocks[i] != 0)
				{
					return;
				}
			}
			_blocks[length] = 1u;
			_length = length + 1;
		}

		public unsafe uint GetBlock(uint index)
		{
			return _blocks[index];
		}

		public int GetLength()
		{
			return _length;
		}

		public bool IsZero()
		{
			return _length == 0;
		}

		public void Multiply(uint value)
		{
			Multiply(ref this, value, out this);
		}

		public void Multiply(ref BigInteger value)
		{
			if (value._length <= 1)
			{
				Multiply(ref this, value.ToUInt32(), out this);
				return;
			}
			SetValue(out var result, ref this);
			Multiply(ref result, ref value, out this);
		}

		public unsafe void Multiply10()
		{
			if (!IsZero())
			{
				int num = 0;
				int length = _length;
				ulong num2 = 0uL;
				do
				{
					ulong num3 = _blocks[num];
					ulong num4 = (num3 << 3) + (num3 << 1) + num2;
					num2 = num4 >> 32;
					_blocks[num] = (uint)num4;
					num++;
				}
				while (num < length);
				if (num2 != 0L)
				{
					_blocks[num] = (uint)num2;
					_length++;
				}
			}
		}

		public void MultiplyPow10(uint exponent)
		{
			if (exponent <= 9)
			{
				Multiply(s_Pow10UInt32Table[exponent]);
			}
			else if (!IsZero())
			{
				Pow10(exponent, out var result);
				Multiply(ref result);
			}
		}

		public unsafe static void SetUInt32(out BigInteger result, uint value)
		{
			if (value == 0)
			{
				SetZero(out result);
				return;
			}
			result._blocks[0] = value;
			result._length = 1;
		}

		public unsafe static void SetUInt64(out BigInteger result, ulong value)
		{
			if (value <= uint.MaxValue)
			{
				SetUInt32(out result, (uint)value);
				return;
			}
			result._blocks[0] = (uint)value;
			result._blocks[1] = (uint)(value >> 32);
			result._length = 2;
		}

		public unsafe static void SetValue(out BigInteger result, ref BigInteger value)
		{
			Buffer.Memmove(elementCount: (nuint)(result._length = value._length), destination: ref result._blocks[0], source: ref value._blocks[0]);
		}

		public static void SetZero(out BigInteger result)
		{
			result._length = 0;
		}

		public unsafe void ShiftLeft(uint shift)
		{
			int length = _length;
			if (length == 0 || shift == 0)
			{
				return;
			}
			uint remainder;
			uint num = DivRem32(shift, out remainder);
			int num2 = length - 1;
			int num3 = num2 + (int)num;
			if (remainder == 0)
			{
				while (num2 >= 0)
				{
					_blocks[num3] = _blocks[num2];
					num2--;
					num3--;
				}
				_length += (int)num;
				Clear(num);
				return;
			}
			num3++;
			_length = num3 + 1;
			uint num4 = 32 - remainder;
			uint num5 = 0u;
			uint num6 = _blocks[num2];
			uint num7 = num6 >> (int)num4;
			while (num2 > 0)
			{
				_blocks[num3] = num5 | num7;
				num5 = num6 << (int)remainder;
				num2--;
				num3--;
				num6 = _blocks[num2];
				num7 = num6 >> (int)num4;
			}
			_blocks[num3] = num5 | num7;
			_blocks[num3 - 1] = num6 << (int)remainder;
			Clear(num);
			if (_blocks[_length - 1] == 0)
			{
				_length--;
			}
		}

		public unsafe uint ToUInt32()
		{
			if (_length > 0)
			{
				return _blocks[0];
			}
			return 0u;
		}

		public unsafe ulong ToUInt64()
		{
			if (_length > 1)
			{
				return ((ulong)_blocks[1] << 32) + _blocks[0];
			}
			if (_length > 0)
			{
				return _blocks[0];
			}
			return 0uL;
		}

		private unsafe void Clear(uint length)
		{
			Buffer.ZeroMemory((byte*)Unsafe.AsPointer(ref _blocks[0]), length * 4);
		}

		private static uint DivRem32(uint value, out uint remainder)
		{
			remainder = value & 0x1Fu;
			return value >> 5;
		}
	}

	internal readonly ref struct DiyFp
	{
		public readonly ulong f;

		public readonly int e;

		public static DiyFp CreateAndGetBoundaries(double value, out DiyFp mMinus, out DiyFp mPlus)
		{
			DiyFp result = new DiyFp(value);
			result.GetBoundaries(52, out mMinus, out mPlus);
			return result;
		}

		public static DiyFp CreateAndGetBoundaries(float value, out DiyFp mMinus, out DiyFp mPlus)
		{
			DiyFp result = new DiyFp(value);
			result.GetBoundaries(23, out mMinus, out mPlus);
			return result;
		}

		public static DiyFp CreateAndGetBoundaries(Half value, out DiyFp mMinus, out DiyFp mPlus)
		{
			DiyFp result = new DiyFp(value);
			result.GetBoundaries(10, out mMinus, out mPlus);
			return result;
		}

		public DiyFp(double value)
		{
			f = ExtractFractionAndBiasedExponent(value, out e);
		}

		public DiyFp(float value)
		{
			f = ExtractFractionAndBiasedExponent(value, out e);
		}

		public DiyFp(Half value)
		{
			f = ExtractFractionAndBiasedExponent(value, out e);
		}

		public DiyFp(ulong f, int e)
		{
			this.f = f;
			this.e = e;
		}

		public DiyFp Multiply(in DiyFp other)
		{
			uint num = (uint)(f >> 32);
			uint num2 = (uint)f;
			uint num3 = (uint)(other.f >> 32);
			uint num4 = (uint)other.f;
			ulong num5 = (ulong)num * (ulong)num3;
			ulong num6 = (ulong)num2 * (ulong)num3;
			ulong num7 = (ulong)num * (ulong)num4;
			ulong num8 = (ulong)num2 * (ulong)num4;
			ulong num9 = (num8 >> 32) + (uint)num7 + (uint)num6;
			num9 += 2147483648u;
			return new DiyFp(num5 + (num7 >> 32) + (num6 >> 32) + (num9 >> 32), e + other.e + 64);
		}

		public DiyFp Normalize()
		{
			int num = BitOperations.LeadingZeroCount(f);
			return new DiyFp(f << num, e - num);
		}

		public DiyFp Subtract(in DiyFp other)
		{
			return new DiyFp(f - other.f, e);
		}

		private void GetBoundaries(int implicitBitIndex, out DiyFp mMinus, out DiyFp mPlus)
		{
			mPlus = new DiyFp((f << 1) + 1, e - 1).Normalize();
			if (f == (ulong)(1L << implicitBitIndex))
			{
				mMinus = new DiyFp((f << 2) - 1, e - 2);
			}
			else
			{
				mMinus = new DiyFp((f << 1) - 1, e - 1);
			}
			mMinus = new DiyFp(mMinus.f << mMinus.e - mPlus.e, mPlus.e);
		}
	}

	internal static class Grisu3
	{
		private static readonly short[] s_CachedPowersBinaryExponent = new short[87]
		{
			-1220, -1193, -1166, -1140, -1113, -1087, -1060, -1034, -1007, -980,
			-954, -927, -901, -874, -847, -821, -794, -768, -741, -715,
			-688, -661, -635, -608, -582, -555, -529, -502, -475, -449,
			-422, -396, -369, -343, -316, -289, -263, -236, -210, -183,
			-157, -130, -103, -77, -50, -24, 3, 30, 56, 83,
			109, 136, 162, 189, 216, 242, 269, 295, 322, 348,
			375, 402, 428, 455, 481, 508, 534, 561, 588, 614,
			641, 667, 694, 720, 747, 774, 800, 827, 853, 880,
			907, 933, 960, 986, 1013, 1039, 1066
		};

		private static readonly short[] s_CachedPowersDecimalExponent = new short[87]
		{
			-348, -340, -332, -324, -316, -308, -300, -292, -284, -276,
			-268, -260, -252, -244, -236, -228, -220, -212, -204, -196,
			-188, -180, -172, -164, -156, -148, -140, -132, -124, -116,
			-108, -100, -92, -84, -76, -68, -60, -52, -44, -36,
			-28, -20, -12, -4, 4, 12, 20, 28, 36, 44,
			52, 60, 68, 76, 84, 92, 100, 108, 116, 124,
			132, 140, 148, 156, 164, 172, 180, 188, 196, 204,
			212, 220, 228, 236, 244, 252, 260, 268, 276, 284,
			292, 300, 308, 316, 324, 332, 340
		};

		private static readonly ulong[] s_CachedPowersSignificand = new ulong[87]
		{
			18054884314459144840uL, 13451937075301367670uL, 10022474136428063862uL, 14934650266808366570uL, 11127181549972568877uL, 16580792590934885855uL, 12353653155963782858uL, 18408377700990114895uL, 13715310171984221708uL, 10218702384817765436uL,
			15227053142812498563uL, 11345038669416679861uL, 16905424996341287883uL, 12595523146049147757uL, 9384396036005875287uL, 13983839803942852151uL, 10418772551374772303uL, 15525180923007089351uL, 11567161174868858868uL, 17236413322193710309uL,
			12842128665889583758uL, 9568131466127621947uL, 14257626930069360058uL, 10622759856335341974uL, 15829145694278690180uL, 11793632577567316726uL, 17573882009934360870uL, 13093562431584567480uL, 9755464219737475723uL, 14536774485912137811uL,
			10830740992659433045uL, 16139061738043178685uL, 12024538023802026127uL, 17917957937422433684uL, 13349918974505688015uL, 9946464728195732843uL, 14821387422376473014uL, 11042794154864902060uL, 16455045573212060422uL, 12259964326927110867uL,
			18268770466636286478uL, 13611294676837538539uL, 10141204801825835212uL, 15111572745182864684uL, 11258999068426240000uL, 16777216000000000000uL, 12500000000000000000uL, 9313225746154785156uL, 13877787807814456755uL, 10339757656912845936uL,
			15407439555097886824uL, 11479437019748901445uL, 17105694144590052135uL, 12744735289059618216uL, 9495567745759798747uL, 14149498560666738074uL, 10542197943230523224uL, 15709099088952724970uL, 11704190886730495818uL, 17440603504673385349uL,
			12994262207056124023uL, 9681479787123295682uL, 14426529090290212157uL, 10748601772107342003uL, 16016664761464807395uL, 11933345169920330789uL, 17782069995880619868uL, 13248674568444952270uL, 9871031767461413346uL, 14708983551653345445uL,
			10959046745042015199uL, 16330252207878254650uL, 12166986024289022870uL, 18130221999122236476uL, 13508068024458167312uL, 10064294952495520794uL, 14996968138956309548uL, 11173611982879273257uL, 16649979327439178909uL, 12405201291620119593uL,
			9242595204427927429uL, 13772540099066387757uL, 10261342003245940623uL, 15290591125556738113uL, 11392378155556871081uL, 16975966327722178521uL, 12648080533535911531uL
		};

		private static readonly uint[] s_SmallPowersOfTen = new uint[10] { 1u, 10u, 100u, 1000u, 10000u, 100000u, 1000000u, 10000000u, 100000000u, 1000000000u };

		public static bool TryRunDouble(double value, int requestedDigits, ref NumberBuffer number)
		{
			double value2 = (double.IsNegative(value) ? (0.0 - value) : value);
			DiyFp diyFp;
			bool flag;
			int length;
			int decimalExponent;
			if (requestedDigits == -1)
			{
				diyFp = DiyFp.CreateAndGetBoundaries(value2, out var mMinus, out var mPlus);
				DiyFp w = diyFp.Normalize();
				flag = TryRunShortest(in mMinus, in w, in mPlus, number.Digits, out length, out decimalExponent);
			}
			else
			{
				diyFp = new DiyFp(value2);
				DiyFp w2 = diyFp.Normalize();
				flag = TryRunCounted(in w2, requestedDigits, number.Digits, out length, out decimalExponent);
			}
			if (flag)
			{
				number.Scale = length + decimalExponent;
				number.Digits[length] = 0;
				number.DigitsCount = length;
			}
			return flag;
		}

		public static bool TryRunHalf(Half value, int requestedDigits, ref NumberBuffer number)
		{
			Half value2 = (Half.IsNegative(value) ? Half.Negate(value) : value);
			DiyFp diyFp;
			bool flag;
			int length;
			int decimalExponent;
			if (requestedDigits == -1)
			{
				diyFp = DiyFp.CreateAndGetBoundaries(value2, out var mMinus, out var mPlus);
				DiyFp w = diyFp.Normalize();
				flag = TryRunShortest(in mMinus, in w, in mPlus, number.Digits, out length, out decimalExponent);
			}
			else
			{
				diyFp = new DiyFp(value2);
				DiyFp w2 = diyFp.Normalize();
				flag = TryRunCounted(in w2, requestedDigits, number.Digits, out length, out decimalExponent);
			}
			if (flag)
			{
				number.Scale = length + decimalExponent;
				number.Digits[length] = 0;
				number.DigitsCount = length;
			}
			return flag;
		}

		public static bool TryRunSingle(float value, int requestedDigits, ref NumberBuffer number)
		{
			float value2 = (float.IsNegative(value) ? (0f - value) : value);
			DiyFp diyFp;
			bool flag;
			int length;
			int decimalExponent;
			if (requestedDigits == -1)
			{
				diyFp = DiyFp.CreateAndGetBoundaries(value2, out var mMinus, out var mPlus);
				DiyFp w = diyFp.Normalize();
				flag = TryRunShortest(in mMinus, in w, in mPlus, number.Digits, out length, out decimalExponent);
			}
			else
			{
				diyFp = new DiyFp(value2);
				DiyFp w2 = diyFp.Normalize();
				flag = TryRunCounted(in w2, requestedDigits, number.Digits, out length, out decimalExponent);
			}
			if (flag)
			{
				number.Scale = length + decimalExponent;
				number.Digits[length] = 0;
				number.DigitsCount = length;
			}
			return flag;
		}

		private static bool TryRunCounted(in DiyFp w, int requestedDigits, Span<byte> buffer, out int length, out int decimalExponent)
		{
			int minExponent = -60 - (w.e + 64);
			int maxExponent = -32 - (w.e + 64);
			int decimalExponent2;
			DiyFp other = GetCachedPowerForBinaryExponentRange(minExponent, maxExponent, out decimalExponent2);
			DiyFp w2 = w.Multiply(in other);
			int kappa;
			bool result = TryDigitGenCounted(in w2, requestedDigits, buffer, out length, out kappa);
			decimalExponent = -decimalExponent2 + kappa;
			return result;
		}

		private static bool TryRunShortest(in DiyFp boundaryMinus, in DiyFp w, in DiyFp boundaryPlus, Span<byte> buffer, out int length, out int decimalExponent)
		{
			int minExponent = -60 - (w.e + 64);
			int maxExponent = -32 - (w.e + 64);
			int decimalExponent2;
			DiyFp other = GetCachedPowerForBinaryExponentRange(minExponent, maxExponent, out decimalExponent2);
			DiyFp w2 = w.Multiply(in other);
			DiyFp low = boundaryMinus.Multiply(in other);
			DiyFp high = boundaryPlus.Multiply(in other);
			int kappa;
			bool result = TryDigitGenShortest(in low, in w2, in high, buffer, out length, out kappa);
			decimalExponent = -decimalExponent2 + kappa;
			return result;
		}

		private static uint BiggestPowerTen(uint number, int numberBits, out int exponentPlusOne)
		{
			int num = (numberBits + 1) * 1233 >> 12;
			uint num2 = s_SmallPowersOfTen[num];
			if (number < num2)
			{
				num--;
				num2 = s_SmallPowersOfTen[num];
			}
			exponentPlusOne = num + 1;
			return num2;
		}

		private static bool TryDigitGenCounted(in DiyFp w, int requestedDigits, Span<byte> buffer, out int length, out int kappa)
		{
			ulong num = 1uL;
			DiyFp diyFp = new DiyFp((ulong)(1L << -w.e), w.e);
			uint num2 = (uint)(w.f >> -diyFp.e);
			ulong num3 = w.f & (diyFp.f - 1);
			if (num3 == 0L && (requestedDigits >= 11 || num2 < s_SmallPowersOfTen[requestedDigits - 1]))
			{
				length = 0;
				kappa = 0;
				return false;
			}
			uint num4 = BiggestPowerTen(num2, 64 - -diyFp.e, out kappa);
			length = 0;
			while (kappa > 0)
			{
				uint num5;
				(num5, num2) = Math.DivRem(num2, num4);
				buffer[length] = (byte)(48 + num5);
				length++;
				requestedDigits--;
				kappa--;
				if (requestedDigits == 0)
				{
					break;
				}
				num4 /= 10;
			}
			if (requestedDigits == 0)
			{
				ulong rest = ((ulong)num2 << -diyFp.e) + num3;
				return TryRoundWeedCounted(buffer, length, rest, (ulong)num4 << -diyFp.e, num, ref kappa);
			}
			while (requestedDigits > 0 && num3 > num)
			{
				num3 *= 10;
				num *= 10;
				uint num6 = (uint)(num3 >> -diyFp.e);
				buffer[length] = (byte)(48 + num6);
				length++;
				requestedDigits--;
				kappa--;
				num3 &= diyFp.f - 1;
			}
			if (requestedDigits != 0)
			{
				buffer[0] = 0;
				length = 0;
				kappa = 0;
				return false;
			}
			return TryRoundWeedCounted(buffer, length, num3, diyFp.f, num, ref kappa);
		}

		private static bool TryDigitGenShortest(in DiyFp low, in DiyFp w, in DiyFp high, Span<byte> buffer, out int length, out int kappa)
		{
			ulong num = 1uL;
			DiyFp other = new DiyFp(low.f - num, low.e);
			DiyFp diyFp = new DiyFp(high.f + num, high.e);
			DiyFp diyFp2 = diyFp.Subtract(in other);
			DiyFp diyFp3 = new DiyFp((ulong)(1L << -w.e), w.e);
			uint num2 = (uint)(diyFp.f >> -diyFp3.e);
			ulong num3 = diyFp.f & (diyFp3.f - 1);
			uint num4 = BiggestPowerTen(num2, 64 - -diyFp3.e, out kappa);
			length = 0;
			while (kappa > 0)
			{
				uint num5;
				(num5, num2) = Math.DivRem(num2, num4);
				buffer[length] = (byte)(48 + num5);
				length++;
				kappa--;
				ulong num6 = ((ulong)num2 << -diyFp3.e) + num3;
				if (num6 < diyFp2.f)
				{
					return TryRoundWeedShortest(buffer, length, diyFp.Subtract(in w).f, diyFp2.f, num6, (ulong)num4 << -diyFp3.e, num);
				}
				num4 /= 10;
			}
			do
			{
				num3 *= 10;
				num *= 10;
				diyFp2 = new DiyFp(diyFp2.f * 10, diyFp2.e);
				uint num7 = (uint)(num3 >> -diyFp3.e);
				buffer[length] = (byte)(48 + num7);
				length++;
				kappa--;
				num3 &= diyFp3.f - 1;
			}
			while (num3 >= diyFp2.f);
			return TryRoundWeedShortest(buffer, length, diyFp.Subtract(in w).f * num, diyFp2.f, num3, diyFp3.f, num);
		}

		private static DiyFp GetCachedPowerForBinaryExponentRange(int minExponent, int maxExponent, out int decimalExponent)
		{
			double num = Math.Ceiling((double)(minExponent + 64 - 1) * 0.3010299956639812);
			int num2 = (348 + (int)num - 1) / 8 + 1;
			decimalExponent = s_CachedPowersDecimalExponent[num2];
			return new DiyFp(s_CachedPowersSignificand[num2], s_CachedPowersBinaryExponent[num2]);
		}

		private static bool TryRoundWeedCounted(Span<byte> buffer, int length, ulong rest, ulong tenKappa, ulong unit, ref int kappa)
		{
			if (unit >= tenKappa || tenKappa - unit <= unit)
			{
				return false;
			}
			if (tenKappa - rest > rest && tenKappa - 2 * rest >= 2 * unit)
			{
				return true;
			}
			if (rest > unit && (tenKappa <= rest - unit || tenKappa - (rest - unit) <= rest - unit))
			{
				buffer[length - 1]++;
				int num = length - 1;
				while (num > 0 && buffer[num] == 58)
				{
					buffer[num] = 48;
					buffer[num - 1]++;
					num--;
				}
				if (buffer[0] == 58)
				{
					buffer[0] = 49;
					kappa++;
				}
				return true;
			}
			return false;
		}

		private static bool TryRoundWeedShortest(Span<byte> buffer, int length, ulong distanceTooHighW, ulong unsafeInterval, ulong rest, ulong tenKappa, ulong unit)
		{
			ulong num = distanceTooHighW - unit;
			ulong num2 = distanceTooHighW + unit;
			while (rest < num && unsafeInterval - rest >= tenKappa && (rest + tenKappa < num || num - rest >= rest + tenKappa - num))
			{
				buffer[length - 1]--;
				rest += tenKappa;
			}
			if (rest < num2 && unsafeInterval - rest >= tenKappa && (rest + tenKappa < num2 || num2 - rest > rest + tenKappa - num2))
			{
				return false;
			}
			if (2 * unit <= rest)
			{
				return rest <= unsafeInterval - 4 * unit;
			}
			return false;
		}
	}

	internal ref struct NumberBuffer
	{
		public int DigitsCount;

		public int Scale;

		public bool IsNegative;

		public bool HasNonZeroTail;

		public NumberBufferKind Kind;

		public Span<byte> Digits;

		public unsafe NumberBuffer(NumberBufferKind kind, byte* digits, int digitsLength)
		{
			DigitsCount = 0;
			Scale = 0;
			IsNegative = false;
			HasNonZeroTail = false;
			Kind = kind;
			Digits = new Span<byte>(digits, digitsLength);
			Digits[0] = 0;
		}

		public unsafe byte* GetDigitsPointer()
		{
			return (byte*)Unsafe.AsPointer(ref Digits[0]);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append('[');
			stringBuilder.Append('"');
			for (int i = 0; i < Digits.Length; i++)
			{
				byte b = Digits[i];
				if (b == 0)
				{
					break;
				}
				stringBuilder.Append((char)b);
			}
			stringBuilder.Append('"');
			stringBuilder.Append(", Length = ").Append(DigitsCount);
			stringBuilder.Append(", Scale = ").Append(Scale);
			stringBuilder.Append(", IsNegative = ").Append(IsNegative);
			stringBuilder.Append(", HasNonZeroTail = ").Append(HasNonZeroTail);
			stringBuilder.Append(", Kind = ").Append(Kind);
			stringBuilder.Append(']');
			return stringBuilder.ToString();
		}
	}

	internal enum NumberBufferKind : byte
	{
		Unknown,
		Integer,
		Decimal,
		FloatingPoint
	}

	public readonly struct FloatingPointInfo
	{
		public static readonly FloatingPointInfo Double = new FloatingPointInfo(52, 11, 1023, 1023, 9218868437227405312uL);

		public static readonly FloatingPointInfo Single = new FloatingPointInfo(23, 8, 127, 127, 2139095040uL);

		public static readonly FloatingPointInfo Half = new FloatingPointInfo(10, 5, 15, 15, 31744uL);

		[CompilerGenerated]
		private readonly ushort _003CExponentBits_003Ek__BackingField;

		public ulong ZeroBits { get; }

		public ulong InfinityBits { get; }

		public ulong NormalMantissaMask { get; }

		public ulong DenormalMantissaMask { get; }

		public int MinBinaryExponent { get; }

		public int MaxBinaryExponent { get; }

		public int ExponentBias { get; }

		public int OverflowDecimalExponent { get; }

		public ushort NormalMantissaBits { get; }

		public ushort DenormalMantissaBits { get; }

		public FloatingPointInfo(ushort denormalMantissaBits, ushort exponentBits, int maxBinaryExponent, int exponentBias, ulong infinityBits)
		{
			_003CExponentBits_003Ek__BackingField = exponentBits;
			DenormalMantissaBits = denormalMantissaBits;
			NormalMantissaBits = (ushort)(denormalMantissaBits + 1);
			OverflowDecimalExponent = (maxBinaryExponent + 2 * NormalMantissaBits) / 3;
			ExponentBias = exponentBias;
			MaxBinaryExponent = maxBinaryExponent;
			MinBinaryExponent = 1 - maxBinaryExponent;
			DenormalMantissaMask = (ulong)((1L << (int)denormalMantissaBits) - 1);
			NormalMantissaMask = (ulong)((1L << (int)NormalMantissaBits) - 1);
			InfinityBits = infinityBits;
			ZeroBits = 0uL;
		}
	}

	internal enum ParsingStatus
	{
		OK,
		Failed,
		Overflow
	}

	private static readonly string[] s_singleDigitStringCache = new string[10] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

	private static readonly string[] s_posCurrencyFormats = new string[4] { "$#", "#$", "$ #", "# $" };

	private static readonly string[] s_negCurrencyFormats = new string[17]
	{
		"($#)", "-$#", "$-#", "$#-", "(#$)", "-#$", "#-$", "#$-", "-# $", "-$ #",
		"# $-", "$ #-", "$ -#", "#- $", "($ #)", "(# $)", "$- #"
	};

	private static readonly string[] s_posPercentFormats = new string[4] { "# %", "#%", "%#", "% #" };

	private static readonly string[] s_negPercentFormats = new string[12]
	{
		"-# %", "-#%", "-%#", "%-#", "%#-", "#-%", "#%-", "-% #", "# %-", "% #-",
		"% -#", "#- %"
	};

	private static readonly string[] s_negNumberFormats = new string[5] { "(#)", "-#", "- #", "#-", "# -" };

	private static readonly float[] s_Pow10SingleTable = new float[11]
	{
		1f, 10f, 100f, 1000f, 10000f, 100000f, 1000000f, 10000000f, 100000000f, 1E+09f,
		1E+10f
	};

	private static readonly double[] s_Pow10DoubleTable = new double[23]
	{
		1.0, 10.0, 100.0, 1000.0, 10000.0, 100000.0, 1000000.0, 10000000.0, 100000000.0, 1000000000.0,
		10000000000.0, 100000000000.0, 1000000000000.0, 10000000000000.0, 100000000000000.0, 1000000000000000.0, 10000000000000000.0, 1E+17, 1E+18, 1E+19,
		1E+20, 1E+21, 1E+22
	};

	public static void Dragon4Double(double value, int cutoffNumber, bool isSignificantDigits, ref NumberBuffer number)
	{
		double num = (double.IsNegative(value) ? (0.0 - value) : value);
		int exponent;
		ulong num2 = ExtractFractionAndBiasedExponent(value, out exponent);
		bool hasUnequalMargins = false;
		uint mantissaHighBitIdx;
		if (num2 >> 52 != 0L)
		{
			mantissaHighBitIdx = 52u;
			hasUnequalMargins = num2 == 4503599627370496L;
		}
		else
		{
			mantissaHighBitIdx = (uint)BitOperations.Log2(num2);
		}
		int decimalExponent;
		int num3 = (int)Dragon4(num2, exponent, mantissaHighBitIdx, hasUnequalMargins, cutoffNumber, isSignificantDigits, number.Digits, out decimalExponent);
		number.Scale = decimalExponent + 1;
		number.Digits[num3] = 0;
		number.DigitsCount = num3;
	}

	public static void Dragon4Half(Half value, int cutoffNumber, bool isSignificantDigits, ref NumberBuffer number)
	{
		Half half = (Half.IsNegative(value) ? Half.Negate(value) : value);
		int exponent;
		ushort num = ExtractFractionAndBiasedExponent(value, out exponent);
		bool hasUnequalMargins = false;
		uint mantissaHighBitIdx;
		if (num >> 10 != 0)
		{
			mantissaHighBitIdx = 10u;
			hasUnequalMargins = num == 1024;
		}
		else
		{
			mantissaHighBitIdx = (uint)BitOperations.Log2(num);
		}
		int decimalExponent;
		int num2 = (int)Dragon4(num, exponent, mantissaHighBitIdx, hasUnequalMargins, cutoffNumber, isSignificantDigits, number.Digits, out decimalExponent);
		number.Scale = decimalExponent + 1;
		number.Digits[num2] = 0;
		number.DigitsCount = num2;
	}

	public static void Dragon4Single(float value, int cutoffNumber, bool isSignificantDigits, ref NumberBuffer number)
	{
		float num = (float.IsNegative(value) ? (0f - value) : value);
		int exponent;
		uint num2 = ExtractFractionAndBiasedExponent(value, out exponent);
		bool hasUnequalMargins = false;
		uint mantissaHighBitIdx;
		if (num2 >> 23 != 0)
		{
			mantissaHighBitIdx = 23u;
			hasUnequalMargins = num2 == 8388608;
		}
		else
		{
			mantissaHighBitIdx = (uint)BitOperations.Log2(num2);
		}
		int decimalExponent;
		int num3 = (int)Dragon4(num2, exponent, mantissaHighBitIdx, hasUnequalMargins, cutoffNumber, isSignificantDigits, number.Digits, out decimalExponent);
		number.Scale = decimalExponent + 1;
		number.Digits[num3] = 0;
		number.DigitsCount = num3;
	}

	private unsafe static uint Dragon4(ulong mantissa, int exponent, uint mantissaHighBitIdx, bool hasUnequalMargins, int cutoffNumber, bool isSignificantDigits, Span<byte> buffer, out int decimalExponent)
	{
		int num = 0;
		BigInteger lhs;
		BigInteger rhs;
		BigInteger result;
		BigInteger* ptr;
		if (hasUnequalMargins)
		{
			BigInteger result2;
			if (exponent > 0)
			{
				BigInteger.SetUInt64(out lhs, 4 * mantissa);
				lhs.ShiftLeft((uint)exponent);
				BigInteger.SetUInt32(out rhs, 4u);
				BigInteger.Pow2((uint)exponent, out result);
				BigInteger.Pow2((uint)(exponent + 1), out result2);
			}
			else
			{
				BigInteger.SetUInt64(out lhs, 4 * mantissa);
				BigInteger.Pow2((uint)(-exponent + 2), out rhs);
				BigInteger.SetUInt32(out result, 1u);
				BigInteger.SetUInt32(out result2, 2u);
			}
			ptr = &result2;
		}
		else
		{
			if (exponent > 0)
			{
				BigInteger.SetUInt64(out lhs, 2 * mantissa);
				lhs.ShiftLeft((uint)exponent);
				BigInteger.SetUInt32(out rhs, 2u);
				BigInteger.Pow2((uint)exponent, out result);
			}
			else
			{
				BigInteger.SetUInt64(out lhs, 2 * mantissa);
				BigInteger.Pow2((uint)(-exponent + 1), out rhs);
				BigInteger.SetUInt32(out result, 1u);
			}
			ptr = &result;
		}
		int num2 = (int)Math.Ceiling((double)((int)mantissaHighBitIdx + exponent) * 0.3010299956639812 - 0.69);
		if (num2 > 0)
		{
			rhs.MultiplyPow10((uint)num2);
		}
		else if (num2 < 0)
		{
			BigInteger.Pow10((uint)(-num2), out var result3);
			lhs.Multiply(ref result3);
			result.Multiply(ref result3);
			if (ptr != &result)
			{
				BigInteger.Multiply(ref result, 2u, out *ptr);
			}
		}
		bool flag = mantissa % 2 == 0;
		bool flag2 = false;
		if (cutoffNumber == -1)
		{
			BigInteger.Add(ref lhs, ref *ptr, out var result4);
			int num3 = BigInteger.Compare(ref result4, ref rhs);
			flag2 = (flag ? (num3 >= 0) : (num3 > 0));
		}
		else
		{
			flag2 = BigInteger.Compare(ref lhs, ref rhs) >= 0;
		}
		if (flag2)
		{
			num2++;
		}
		else
		{
			lhs.Multiply10();
			result.Multiply10();
			if (ptr != &result)
			{
				BigInteger.Multiply(ref result, 2u, out *ptr);
			}
		}
		int num4 = num2 - buffer.Length;
		if (cutoffNumber != -1)
		{
			int num5 = 0;
			num5 = ((!isSignificantDigits) ? (-cutoffNumber) : (num2 - cutoffNumber));
			if (num5 > num4)
			{
				num4 = num5;
			}
		}
		num2 = (decimalExponent = num2 - 1);
		uint block = rhs.GetBlock((uint)(rhs.GetLength() - 1));
		if (block < 8 || block > 429496729)
		{
			uint num6 = (uint)BitOperations.Log2(block);
			uint shift = (59 - num6) % 32;
			rhs.ShiftLeft(shift);
			lhs.ShiftLeft(shift);
			result.ShiftLeft(shift);
			if (ptr != &result)
			{
				BigInteger.Multiply(ref result, 2u, out *ptr);
			}
		}
		bool flag3;
		bool flag4;
		uint num7;
		if (cutoffNumber == -1)
		{
			while (true)
			{
				num7 = BigInteger.HeuristicDivide(ref lhs, ref rhs);
				BigInteger.Add(ref lhs, ref *ptr, out var result5);
				int num8 = BigInteger.Compare(ref lhs, ref result);
				int num9 = BigInteger.Compare(ref result5, ref rhs);
				if (flag)
				{
					flag3 = num8 <= 0;
					flag4 = num9 >= 0;
				}
				else
				{
					flag3 = num8 < 0;
					flag4 = num9 > 0;
				}
				if (flag3 || flag4 || num2 == num4)
				{
					break;
				}
				buffer[num] = (byte)(48 + num7);
				num++;
				lhs.Multiply10();
				result.Multiply10();
				if (ptr != &result)
				{
					BigInteger.Multiply(ref result, 2u, out *ptr);
				}
				num2--;
			}
		}
		else
		{
			if (num2 < num4)
			{
				num7 = BigInteger.HeuristicDivide(ref lhs, ref rhs);
				if (num7 > 5 || (num7 == 5 && !lhs.IsZero()))
				{
					decimalExponent++;
					num7 = 1u;
				}
				buffer[num] = (byte)(48 + num7);
				return (uint)(num + 1);
			}
			flag3 = false;
			flag4 = false;
			while (true)
			{
				num7 = BigInteger.HeuristicDivide(ref lhs, ref rhs);
				if (lhs.IsZero() || num2 <= num4)
				{
					break;
				}
				buffer[num] = (byte)(48 + num7);
				num++;
				lhs.Multiply10();
				num2--;
			}
		}
		bool flag5 = flag3;
		if (flag3 == flag4)
		{
			lhs.ShiftLeft(1u);
			int num10 = BigInteger.Compare(ref lhs, ref rhs);
			flag5 = num10 < 0;
			if (num10 == 0)
			{
				flag5 = (num7 & 1) == 0;
			}
		}
		if (flag5)
		{
			buffer[num] = (byte)(48 + num7);
			num++;
		}
		else if (num7 == 9)
		{
			while (true)
			{
				if (num == 0)
				{
					buffer[num] = 49;
					num++;
					decimalExponent++;
					break;
				}
				num--;
				if (buffer[num] != 57)
				{
					buffer[num]++;
					num++;
					break;
				}
			}
		}
		else
		{
			buffer[num] = (byte)(48 + num7 + 1);
			num++;
		}
		return (uint)num;
	}

	public unsafe static string FormatDecimal(decimal value, ReadOnlySpan<char> format, NumberFormatInfo info)
	{
		int digits;
		char c = ParseFormatSpecifier(format, out digits);
		byte* digits2 = stackalloc byte[31];
		NumberBuffer number = new NumberBuffer(NumberBufferKind.Decimal, digits2, 31);
		DecimalToNumber(ref value, ref number);
		char* pointer = stackalloc char[32];
		ValueStringBuilder sb = new ValueStringBuilder(new Span<char>(pointer, 32));
		if (c != 0)
		{
			NumberToString(ref sb, ref number, c, digits, info);
		}
		else
		{
			NumberToStringFormat(ref sb, ref number, format, info);
		}
		return sb.ToString();
	}

	public unsafe static bool TryFormatDecimal(decimal value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<char> destination, out int charsWritten)
	{
		int digits;
		char c = ParseFormatSpecifier(format, out digits);
		byte* digits2 = stackalloc byte[31];
		NumberBuffer number = new NumberBuffer(NumberBufferKind.Decimal, digits2, 31);
		DecimalToNumber(ref value, ref number);
		char* pointer = stackalloc char[32];
		ValueStringBuilder sb = new ValueStringBuilder(new Span<char>(pointer, 32));
		if (c != 0)
		{
			NumberToString(ref sb, ref number, c, digits, info);
		}
		else
		{
			NumberToStringFormat(ref sb, ref number, format, info);
		}
		return sb.TryCopyTo(destination, out charsWritten);
	}

	internal unsafe static void DecimalToNumber(ref decimal d, ref NumberBuffer number)
	{
		byte* digitsPointer = number.GetDigitsPointer();
		number.DigitsCount = 29;
		number.IsNegative = d.IsNegative;
		byte* bufferEnd = digitsPointer + 29;
		while ((d.Mid | d.High) != 0)
		{
			bufferEnd = UInt32ToDecChars(bufferEnd, decimal.DecDivMod1E9(ref d), 9);
		}
		bufferEnd = UInt32ToDecChars(bufferEnd, d.Low, 0);
		int num = (number.DigitsCount = (int)(digitsPointer + 29 - bufferEnd));
		number.Scale = num - d.Scale;
		byte* digitsPointer2 = number.GetDigitsPointer();
		while (--num >= 0)
		{
			*(digitsPointer2++) = *(bufferEnd++);
		}
		*digitsPointer2 = 0;
	}

	public static string FormatDouble(double value, string format, NumberFormatInfo info)
	{
		Span<char> initialBuffer = stackalloc char[32];
		ValueStringBuilder sb = new ValueStringBuilder(initialBuffer);
		return FormatDouble(ref sb, value, format, info) ?? sb.ToString();
	}

	public static bool TryFormatDouble(double value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<char> destination, out int charsWritten)
	{
		Span<char> initialBuffer = stackalloc char[32];
		ValueStringBuilder sb = new ValueStringBuilder(initialBuffer);
		string text = FormatDouble(ref sb, value, format, info);
		if (text == null)
		{
			return sb.TryCopyTo(destination, out charsWritten);
		}
		return TryCopyTo(text, destination, out charsWritten);
	}

	private static int GetFloatingPointMaxDigitsAndPrecision(char fmt, ref int precision, NumberFormatInfo info, out bool isSignificantDigits)
	{
		if (fmt == '\0')
		{
			isSignificantDigits = true;
			return precision;
		}
		int result = precision;
		switch (fmt)
		{
		case 'C':
		case 'c':
			if (precision == -1)
			{
				precision = info.CurrencyDecimalDigits;
			}
			isSignificantDigits = false;
			break;
		case 'E':
		case 'e':
			if (precision == -1)
			{
				precision = 6;
			}
			precision++;
			isSignificantDigits = true;
			break;
		case 'F':
		case 'N':
		case 'f':
		case 'n':
			if (precision == -1)
			{
				precision = info.NumberDecimalDigits;
			}
			isSignificantDigits = false;
			break;
		case 'G':
		case 'g':
			if (precision == 0)
			{
				precision = -1;
			}
			isSignificantDigits = true;
			break;
		case 'P':
		case 'p':
			if (precision == -1)
			{
				precision = info.PercentDecimalDigits;
			}
			precision += 2;
			isSignificantDigits = false;
			break;
		case 'R':
		case 'r':
			precision = -1;
			isSignificantDigits = true;
			break;
		default:
			throw new FormatException(SR.Argument_BadFormatSpecifier);
		}
		return result;
	}

	private unsafe static string FormatDouble(ref ValueStringBuilder sb, double value, ReadOnlySpan<char> format, NumberFormatInfo info)
	{
		if (!double.IsFinite(value))
		{
			if (double.IsNaN(value))
			{
				return info.NaNSymbol;
			}
			if (!double.IsNegative(value))
			{
				return info.PositiveInfinitySymbol;
			}
			return info.NegativeInfinitySymbol;
		}
		int digits;
		char c = ParseFormatSpecifier(format, out digits);
		byte* digits2 = stackalloc byte[769];
		if (c == '\0')
		{
			digits = 15;
		}
		NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, digits2, 769);
		number.IsNegative = double.IsNegative(value);
		bool isSignificantDigits;
		int nMaxDigits = GetFloatingPointMaxDigitsAndPrecision(c, ref digits, info, out isSignificantDigits);
		if (value != 0.0 && (!isSignificantDigits || !Grisu3.TryRunDouble(value, digits, ref number)))
		{
			Dragon4Double(value, digits, isSignificantDigits, ref number);
		}
		if (c != 0)
		{
			if (digits == -1)
			{
				nMaxDigits = Math.Max(number.DigitsCount, 17);
			}
			NumberToString(ref sb, ref number, c, nMaxDigits, info);
		}
		else
		{
			NumberToStringFormat(ref sb, ref number, format, info);
		}
		return null;
	}

	public static string FormatSingle(float value, string format, NumberFormatInfo info)
	{
		Span<char> initialBuffer = stackalloc char[32];
		ValueStringBuilder sb = new ValueStringBuilder(initialBuffer);
		return FormatSingle(ref sb, value, format, info) ?? sb.ToString();
	}

	public static bool TryFormatSingle(float value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<char> destination, out int charsWritten)
	{
		Span<char> initialBuffer = stackalloc char[32];
		ValueStringBuilder sb = new ValueStringBuilder(initialBuffer);
		string text = FormatSingle(ref sb, value, format, info);
		if (text == null)
		{
			return sb.TryCopyTo(destination, out charsWritten);
		}
		return TryCopyTo(text, destination, out charsWritten);
	}

	private unsafe static string FormatSingle(ref ValueStringBuilder sb, float value, ReadOnlySpan<char> format, NumberFormatInfo info)
	{
		if (!float.IsFinite(value))
		{
			if (float.IsNaN(value))
			{
				return info.NaNSymbol;
			}
			if (!float.IsNegative(value))
			{
				return info.PositiveInfinitySymbol;
			}
			return info.NegativeInfinitySymbol;
		}
		int digits;
		char c = ParseFormatSpecifier(format, out digits);
		byte* digits2 = stackalloc byte[114];
		if (c == '\0')
		{
			digits = 7;
		}
		NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, digits2, 114);
		number.IsNegative = float.IsNegative(value);
		bool isSignificantDigits;
		int nMaxDigits = GetFloatingPointMaxDigitsAndPrecision(c, ref digits, info, out isSignificantDigits);
		if (value != 0f && (!isSignificantDigits || !Grisu3.TryRunSingle(value, digits, ref number)))
		{
			Dragon4Single(value, digits, isSignificantDigits, ref number);
		}
		if (c != 0)
		{
			if (digits == -1)
			{
				nMaxDigits = Math.Max(number.DigitsCount, 9);
			}
			NumberToString(ref sb, ref number, c, nMaxDigits, info);
		}
		else
		{
			NumberToStringFormat(ref sb, ref number, format, info);
		}
		return null;
	}

	public static string FormatHalf(Half value, string format, NumberFormatInfo info)
	{
		Span<char> initialBuffer = stackalloc char[32];
		ValueStringBuilder sb = new ValueStringBuilder(initialBuffer);
		return FormatHalf(ref sb, value, format, info) ?? sb.ToString();
	}

	private unsafe static string FormatHalf(ref ValueStringBuilder sb, Half value, ReadOnlySpan<char> format, NumberFormatInfo info)
	{
		if (!Half.IsFinite(value))
		{
			if (Half.IsNaN(value))
			{
				return info.NaNSymbol;
			}
			if (!Half.IsNegative(value))
			{
				return info.PositiveInfinitySymbol;
			}
			return info.NegativeInfinitySymbol;
		}
		int digits;
		char c = ParseFormatSpecifier(format, out digits);
		byte* digits2 = stackalloc byte[21];
		if (c == '\0')
		{
			digits = 5;
		}
		NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, digits2, 21);
		number.IsNegative = Half.IsNegative(value);
		bool isSignificantDigits;
		int nMaxDigits = GetFloatingPointMaxDigitsAndPrecision(c, ref digits, info, out isSignificantDigits);
		if (value != default(Half) && (!isSignificantDigits || !Grisu3.TryRunHalf(value, digits, ref number)))
		{
			Dragon4Half(value, digits, isSignificantDigits, ref number);
		}
		if (c != 0)
		{
			if (digits == -1)
			{
				nMaxDigits = Math.Max(number.DigitsCount, 5);
			}
			NumberToString(ref sb, ref number, c, nMaxDigits, info);
		}
		else
		{
			NumberToStringFormat(ref sb, ref number, format, info);
		}
		return null;
	}

	public static bool TryFormatHalf(Half value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<char> destination, out int charsWritten)
	{
		Span<char> initialBuffer = stackalloc char[32];
		ValueStringBuilder sb = new ValueStringBuilder(initialBuffer);
		string text = FormatHalf(ref sb, value, format, info);
		if (text == null)
		{
			return sb.TryCopyTo(destination, out charsWritten);
		}
		return TryCopyTo(text, destination, out charsWritten);
	}

	private static bool TryCopyTo(string source, Span<char> destination, out int charsWritten)
	{
		if (source.TryCopyTo(destination))
		{
			charsWritten = source.Length;
			return true;
		}
		charsWritten = 0;
		return false;
	}

	private static char GetHexBase(char fmt)
	{
		return (char)(fmt - 33);
	}

	public static string FormatInt32(int value, int hexMask, string format, IFormatProvider provider)
	{
		if (string.IsNullOrEmpty(format))
		{
			if (value < 0)
			{
				return NegativeInt32ToDecStr(value, -1, NumberFormatInfo.GetInstance(provider).NegativeSign);
			}
			return UInt32ToDecStr((uint)value);
		}
		return FormatInt32Slow(value, hexMask, format, provider);
		unsafe static string FormatInt32Slow(int value, int hexMask, string format, IFormatProvider provider)
		{
			ReadOnlySpan<char> format2 = format;
			int digits;
			char c = ParseFormatSpecifier(format2, out digits);
			char c2 = (char)(c & 0xFFDFu);
			if ((c2 == 'G') ? (digits < 1) : (c2 == 'D'))
			{
				if (value < 0)
				{
					return NegativeInt32ToDecStr(value, digits, NumberFormatInfo.GetInstance(provider).NegativeSign);
				}
				return UInt32ToDecStr((uint)value, digits);
			}
			if (c2 == 'X')
			{
				return Int32ToHexStr(value & hexMask, GetHexBase(c), digits);
			}
			NumberFormatInfo instance = NumberFormatInfo.GetInstance(provider);
			byte* digits2 = stackalloc byte[11];
			NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, digits2, 11);
			Int32ToNumber(value, ref number);
			char* pointer = stackalloc char[32];
			ValueStringBuilder sb = new ValueStringBuilder(new Span<char>(pointer, 32));
			if (c != 0)
			{
				NumberToString(ref sb, ref number, c, digits, instance);
			}
			else
			{
				NumberToStringFormat(ref sb, ref number, format2, instance);
			}
			return sb.ToString();
		}
	}

	public static bool TryFormatInt32(int value, int hexMask, ReadOnlySpan<char> format, IFormatProvider provider, Span<char> destination, out int charsWritten)
	{
		if (format.Length == 0)
		{
			if (value < 0)
			{
				return TryNegativeInt32ToDecStr(value, -1, NumberFormatInfo.GetInstance(provider).NegativeSign, destination, out charsWritten);
			}
			return TryUInt32ToDecStr((uint)value, -1, destination, out charsWritten);
		}
		return TryFormatInt32Slow(value, hexMask, format, provider, destination, out charsWritten);
		unsafe static bool TryFormatInt32Slow(int value, int hexMask, ReadOnlySpan<char> format, IFormatProvider provider, Span<char> destination, out int charsWritten)
		{
			int digits;
			char c = ParseFormatSpecifier(format, out digits);
			char c2 = (char)(c & 0xFFDFu);
			if ((c2 == 'G') ? (digits < 1) : (c2 == 'D'))
			{
				if (value < 0)
				{
					return TryNegativeInt32ToDecStr(value, digits, NumberFormatInfo.GetInstance(provider).NegativeSign, destination, out charsWritten);
				}
				return TryUInt32ToDecStr((uint)value, digits, destination, out charsWritten);
			}
			if (c2 == 'X')
			{
				return TryInt32ToHexStr(value & hexMask, GetHexBase(c), digits, destination, out charsWritten);
			}
			NumberFormatInfo instance = NumberFormatInfo.GetInstance(provider);
			byte* digits2 = stackalloc byte[11];
			NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, digits2, 11);
			Int32ToNumber(value, ref number);
			char* pointer = stackalloc char[32];
			ValueStringBuilder sb = new ValueStringBuilder(new Span<char>(pointer, 32));
			if (c != 0)
			{
				NumberToString(ref sb, ref number, c, digits, instance);
			}
			else
			{
				NumberToStringFormat(ref sb, ref number, format, instance);
			}
			return sb.TryCopyTo(destination, out charsWritten);
		}
	}

	public static string FormatUInt32(uint value, string format, IFormatProvider provider)
	{
		if (string.IsNullOrEmpty(format))
		{
			return UInt32ToDecStr(value);
		}
		return FormatUInt32Slow(value, format, provider);
		unsafe static string FormatUInt32Slow(uint value, string format, IFormatProvider provider)
		{
			ReadOnlySpan<char> format2 = format;
			int digits;
			char c = ParseFormatSpecifier(format2, out digits);
			char c2 = (char)(c & 0xFFDFu);
			if ((c2 == 'G') ? (digits < 1) : (c2 == 'D'))
			{
				return UInt32ToDecStr(value, digits);
			}
			if (c2 == 'X')
			{
				return Int32ToHexStr((int)value, GetHexBase(c), digits);
			}
			NumberFormatInfo instance = NumberFormatInfo.GetInstance(provider);
			byte* digits2 = stackalloc byte[11];
			NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, digits2, 11);
			UInt32ToNumber(value, ref number);
			char* pointer = stackalloc char[32];
			ValueStringBuilder sb = new ValueStringBuilder(new Span<char>(pointer, 32));
			if (c != 0)
			{
				NumberToString(ref sb, ref number, c, digits, instance);
			}
			else
			{
				NumberToStringFormat(ref sb, ref number, format2, instance);
			}
			return sb.ToString();
		}
	}

	public static bool TryFormatUInt32(uint value, ReadOnlySpan<char> format, IFormatProvider provider, Span<char> destination, out int charsWritten)
	{
		if (format.Length == 0)
		{
			return TryUInt32ToDecStr(value, -1, destination, out charsWritten);
		}
		return TryFormatUInt32Slow(value, format, provider, destination, out charsWritten);
		unsafe static bool TryFormatUInt32Slow(uint value, ReadOnlySpan<char> format, IFormatProvider provider, Span<char> destination, out int charsWritten)
		{
			int digits;
			char c = ParseFormatSpecifier(format, out digits);
			char c2 = (char)(c & 0xFFDFu);
			if ((c2 == 'G') ? (digits < 1) : (c2 == 'D'))
			{
				return TryUInt32ToDecStr(value, digits, destination, out charsWritten);
			}
			if (c2 == 'X')
			{
				return TryInt32ToHexStr((int)value, GetHexBase(c), digits, destination, out charsWritten);
			}
			NumberFormatInfo instance = NumberFormatInfo.GetInstance(provider);
			byte* digits2 = stackalloc byte[11];
			NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, digits2, 11);
			UInt32ToNumber(value, ref number);
			char* pointer = stackalloc char[32];
			ValueStringBuilder sb = new ValueStringBuilder(new Span<char>(pointer, 32));
			if (c != 0)
			{
				NumberToString(ref sb, ref number, c, digits, instance);
			}
			else
			{
				NumberToStringFormat(ref sb, ref number, format, instance);
			}
			return sb.TryCopyTo(destination, out charsWritten);
		}
	}

	public static string FormatInt64(long value, string format, IFormatProvider provider)
	{
		if (string.IsNullOrEmpty(format))
		{
			if (value < 0)
			{
				return NegativeInt64ToDecStr(value, -1, NumberFormatInfo.GetInstance(provider).NegativeSign);
			}
			return UInt64ToDecStr((ulong)value, -1);
		}
		return FormatInt64Slow(value, format, provider);
		unsafe static string FormatInt64Slow(long value, string format, IFormatProvider provider)
		{
			ReadOnlySpan<char> format2 = format;
			int digits;
			char c = ParseFormatSpecifier(format2, out digits);
			char c2 = (char)(c & 0xFFDFu);
			if ((c2 == 'G') ? (digits < 1) : (c2 == 'D'))
			{
				if (value < 0)
				{
					return NegativeInt64ToDecStr(value, digits, NumberFormatInfo.GetInstance(provider).NegativeSign);
				}
				return UInt64ToDecStr((ulong)value, digits);
			}
			if (c2 == 'X')
			{
				return Int64ToHexStr(value, GetHexBase(c), digits);
			}
			NumberFormatInfo instance = NumberFormatInfo.GetInstance(provider);
			byte* digits2 = stackalloc byte[20];
			NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, digits2, 20);
			Int64ToNumber(value, ref number);
			char* pointer = stackalloc char[32];
			ValueStringBuilder sb = new ValueStringBuilder(new Span<char>(pointer, 32));
			if (c != 0)
			{
				NumberToString(ref sb, ref number, c, digits, instance);
			}
			else
			{
				NumberToStringFormat(ref sb, ref number, format2, instance);
			}
			return sb.ToString();
		}
	}

	public static bool TryFormatInt64(long value, ReadOnlySpan<char> format, IFormatProvider provider, Span<char> destination, out int charsWritten)
	{
		if (format.Length == 0)
		{
			if (value < 0)
			{
				return TryNegativeInt64ToDecStr(value, -1, NumberFormatInfo.GetInstance(provider).NegativeSign, destination, out charsWritten);
			}
			return TryUInt64ToDecStr((ulong)value, -1, destination, out charsWritten);
		}
		return TryFormatInt64Slow(value, format, provider, destination, out charsWritten);
		unsafe static bool TryFormatInt64Slow(long value, ReadOnlySpan<char> format, IFormatProvider provider, Span<char> destination, out int charsWritten)
		{
			int digits;
			char c = ParseFormatSpecifier(format, out digits);
			char c2 = (char)(c & 0xFFDFu);
			if ((c2 == 'G') ? (digits < 1) : (c2 == 'D'))
			{
				if (value < 0)
				{
					return TryNegativeInt64ToDecStr(value, digits, NumberFormatInfo.GetInstance(provider).NegativeSign, destination, out charsWritten);
				}
				return TryUInt64ToDecStr((ulong)value, digits, destination, out charsWritten);
			}
			if (c2 == 'X')
			{
				return TryInt64ToHexStr(value, GetHexBase(c), digits, destination, out charsWritten);
			}
			NumberFormatInfo instance = NumberFormatInfo.GetInstance(provider);
			byte* digits2 = stackalloc byte[20];
			NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, digits2, 20);
			Int64ToNumber(value, ref number);
			char* pointer = stackalloc char[32];
			ValueStringBuilder sb = new ValueStringBuilder(new Span<char>(pointer, 32));
			if (c != 0)
			{
				NumberToString(ref sb, ref number, c, digits, instance);
			}
			else
			{
				NumberToStringFormat(ref sb, ref number, format, instance);
			}
			return sb.TryCopyTo(destination, out charsWritten);
		}
	}

	public static string FormatUInt64(ulong value, string format, IFormatProvider provider)
	{
		if (string.IsNullOrEmpty(format))
		{
			return UInt64ToDecStr(value, -1);
		}
		return FormatUInt64Slow(value, format, provider);
		unsafe static string FormatUInt64Slow(ulong value, string format, IFormatProvider provider)
		{
			ReadOnlySpan<char> format2 = format;
			int digits;
			char c = ParseFormatSpecifier(format2, out digits);
			char c2 = (char)(c & 0xFFDFu);
			if ((c2 == 'G') ? (digits < 1) : (c2 == 'D'))
			{
				return UInt64ToDecStr(value, digits);
			}
			if (c2 == 'X')
			{
				return Int64ToHexStr((long)value, GetHexBase(c), digits);
			}
			NumberFormatInfo instance = NumberFormatInfo.GetInstance(provider);
			byte* digits2 = stackalloc byte[21];
			NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, digits2, 21);
			UInt64ToNumber(value, ref number);
			char* pointer = stackalloc char[32];
			ValueStringBuilder sb = new ValueStringBuilder(new Span<char>(pointer, 32));
			if (c != 0)
			{
				NumberToString(ref sb, ref number, c, digits, instance);
			}
			else
			{
				NumberToStringFormat(ref sb, ref number, format2, instance);
			}
			return sb.ToString();
		}
	}

	public static bool TryFormatUInt64(ulong value, ReadOnlySpan<char> format, IFormatProvider provider, Span<char> destination, out int charsWritten)
	{
		if (format.Length == 0)
		{
			return TryUInt64ToDecStr(value, -1, destination, out charsWritten);
		}
		return TryFormatUInt64Slow(value, format, provider, destination, out charsWritten);
		unsafe static bool TryFormatUInt64Slow(ulong value, ReadOnlySpan<char> format, IFormatProvider provider, Span<char> destination, out int charsWritten)
		{
			int digits;
			char c = ParseFormatSpecifier(format, out digits);
			char c2 = (char)(c & 0xFFDFu);
			if ((c2 == 'G') ? (digits < 1) : (c2 == 'D'))
			{
				return TryUInt64ToDecStr(value, digits, destination, out charsWritten);
			}
			if (c2 == 'X')
			{
				return TryInt64ToHexStr((long)value, GetHexBase(c), digits, destination, out charsWritten);
			}
			NumberFormatInfo instance = NumberFormatInfo.GetInstance(provider);
			byte* digits2 = stackalloc byte[21];
			NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, digits2, 21);
			UInt64ToNumber(value, ref number);
			char* pointer = stackalloc char[32];
			ValueStringBuilder sb = new ValueStringBuilder(new Span<char>(pointer, 32));
			if (c != 0)
			{
				NumberToString(ref sb, ref number, c, digits, instance);
			}
			else
			{
				NumberToStringFormat(ref sb, ref number, format, instance);
			}
			return sb.TryCopyTo(destination, out charsWritten);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static void Int32ToNumber(int value, ref NumberBuffer number)
	{
		number.DigitsCount = 10;
		if (value >= 0)
		{
			number.IsNegative = false;
		}
		else
		{
			number.IsNegative = true;
			value = -value;
		}
		byte* digitsPointer = number.GetDigitsPointer();
		byte* ptr = UInt32ToDecChars(digitsPointer + 10, (uint)value, 0);
		int num = (number.Scale = (number.DigitsCount = (int)(digitsPointer + 10 - ptr)));
		byte* digitsPointer2 = number.GetDigitsPointer();
		while (--num >= 0)
		{
			*(digitsPointer2++) = *(ptr++);
		}
		*digitsPointer2 = 0;
	}

	public static string Int32ToDecStr(int value)
	{
		if (value < 0)
		{
			return NegativeInt32ToDecStr(value, -1, NumberFormatInfo.CurrentInfo.NegativeSign);
		}
		return UInt32ToDecStr((uint)value);
	}

	private unsafe static string NegativeInt32ToDecStr(int value, int digits, string sNegative)
	{
		if (digits < 1)
		{
			digits = 1;
		}
		int num = Math.Max(digits, FormattingHelpers.CountDigits((uint)(-value))) + sNegative.Length;
		string text = string.FastAllocateString(num);
		fixed (char* ptr = text)
		{
			char* ptr2 = UInt32ToDecChars(ptr + num, (uint)(-value), digits);
			for (int num2 = sNegative.Length - 1; num2 >= 0; num2--)
			{
				*(--ptr2) = sNegative[num2];
			}
		}
		return text;
	}

	private unsafe static bool TryNegativeInt32ToDecStr(int value, int digits, string sNegative, Span<char> destination, out int charsWritten)
	{
		if (digits < 1)
		{
			digits = 1;
		}
		int num = Math.Max(digits, FormattingHelpers.CountDigits((uint)(-value))) + sNegative.Length;
		if (num > destination.Length)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = num;
		fixed (char* ptr = &MemoryMarshal.GetReference(destination))
		{
			char* ptr2 = UInt32ToDecChars(ptr + num, (uint)(-value), digits);
			for (int num2 = sNegative.Length - 1; num2 >= 0; num2--)
			{
				*(--ptr2) = sNegative[num2];
			}
		}
		return true;
	}

	private unsafe static string Int32ToHexStr(int value, char hexBase, int digits)
	{
		if (digits < 1)
		{
			digits = 1;
		}
		int num = Math.Max(digits, FormattingHelpers.CountHexDigits((uint)value));
		string text = string.FastAllocateString(num);
		fixed (char* ptr = text)
		{
			char* ptr2 = Int32ToHexChars(ptr + num, (uint)value, hexBase, digits);
		}
		return text;
	}

	private unsafe static bool TryInt32ToHexStr(int value, char hexBase, int digits, Span<char> destination, out int charsWritten)
	{
		if (digits < 1)
		{
			digits = 1;
		}
		int num = Math.Max(digits, FormattingHelpers.CountHexDigits((uint)value));
		if (num > destination.Length)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = num;
		fixed (char* ptr = &MemoryMarshal.GetReference(destination))
		{
			char* ptr2 = Int32ToHexChars(ptr + num, (uint)value, hexBase, digits);
		}
		return true;
	}

	private unsafe static char* Int32ToHexChars(char* buffer, uint value, int hexBase, int digits)
	{
		while (--digits >= 0 || value != 0)
		{
			byte b = (byte)(value & 0xFu);
			*(--buffer) = (char)(b + ((b < 10) ? 48 : hexBase));
			value >>= 4;
		}
		return buffer;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static void UInt32ToNumber(uint value, ref NumberBuffer number)
	{
		number.DigitsCount = 10;
		number.IsNegative = false;
		byte* digitsPointer = number.GetDigitsPointer();
		byte* ptr = UInt32ToDecChars(digitsPointer + 10, value, 0);
		int num = (number.Scale = (number.DigitsCount = (int)(digitsPointer + 10 - ptr)));
		byte* digitsPointer2 = number.GetDigitsPointer();
		while (--num >= 0)
		{
			*(digitsPointer2++) = *(ptr++);
		}
		*digitsPointer2 = 0;
	}

	internal unsafe static byte* UInt32ToDecChars(byte* bufferEnd, uint value, int digits)
	{
		while (--digits >= 0 || value != 0)
		{
			uint num;
			(value, num) = Math.DivRem(value, 10u);
			*(--bufferEnd) = (byte)(num + 48);
		}
		return bufferEnd;
	}

	internal unsafe static char* UInt32ToDecChars(char* bufferEnd, uint value, int digits)
	{
		while (--digits >= 0 || value != 0)
		{
			uint num;
			(value, num) = Math.DivRem(value, 10u);
			*(--bufferEnd) = (char)(num + 48);
		}
		return bufferEnd;
	}

	internal unsafe static string UInt32ToDecStr(uint value)
	{
		int num = FormattingHelpers.CountDigits(value);
		if (num == 1)
		{
			return s_singleDigitStringCache[value];
		}
		string text = string.FastAllocateString(num);
		fixed (char* ptr = text)
		{
			char* ptr2 = ptr + num;
			do
			{
				uint num2;
				(value, num2) = Math.DivRem(value, 10u);
				*(--ptr2) = (char)(num2 + 48);
			}
			while (value != 0);
		}
		return text;
	}

	private unsafe static string UInt32ToDecStr(uint value, int digits)
	{
		if (digits <= 1)
		{
			return UInt32ToDecStr(value);
		}
		int num = Math.Max(digits, FormattingHelpers.CountDigits(value));
		string text = string.FastAllocateString(num);
		fixed (char* ptr = text)
		{
			char* bufferEnd = ptr + num;
			bufferEnd = UInt32ToDecChars(bufferEnd, value, digits);
		}
		return text;
	}

	private unsafe static bool TryUInt32ToDecStr(uint value, int digits, Span<char> destination, out int charsWritten)
	{
		int num = Math.Max(digits, FormattingHelpers.CountDigits(value));
		if (num > destination.Length)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = num;
		fixed (char* ptr = &MemoryMarshal.GetReference(destination))
		{
			char* ptr2 = ptr + num;
			if (digits <= 1)
			{
				do
				{
					uint num2;
					(value, num2) = Math.DivRem(value, 10u);
					*(--ptr2) = (char)(num2 + 48);
				}
				while (value != 0);
			}
			else
			{
				ptr2 = UInt32ToDecChars(ptr2, value, digits);
			}
		}
		return true;
	}

	private unsafe static void Int64ToNumber(long input, ref NumberBuffer number)
	{
		ulong value = (ulong)input;
		number.IsNegative = input < 0;
		number.DigitsCount = 19;
		if (number.IsNegative)
		{
			value = (ulong)(-input);
		}
		byte* digitsPointer = number.GetDigitsPointer();
		byte* bufferEnd = digitsPointer + 19;
		while (High32(value) != 0)
		{
			bufferEnd = UInt32ToDecChars(bufferEnd, Int64DivMod1E9(ref value), 9);
		}
		bufferEnd = UInt32ToDecChars(bufferEnd, Low32(value), 0);
		int num = (number.Scale = (number.DigitsCount = (int)(digitsPointer + 19 - bufferEnd)));
		byte* digitsPointer2 = number.GetDigitsPointer();
		while (--num >= 0)
		{
			*(digitsPointer2++) = *(bufferEnd++);
		}
		*digitsPointer2 = 0;
	}

	public static string Int64ToDecStr(long value)
	{
		if (value < 0)
		{
			return NegativeInt64ToDecStr(value, -1, NumberFormatInfo.CurrentInfo.NegativeSign);
		}
		return UInt64ToDecStr((ulong)value, -1);
	}

	private unsafe static string NegativeInt64ToDecStr(long input, int digits, string sNegative)
	{
		if (digits < 1)
		{
			digits = 1;
		}
		ulong value = (ulong)(-input);
		int num = Math.Max(digits, FormattingHelpers.CountDigits(value)) + sNegative.Length;
		string text = string.FastAllocateString(num);
		fixed (char* ptr = text)
		{
			char* bufferEnd = ptr + num;
			while (High32(value) != 0)
			{
				bufferEnd = UInt32ToDecChars(bufferEnd, Int64DivMod1E9(ref value), 9);
				digits -= 9;
			}
			bufferEnd = UInt32ToDecChars(bufferEnd, Low32(value), digits);
			for (int num2 = sNegative.Length - 1; num2 >= 0; num2--)
			{
				*(--bufferEnd) = sNegative[num2];
			}
		}
		return text;
	}

	private unsafe static bool TryNegativeInt64ToDecStr(long input, int digits, string sNegative, Span<char> destination, out int charsWritten)
	{
		if (digits < 1)
		{
			digits = 1;
		}
		ulong value = (ulong)(-input);
		int num = Math.Max(digits, FormattingHelpers.CountDigits((ulong)(-input))) + sNegative.Length;
		if (num > destination.Length)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = num;
		fixed (char* ptr = &MemoryMarshal.GetReference(destination))
		{
			char* bufferEnd = ptr + num;
			while (High32(value) != 0)
			{
				bufferEnd = UInt32ToDecChars(bufferEnd, Int64DivMod1E9(ref value), 9);
				digits -= 9;
			}
			bufferEnd = UInt32ToDecChars(bufferEnd, Low32(value), digits);
			for (int num2 = sNegative.Length - 1; num2 >= 0; num2--)
			{
				*(--bufferEnd) = sNegative[num2];
			}
		}
		return true;
	}

	private unsafe static string Int64ToHexStr(long value, char hexBase, int digits)
	{
		int num = Math.Max(digits, FormattingHelpers.CountHexDigits((ulong)value));
		string text = string.FastAllocateString(num);
		fixed (char* ptr = text)
		{
			char* buffer = ptr + num;
			if (High32((ulong)value) != 0)
			{
				buffer = Int32ToHexChars(buffer, Low32((ulong)value), hexBase, 8);
				buffer = Int32ToHexChars(buffer, High32((ulong)value), hexBase, digits - 8);
			}
			else
			{
				buffer = Int32ToHexChars(buffer, Low32((ulong)value), hexBase, Math.Max(digits, 1));
			}
		}
		return text;
	}

	private unsafe static bool TryInt64ToHexStr(long value, char hexBase, int digits, Span<char> destination, out int charsWritten)
	{
		int num = Math.Max(digits, FormattingHelpers.CountHexDigits((ulong)value));
		if (num > destination.Length)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = num;
		fixed (char* ptr = &MemoryMarshal.GetReference(destination))
		{
			char* buffer = ptr + num;
			if (High32((ulong)value) != 0)
			{
				buffer = Int32ToHexChars(buffer, Low32((ulong)value), hexBase, 8);
				buffer = Int32ToHexChars(buffer, High32((ulong)value), hexBase, digits - 8);
			}
			else
			{
				buffer = Int32ToHexChars(buffer, Low32((ulong)value), hexBase, Math.Max(digits, 1));
			}
		}
		return true;
	}

	private unsafe static void UInt64ToNumber(ulong value, ref NumberBuffer number)
	{
		number.DigitsCount = 20;
		number.IsNegative = false;
		byte* digitsPointer = number.GetDigitsPointer();
		byte* bufferEnd = digitsPointer + 20;
		while (High32(value) != 0)
		{
			bufferEnd = UInt32ToDecChars(bufferEnd, Int64DivMod1E9(ref value), 9);
		}
		bufferEnd = UInt32ToDecChars(bufferEnd, Low32(value), 0);
		int num = (number.Scale = (number.DigitsCount = (int)(digitsPointer + 20 - bufferEnd)));
		byte* digitsPointer2 = number.GetDigitsPointer();
		while (--num >= 0)
		{
			*(digitsPointer2++) = *(bufferEnd++);
		}
		*digitsPointer2 = 0;
	}

	internal unsafe static string UInt64ToDecStr(ulong value, int digits)
	{
		if (digits < 1)
		{
			digits = 1;
		}
		int num = Math.Max(digits, FormattingHelpers.CountDigits(value));
		if (num == 1)
		{
			return s_singleDigitStringCache[value];
		}
		string text = string.FastAllocateString(num);
		fixed (char* ptr = text)
		{
			char* bufferEnd = ptr + num;
			while (High32(value) != 0)
			{
				bufferEnd = UInt32ToDecChars(bufferEnd, Int64DivMod1E9(ref value), 9);
				digits -= 9;
			}
			bufferEnd = UInt32ToDecChars(bufferEnd, Low32(value), digits);
		}
		return text;
	}

	private unsafe static bool TryUInt64ToDecStr(ulong value, int digits, Span<char> destination, out int charsWritten)
	{
		if (digits < 1)
		{
			digits = 1;
		}
		int num = Math.Max(digits, FormattingHelpers.CountDigits(value));
		if (num > destination.Length)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = num;
		fixed (char* ptr = &MemoryMarshal.GetReference(destination))
		{
			char* bufferEnd = ptr + num;
			while (High32(value) != 0)
			{
				bufferEnd = UInt32ToDecChars(bufferEnd, Int64DivMod1E9(ref value), 9);
				digits -= 9;
			}
			bufferEnd = UInt32ToDecChars(bufferEnd, Low32(value), digits);
		}
		return true;
	}

	internal static char ParseFormatSpecifier(ReadOnlySpan<char> format, out int digits)
	{
		char c = '\0';
		if (format.Length > 0)
		{
			c = format[0];
			if ((uint)(c - 65) <= 25u || (uint)(c - 97) <= 25u)
			{
				if (format.Length == 1)
				{
					digits = -1;
					return c;
				}
				if (format.Length == 2)
				{
					int num = format[1] - 48;
					if ((uint)num < 10u)
					{
						digits = num;
						return c;
					}
				}
				else if (format.Length == 3)
				{
					int num2 = format[1] - 48;
					int num3 = format[2] - 48;
					if ((uint)num2 < 10u && (uint)num3 < 10u)
					{
						digits = num2 * 10 + num3;
						return c;
					}
				}
				int num4 = 0;
				int num5 = 1;
				while (num5 < format.Length && (uint)(format[num5] - 48) < 10u)
				{
					int num6 = num4 * 10 + format[num5++] - 48;
					if (num6 < num4)
					{
						throw new FormatException(SR.Argument_BadFormatSpecifier);
					}
					num4 = num6;
				}
				if (num5 == format.Length || format[num5] == '\0')
				{
					digits = num4;
					return c;
				}
			}
		}
		digits = -1;
		if (format.Length != 0 && c != 0)
		{
			return '\0';
		}
		return 'G';
	}

	internal static void NumberToString(ref ValueStringBuilder sb, ref NumberBuffer number, char format, int nMaxDigits, NumberFormatInfo info)
	{
		bool isCorrectlyRounded = number.Kind == NumberBufferKind.FloatingPoint;
		bool bSuppressScientific;
		switch (format)
		{
		case 'C':
		case 'c':
			if (nMaxDigits < 0)
			{
				nMaxDigits = info.CurrencyDecimalDigits;
			}
			RoundNumber(ref number, number.Scale + nMaxDigits, isCorrectlyRounded);
			FormatCurrency(ref sb, ref number, nMaxDigits, info);
			return;
		case 'F':
		case 'f':
			if (nMaxDigits < 0)
			{
				nMaxDigits = info.NumberDecimalDigits;
			}
			RoundNumber(ref number, number.Scale + nMaxDigits, isCorrectlyRounded);
			if (number.IsNegative)
			{
				sb.Append(info.NegativeSign);
			}
			FormatFixed(ref sb, ref number, nMaxDigits, null, info.NumberDecimalSeparator, null);
			return;
		case 'N':
		case 'n':
			if (nMaxDigits < 0)
			{
				nMaxDigits = info.NumberDecimalDigits;
			}
			RoundNumber(ref number, number.Scale + nMaxDigits, isCorrectlyRounded);
			FormatNumber(ref sb, ref number, nMaxDigits, info);
			return;
		case 'E':
		case 'e':
			if (nMaxDigits < 0)
			{
				nMaxDigits = 6;
			}
			nMaxDigits++;
			RoundNumber(ref number, nMaxDigits, isCorrectlyRounded);
			if (number.IsNegative)
			{
				sb.Append(info.NegativeSign);
			}
			FormatScientific(ref sb, ref number, nMaxDigits, info, format);
			return;
		case 'G':
		case 'g':
			bSuppressScientific = false;
			if (nMaxDigits < 1)
			{
				if (number.Kind == NumberBufferKind.Decimal && nMaxDigits == -1)
				{
					bSuppressScientific = true;
					if (number.Digits[0] != 0)
					{
						goto IL_0189;
					}
					goto IL_019e;
				}
				nMaxDigits = number.DigitsCount;
			}
			RoundNumber(ref number, nMaxDigits, isCorrectlyRounded);
			goto IL_0189;
		case 'P':
		case 'p':
			if (nMaxDigits < 0)
			{
				nMaxDigits = info.PercentDecimalDigits;
			}
			number.Scale += 2;
			RoundNumber(ref number, number.Scale + nMaxDigits, isCorrectlyRounded);
			FormatPercent(ref sb, ref number, nMaxDigits, info);
			return;
		case 'R':
		case 'r':
			{
				if (number.Kind != NumberBufferKind.FloatingPoint)
				{
					break;
				}
				format = (char)(format - 11);
				goto case 'G';
			}
			IL_0189:
			if (number.IsNegative)
			{
				sb.Append(info.NegativeSign);
			}
			goto IL_019e;
			IL_019e:
			FormatGeneral(ref sb, ref number, nMaxDigits, info, (char)(format - 2), bSuppressScientific);
			return;
		}
		throw new FormatException(SR.Argument_BadFormatSpecifier);
	}

	internal unsafe static void NumberToStringFormat(ref ValueStringBuilder sb, ref NumberBuffer number, ReadOnlySpan<char> format, NumberFormatInfo info)
	{
		int num = 0;
		byte* digitsPointer = number.GetDigitsPointer();
		int num2 = FindSection(format, (*digitsPointer == 0) ? 2 : (number.IsNegative ? 1 : 0));
		int num3;
		int num4;
		bool flag;
		bool flag2;
		int num5;
		int num6;
		int num9;
		while (true)
		{
			num3 = 0;
			num4 = -1;
			num5 = int.MaxValue;
			num6 = 0;
			flag = false;
			int num7 = -1;
			flag2 = false;
			int num8 = 0;
			num9 = num2;
			fixed (char* ptr = &MemoryMarshal.GetReference(format))
			{
				char c;
				while (num9 < format.Length && (c = ptr[num9++]) != 0)
				{
					switch (c)
					{
					case ';':
						break;
					case '#':
						num3++;
						continue;
					case '0':
						if (num5 == int.MaxValue)
						{
							num5 = num3;
						}
						num3++;
						num6 = num3;
						continue;
					case '.':
						if (num4 < 0)
						{
							num4 = num3;
						}
						continue;
					case ',':
						if (num3 <= 0 || num4 >= 0)
						{
							continue;
						}
						if (num7 >= 0)
						{
							if (num7 == num3)
							{
								num++;
								continue;
							}
							flag2 = true;
						}
						num7 = num3;
						num = 1;
						continue;
					case '%':
						num8 += 2;
						continue;
					case '‰':
						num8 += 3;
						continue;
					case '"':
					case '\'':
						while (num9 < format.Length && ptr[num9] != 0 && ptr[num9++] != c)
						{
						}
						continue;
					case '\\':
						if (num9 < format.Length && ptr[num9] != 0)
						{
							num9++;
						}
						continue;
					case 'E':
					case 'e':
						if ((num9 < format.Length && ptr[num9] == '0') || (num9 + 1 < format.Length && (ptr[num9] == '+' || ptr[num9] == '-') && ptr[num9 + 1] == '0'))
						{
							while (++num9 < format.Length && ptr[num9] == '0')
							{
							}
							flag = true;
						}
						continue;
					default:
						continue;
					}
					break;
				}
			}
			if (num4 < 0)
			{
				num4 = num3;
			}
			if (num7 >= 0)
			{
				if (num7 == num4)
				{
					num8 -= num * 3;
				}
				else
				{
					flag2 = true;
				}
			}
			if (*digitsPointer != 0)
			{
				number.Scale += num8;
				int pos = (flag ? num3 : (number.Scale + num3 - num4));
				RoundNumber(ref number, pos, isCorrectlyRounded: false);
				if (*digitsPointer != 0)
				{
					break;
				}
				num9 = FindSection(format, 2);
				if (num9 == num2)
				{
					break;
				}
				num2 = num9;
				continue;
			}
			if (number.Kind != NumberBufferKind.FloatingPoint)
			{
				number.IsNegative = false;
			}
			number.Scale = 0;
			break;
		}
		num5 = ((num5 < num4) ? (num4 - num5) : 0);
		num6 = ((num6 > num4) ? (num4 - num6) : 0);
		int num10;
		int num11;
		if (flag)
		{
			num10 = num4;
			num11 = 0;
		}
		else
		{
			num10 = ((number.Scale > num4) ? number.Scale : num4);
			num11 = number.Scale - num4;
		}
		num9 = num2;
		Span<int> span = stackalloc int[4];
		int num12 = -1;
		if (flag2 && info.NumberGroupSeparator.Length > 0)
		{
			int[] numberGroupSizes = info._numberGroupSizes;
			int num13 = 0;
			int i = 0;
			int num14 = numberGroupSizes.Length;
			if (num14 != 0)
			{
				i = numberGroupSizes[num13];
			}
			int num15 = i;
			int num16 = num10 + ((num11 < 0) ? num11 : 0);
			for (int num17 = ((num5 > num16) ? num5 : num16); num17 > i; i += num15)
			{
				if (num15 == 0)
				{
					break;
				}
				num12++;
				if (num12 >= span.Length)
				{
					int[] array = new int[span.Length * 2];
					span.CopyTo(array);
					span = array;
				}
				span[num12] = i;
				if (num13 < num14 - 1)
				{
					num13++;
					num15 = numberGroupSizes[num13];
				}
			}
		}
		if (number.IsNegative && num2 == 0 && number.Scale != 0)
		{
			sb.Append(info.NegativeSign);
		}
		bool flag3 = false;
		fixed (char* ptr3 = &MemoryMarshal.GetReference(format))
		{
			byte* ptr2 = digitsPointer;
			char c;
			while (num9 < format.Length && (c = ptr3[num9++]) != 0 && c != ';')
			{
				if (num11 > 0 && (c == '#' || c == '.' || c == '0'))
				{
					while (num11 > 0)
					{
						sb.Append((char)((*ptr2 != 0) ? (*(ptr2++)) : 48));
						if (flag2 && num10 > 1 && num12 >= 0 && num10 == span[num12] + 1)
						{
							sb.Append(info.NumberGroupSeparator);
							num12--;
						}
						num10--;
						num11--;
					}
				}
				switch (c)
				{
				case '#':
				case '0':
					if (num11 < 0)
					{
						num11++;
						c = ((num10 <= num5) ? '0' : '\0');
					}
					else
					{
						c = ((*ptr2 != 0) ? ((char)(*(ptr2++))) : ((num10 > num6) ? '0' : '\0'));
					}
					if (c != 0)
					{
						sb.Append(c);
						if (flag2 && num10 > 1 && num12 >= 0 && num10 == span[num12] + 1)
						{
							sb.Append(info.NumberGroupSeparator);
							num12--;
						}
					}
					num10--;
					break;
				case '.':
					if (!(num10 != 0 || flag3) && (num6 < 0 || (num4 < num3 && *ptr2 != 0)))
					{
						sb.Append(info.NumberDecimalSeparator);
						flag3 = true;
					}
					break;
				case '‰':
					sb.Append(info.PerMilleSymbol);
					break;
				case '%':
					sb.Append(info.PercentSymbol);
					break;
				case '"':
				case '\'':
					while (num9 < format.Length && ptr3[num9] != 0 && ptr3[num9] != c)
					{
						sb.Append(ptr3[num9++]);
					}
					if (num9 < format.Length && ptr3[num9] != 0)
					{
						num9++;
					}
					break;
				case '\\':
					if (num9 < format.Length && ptr3[num9] != 0)
					{
						sb.Append(ptr3[num9++]);
					}
					break;
				case 'E':
				case 'e':
				{
					bool positiveSign = false;
					int num18 = 0;
					if (flag)
					{
						if (num9 < format.Length && ptr3[num9] == '0')
						{
							num18++;
						}
						else if (num9 + 1 < format.Length && ptr3[num9] == '+' && ptr3[num9 + 1] == '0')
						{
							positiveSign = true;
						}
						else if (num9 + 1 >= format.Length || ptr3[num9] != '-' || ptr3[num9 + 1] != '0')
						{
							sb.Append(c);
							break;
						}
						while (++num9 < format.Length && ptr3[num9] == '0')
						{
							num18++;
						}
						if (num18 > 10)
						{
							num18 = 10;
						}
						int value = ((*digitsPointer != 0) ? (number.Scale - num4) : 0);
						FormatExponent(ref sb, info, value, c, num18, positiveSign);
						flag = false;
						break;
					}
					sb.Append(c);
					if (num9 < format.Length)
					{
						if (ptr3[num9] == '+' || ptr3[num9] == '-')
						{
							sb.Append(ptr3[num9++]);
						}
						while (num9 < format.Length && ptr3[num9] == '0')
						{
							sb.Append(ptr3[num9++]);
						}
					}
					break;
				}
				default:
					sb.Append(c);
					break;
				case ',':
					break;
				}
			}
		}
		if (number.IsNegative && num2 == 0 && number.Scale == 0 && sb.Length > 0)
		{
			sb.Insert(0, info.NegativeSign);
		}
	}

	private static void FormatCurrency(ref ValueStringBuilder sb, ref NumberBuffer number, int nMaxDigits, NumberFormatInfo info)
	{
		string text = (number.IsNegative ? s_negCurrencyFormats[info.CurrencyNegativePattern] : s_posCurrencyFormats[info.CurrencyPositivePattern]);
		string text2 = text;
		foreach (char c in text2)
		{
			switch (c)
			{
			case '#':
				FormatFixed(ref sb, ref number, nMaxDigits, info._currencyGroupSizes, info.CurrencyDecimalSeparator, info.CurrencyGroupSeparator);
				break;
			case '-':
				sb.Append(info.NegativeSign);
				break;
			case '$':
				sb.Append(info.CurrencySymbol);
				break;
			default:
				sb.Append(c);
				break;
			}
		}
	}

	private unsafe static void FormatFixed(ref ValueStringBuilder sb, ref NumberBuffer number, int nMaxDigits, int[] groupDigits, string sDecimal, string sGroup)
	{
		int num = number.Scale;
		byte* ptr = number.GetDigitsPointer();
		if (num > 0)
		{
			if (groupDigits != null)
			{
				int num2 = 0;
				int num3 = num;
				int num4 = 0;
				if (groupDigits.Length != 0)
				{
					int num5 = groupDigits[num2];
					while (num > num5 && groupDigits[num2] != 0)
					{
						num3 += sGroup.Length;
						if (num2 < groupDigits.Length - 1)
						{
							num2++;
						}
						num5 += groupDigits[num2];
						if (num5 < 0 || num3 < 0)
						{
							throw new ArgumentOutOfRangeException();
						}
					}
					num4 = ((num5 != 0) ? groupDigits[0] : 0);
				}
				num2 = 0;
				int num6 = 0;
				int digitsCount = number.DigitsCount;
				int num7 = ((num < digitsCount) ? num : digitsCount);
				fixed (char* ptr2 = &MemoryMarshal.GetReference(sb.AppendSpan(num3)))
				{
					char* ptr3 = ptr2 + num3 - 1;
					for (int num8 = num - 1; num8 >= 0; num8--)
					{
						*(ptr3--) = (char)((num8 < num7) ? ptr[num8] : 48);
						if (num4 > 0)
						{
							num6++;
							if (num6 == num4 && num8 != 0)
							{
								for (int num9 = sGroup.Length - 1; num9 >= 0; num9--)
								{
									*(ptr3--) = sGroup[num9];
								}
								if (num2 < groupDigits.Length - 1)
								{
									num2++;
									num4 = groupDigits[num2];
								}
								num6 = 0;
							}
						}
					}
					ptr += num7;
				}
			}
			else
			{
				do
				{
					sb.Append((char)((*ptr != 0) ? (*(ptr++)) : 48));
				}
				while (--num > 0);
			}
		}
		else
		{
			sb.Append('0');
		}
		if (nMaxDigits > 0)
		{
			sb.Append(sDecimal);
			if (num < 0 && nMaxDigits > 0)
			{
				int num10 = Math.Min(-num, nMaxDigits);
				sb.Append('0', num10);
				num += num10;
				nMaxDigits -= num10;
			}
			while (nMaxDigits > 0)
			{
				sb.Append((char)((*ptr != 0) ? (*(ptr++)) : 48));
				nMaxDigits--;
			}
		}
	}

	private static void FormatNumber(ref ValueStringBuilder sb, ref NumberBuffer number, int nMaxDigits, NumberFormatInfo info)
	{
		string text = (number.IsNegative ? s_negNumberFormats[info.NumberNegativePattern] : "#");
		string text2 = text;
		foreach (char c in text2)
		{
			switch (c)
			{
			case '#':
				FormatFixed(ref sb, ref number, nMaxDigits, info._numberGroupSizes, info.NumberDecimalSeparator, info.NumberGroupSeparator);
				break;
			case '-':
				sb.Append(info.NegativeSign);
				break;
			default:
				sb.Append(c);
				break;
			}
		}
	}

	private unsafe static void FormatScientific(ref ValueStringBuilder sb, ref NumberBuffer number, int nMaxDigits, NumberFormatInfo info, char expChar)
	{
		byte* digitsPointer = number.GetDigitsPointer();
		sb.Append((char)((*digitsPointer != 0) ? (*(digitsPointer++)) : 48));
		if (nMaxDigits != 1)
		{
			sb.Append(info.NumberDecimalSeparator);
		}
		while (--nMaxDigits > 0)
		{
			sb.Append((char)((*digitsPointer != 0) ? (*(digitsPointer++)) : 48));
		}
		int value = ((number.Digits[0] != 0) ? (number.Scale - 1) : 0);
		FormatExponent(ref sb, info, value, expChar, 3, positiveSign: true);
	}

	private unsafe static void FormatExponent(ref ValueStringBuilder sb, NumberFormatInfo info, int value, char expChar, int minDigits, bool positiveSign)
	{
		sb.Append(expChar);
		if (value < 0)
		{
			sb.Append(info.NegativeSign);
			value = -value;
		}
		else if (positiveSign)
		{
			sb.Append(info.PositiveSign);
		}
		char* ptr = stackalloc char[10];
		char* ptr2 = UInt32ToDecChars(ptr + 10, (uint)value, minDigits);
		sb.Append(ptr2, (int)(ptr + 10 - ptr2));
	}

	private unsafe static void FormatGeneral(ref ValueStringBuilder sb, ref NumberBuffer number, int nMaxDigits, NumberFormatInfo info, char expChar, bool bSuppressScientific)
	{
		int i = number.Scale;
		bool flag = false;
		if (!bSuppressScientific && (i > nMaxDigits || i < -3))
		{
			i = 1;
			flag = true;
		}
		byte* digitsPointer = number.GetDigitsPointer();
		if (i > 0)
		{
			do
			{
				sb.Append((char)((*digitsPointer != 0) ? (*(digitsPointer++)) : 48));
			}
			while (--i > 0);
		}
		else
		{
			sb.Append('0');
		}
		if (*digitsPointer != 0 || i < 0)
		{
			sb.Append(info.NumberDecimalSeparator);
			for (; i < 0; i++)
			{
				sb.Append('0');
			}
			while (*digitsPointer != 0)
			{
				sb.Append((char)(*(digitsPointer++)));
			}
		}
		if (flag)
		{
			FormatExponent(ref sb, info, number.Scale - 1, expChar, 2, positiveSign: true);
		}
	}

	private static void FormatPercent(ref ValueStringBuilder sb, ref NumberBuffer number, int nMaxDigits, NumberFormatInfo info)
	{
		string text = (number.IsNegative ? s_negPercentFormats[info.PercentNegativePattern] : s_posPercentFormats[info.PercentPositivePattern]);
		string text2 = text;
		foreach (char c in text2)
		{
			switch (c)
			{
			case '#':
				FormatFixed(ref sb, ref number, nMaxDigits, info._percentGroupSizes, info.PercentDecimalSeparator, info.PercentGroupSeparator);
				break;
			case '-':
				sb.Append(info.NegativeSign);
				break;
			case '%':
				sb.Append(info.PercentSymbol);
				break;
			default:
				sb.Append(c);
				break;
			}
		}
	}

	internal unsafe static void RoundNumber(ref NumberBuffer number, int pos, bool isCorrectlyRounded)
	{
		byte* digitsPointer = number.GetDigitsPointer();
		int j;
		for (j = 0; j < pos && digitsPointer[j] != 0; j++)
		{
		}
		if (j == pos && ShouldRoundUp(digitsPointer, j, number.Kind, isCorrectlyRounded))
		{
			while (j > 0 && digitsPointer[j - 1] == 57)
			{
				j--;
			}
			if (j > 0)
			{
				byte* num = digitsPointer + (j - 1);
				(*num)++;
			}
			else
			{
				number.Scale++;
				*digitsPointer = 49;
				j = 1;
			}
		}
		else
		{
			while (j > 0 && digitsPointer[j - 1] == 48)
			{
				j--;
			}
		}
		if (j == 0)
		{
			if (number.Kind != NumberBufferKind.FloatingPoint)
			{
				number.IsNegative = false;
			}
			number.Scale = 0;
		}
		digitsPointer[j] = 0;
		number.DigitsCount = j;
		unsafe static bool ShouldRoundUp(byte* dig, int i, NumberBufferKind numberKind, bool isCorrectlyRounded)
		{
			byte b = dig[i];
			if (b == 0 || isCorrectlyRounded)
			{
				return false;
			}
			return b >= 53;
		}
	}

	private unsafe static int FindSection(ReadOnlySpan<char> format, int section)
	{
		if (section == 0)
		{
			return 0;
		}
		fixed (char* ptr = &MemoryMarshal.GetReference(format))
		{
			int num = 0;
			while (true)
			{
				if (num >= format.Length)
				{
					return 0;
				}
				char c;
				char c2 = (c = ptr[num++]);
				if ((uint)c2 <= 34u)
				{
					if (c2 == '\0')
					{
						break;
					}
					if (c2 != '"')
					{
						continue;
					}
				}
				else if (c2 != '\'')
				{
					switch (c2)
					{
					default:
						continue;
					case '\\':
						if (num < format.Length && ptr[num] != 0)
						{
							num++;
						}
						continue;
					case ';':
						break;
					}
					if (--section == 0)
					{
						if (num >= format.Length || ptr[num] == '\0' || ptr[num] == ';')
						{
							break;
						}
						return num;
					}
					continue;
				}
				while (num < format.Length && ptr[num] != 0 && ptr[num++] != c)
				{
				}
			}
			return 0;
		}
	}

	private static uint Low32(ulong value)
	{
		return (uint)value;
	}

	private static uint High32(ulong value)
	{
		return (uint)((value & 0xFFFFFFFF00000000uL) >> 32);
	}

	private static uint Int64DivMod1E9(ref ulong value)
	{
		uint result = (uint)(value % 1000000000);
		value /= 1000000000uL;
		return result;
	}

	private static ulong ExtractFractionAndBiasedExponent(double value, out int exponent)
	{
		ulong num = BitConverter.DoubleToUInt64Bits(value);
		ulong num2 = num & 0xFFFFFFFFFFFFFuL;
		exponent = (int)(num >> 52) & 0x7FF;
		if (exponent != 0)
		{
			num2 |= 0x10000000000000uL;
			exponent -= 1075;
		}
		else
		{
			exponent = -1074;
		}
		return num2;
	}

	private static ushort ExtractFractionAndBiasedExponent(Half value, out int exponent)
	{
		ushort num = BitConverter.HalfToUInt16Bits(value);
		ushort num2 = (ushort)(num & 0x3FFu);
		exponent = (num >> 10) & 0x1F;
		if (exponent != 0)
		{
			num2 = (ushort)(num2 | 0x400u);
			exponent -= 25;
		}
		else
		{
			exponent = -24;
		}
		return num2;
	}

	private static uint ExtractFractionAndBiasedExponent(float value, out int exponent)
	{
		uint num = BitConverter.SingleToUInt32Bits(value);
		uint num2 = num & 0x7FFFFFu;
		exponent = (int)((num >> 23) & 0xFF);
		if (exponent != 0)
		{
			num2 |= 0x800000u;
			exponent -= 150;
		}
		else
		{
			exponent = -149;
		}
		return num2;
	}

	private unsafe static void AccumulateDecimalDigitsIntoBigInteger(ref NumberBuffer number, uint firstIndex, uint lastIndex, out BigInteger result)
	{
		BigInteger.SetZero(out result);
		byte* ptr = number.GetDigitsPointer() + firstIndex;
		uint num = lastIndex - firstIndex;
		while (num != 0)
		{
			uint num2 = Math.Min(num, 9u);
			uint value = DigitsToUInt32(ptr, (int)num2);
			result.MultiplyPow10(num2);
			result.Add(value);
			ptr += num2;
			num -= num2;
		}
	}

	private static ulong AssembleFloatingPointBits(in FloatingPointInfo info, ulong initialMantissa, int initialExponent, bool hasZeroTail)
	{
		uint num = BigInteger.CountSignificantBits(initialMantissa);
		int num2 = (int)(info.NormalMantissaBits - num);
		int num3 = initialExponent - num2;
		ulong num4 = initialMantissa;
		int num5 = num3;
		if (num3 > info.MaxBinaryExponent)
		{
			return info.InfinityBits;
		}
		if (num3 < info.MinBinaryExponent)
		{
			int num6 = num2 + num3 + info.ExponentBias - 1;
			num5 = -info.ExponentBias;
			if (num6 < 0)
			{
				num4 = RightShiftWithRounding(num4, -num6, hasZeroTail);
				if (num4 == 0L)
				{
					return info.ZeroBits;
				}
				if (num4 > info.DenormalMantissaMask)
				{
					num5 = initialExponent - (num6 + 1) - num2;
				}
			}
			else
			{
				num4 <<= num6;
			}
		}
		else if (num2 < 0)
		{
			num4 = RightShiftWithRounding(num4, -num2, hasZeroTail);
			if (num4 > info.NormalMantissaMask)
			{
				num4 >>= 1;
				num5++;
				if (num5 > info.MaxBinaryExponent)
				{
					return info.InfinityBits;
				}
			}
		}
		else if (num2 > 0)
		{
			num4 <<= num2;
		}
		num4 &= info.DenormalMantissaMask;
		ulong num7 = (ulong)((long)(num5 + info.ExponentBias) << (int)info.DenormalMantissaBits);
		return num7 | num4;
	}

	private static ulong ConvertBigIntegerToFloatingPointBits(ref BigInteger value, in FloatingPointInfo info, uint integerBitsOfPrecision, bool hasNonZeroFractionalPart)
	{
		int denormalMantissaBits = info.DenormalMantissaBits;
		if (integerBitsOfPrecision <= 64)
		{
			return AssembleFloatingPointBits(in info, value.ToUInt64(), denormalMantissaBits, !hasNonZeroFractionalPart);
		}
		(uint Quotient, uint Remainder) tuple = Math.DivRem(integerBitsOfPrecision, 32u);
		uint item = tuple.Quotient;
		uint item2 = tuple.Remainder;
		uint num = item - 1;
		uint num2 = num - 1;
		int num3 = denormalMantissaBits + (int)(num2 * 32);
		bool flag = !hasNonZeroFractionalPart;
		ulong initialMantissa;
		if (item2 == 0)
		{
			initialMantissa = ((ulong)value.GetBlock(num) << 32) + value.GetBlock(num2);
		}
		else
		{
			int num4 = (int)item2;
			int num5 = 64 - num4;
			int num6 = num5 - 32;
			num3 += (int)item2;
			uint block = value.GetBlock(num2);
			uint num7 = block >> num4;
			ulong num8 = (ulong)value.GetBlock(num) << num6;
			ulong num9 = (ulong)value.GetBlock(item) << num5;
			initialMantissa = num9 + num8 + num7;
			uint num10 = (uint)((1 << (int)item2) - 1);
			flag = flag && (block & num10) == 0;
		}
		for (uint num11 = 0u; num11 != num2; num11++)
		{
			flag &= value.GetBlock(num11) == 0;
		}
		return AssembleFloatingPointBits(in info, initialMantissa, num3, flag);
	}

	private unsafe static uint DigitsToUInt32(byte* p, int count)
	{
		byte* ptr = p + count;
		uint num = (uint)(*p - 48);
		for (p++; p < ptr; p++)
		{
			num = 10 * num + *p - 48;
		}
		return num;
	}

	private unsafe static ulong DigitsToUInt64(byte* p, int count)
	{
		byte* ptr = p + count;
		ulong num = (ulong)(*p - 48);
		for (p++; p < ptr; p++)
		{
			num = 10 * num + *p - 48;
		}
		return num;
	}

	private unsafe static ulong NumberToDoubleFloatingPointBits(ref NumberBuffer number, in FloatingPointInfo info)
	{
		uint digitsCount = (uint)number.DigitsCount;
		uint num = (uint)Math.Max(0, number.Scale);
		uint num2 = Math.Min(num, digitsCount);
		uint num3 = digitsCount - num2;
		uint num4 = (uint)Math.Abs(number.Scale - num2 - num3);
		byte* digitsPointer = number.GetDigitsPointer();
		if (digitsCount <= 15 && num4 <= 22)
		{
			double num5 = DigitsToUInt64(digitsPointer, (int)digitsCount);
			double num6 = s_Pow10DoubleTable[num4];
			num5 = ((num3 == 0) ? (num5 * num6) : (num5 / num6));
			return BitConverter.DoubleToUInt64Bits(num5);
		}
		return NumberToFloatingPointBitsSlow(ref number, in info, num, num2, num3);
	}

	private unsafe static ushort NumberToHalfFloatingPointBits(ref NumberBuffer number, in FloatingPointInfo info)
	{
		uint digitsCount = (uint)number.DigitsCount;
		uint num = (uint)Math.Max(0, number.Scale);
		uint num2 = Math.Min(num, digitsCount);
		uint num3 = digitsCount - num2;
		uint num4 = (uint)Math.Abs(number.Scale - num2 - num3);
		byte* digitsPointer = number.GetDigitsPointer();
		if (digitsCount <= 7 && num4 <= 10)
		{
			float num5 = DigitsToUInt32(digitsPointer, (int)digitsCount);
			float num6 = s_Pow10SingleTable[num4];
			num5 = ((num3 == 0) ? (num5 * num6) : (num5 / num6));
			return BitConverter.HalfToUInt16Bits((Half)num5);
		}
		if (digitsCount <= 15 && num4 <= 22)
		{
			double num7 = DigitsToUInt64(digitsPointer, (int)digitsCount);
			double num8 = s_Pow10DoubleTable[num4];
			num7 = ((num3 == 0) ? (num7 * num8) : (num7 / num8));
			return BitConverter.HalfToUInt16Bits((Half)num7);
		}
		return (ushort)NumberToFloatingPointBitsSlow(ref number, in info, num, num2, num3);
	}

	private unsafe static uint NumberToSingleFloatingPointBits(ref NumberBuffer number, in FloatingPointInfo info)
	{
		uint digitsCount = (uint)number.DigitsCount;
		uint num = (uint)Math.Max(0, number.Scale);
		uint num2 = Math.Min(num, digitsCount);
		uint num3 = digitsCount - num2;
		uint num4 = (uint)Math.Abs(number.Scale - num2 - num3);
		byte* digitsPointer = number.GetDigitsPointer();
		if (digitsCount <= 7 && num4 <= 10)
		{
			float num5 = DigitsToUInt32(digitsPointer, (int)digitsCount);
			float num6 = s_Pow10SingleTable[num4];
			num5 = ((num3 == 0) ? (num5 * num6) : (num5 / num6));
			return BitConverter.SingleToUInt32Bits(num5);
		}
		if (digitsCount <= 15 && num4 <= 22)
		{
			double num7 = DigitsToUInt64(digitsPointer, (int)digitsCount);
			double num8 = s_Pow10DoubleTable[num4];
			num7 = ((num3 == 0) ? (num7 * num8) : (num7 / num8));
			return BitConverter.SingleToUInt32Bits((float)num7);
		}
		return (uint)NumberToFloatingPointBitsSlow(ref number, in info, num, num2, num3);
	}

	private static ulong NumberToFloatingPointBitsSlow(ref NumberBuffer number, in FloatingPointInfo info, uint positiveExponent, uint integerDigitsPresent, uint fractionalDigitsPresent)
	{
		uint num = (uint)(info.NormalMantissaBits + 1);
		uint digitsCount = (uint)number.DigitsCount;
		uint num2 = positiveExponent - integerDigitsPresent;
		uint lastIndex = digitsCount;
		AccumulateDecimalDigitsIntoBigInteger(ref number, 0u, integerDigitsPresent, out var result);
		if (num2 != 0)
		{
			if (num2 > info.OverflowDecimalExponent)
			{
				return info.InfinityBits;
			}
			result.MultiplyPow10(num2);
		}
		uint num3 = BigInteger.CountSignificantBits(ref result);
		if (num3 >= num || fractionalDigitsPresent == 0)
		{
			return ConvertBigIntegerToFloatingPointBits(ref result, in info, num3, fractionalDigitsPresent != 0);
		}
		uint num4 = fractionalDigitsPresent;
		if (number.Scale < 0)
		{
			num4 += (uint)(-number.Scale);
		}
		if (num3 == 0 && num4 - (int)digitsCount > info.OverflowDecimalExponent)
		{
			return info.ZeroBits;
		}
		AccumulateDecimalDigitsIntoBigInteger(ref number, integerDigitsPresent, lastIndex, out var result2);
		if (result2.IsZero())
		{
			return ConvertBigIntegerToFloatingPointBits(ref result, in info, num3, fractionalDigitsPresent != 0);
		}
		BigInteger.Pow10(num4, out var result3);
		uint num5 = BigInteger.CountSignificantBits(ref result2);
		uint num6 = BigInteger.CountSignificantBits(ref result3);
		uint num7 = 0u;
		if (num6 > num5)
		{
			num7 = num6 - num5;
		}
		if (num7 != 0)
		{
			result2.ShiftLeft(num7);
		}
		uint num8 = num - num3;
		uint num9 = num8;
		if (num3 != 0)
		{
			if (num7 > num9)
			{
				return ConvertBigIntegerToFloatingPointBits(ref result, in info, num3, fractionalDigitsPresent != 0);
			}
			num9 -= num7;
		}
		uint num10 = num7;
		if (BigInteger.Compare(ref result2, ref result3) < 0)
		{
			num10++;
		}
		result2.ShiftLeft(num9);
		BigInteger.DivRem(ref result2, ref result3, out var quo, out var rem);
		ulong num11 = quo.ToUInt64();
		bool flag = !number.HasNonZeroTail && rem.IsZero();
		uint num12 = BigInteger.CountSignificantBits(num11);
		if (num12 > num8)
		{
			int num13 = (int)(num12 - num8);
			flag = flag && (num11 & (ulong)((1L << num13) - 1)) == 0;
			num11 >>= num13;
		}
		ulong num14 = result.ToUInt64();
		ulong initialMantissa = (num14 << (int)num8) + num11;
		int initialExponent = (int)((num3 != 0) ? (num3 - 2) : (0 - num10 - 1));
		return AssembleFloatingPointBits(in info, initialMantissa, initialExponent, flag);
	}

	private static ulong RightShiftWithRounding(ulong value, int shift, bool hasZeroTail)
	{
		if (shift >= 64)
		{
			return 0uL;
		}
		ulong num = (ulong)((1L << shift - 1) - 1);
		ulong num2 = (ulong)(1L << shift - 1);
		ulong num3 = (ulong)(1L << shift);
		bool lsbBit = (value & num3) != 0;
		bool roundBit = (value & num2) != 0;
		bool hasTailBits = !hasZeroTail || (value & num) != 0;
		return (value >> shift) + (ulong)(ShouldRoundUp(lsbBit, roundBit, hasTailBits) ? 1 : 0);
	}

	private static bool ShouldRoundUp(bool lsbBit, bool roundBit, bool hasTailBits)
	{
		if (roundBit)
		{
			return hasTailBits || lsbBit;
		}
		return false;
	}

	private unsafe static bool TryNumberToInt32(ref NumberBuffer number, ref int value)
	{
		int num = number.Scale;
		if (num > 10 || num < number.DigitsCount)
		{
			return false;
		}
		byte* digitsPointer = number.GetDigitsPointer();
		int num2 = 0;
		while (--num >= 0)
		{
			if ((uint)num2 > 214748364u)
			{
				return false;
			}
			num2 *= 10;
			if (*digitsPointer != 0)
			{
				num2 += *(digitsPointer++) - 48;
			}
		}
		if (number.IsNegative)
		{
			num2 = -num2;
			if (num2 > 0)
			{
				return false;
			}
		}
		else if (num2 < 0)
		{
			return false;
		}
		value = num2;
		return true;
	}

	private unsafe static bool TryNumberToInt64(ref NumberBuffer number, ref long value)
	{
		int num = number.Scale;
		if (num > 19 || num < number.DigitsCount)
		{
			return false;
		}
		byte* digitsPointer = number.GetDigitsPointer();
		long num2 = 0L;
		while (--num >= 0)
		{
			if ((ulong)num2 > 922337203685477580uL)
			{
				return false;
			}
			num2 *= 10;
			if (*digitsPointer != 0)
			{
				num2 += *(digitsPointer++) - 48;
			}
		}
		if (number.IsNegative)
		{
			num2 = -num2;
			if (num2 > 0)
			{
				return false;
			}
		}
		else if (num2 < 0)
		{
			return false;
		}
		value = num2;
		return true;
	}

	private unsafe static bool TryNumberToUInt32(ref NumberBuffer number, ref uint value)
	{
		int num = number.Scale;
		if (num > 10 || num < number.DigitsCount || number.IsNegative)
		{
			return false;
		}
		byte* digitsPointer = number.GetDigitsPointer();
		uint num2 = 0u;
		while (--num >= 0)
		{
			if (num2 > 429496729)
			{
				return false;
			}
			num2 *= 10;
			if (*digitsPointer != 0)
			{
				uint num3 = num2 + (uint)(*(digitsPointer++) - 48);
				if (num3 < num2)
				{
					return false;
				}
				num2 = num3;
			}
		}
		value = num2;
		return true;
	}

	private unsafe static bool TryNumberToUInt64(ref NumberBuffer number, ref ulong value)
	{
		int num = number.Scale;
		if (num > 20 || num < number.DigitsCount || number.IsNegative)
		{
			return false;
		}
		byte* digitsPointer = number.GetDigitsPointer();
		ulong num2 = 0uL;
		while (--num >= 0)
		{
			if (num2 > 1844674407370955161L)
			{
				return false;
			}
			num2 *= 10;
			if (*digitsPointer != 0)
			{
				ulong num3 = num2 + (ulong)(*(digitsPointer++) - 48);
				if (num3 < num2)
				{
					return false;
				}
				num2 = num3;
			}
		}
		value = num2;
		return true;
	}

	internal static int ParseInt32(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info)
	{
		int result;
		ParsingStatus parsingStatus = TryParseInt32(value, styles, info, out result);
		if (parsingStatus != 0)
		{
			ThrowOverflowOrFormatException(parsingStatus, TypeCode.Int32);
		}
		return result;
	}

	internal static long ParseInt64(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info)
	{
		long result;
		ParsingStatus parsingStatus = TryParseInt64(value, styles, info, out result);
		if (parsingStatus != 0)
		{
			ThrowOverflowOrFormatException(parsingStatus, TypeCode.Int64);
		}
		return result;
	}

	internal static uint ParseUInt32(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info)
	{
		uint result;
		ParsingStatus parsingStatus = TryParseUInt32(value, styles, info, out result);
		if (parsingStatus != 0)
		{
			ThrowOverflowOrFormatException(parsingStatus, TypeCode.UInt32);
		}
		return result;
	}

	internal static ulong ParseUInt64(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info)
	{
		ulong result;
		ParsingStatus parsingStatus = TryParseUInt64(value, styles, info, out result);
		if (parsingStatus != 0)
		{
			ThrowOverflowOrFormatException(parsingStatus, TypeCode.UInt64);
		}
		return result;
	}

	private unsafe static bool TryParseNumber(ref char* str, char* strEnd, NumberStyles styles, ref NumberBuffer number, NumberFormatInfo info)
	{
		string text = null;
		bool flag = false;
		string value;
		string value2;
		if ((styles & NumberStyles.AllowCurrencySymbol) != 0)
		{
			text = info.CurrencySymbol;
			value = info.CurrencyDecimalSeparator;
			value2 = info.CurrencyGroupSeparator;
			flag = true;
		}
		else
		{
			value = info.NumberDecimalSeparator;
			value2 = info.NumberGroupSeparator;
		}
		int num = 0;
		char* ptr = str;
		char c = ((ptr < strEnd) ? (*ptr) : '\0');
		while (true)
		{
			if (!IsWhite(c) || (styles & NumberStyles.AllowLeadingWhite) == 0 || (((uint)num & (true ? 1u : 0u)) != 0 && (num & 0x20) == 0 && info.NumberNegativePattern != 2))
			{
				char* ptr2;
				if ((styles & NumberStyles.AllowLeadingSign) != 0 && (num & 1) == 0 && ((ptr2 = MatchChars(ptr, strEnd, info.PositiveSign)) != null || ((ptr2 = MatchNegativeSignChars(ptr, strEnd, info)) != null && (number.IsNegative = true))))
				{
					num |= 1;
					ptr = ptr2 - 1;
				}
				else if (c == '(' && (styles & NumberStyles.AllowParentheses) != 0 && (num & 1) == 0)
				{
					num |= 3;
					number.IsNegative = true;
				}
				else
				{
					if (text == null || (ptr2 = MatchChars(ptr, strEnd, text)) == null)
					{
						break;
					}
					num |= 0x20;
					text = null;
					ptr = ptr2 - 1;
				}
			}
			c = ((++ptr < strEnd) ? (*ptr) : '\0');
		}
		int num2 = 0;
		int num3 = 0;
		int num4 = number.Digits.Length - 1;
		int num5 = 0;
		while (true)
		{
			char* ptr2;
			if (IsDigit(c))
			{
				num |= 4;
				if (c != '0' || ((uint)num & 8u) != 0)
				{
					if (num2 < num4)
					{
						number.Digits[num2] = (byte)c;
						if (c != '0' || number.Kind != NumberBufferKind.Integer)
						{
							num3 = num2 + 1;
						}
					}
					else if (c != '0')
					{
						number.HasNonZeroTail = true;
					}
					if ((num & 0x10) == 0)
					{
						number.Scale++;
					}
					if (num2 < num4)
					{
						num5 = ((c == '0') ? (num5 + 1) : 0);
					}
					num2++;
					num |= 8;
				}
				else if (((uint)num & 0x10u) != 0)
				{
					number.Scale--;
				}
			}
			else if ((styles & NumberStyles.AllowDecimalPoint) != 0 && (num & 0x10) == 0 && ((ptr2 = MatchChars(ptr, strEnd, value)) != null || (flag && (num & 0x20) == 0 && (ptr2 = MatchChars(ptr, strEnd, info.NumberDecimalSeparator)) != null)))
			{
				num |= 0x10;
				ptr = ptr2 - 1;
			}
			else
			{
				if ((styles & NumberStyles.AllowThousands) == 0 || (num & 4) == 0 || ((uint)num & 0x10u) != 0 || ((ptr2 = MatchChars(ptr, strEnd, value2)) == null && (!flag || ((uint)num & 0x20u) != 0 || (ptr2 = MatchChars(ptr, strEnd, info.NumberGroupSeparator)) == null)))
				{
					break;
				}
				ptr = ptr2 - 1;
			}
			c = ((++ptr < strEnd) ? (*ptr) : '\0');
		}
		bool flag2 = false;
		number.DigitsCount = num3;
		number.Digits[num3] = 0;
		if (((uint)num & 4u) != 0)
		{
			if ((c == 'E' || c == 'e') && (styles & NumberStyles.AllowExponent) != 0)
			{
				char* ptr3 = ptr;
				c = ((++ptr < strEnd) ? (*ptr) : '\0');
				char* ptr2;
				if ((ptr2 = MatchChars(ptr, strEnd, info._positiveSign)) != null)
				{
					c = (((ptr = ptr2) < strEnd) ? (*ptr) : '\0');
				}
				else if ((ptr2 = MatchNegativeSignChars(ptr, strEnd, info)) != null)
				{
					c = (((ptr = ptr2) < strEnd) ? (*ptr) : '\0');
					flag2 = true;
				}
				if (IsDigit(c))
				{
					int num6 = 0;
					do
					{
						num6 = num6 * 10 + (c - 48);
						c = ((++ptr < strEnd) ? (*ptr) : '\0');
						if (num6 > 1000)
						{
							num6 = 9999;
							while (IsDigit(c))
							{
								c = ((++ptr < strEnd) ? (*ptr) : '\0');
							}
						}
					}
					while (IsDigit(c));
					if (flag2)
					{
						num6 = -num6;
					}
					number.Scale += num6;
				}
				else
				{
					ptr = ptr3;
					c = ((ptr < strEnd) ? (*ptr) : '\0');
				}
			}
			if (number.Kind == NumberBufferKind.FloatingPoint && !number.HasNonZeroTail)
			{
				int num7 = num3 - number.Scale;
				if (num7 > 0)
				{
					num5 = Math.Min(num5, num7);
					number.DigitsCount = num3 - num5;
					number.Digits[number.DigitsCount] = 0;
				}
			}
			while (true)
			{
				if (!IsWhite(c) || (styles & NumberStyles.AllowTrailingWhite) == 0)
				{
					char* ptr2;
					if ((styles & NumberStyles.AllowTrailingSign) != 0 && (num & 1) == 0 && ((ptr2 = MatchChars(ptr, strEnd, info.PositiveSign)) != null || ((ptr2 = MatchNegativeSignChars(ptr, strEnd, info)) != null && (number.IsNegative = true))))
					{
						num |= 1;
						ptr = ptr2 - 1;
					}
					else if (c == ')' && ((uint)num & 2u) != 0)
					{
						num &= -3;
					}
					else
					{
						if (text == null || (ptr2 = MatchChars(ptr, strEnd, text)) == null)
						{
							break;
						}
						text = null;
						ptr = ptr2 - 1;
					}
				}
				c = ((++ptr < strEnd) ? (*ptr) : '\0');
			}
			if ((num & 2) == 0)
			{
				if ((num & 8) == 0)
				{
					if (number.Kind != NumberBufferKind.Decimal)
					{
						number.Scale = 0;
					}
					if (number.Kind == NumberBufferKind.Integer && (num & 0x10) == 0)
					{
						number.IsNegative = false;
					}
				}
				str = ptr;
				return true;
			}
		}
		str = ptr;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ParsingStatus TryParseInt32(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out int result)
	{
		if ((styles & ~NumberStyles.Integer) == 0)
		{
			return TryParseInt32IntegerStyle(value, styles, info, out result);
		}
		if ((styles & NumberStyles.AllowHexSpecifier) != 0)
		{
			result = 0;
			return TryParseUInt32HexNumberStyle(value, styles, out Unsafe.As<int, uint>(ref result));
		}
		return TryParseInt32Number(value, styles, info, out result);
	}

	private unsafe static ParsingStatus TryParseInt32Number(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out int result)
	{
		result = 0;
		byte* digits = stackalloc byte[11];
		NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, digits, 11);
		if (!TryStringToNumber(value, styles, ref number, info))
		{
			return ParsingStatus.Failed;
		}
		if (!TryNumberToInt32(ref number, ref result))
		{
			return ParsingStatus.Overflow;
		}
		return ParsingStatus.OK;
	}

	internal static ParsingStatus TryParseInt32IntegerStyle(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out int result)
	{
		int i;
		int num;
		if (!value.IsEmpty)
		{
			i = 0;
			num = value[0];
			if ((styles & NumberStyles.AllowLeadingWhite) == 0 || !IsWhite(num))
			{
				goto IL_0048;
			}
			while (true)
			{
				i++;
				if ((uint)i >= (uint)value.Length)
				{
					break;
				}
				num = value[i];
				if (IsWhite(num))
				{
					continue;
				}
				goto IL_0048;
			}
		}
		goto IL_0289;
		IL_027d:
		int num2;
		int num3;
		result = num2 * num3;
		return ParsingStatus.OK;
		IL_0291:
		result = 0;
		return ParsingStatus.Overflow;
		IL_019f:
		if (IsDigit(num))
		{
			goto IL_01aa;
		}
		goto IL_0299;
		IL_027a:
		bool flag;
		if (!flag)
		{
			goto IL_027d;
		}
		goto IL_0291;
		IL_0048:
		num3 = 1;
		if ((styles & NumberStyles.AllowLeadingSign) != 0)
		{
			if (info.HasInvariantNumberSigns)
			{
				if (num == 45)
				{
					num3 = -1;
					i++;
					if ((uint)i >= (uint)value.Length)
					{
						goto IL_0289;
					}
					num = value[i];
				}
				else if (num == 43)
				{
					i++;
					if ((uint)i >= (uint)value.Length)
					{
						goto IL_0289;
					}
					num = value[i];
				}
			}
			else if (info.AllowHyphenDuringParsing && num == 45)
			{
				num3 = -1;
				i++;
				if ((uint)i >= (uint)value.Length)
				{
					goto IL_0289;
				}
				num = value[i];
			}
			else
			{
				value = value.Slice(i);
				i = 0;
				string positiveSign = info.PositiveSign;
				string negativeSign = info.NegativeSign;
				if (!string.IsNullOrEmpty(positiveSign) && value.StartsWith(positiveSign))
				{
					i += positiveSign.Length;
					if ((uint)i >= (uint)value.Length)
					{
						goto IL_0289;
					}
					num = value[i];
				}
				else if (!string.IsNullOrEmpty(negativeSign) && value.StartsWith(negativeSign))
				{
					num3 = -1;
					i += negativeSign.Length;
					if ((uint)i >= (uint)value.Length)
					{
						goto IL_0289;
					}
					num = value[i];
				}
			}
		}
		flag = false;
		num2 = 0;
		if (IsDigit(num))
		{
			if (num != 48)
			{
				goto IL_01aa;
			}
			while (true)
			{
				i++;
				if ((uint)i >= (uint)value.Length)
				{
					break;
				}
				num = value[i];
				if (num == 48)
				{
					continue;
				}
				goto IL_019f;
			}
			goto IL_027d;
		}
		goto IL_0289;
		IL_01aa:
		num2 = num - 48;
		i++;
		int num4 = 0;
		while (num4 < 8)
		{
			if ((uint)i >= (uint)value.Length)
			{
				goto IL_027d;
			}
			num = value[i];
			if (IsDigit(num))
			{
				i++;
				num2 = 10 * num2 + num - 48;
				num4++;
				continue;
			}
			goto IL_0299;
		}
		if ((uint)i >= (uint)value.Length)
		{
			goto IL_027d;
		}
		num = value[i];
		if (IsDigit(num))
		{
			i++;
			flag = num2 > 214748364;
			num2 = num2 * 10 + num - 48;
			flag = flag || (uint)num2 > (uint)(int.MaxValue + (num3 >>> 31));
			if ((uint)i >= (uint)value.Length)
			{
				goto IL_027a;
			}
			num = value[i];
			while (IsDigit(num))
			{
				flag = true;
				i++;
				if ((uint)i < (uint)value.Length)
				{
					num = value[i];
					continue;
				}
				goto IL_0291;
			}
		}
		goto IL_0299;
		IL_0299:
		if (IsWhite(num))
		{
			if ((styles & NumberStyles.AllowTrailingWhite) == 0)
			{
				goto IL_0289;
			}
			for (i++; i < value.Length && IsWhite(value[i]); i++)
			{
			}
			if ((uint)i >= (uint)value.Length)
			{
				goto IL_027a;
			}
		}
		if (TrailingZeros(value, i))
		{
			goto IL_027a;
		}
		goto IL_0289;
		IL_0289:
		result = 0;
		return ParsingStatus.Failed;
	}

	internal static ParsingStatus TryParseInt64IntegerStyle(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out long result)
	{
		int i;
		int num;
		if (!value.IsEmpty)
		{
			i = 0;
			num = value[0];
			if ((styles & NumberStyles.AllowLeadingWhite) == 0 || !IsWhite(num))
			{
				goto IL_0048;
			}
			while (true)
			{
				i++;
				if ((uint)i >= (uint)value.Length)
				{
					break;
				}
				num = value[i];
				if (IsWhite(num))
				{
					continue;
				}
				goto IL_0048;
			}
		}
		goto IL_029f;
		IL_0292:
		long num2;
		int num3;
		result = num2 * num3;
		return ParsingStatus.OK;
		IL_02a8:
		result = 0L;
		return ParsingStatus.Overflow;
		IL_01a0:
		if (IsDigit(num))
		{
			goto IL_01ab;
		}
		goto IL_02b1;
		IL_028f:
		bool flag;
		if (!flag)
		{
			goto IL_0292;
		}
		goto IL_02a8;
		IL_0048:
		num3 = 1;
		if ((styles & NumberStyles.AllowLeadingSign) != 0)
		{
			if (info.HasInvariantNumberSigns)
			{
				if (num == 45)
				{
					num3 = -1;
					i++;
					if ((uint)i >= (uint)value.Length)
					{
						goto IL_029f;
					}
					num = value[i];
				}
				else if (num == 43)
				{
					i++;
					if ((uint)i >= (uint)value.Length)
					{
						goto IL_029f;
					}
					num = value[i];
				}
			}
			else if (info.AllowHyphenDuringParsing && num == 45)
			{
				num3 = -1;
				i++;
				if ((uint)i >= (uint)value.Length)
				{
					goto IL_029f;
				}
				num = value[i];
			}
			else
			{
				value = value.Slice(i);
				i = 0;
				string positiveSign = info.PositiveSign;
				string negativeSign = info.NegativeSign;
				if (!string.IsNullOrEmpty(positiveSign) && value.StartsWith(positiveSign))
				{
					i += positiveSign.Length;
					if ((uint)i >= (uint)value.Length)
					{
						goto IL_029f;
					}
					num = value[i];
				}
				else if (!string.IsNullOrEmpty(negativeSign) && value.StartsWith(negativeSign))
				{
					num3 = -1;
					i += negativeSign.Length;
					if ((uint)i >= (uint)value.Length)
					{
						goto IL_029f;
					}
					num = value[i];
				}
			}
		}
		flag = false;
		num2 = 0L;
		if (IsDigit(num))
		{
			if (num != 48)
			{
				goto IL_01ab;
			}
			while (true)
			{
				i++;
				if ((uint)i >= (uint)value.Length)
				{
					break;
				}
				num = value[i];
				if (num == 48)
				{
					continue;
				}
				goto IL_01a0;
			}
			goto IL_0292;
		}
		goto IL_029f;
		IL_01ab:
		num2 = num - 48;
		i++;
		int num4 = 0;
		while (num4 < 17)
		{
			if ((uint)i >= (uint)value.Length)
			{
				goto IL_0292;
			}
			num = value[i];
			if (IsDigit(num))
			{
				i++;
				num2 = 10 * num2 + num - 48;
				num4++;
				continue;
			}
			goto IL_02b1;
		}
		if ((uint)i >= (uint)value.Length)
		{
			goto IL_0292;
		}
		num = value[i];
		if (IsDigit(num))
		{
			i++;
			flag = num2 > 922337203685477580L;
			num2 = num2 * 10 + num - 48;
			flag = flag || (ulong)num2 > (ulong)(long.MaxValue + (long)(uint)(num3 >>> 31));
			if ((uint)i >= (uint)value.Length)
			{
				goto IL_028f;
			}
			num = value[i];
			while (IsDigit(num))
			{
				flag = true;
				i++;
				if ((uint)i < (uint)value.Length)
				{
					num = value[i];
					continue;
				}
				goto IL_02a8;
			}
		}
		goto IL_02b1;
		IL_02b1:
		if (IsWhite(num))
		{
			if ((styles & NumberStyles.AllowTrailingWhite) == 0)
			{
				goto IL_029f;
			}
			for (i++; i < value.Length && IsWhite(value[i]); i++)
			{
			}
			if ((uint)i >= (uint)value.Length)
			{
				goto IL_028f;
			}
		}
		if (TrailingZeros(value, i))
		{
			goto IL_028f;
		}
		goto IL_029f;
		IL_029f:
		result = 0L;
		return ParsingStatus.Failed;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ParsingStatus TryParseInt64(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out long result)
	{
		if ((styles & ~NumberStyles.Integer) == 0)
		{
			return TryParseInt64IntegerStyle(value, styles, info, out result);
		}
		if ((styles & NumberStyles.AllowHexSpecifier) != 0)
		{
			result = 0L;
			return TryParseUInt64HexNumberStyle(value, styles, out Unsafe.As<long, ulong>(ref result));
		}
		return TryParseInt64Number(value, styles, info, out result);
	}

	private unsafe static ParsingStatus TryParseInt64Number(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out long result)
	{
		result = 0L;
		byte* digits = stackalloc byte[20];
		NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, digits, 20);
		if (!TryStringToNumber(value, styles, ref number, info))
		{
			return ParsingStatus.Failed;
		}
		if (!TryNumberToInt64(ref number, ref result))
		{
			return ParsingStatus.Overflow;
		}
		return ParsingStatus.OK;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ParsingStatus TryParseUInt32(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out uint result)
	{
		if ((styles & ~NumberStyles.Integer) == 0)
		{
			return TryParseUInt32IntegerStyle(value, styles, info, out result);
		}
		if ((styles & NumberStyles.AllowHexSpecifier) != 0)
		{
			return TryParseUInt32HexNumberStyle(value, styles, out result);
		}
		return TryParseUInt32Number(value, styles, info, out result);
	}

	private unsafe static ParsingStatus TryParseUInt32Number(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out uint result)
	{
		result = 0u;
		byte* digits = stackalloc byte[11];
		NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, digits, 11);
		if (!TryStringToNumber(value, styles, ref number, info))
		{
			return ParsingStatus.Failed;
		}
		if (!TryNumberToUInt32(ref number, ref result))
		{
			return ParsingStatus.Overflow;
		}
		return ParsingStatus.OK;
	}

	internal static ParsingStatus TryParseUInt32IntegerStyle(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out uint result)
	{
		int i;
		int num;
		if (!value.IsEmpty)
		{
			i = 0;
			num = value[0];
			if ((styles & NumberStyles.AllowLeadingWhite) == 0 || !IsWhite(num))
			{
				goto IL_0048;
			}
			while (true)
			{
				i++;
				if ((uint)i >= (uint)value.Length)
				{
					break;
				}
				num = value[i];
				if (IsWhite(num))
				{
					continue;
				}
				goto IL_0048;
			}
		}
		goto IL_0281;
		IL_01a7:
		int num2 = num - 48;
		i++;
		int num3 = 0;
		while (num3 < 8)
		{
			if ((uint)i >= (uint)value.Length)
			{
				goto IL_0275;
			}
			num = value[i];
			if (IsDigit(num))
			{
				i++;
				num2 = 10 * num2 + num - 48;
				num3++;
				continue;
			}
			goto IL_0293;
		}
		if ((uint)i >= (uint)value.Length)
		{
			goto IL_0275;
		}
		num = value[i];
		bool flag;
		if (IsDigit(num))
		{
			i++;
			flag = flag || (uint)num2 > 429496729u || (num2 == 429496729 && num > 53);
			num2 = num2 * 10 + num - 48;
			if ((uint)i >= (uint)value.Length)
			{
				goto IL_0275;
			}
			num = value[i];
			while (IsDigit(num))
			{
				flag = true;
				i++;
				if ((uint)i < (uint)value.Length)
				{
					num = value[i];
					continue;
				}
				goto IL_0289;
			}
		}
		goto IL_0293;
		IL_0278:
		result = (uint)num2;
		return ParsingStatus.OK;
		IL_019c:
		if (IsDigit(num))
		{
			goto IL_01a7;
		}
		flag = false;
		goto IL_0293;
		IL_0275:
		if (!flag)
		{
			goto IL_0278;
		}
		goto IL_0289;
		IL_0048:
		flag = false;
		if ((styles & NumberStyles.AllowLeadingSign) != 0)
		{
			if (info.HasInvariantNumberSigns)
			{
				if (num == 43)
				{
					i++;
					if ((uint)i >= (uint)value.Length)
					{
						goto IL_0281;
					}
					num = value[i];
				}
				else if (num == 45)
				{
					flag = true;
					i++;
					if ((uint)i >= (uint)value.Length)
					{
						goto IL_0281;
					}
					num = value[i];
				}
			}
			else if (info.AllowHyphenDuringParsing && num == 45)
			{
				flag = true;
				i++;
				if ((uint)i >= (uint)value.Length)
				{
					goto IL_0281;
				}
				num = value[i];
			}
			else
			{
				value = value.Slice(i);
				i = 0;
				string positiveSign = info.PositiveSign;
				string negativeSign = info.NegativeSign;
				if (!string.IsNullOrEmpty(positiveSign) && value.StartsWith(positiveSign))
				{
					i += positiveSign.Length;
					if ((uint)i >= (uint)value.Length)
					{
						goto IL_0281;
					}
					num = value[i];
				}
				else if (!string.IsNullOrEmpty(negativeSign) && value.StartsWith(negativeSign))
				{
					flag = true;
					i += negativeSign.Length;
					if ((uint)i >= (uint)value.Length)
					{
						goto IL_0281;
					}
					num = value[i];
				}
			}
		}
		num2 = 0;
		if (IsDigit(num))
		{
			if (num != 48)
			{
				goto IL_01a7;
			}
			while (true)
			{
				i++;
				if ((uint)i >= (uint)value.Length)
				{
					break;
				}
				num = value[i];
				if (num == 48)
				{
					continue;
				}
				goto IL_019c;
			}
			goto IL_0278;
		}
		goto IL_0281;
		IL_0293:
		if (IsWhite(num))
		{
			if ((styles & NumberStyles.AllowTrailingWhite) == 0)
			{
				goto IL_0281;
			}
			for (i++; i < value.Length && IsWhite(value[i]); i++)
			{
			}
			if ((uint)i >= (uint)value.Length)
			{
				goto IL_0275;
			}
		}
		if (TrailingZeros(value, i))
		{
			goto IL_0275;
		}
		goto IL_0281;
		IL_0289:
		result = 0u;
		return ParsingStatus.Overflow;
		IL_0281:
		result = 0u;
		return ParsingStatus.Failed;
	}

	internal static ParsingStatus TryParseUInt32HexNumberStyle(ReadOnlySpan<char> value, NumberStyles styles, out uint result)
	{
		int i;
		int num;
		if (!value.IsEmpty)
		{
			i = 0;
			num = value[0];
			if ((styles & NumberStyles.AllowLeadingWhite) == 0 || !IsWhite(num))
			{
				goto IL_0048;
			}
			while (true)
			{
				i++;
				if ((uint)i >= (uint)value.Length)
				{
					break;
				}
				num = value[i];
				if (IsWhite(num))
				{
					continue;
				}
				goto IL_0048;
			}
		}
		goto IL_011f;
		IL_0087:
		uint num2 = (uint)HexConverter.FromChar(num);
		i++;
		int num3 = 0;
		while (num3 < 7)
		{
			if ((uint)i >= (uint)value.Length)
			{
				goto IL_0116;
			}
			num = value[i];
			uint num4 = (uint)HexConverter.FromChar(num);
			if (num4 != 255)
			{
				i++;
				num2 = 16 * num2 + num4;
				num3++;
				continue;
			}
			goto IL_012f;
		}
		if ((uint)i >= (uint)value.Length)
		{
			goto IL_0116;
		}
		num = value[i];
		if (HexConverter.IsHexChar(num))
		{
			while (true)
			{
				i++;
				if ((uint)i >= (uint)value.Length)
				{
					break;
				}
				num = value[i];
				if (HexConverter.IsHexChar(num))
				{
					continue;
				}
				goto IL_010f;
			}
			goto IL_0127;
		}
		goto IL_012f;
		IL_0127:
		result = 0u;
		return ParsingStatus.Overflow;
		IL_0113:
		bool flag;
		if (!flag)
		{
			goto IL_0116;
		}
		goto IL_0127;
		IL_011f:
		result = 0u;
		return ParsingStatus.Failed;
		IL_0048:
		flag = false;
		num2 = 0u;
		if (HexConverter.IsHexChar(num))
		{
			if (num != 48)
			{
				goto IL_0087;
			}
			while (true)
			{
				i++;
				if ((uint)i >= (uint)value.Length)
				{
					break;
				}
				num = value[i];
				if (num == 48)
				{
					continue;
				}
				goto IL_007c;
			}
			goto IL_0116;
		}
		goto IL_011f;
		IL_010f:
		flag = true;
		goto IL_012f;
		IL_012f:
		if (IsWhite(num))
		{
			if ((styles & NumberStyles.AllowTrailingWhite) == 0)
			{
				goto IL_011f;
			}
			for (i++; i < value.Length && IsWhite(value[i]); i++)
			{
			}
			if ((uint)i >= (uint)value.Length)
			{
				goto IL_0113;
			}
		}
		if (TrailingZeros(value, i))
		{
			goto IL_0113;
		}
		goto IL_011f;
		IL_0116:
		result = num2;
		return ParsingStatus.OK;
		IL_007c:
		if (HexConverter.IsHexChar(num))
		{
			goto IL_0087;
		}
		goto IL_012f;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ParsingStatus TryParseUInt64(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out ulong result)
	{
		if ((styles & ~NumberStyles.Integer) == 0)
		{
			return TryParseUInt64IntegerStyle(value, styles, info, out result);
		}
		if ((styles & NumberStyles.AllowHexSpecifier) != 0)
		{
			return TryParseUInt64HexNumberStyle(value, styles, out result);
		}
		return TryParseUInt64Number(value, styles, info, out result);
	}

	private unsafe static ParsingStatus TryParseUInt64Number(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out ulong result)
	{
		result = 0uL;
		byte* digits = stackalloc byte[21];
		NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, digits, 21);
		if (!TryStringToNumber(value, styles, ref number, info))
		{
			return ParsingStatus.Failed;
		}
		if (!TryNumberToUInt64(ref number, ref result))
		{
			return ParsingStatus.Overflow;
		}
		return ParsingStatus.OK;
	}

	internal static ParsingStatus TryParseUInt64IntegerStyle(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out ulong result)
	{
		int i;
		int num;
		if (!value.IsEmpty)
		{
			i = 0;
			num = value[0];
			if ((styles & NumberStyles.AllowLeadingWhite) == 0 || !IsWhite(num))
			{
				goto IL_0048;
			}
			while (true)
			{
				i++;
				if ((uint)i >= (uint)value.Length)
				{
					break;
				}
				num = value[i];
				if (IsWhite(num))
				{
					continue;
				}
				goto IL_0048;
			}
		}
		goto IL_0295;
		IL_01a8:
		long num2 = num - 48;
		i++;
		int num3 = 0;
		while (num3 < 18)
		{
			if ((uint)i >= (uint)value.Length)
			{
				goto IL_0289;
			}
			num = value[i];
			if (IsDigit(num))
			{
				i++;
				num2 = 10 * num2 + num - 48;
				num3++;
				continue;
			}
			goto IL_02a9;
		}
		if ((uint)i >= (uint)value.Length)
		{
			goto IL_0289;
		}
		num = value[i];
		bool flag;
		if (IsDigit(num))
		{
			i++;
			flag = flag || (ulong)num2 > 1844674407370955161uL || (num2 == 1844674407370955161L && num > 53);
			num2 = num2 * 10 + num - 48;
			if ((uint)i >= (uint)value.Length)
			{
				goto IL_0289;
			}
			num = value[i];
			while (IsDigit(num))
			{
				flag = true;
				i++;
				if ((uint)i < (uint)value.Length)
				{
					num = value[i];
					continue;
				}
				goto IL_029e;
			}
		}
		goto IL_02a9;
		IL_028c:
		result = (ulong)num2;
		return ParsingStatus.OK;
		IL_019d:
		if (IsDigit(num))
		{
			goto IL_01a8;
		}
		flag = false;
		goto IL_02a9;
		IL_0289:
		if (!flag)
		{
			goto IL_028c;
		}
		goto IL_029e;
		IL_0048:
		flag = false;
		if ((styles & NumberStyles.AllowLeadingSign) != 0)
		{
			if (info.HasInvariantNumberSigns)
			{
				if (num == 43)
				{
					i++;
					if ((uint)i >= (uint)value.Length)
					{
						goto IL_0295;
					}
					num = value[i];
				}
				else if (num == 45)
				{
					flag = true;
					i++;
					if ((uint)i >= (uint)value.Length)
					{
						goto IL_0295;
					}
					num = value[i];
				}
			}
			else if (info.AllowHyphenDuringParsing && num == 45)
			{
				flag = true;
				i++;
				if ((uint)i >= (uint)value.Length)
				{
					goto IL_0295;
				}
				num = value[i];
			}
			else
			{
				value = value.Slice(i);
				i = 0;
				string positiveSign = info.PositiveSign;
				string negativeSign = info.NegativeSign;
				if (!string.IsNullOrEmpty(positiveSign) && value.StartsWith(positiveSign))
				{
					i += positiveSign.Length;
					if ((uint)i >= (uint)value.Length)
					{
						goto IL_0295;
					}
					num = value[i];
				}
				else if (!string.IsNullOrEmpty(negativeSign) && value.StartsWith(negativeSign))
				{
					flag = true;
					i += negativeSign.Length;
					if ((uint)i >= (uint)value.Length)
					{
						goto IL_0295;
					}
					num = value[i];
				}
			}
		}
		num2 = 0L;
		if (IsDigit(num))
		{
			if (num != 48)
			{
				goto IL_01a8;
			}
			while (true)
			{
				i++;
				if ((uint)i >= (uint)value.Length)
				{
					break;
				}
				num = value[i];
				if (num == 48)
				{
					continue;
				}
				goto IL_019d;
			}
			goto IL_028c;
		}
		goto IL_0295;
		IL_02a9:
		if (IsWhite(num))
		{
			if ((styles & NumberStyles.AllowTrailingWhite) == 0)
			{
				goto IL_0295;
			}
			for (i++; i < value.Length && IsWhite(value[i]); i++)
			{
			}
			if ((uint)i >= (uint)value.Length)
			{
				goto IL_0289;
			}
		}
		if (TrailingZeros(value, i))
		{
			goto IL_0289;
		}
		goto IL_0295;
		IL_029e:
		result = 0uL;
		return ParsingStatus.Overflow;
		IL_0295:
		result = 0uL;
		return ParsingStatus.Failed;
	}

	private static ParsingStatus TryParseUInt64HexNumberStyle(ReadOnlySpan<char> value, NumberStyles styles, out ulong result)
	{
		int i;
		int num;
		if (!value.IsEmpty)
		{
			i = 0;
			num = value[0];
			if ((styles & NumberStyles.AllowLeadingWhite) == 0 || !IsWhite(num))
			{
				goto IL_0048;
			}
			while (true)
			{
				i++;
				if ((uint)i >= (uint)value.Length)
				{
					break;
				}
				num = value[i];
				if (IsWhite(num))
				{
					continue;
				}
				goto IL_0048;
			}
		}
		goto IL_0124;
		IL_0088:
		ulong num2 = (uint)HexConverter.FromChar(num);
		i++;
		int num3 = 0;
		while (num3 < 15)
		{
			if ((uint)i >= (uint)value.Length)
			{
				goto IL_011b;
			}
			num = value[i];
			uint num4 = (uint)HexConverter.FromChar(num);
			if (num4 != 255)
			{
				i++;
				num2 = 16 * num2 + num4;
				num3++;
				continue;
			}
			goto IL_0136;
		}
		if ((uint)i >= (uint)value.Length)
		{
			goto IL_011b;
		}
		num = value[i];
		if (HexConverter.IsHexChar(num))
		{
			while (true)
			{
				i++;
				if ((uint)i >= (uint)value.Length)
				{
					break;
				}
				num = value[i];
				if (HexConverter.IsHexChar(num))
				{
					continue;
				}
				goto IL_0114;
			}
			goto IL_012d;
		}
		goto IL_0136;
		IL_012d:
		result = 0uL;
		return ParsingStatus.Overflow;
		IL_0118:
		bool flag;
		if (!flag)
		{
			goto IL_011b;
		}
		goto IL_012d;
		IL_0124:
		result = 0uL;
		return ParsingStatus.Failed;
		IL_0048:
		flag = false;
		num2 = 0uL;
		if (HexConverter.IsHexChar(num))
		{
			if (num != 48)
			{
				goto IL_0088;
			}
			while (true)
			{
				i++;
				if ((uint)i >= (uint)value.Length)
				{
					break;
				}
				num = value[i];
				if (num == 48)
				{
					continue;
				}
				goto IL_007d;
			}
			goto IL_011b;
		}
		goto IL_0124;
		IL_0114:
		flag = true;
		goto IL_0136;
		IL_0136:
		if (IsWhite(num))
		{
			if ((styles & NumberStyles.AllowTrailingWhite) == 0)
			{
				goto IL_0124;
			}
			for (i++; i < value.Length && IsWhite(value[i]); i++)
			{
			}
			if ((uint)i >= (uint)value.Length)
			{
				goto IL_0118;
			}
		}
		if (TrailingZeros(value, i))
		{
			goto IL_0118;
		}
		goto IL_0124;
		IL_011b:
		result = num2;
		return ParsingStatus.OK;
		IL_007d:
		if (HexConverter.IsHexChar(num))
		{
			goto IL_0088;
		}
		goto IL_0136;
	}

	internal static decimal ParseDecimal(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info)
	{
		decimal result;
		ParsingStatus parsingStatus = TryParseDecimal(value, styles, info, out result);
		if (parsingStatus != 0)
		{
			ThrowOverflowOrFormatException(parsingStatus, TypeCode.Decimal);
		}
		return result;
	}

	internal unsafe static bool TryNumberToDecimal(ref NumberBuffer number, ref decimal value)
	{
		byte* ptr = number.GetDigitsPointer();
		int num = number.Scale;
		bool isNegative = number.IsNegative;
		uint num2 = *ptr;
		if (num2 == 0)
		{
			value = new decimal(0, 0, 0, isNegative, (byte)Math.Clamp(-num, 0, 28));
			return true;
		}
		if (num > 29)
		{
			return false;
		}
		ulong num3 = 0uL;
		while (num > -28)
		{
			num--;
			num3 *= 10;
			num3 += num2 - 48;
			num2 = *(++ptr);
			if (num3 >= 1844674407370955161L)
			{
				break;
			}
			if (num2 != 0)
			{
				continue;
			}
			while (num > 0)
			{
				num--;
				num3 *= 10;
				if (num3 >= 1844674407370955161L)
				{
					break;
				}
			}
			break;
		}
		uint num4 = 0u;
		while ((num > 0 || (num2 != 0 && num > -28)) && (num4 < 429496729 || (num4 == 429496729 && (num3 < 11068046444225730969uL || (num3 == 11068046444225730969uL && num2 <= 53)))))
		{
			ulong num5 = (ulong)(uint)num3 * 10uL;
			ulong num6 = (ulong)((long)(uint)(num3 >> 32) * 10L) + (num5 >> 32);
			num3 = (uint)num5 + (num6 << 32);
			num4 = (uint)(int)(num6 >> 32) + num4 * 10;
			if (num2 != 0)
			{
				num2 -= 48;
				num3 += num2;
				if (num3 < num2)
				{
					num4++;
				}
				num2 = *(++ptr);
			}
			num--;
		}
		if (num2 >= 53)
		{
			if (num2 == 53 && (num3 & 1) == 0L)
			{
				num2 = *(++ptr);
				bool flag = !number.HasNonZeroTail;
				while (num2 != 0 && flag)
				{
					flag = flag && num2 == 48;
					num2 = *(++ptr);
				}
				if (flag)
				{
					goto IL_01a8;
				}
			}
			if (++num3 == 0L && ++num4 == 0)
			{
				num3 = 11068046444225730970uL;
				num4 = 429496729u;
				num++;
			}
		}
		goto IL_01a8;
		IL_01a8:
		if (num > 0)
		{
			return false;
		}
		if (num <= -29)
		{
			value = new decimal(0, 0, 0, isNegative, 28);
		}
		else
		{
			value = new decimal((int)num3, (int)(num3 >> 32), (int)num4, isNegative, (byte)(-num));
		}
		return true;
	}

	internal static double ParseDouble(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info)
	{
		if (!TryParseDouble(value, styles, info, out var result))
		{
			ThrowOverflowOrFormatException(ParsingStatus.Failed);
		}
		return result;
	}

	internal static float ParseSingle(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info)
	{
		if (!TryParseSingle(value, styles, info, out var result))
		{
			ThrowOverflowOrFormatException(ParsingStatus.Failed);
		}
		return result;
	}

	internal static Half ParseHalf(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info)
	{
		if (!TryParseHalf(value, styles, info, out var result))
		{
			ThrowOverflowOrFormatException(ParsingStatus.Failed);
		}
		return result;
	}

	internal unsafe static ParsingStatus TryParseDecimal(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out decimal result)
	{
		byte* digits = stackalloc byte[31];
		NumberBuffer number = new NumberBuffer(NumberBufferKind.Decimal, digits, 31);
		result = default(decimal);
		if (!TryStringToNumber(value, styles, ref number, info))
		{
			return ParsingStatus.Failed;
		}
		if (!TryNumberToDecimal(ref number, ref result))
		{
			return ParsingStatus.Overflow;
		}
		return ParsingStatus.OK;
	}

	internal static bool SpanStartsWith(ReadOnlySpan<char> span, char c)
	{
		if (!span.IsEmpty)
		{
			return span[0] == c;
		}
		return false;
	}

	internal unsafe static bool TryParseDouble(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out double result)
	{
		byte* digits = stackalloc byte[769];
		NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, digits, 769);
		if (!TryStringToNumber(value, styles, ref number, info))
		{
			ReadOnlySpan<char> span = value.Trim();
			if (span.EqualsOrdinalIgnoreCase(info.PositiveInfinitySymbol))
			{
				result = double.PositiveInfinity;
			}
			else if (span.EqualsOrdinalIgnoreCase(info.NegativeInfinitySymbol))
			{
				result = double.NegativeInfinity;
			}
			else if (span.EqualsOrdinalIgnoreCase(info.NaNSymbol))
			{
				result = double.NaN;
			}
			else if (span.StartsWith(info.PositiveSign, StringComparison.OrdinalIgnoreCase))
			{
				span = span.Slice(info.PositiveSign.Length);
				if (span.EqualsOrdinalIgnoreCase(info.PositiveInfinitySymbol))
				{
					result = double.PositiveInfinity;
				}
				else
				{
					if (!span.EqualsOrdinalIgnoreCase(info.NaNSymbol))
					{
						result = 0.0;
						return false;
					}
					result = double.NaN;
				}
			}
			else
			{
				if ((!span.StartsWith(info.NegativeSign, StringComparison.OrdinalIgnoreCase) || !span.Slice(info.NegativeSign.Length).EqualsOrdinalIgnoreCase(info.NaNSymbol)) && (!info.AllowHyphenDuringParsing || !SpanStartsWith(span, '-') || !span.Slice(1).EqualsOrdinalIgnoreCase(info.NaNSymbol)))
				{
					result = 0.0;
					return false;
				}
				result = double.NaN;
			}
		}
		else
		{
			result = NumberToDouble(ref number);
		}
		return true;
	}

	internal unsafe static bool TryParseHalf(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out Half result)
	{
		byte* digits = stackalloc byte[21];
		NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, digits, 21);
		if (!TryStringToNumber(value, styles, ref number, info))
		{
			ReadOnlySpan<char> span = value.Trim();
			if (span.EqualsOrdinalIgnoreCase(info.PositiveInfinitySymbol))
			{
				result = Half.PositiveInfinity;
			}
			else if (span.EqualsOrdinalIgnoreCase(info.NegativeInfinitySymbol))
			{
				result = Half.NegativeInfinity;
			}
			else if (span.EqualsOrdinalIgnoreCase(info.NaNSymbol))
			{
				result = Half.NaN;
			}
			else if (span.StartsWith(info.PositiveSign, StringComparison.OrdinalIgnoreCase))
			{
				span = span.Slice(info.PositiveSign.Length);
				if (!info.PositiveInfinitySymbol.StartsWith(info.PositiveSign, StringComparison.OrdinalIgnoreCase) && span.EqualsOrdinalIgnoreCase(info.PositiveInfinitySymbol))
				{
					result = Half.PositiveInfinity;
				}
				else
				{
					if (info.NaNSymbol.StartsWith(info.PositiveSign, StringComparison.OrdinalIgnoreCase) || !span.EqualsOrdinalIgnoreCase(info.NaNSymbol))
					{
						result = (Half)0f;
						return false;
					}
					result = Half.NaN;
				}
			}
			else if (span.StartsWith(info.NegativeSign, StringComparison.OrdinalIgnoreCase) && !info.NaNSymbol.StartsWith(info.NegativeSign, StringComparison.OrdinalIgnoreCase) && span.Slice(info.NegativeSign.Length).EqualsOrdinalIgnoreCase(info.NaNSymbol))
			{
				result = Half.NaN;
			}
			else
			{
				if (!info.AllowHyphenDuringParsing || !SpanStartsWith(span, '-') || info.NaNSymbol.StartsWith(info.NegativeSign, StringComparison.OrdinalIgnoreCase) || info.NaNSymbol.StartsWith('-') || !span.Slice(1).EqualsOrdinalIgnoreCase(info.NaNSymbol))
				{
					result = (Half)0f;
					return false;
				}
				result = Half.NaN;
			}
		}
		else
		{
			result = NumberToHalf(ref number);
		}
		return true;
	}

	internal unsafe static bool TryParseSingle(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out float result)
	{
		byte* digits = stackalloc byte[114];
		NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, digits, 114);
		if (!TryStringToNumber(value, styles, ref number, info))
		{
			ReadOnlySpan<char> span = value.Trim();
			if (span.EqualsOrdinalIgnoreCase(info.PositiveInfinitySymbol))
			{
				result = float.PositiveInfinity;
			}
			else if (span.EqualsOrdinalIgnoreCase(info.NegativeInfinitySymbol))
			{
				result = float.NegativeInfinity;
			}
			else if (span.EqualsOrdinalIgnoreCase(info.NaNSymbol))
			{
				result = float.NaN;
			}
			else if (span.StartsWith(info.PositiveSign, StringComparison.OrdinalIgnoreCase))
			{
				span = span.Slice(info.PositiveSign.Length);
				if (!info.PositiveInfinitySymbol.StartsWith(info.PositiveSign, StringComparison.OrdinalIgnoreCase) && span.EqualsOrdinalIgnoreCase(info.PositiveInfinitySymbol))
				{
					result = float.PositiveInfinity;
				}
				else
				{
					if (info.NaNSymbol.StartsWith(info.PositiveSign, StringComparison.OrdinalIgnoreCase) || !span.EqualsOrdinalIgnoreCase(info.NaNSymbol))
					{
						result = 0f;
						return false;
					}
					result = float.NaN;
				}
			}
			else if (span.StartsWith(info.NegativeSign, StringComparison.OrdinalIgnoreCase) && !info.NaNSymbol.StartsWith(info.NegativeSign, StringComparison.OrdinalIgnoreCase) && span.Slice(info.NegativeSign.Length).EqualsOrdinalIgnoreCase(info.NaNSymbol))
			{
				result = float.NaN;
			}
			else
			{
				if (!info.AllowHyphenDuringParsing || !SpanStartsWith(span, '-') || info.NaNSymbol.StartsWith(info.NegativeSign, StringComparison.OrdinalIgnoreCase) || info.NaNSymbol.StartsWith('-') || !span.Slice(1).EqualsOrdinalIgnoreCase(info.NaNSymbol))
				{
					result = 0f;
					return false;
				}
				result = float.NaN;
			}
		}
		else
		{
			result = NumberToSingle(ref number);
		}
		return true;
	}

	internal unsafe static bool TryStringToNumber(ReadOnlySpan<char> value, NumberStyles styles, ref NumberBuffer number, NumberFormatInfo info)
	{
		fixed (char* ptr = &MemoryMarshal.GetReference(value))
		{
			char* str = ptr;
			if (!TryParseNumber(ref str, str + value.Length, styles, ref number, info) || ((int)(str - ptr) < value.Length && !TrailingZeros(value, (int)(str - ptr))))
			{
				return false;
			}
		}
		return true;
	}

	private static bool TrailingZeros(ReadOnlySpan<char> value, int index)
	{
		for (int i = index; (uint)i < (uint)value.Length; i++)
		{
			if (value[i] != 0)
			{
				return false;
			}
		}
		return true;
	}

	private static bool IsSpaceReplacingChar(char c)
	{
		if (c != '\u00a0')
		{
			return c == '\u202f';
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static char* MatchNegativeSignChars(char* p, char* pEnd, NumberFormatInfo info)
	{
		char* ptr = MatchChars(p, pEnd, info.NegativeSign);
		if (ptr == null && info.AllowHyphenDuringParsing && p < pEnd && *p == '-')
		{
			ptr = p + 1;
		}
		return ptr;
	}

	private unsafe static char* MatchChars(char* p, char* pEnd, string value)
	{
		fixed (char* ptr = value)
		{
			char* ptr2 = ptr;
			if (*ptr2 != 0)
			{
				while (true)
				{
					char c = ((p < pEnd) ? (*p) : '\0');
					if (c != *ptr2 && (!IsSpaceReplacingChar(*ptr2) || c != ' '))
					{
						break;
					}
					p++;
					ptr2++;
					if (*ptr2 == '\0')
					{
						return p;
					}
				}
			}
		}
		return null;
	}

	private static bool IsWhite(int ch)
	{
		if (ch != 32 && (uint)(ch - 9) > 4u)
		{
			return false;
		}
		return true;
	}

	private static bool IsDigit(int ch)
	{
		return (uint)(ch - 48) <= 9u;
	}

	[DoesNotReturn]
	internal static void ThrowOverflowOrFormatException(ParsingStatus status, TypeCode type = TypeCode.Empty)
	{
		throw GetException(status, type);
	}

	[DoesNotReturn]
	internal static void ThrowOverflowException(TypeCode type)
	{
		throw GetException(ParsingStatus.Overflow, type);
	}

	private static Exception GetException(ParsingStatus status, TypeCode type)
	{
		if (status == ParsingStatus.Failed)
		{
			return new FormatException(SR.Format_InvalidString);
		}
		return new OverflowException(type switch
		{
			TypeCode.SByte => SR.Overflow_SByte, 
			TypeCode.Byte => SR.Overflow_Byte, 
			TypeCode.Int16 => SR.Overflow_Int16, 
			TypeCode.UInt16 => SR.Overflow_UInt16, 
			TypeCode.Int32 => SR.Overflow_Int32, 
			TypeCode.UInt32 => SR.Overflow_UInt32, 
			TypeCode.Int64 => SR.Overflow_Int64, 
			TypeCode.UInt64 => SR.Overflow_UInt64, 
			_ => SR.Overflow_Decimal, 
		});
	}

	internal static double NumberToDouble(ref NumberBuffer number)
	{
		double num;
		if (number.DigitsCount == 0 || number.Scale < -324)
		{
			num = 0.0;
		}
		else if (number.Scale > 309)
		{
			num = double.PositiveInfinity;
		}
		else
		{
			ulong value = NumberToDoubleFloatingPointBits(ref number, in FloatingPointInfo.Double);
			num = BitConverter.UInt64BitsToDouble(value);
		}
		if (!number.IsNegative)
		{
			return num;
		}
		return 0.0 - num;
	}

	internal static Half NumberToHalf(ref NumberBuffer number)
	{
		Half half;
		if (number.DigitsCount == 0 || number.Scale < -8)
		{
			half = default(Half);
		}
		else if (number.Scale > 5)
		{
			half = Half.PositiveInfinity;
		}
		else
		{
			ushort value = NumberToHalfFloatingPointBits(ref number, in FloatingPointInfo.Half);
			half = new Half(value);
		}
		if (!number.IsNegative)
		{
			return half;
		}
		return Half.Negate(half);
	}

	internal static float NumberToSingle(ref NumberBuffer number)
	{
		float num;
		if (number.DigitsCount == 0 || number.Scale < -45)
		{
			num = 0f;
		}
		else if (number.Scale > 39)
		{
			num = float.PositiveInfinity;
		}
		else
		{
			uint value = NumberToSingleFloatingPointBits(ref number, in FloatingPointInfo.Single);
			num = BitConverter.UInt32BitsToSingle(value);
		}
		if (!number.IsNegative)
		{
			return num;
		}
		return 0f - num;
	}
}
