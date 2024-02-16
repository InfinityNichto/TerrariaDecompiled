using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Xml.Xsl;

internal static class XPathConvert
{
	private struct BigNumber
	{
		private uint _u0;

		private uint _u1;

		private uint _u2;

		private int _exp;

		private uint _error;

		private static readonly BigNumber[] s_tenPowersPos = new BigNumber[46]
		{
			new BigNumber(0u, 0u, 2684354560u, 4, 0u),
			new BigNumber(0u, 0u, 3355443200u, 7, 0u),
			new BigNumber(0u, 0u, 4194304000u, 10, 0u),
			new BigNumber(0u, 0u, 2621440000u, 14, 0u),
			new BigNumber(0u, 0u, 3276800000u, 17, 0u),
			new BigNumber(0u, 0u, 4096000000u, 20, 0u),
			new BigNumber(0u, 0u, 2560000000u, 24, 0u),
			new BigNumber(0u, 0u, 3200000000u, 27, 0u),
			new BigNumber(0u, 0u, 4000000000u, 30, 0u),
			new BigNumber(0u, 0u, 2500000000u, 34, 0u),
			new BigNumber(0u, 0u, 3125000000u, 37, 0u),
			new BigNumber(0u, 0u, 3906250000u, 40, 0u),
			new BigNumber(0u, 0u, 2441406250u, 44, 0u),
			new BigNumber(0u, 2147483648u, 3051757812u, 47, 0u),
			new BigNumber(0u, 2684354560u, 3814697265u, 50, 0u),
			new BigNumber(0u, 67108864u, 2384185791u, 54, 0u),
			new BigNumber(0u, 3305111552u, 2980232238u, 57, 0u),
			new BigNumber(0u, 1983905792u, 3725290298u, 60, 0u),
			new BigNumber(0u, 2313682944u, 2328306436u, 64, 0u),
			new BigNumber(0u, 2892103680u, 2910383045u, 67, 0u),
			new BigNumber(0u, 393904128u, 3637978807u, 70, 0u),
			new BigNumber(0u, 1856802816u, 2273736754u, 74, 0u),
			new BigNumber(0u, 173519872u, 2842170943u, 77, 0u),
			new BigNumber(0u, 3438125312u, 3552713678u, 80, 0u),
			new BigNumber(0u, 1075086496u, 2220446049u, 84, 0u),
			new BigNumber(0u, 2417599944u, 2775557561u, 87, 0u),
			new BigNumber(0u, 4095741754u, 3469446951u, 90, 0u),
			new BigNumber(1073741824u, 4170451332u, 2168404344u, 94, 0u),
			new BigNumber(1342177280u, 918096869u, 2710505431u, 97, 0u),
			new BigNumber(2751463424u, 73879262u, 3388131789u, 100, 0u),
			new BigNumber(1291845632u, 1166090902u, 4235164736u, 103, 0u),
			new BigNumber(4028628992u, 728806813u, 2646977960u, 107, 0u),
			new BigNumber(1019177842u, 4291798741u, 3262652233u, 213, 1u),
			new BigNumber(3318737231u, 3315274914u, 4021529366u, 319, 1u),
			new BigNumber(3329176428u, 2162789599u, 2478458825u, 426, 1u),
			new BigNumber(1467717739u, 2145785770u, 3054936363u, 532, 1u),
			new BigNumber(2243682900u, 958879082u, 3765499789u, 638, 1u),
			new BigNumber(2193451889u, 3812411695u, 2320668415u, 745, 1u),
			new BigNumber(3720056860u, 2650398349u, 2860444667u, 851, 1u),
			new BigNumber(1937977068u, 1550462860u, 3525770265u, 957, 1u),
			new BigNumber(3869316483u, 4073513845u, 2172923689u, 1064, 1u),
			new BigNumber(1589582007u, 3683650258u, 2678335232u, 1170, 1u),
			new BigNumber(271056885u, 2935532055u, 3301303056u, 1276, 1u),
			new BigNumber(3051704177u, 3920665688u, 4069170183u, 1382, 1u),
			new BigNumber(2817170568u, 3958895571u, 2507819745u, 1489, 1u),
			new BigNumber(2113145460u, 127246946u, 3091126492u, 1595, 1u)
		};

		private static readonly BigNumber[] s_tenPowersNeg = new BigNumber[46]
		{
			new BigNumber(3435973837u, 3435973836u, 3435973836u, -3, 1u),
			new BigNumber(1030792151u, 1889785610u, 2748779069u, -6, 1u),
			new BigNumber(1683627180u, 2370821947u, 2199023255u, -9, 1u),
			new BigNumber(3552796947u, 3793315115u, 3518437208u, -13, 1u),
			new BigNumber(265257180u, 457671715u, 2814749767u, -16, 1u),
			new BigNumber(2789186122u, 2943117749u, 2251799813u, -19, 1u),
			new BigNumber(1026723958u, 3849994940u, 3602879701u, -23, 1u),
			new BigNumber(4257353003u, 2221002492u, 2882303761u, -26, 1u),
			new BigNumber(828902025u, 917808535u, 2305843009u, -29, 1u),
			new BigNumber(3044230158u, 3186480574u, 3689348814u, -33, 1u),
			new BigNumber(4153371045u, 3408177918u, 2951479051u, -36, 1u),
			new BigNumber(4181690295u, 1867548875u, 2361183241u, -39, 1u),
			new BigNumber(677750258u, 1270091283u, 3777893186u, -43, 1u),
			new BigNumber(1401193666u, 157079567u, 3022314549u, -46, 1u),
			new BigNumber(261961473u, 984657113u, 2417851639u, -49, 1u),
			new BigNumber(1278131816u, 3293438299u, 3868562622u, -53, 1u),
			new BigNumber(163511994u, 916763721u, 3094850098u, -56, 1u),
			new BigNumber(989803054u, 2451397895u, 2475880078u, -59, 1u),
			new BigNumber(724691428u, 3063243173u, 3961408125u, -63, 1u),
			new BigNumber(2297740061u, 2450594538u, 3169126500u, -66, 1u),
			new BigNumber(3556178967u, 1960475630u, 2535301200u, -69, 1u),
			new BigNumber(1394919051u, 3136761009u, 4056481920u, -73, 1u),
			new BigNumber(1974928700u, 2509408807u, 3245185536u, -76, 1u),
			new BigNumber(3297929878u, 1148533586u, 2596148429u, -79, 1u),
			new BigNumber(981720510u, 3555640657u, 4153837486u, -83, 1u),
			new BigNumber(2503363326u, 1985519066u, 3323069989u, -86, 1u),
			new BigNumber(2002690661u, 2447408712u, 2658455991u, -89, 1u),
			new BigNumber(2345311598u, 2197867021u, 4253529586u, -93, 1u),
			new BigNumber(158262360u, 899300158u, 3402823669u, -96, 1u),
			new BigNumber(2703590266u, 1578433585u, 2722258935u, -99, 1u),
			new BigNumber(2162872213u, 1262746868u, 2177807148u, -102, 1u),
			new BigNumber(1742608622u, 1161401530u, 3484491437u, -106, 1u),
			new BigNumber(1059297495u, 2772036005u, 2826955303u, -212, 1u),
			new BigNumber(299617026u, 4252324763u, 2293498615u, -318, 1u),
			new BigNumber(2893853687u, 1690100896u, 3721414268u, -425, 1u),
			new BigNumber(1508712807u, 3681788051u, 3019169939u, -531, 1u),
			new BigNumber(2070087331u, 1411632134u, 2449441655u, -637, 1u),
			new BigNumber(2767765334u, 1244745405u, 3974446316u, -744, 1u),
			new BigNumber(4203811158u, 1668946233u, 3224453925u, -850, 1u),
			new BigNumber(1323526137u, 2204812663u, 2615987810u, -956, 1u),
			new BigNumber(2300620953u, 1199716560u, 4244682903u, -1063, 1u),
			new BigNumber(9598332u, 1190350717u, 3443695891u, -1169, 1u),
			new BigNumber(2296094720u, 2971338839u, 2793858024u, -1275, 1u),
			new BigNumber(441364487u, 1073506470u, 2266646913u, -1381, 1u),
			new BigNumber(2227594191u, 3053929028u, 3677844889u, -1488, 1u),
			new BigNumber(1642812130u, 2030073654u, 2983822260u, -1594, 1u)
		};

		public uint Error => _error;

		private bool IsZero
		{
			get
			{
				if (_u2 == 0 && _u1 == 0)
				{
					return _u0 == 0;
				}
				return false;
			}
		}

		public BigNumber(uint u0, uint u1, uint u2, int exp, uint error)
		{
			_u0 = u0;
			_u1 = u1;
			_u2 = u2;
			_exp = exp;
			_error = error;
		}

		public BigNumber(FloatingDecimal dec)
		{
			int num = 0;
			int exponent = dec.Exponent;
			int mantissaSize = dec.MantissaSize;
			_u2 = (uint)(dec[num] << 28);
			_u1 = 0u;
			_u0 = 0u;
			_exp = 4;
			_error = 0u;
			exponent--;
			Normalize();
			while (++num < mantissaSize)
			{
				uint num2 = MulTenAdd(dec[num]);
				exponent--;
				if (num2 != 0)
				{
					Round(num2);
					if (num < mantissaSize - 1)
					{
						_error++;
					}
					break;
				}
			}
			if (exponent != 0)
			{
				BigNumber[] array;
				if (exponent < 0)
				{
					array = s_tenPowersNeg;
					exponent = -exponent;
				}
				else
				{
					array = s_tenPowersPos;
				}
				int num3 = exponent & 0x1F;
				if (num3 > 0)
				{
					Mul(ref array[num3 - 1]);
				}
				num3 = (exponent >> 5) & 0xF;
				if (num3 > 0)
				{
					Mul(ref array[num3 + 30]);
				}
			}
		}

		private unsafe uint MulTenAdd(uint digit)
		{
			_exp += 3;
			uint* ptr = stackalloc uint[5];
			for (int i = 0; i < 5; i++)
			{
				ptr[i] = 0u;
			}
			if (digit != 0)
			{
				int num = 3 - (_exp >> 5);
				if (num < 0)
				{
					*ptr = 1u;
				}
				else
				{
					int num2 = _exp & 0x1F;
					if (num2 < 4)
					{
						ptr[num + 1] = digit >> num2;
						if (num2 > 0)
						{
							ptr[num] = digit << 32 - num2;
						}
					}
					else
					{
						ptr[num] = digit << 32 - num2;
					}
				}
			}
			ptr[1] += AddU(ref *ptr, _u0 << 30);
			ptr[2] += AddU(ref _u0, (_u0 >> 2) + (_u1 << 30));
			if (ptr[1] != 0)
			{
				ptr[2] += AddU(ref _u0, ptr[1]);
			}
			ptr[3] += AddU(ref _u1, (_u1 >> 2) + (_u2 << 30));
			if (ptr[2] != 0)
			{
				ptr[3] += AddU(ref _u1, ptr[2]);
			}
			ptr[4] = AddU(ref _u2, (_u2 >> 2) + ptr[3]);
			if (ptr[4] != 0)
			{
				*ptr = (*ptr >> 1) | (*ptr & 1u) | (_u0 << 31);
				_u0 = (_u0 >> 1) | (_u1 << 31);
				_u1 = (_u1 >> 1) | (_u2 << 31);
				_u2 = (_u2 >> 1) | 0x80000000u;
				_exp++;
			}
			return *ptr;
		}

		private void Round(uint uExtra)
		{
			if ((uExtra & 0x80000000u) == 0 || ((uExtra & 0x7FFFFFFF) == 0 && (_u0 & 1) == 0))
			{
				if (uExtra != 0)
				{
					_error++;
				}
				return;
			}
			_error++;
			if (AddU(ref _u0, 1u) != 0 && AddU(ref _u1, 1u) != 0 && AddU(ref _u2, 1u) != 0)
			{
				_u2 = 2147483648u;
				_exp++;
			}
		}

		private void Normalize()
		{
			if (_u2 == 0)
			{
				if (_u1 == 0)
				{
					if (_u0 == 0)
					{
						_exp = 0;
						return;
					}
					_u2 = _u0;
					_u0 = 0u;
					_exp -= 64;
				}
				else
				{
					_u2 = _u1;
					_u1 = _u0;
					_u0 = 0u;
					_exp -= 32;
				}
			}
			int num;
			if ((num = CbitZeroLeft(_u2)) != 0)
			{
				int num2 = 32 - num;
				_u2 = (_u2 << num) | (_u1 >> num2);
				_u1 = (_u1 << num) | (_u0 >> num2);
				_u0 <<= num;
				_exp -= num;
			}
		}

		private void Mul(ref BigNumber numOp)
		{
			uint num = 0u;
			uint u = 0u;
			uint u2 = 0u;
			uint u3 = 0u;
			uint u4 = 0u;
			uint u5 = 0u;
			uint uHi;
			uint u6;
			uint num2;
			uint num3;
			if ((u6 = _u0) != 0)
			{
				num2 = MulU(u6, numOp._u0, out uHi);
				num = num2;
				u = uHi;
				num2 = MulU(u6, numOp._u1, out uHi);
				num3 = AddU(ref u, num2);
				AddU(ref u2, uHi + num3);
				num2 = MulU(u6, numOp._u2, out uHi);
				num3 = AddU(ref u2, num2);
				AddU(ref u3, uHi + num3);
			}
			if ((u6 = _u1) != 0)
			{
				num2 = MulU(u6, numOp._u0, out uHi);
				num3 = AddU(ref u, num2);
				if (AddU(ref u2, uHi + num3) != 0 && AddU(ref u3, 1u) != 0)
				{
					AddU(ref u4, 1u);
				}
				num2 = MulU(u6, numOp._u1, out uHi);
				num3 = AddU(ref u2, num2);
				if (AddU(ref u3, uHi + num3) != 0)
				{
					AddU(ref u4, 1u);
				}
				num2 = MulU(u6, numOp._u2, out uHi);
				num3 = AddU(ref u3, num2);
				AddU(ref u4, uHi + num3);
			}
			u6 = _u2;
			num2 = MulU(u6, numOp._u0, out uHi);
			num3 = AddU(ref u2, num2);
			if (AddU(ref u3, uHi + num3) != 0 && AddU(ref u4, 1u) != 0)
			{
				AddU(ref u5, 1u);
			}
			num2 = MulU(u6, numOp._u1, out uHi);
			num3 = AddU(ref u3, num2);
			if (AddU(ref u4, uHi + num3) != 0)
			{
				AddU(ref u5, 1u);
			}
			num2 = MulU(u6, numOp._u2, out uHi);
			num3 = AddU(ref u4, num2);
			AddU(ref u5, uHi + num3);
			_exp += numOp._exp;
			_error += numOp._error;
			if ((u5 & 0x80000000u) == 0)
			{
				if ((u2 & 0x40000000u) != 0 && ((u2 & 0xBFFFFFFFu) != 0 || u != 0 || num != 0) && AddU(ref u2, 1073741824u) != 0 && AddU(ref u3, 1u) != 0 && AddU(ref u4, 1u) != 0)
				{
					AddU(ref u5, 1u);
					if ((u5 & 0x80000000u) != 0)
					{
						goto IL_0314;
					}
				}
				_u2 = (u5 << 1) | (u4 >> 31);
				_u1 = (u4 << 1) | (u3 >> 31);
				_u0 = (u3 << 1) | (u2 >> 31);
				_exp--;
				_error <<= 1;
				if ((u2 & 0x7FFFFFFFu) != 0 || u != 0 || num != 0)
				{
					_error++;
				}
				return;
			}
			if ((u2 & 0x80000000u) != 0 && ((u3 & (true ? 1u : 0u)) != 0 || (u2 & 0x7FFFFFFFu) != 0 || u != 0 || num != 0) && AddU(ref u3, 1u) != 0 && AddU(ref u4, 1u) != 0 && AddU(ref u5, 1u) != 0)
			{
				u5 = 2147483648u;
				_exp++;
			}
			goto IL_0314;
			IL_0314:
			_u2 = u5;
			_u1 = u4;
			_u0 = u3;
			if (u2 != 0 || u != 0 || num != 0)
			{
				_error++;
			}
		}

		public static explicit operator double(BigNumber bn)
		{
			int num = bn._exp + 1022;
			if (num >= 2047)
			{
				return double.PositiveInfinity;
			}
			uint u;
			uint u2;
			uint num2;
			if (num > 0)
			{
				u = (uint)(num << 20) | ((bn._u2 & 0x7FFFFFFF) >> 11);
				u2 = (bn._u2 << 21) | (bn._u1 >> 11);
				num2 = (bn._u1 << 21) | NotZero(bn._u0);
			}
			else if (num > -20)
			{
				int num3 = 12 - num;
				u = bn._u2 >> num3;
				u2 = (bn._u2 << 32 - num3) | (bn._u1 >> num3);
				num2 = (bn._u1 << 32 - num3) | NotZero(bn._u0);
			}
			else if (num == -20)
			{
				u = 0u;
				u2 = bn._u2;
				num2 = bn._u1 | ((bn._u0 != 0) ? 1u : 0u);
			}
			else if (num > -52)
			{
				int num4 = -num - 20;
				u = 0u;
				u2 = bn._u2 >> num4;
				num2 = (bn._u2 << 32 - num4) | NotZero(bn._u1) | NotZero(bn._u0);
			}
			else
			{
				if (num != -52)
				{
					return 0.0;
				}
				u = 0u;
				u2 = 0u;
				num2 = bn._u2 | NotZero(bn._u1) | NotZero(bn._u0);
			}
			if ((num2 & 0x80000000u) != 0 && ((num2 & 0x7FFFFFFFu) != 0 || (u2 & (true ? 1u : 0u)) != 0) && AddU(ref u2, 1u) != 0)
			{
				AddU(ref u, 1u);
			}
			return BitConverter.UInt64BitsToDouble(((ulong)u << 32) | u2);
		}

		private uint UMod1()
		{
			if (_exp <= 0)
			{
				return 0u;
			}
			uint result = _u2 >> 32 - _exp;
			_u2 &= (uint)(int.MaxValue >>> _exp - 1);
			Normalize();
			return result;
		}

		public void MakeUpperBound()
		{
			uint num = _error + 1 >> 1;
			if (num != 0 && AddU(ref _u0, num) != 0 && AddU(ref _u1, 1u) != 0 && AddU(ref _u2, 1u) != 0)
			{
				_u2 = 2147483648u;
				_u0 = (_u0 >> 1) + (_u0 & 1);
				_exp++;
			}
			_error = 0u;
		}

		public void MakeLowerBound()
		{
			uint num = _error + 1 >> 1;
			if (num != 0 && AddU(ref _u0, 0 - num) == 0 && AddU(ref _u1, uint.MaxValue) == 0)
			{
				AddU(ref _u2, uint.MaxValue);
				if ((0x80000000u & _u2) == 0)
				{
					Normalize();
				}
			}
			_error = 0u;
		}

		public static bool DblToRgbFast(double dbl, byte[] mantissa, out int exponent, out int mantissaSize)
		{
			int num = 0;
			uint num2 = DblHi(dbl);
			uint num3 = DblLo(dbl);
			int num4 = (int)((num2 >> 20) & 0x7FF);
			Unsafe.SkipInit(out BigNumber bigNumber);
			BigNumber bigNumber2;
			BigNumber bigNumber3;
			int num7;
			if (num4 > 0)
			{
				if (num4 >= 1023 && num4 <= 1075 && dbl == Math.Floor(dbl))
				{
					double num5 = dbl;
					int num6 = 0;
					if (num5 >= C10toN[num6 + 8])
					{
						num6 += 8;
					}
					if (num5 >= C10toN[num6 + 4])
					{
						num6 += 4;
					}
					if (num5 >= C10toN[num6 + 2])
					{
						num6 += 2;
					}
					if (num5 >= C10toN[num6 + 1])
					{
						num6++;
					}
					exponent = num6 + 1;
					num7 = 0;
					while (0.0 != num5)
					{
						byte b = (byte)(num5 / C10toN[num6]);
						num5 -= (double)(int)b * C10toN[num6];
						mantissa[num7++] = b;
						num6--;
					}
					mantissaSize = num7;
					goto IL_05a9;
				}
				bigNumber._u2 = 0x80000000u | ((num2 & 0xFFFFFF) << 11) | (num3 >> 21);
				bigNumber._u1 = num3 << 11;
				bigNumber._u0 = 0u;
				bigNumber._exp = num4 - 1022;
				bigNumber._error = 0u;
				bigNumber2 = bigNumber;
				bigNumber2._u1 |= 1024u;
				bigNumber3 = bigNumber;
				if (AddU(u2: (int.MinValue != (int)bigNumber3._u2 || bigNumber3._u1 != 0) ? 4294966272u : 4294966784u, u1: ref bigNumber3._u1) == 0)
				{
					AddU(ref bigNumber3._u2, uint.MaxValue);
					if ((0x80000000u & bigNumber3._u2) == 0)
					{
						bigNumber3.Normalize();
					}
				}
			}
			else
			{
				bigNumber._u2 = num2 & 0xFFFFFu;
				bigNumber._u1 = num3;
				bigNumber._u0 = 0u;
				bigNumber._exp = -1010;
				bigNumber._error = 0u;
				bigNumber2 = bigNumber;
				bigNumber2._u0 = 2147483648u;
				bigNumber3 = bigNumber2;
				if (AddU(ref bigNumber3._u1, uint.MaxValue) == 0)
				{
					AddU(ref bigNumber3._u2, uint.MaxValue);
				}
				bigNumber.Normalize();
				bigNumber2.Normalize();
				bigNumber3.Normalize();
			}
			if (bigNumber2._exp >= 32)
			{
				int num6 = (bigNumber2._exp - 25) * 15 / -s_tenPowersNeg[45]._exp;
				if (num6 > 0)
				{
					BigNumber numOp = s_tenPowersNeg[30 + num6];
					bigNumber2.Mul(ref numOp);
					bigNumber3.Mul(ref numOp);
					num += num6 * 32;
				}
				if (bigNumber2._exp >= 32)
				{
					num6 = (bigNumber2._exp - 25) * 32 / -s_tenPowersNeg[31]._exp;
					BigNumber numOp = s_tenPowersNeg[num6 - 1];
					bigNumber2.Mul(ref numOp);
					bigNumber3.Mul(ref numOp);
					num += num6;
				}
			}
			else if (bigNumber2._exp < 1)
			{
				int num6 = (25 - bigNumber2._exp) * 15 / s_tenPowersPos[45]._exp;
				if (num6 > 0)
				{
					BigNumber numOp = s_tenPowersPos[30 + num6];
					bigNumber2.Mul(ref numOp);
					bigNumber3.Mul(ref numOp);
					num -= num6 * 32;
				}
				if (bigNumber2._exp < 1)
				{
					num6 = (25 - bigNumber2._exp) * 32 / s_tenPowersPos[31]._exp;
					BigNumber numOp = s_tenPowersPos[num6 - 1];
					bigNumber2.Mul(ref numOp);
					bigNumber3.Mul(ref numOp);
					num -= num6;
				}
			}
			BigNumber bigNumber4 = bigNumber2;
			bigNumber2.MakeUpperBound();
			bigNumber4.MakeLowerBound();
			uint num8 = bigNumber2.UMod1();
			uint num9 = bigNumber4.UMod1();
			BigNumber bigNumber5 = bigNumber3;
			bigNumber5.MakeUpperBound();
			bigNumber3.MakeLowerBound();
			uint num10 = bigNumber5.UMod1();
			uint num11 = bigNumber3.UMod1();
			uint num12 = 1u;
			if (num8 >= 100000000)
			{
				num12 = 100000000u;
				num += 8;
			}
			else
			{
				if (num8 >= 10000)
				{
					num12 = 10000u;
					num += 4;
				}
				if (num8 >= 100 * num12)
				{
					num12 *= 100;
					num += 2;
				}
			}
			if (num8 >= 10 * num12)
			{
				num12 *= 10;
				num++;
			}
			num++;
			num7 = 0;
			while (true)
			{
				byte b = (byte)(num8 / num12);
				num8 %= num12;
				byte b2 = (byte)(num11 / num12);
				num11 %= num12;
				if (b == b2)
				{
					mantissa[num7++] = b;
					if (1 != num12)
					{
						num12 /= 10;
						continue;
					}
					num12 = 10000000u;
					bigNumber2.Mul(ref s_tenPowersPos[7]);
					bigNumber2.MakeUpperBound();
					num8 = bigNumber2.UMod1();
					if (num8 < 100000000)
					{
						bigNumber4.Mul(ref s_tenPowersPos[7]);
						bigNumber4.MakeLowerBound();
						num9 = bigNumber4.UMod1();
						bigNumber5.Mul(ref s_tenPowersPos[7]);
						bigNumber5.MakeUpperBound();
						num10 = bigNumber5.UMod1();
						bigNumber3.Mul(ref s_tenPowersPos[7]);
						bigNumber3.MakeLowerBound();
						num11 = bigNumber3.UMod1();
						continue;
					}
				}
				else
				{
					byte b3 = (byte)(num10 / num12 % 10);
					num10 %= num12;
					byte b4 = (byte)(num9 / num12 % 10);
					num9 %= num12;
					if (b3 < b4)
					{
						if (b3 == 0 && num10 == 0 && bigNumber5.IsZero && (num3 & 1) == 0)
						{
							break;
						}
						if (b4 - b3 > 1)
						{
							mantissa[num7++] = (byte)((b4 + b3 + 1) / 2);
							break;
						}
						if (num9 != 0 || !bigNumber4.IsZero || (num3 & 1) == 0)
						{
							mantissa[num7++] = b4;
							break;
						}
					}
				}
				exponent = (mantissaSize = 0);
				return false;
			}
			exponent = num;
			mantissaSize = num7;
			goto IL_05a9;
			IL_05a9:
			return true;
		}

		public static void DblToRgbPrecise(double dbl, byte[] mantissa, out int exponent, out int mantissaSize)
		{
			BigInteger bigInteger = new BigInteger();
			BigInteger bigInteger2 = new BigInteger();
			BigInteger bigInteger3 = new BigInteger();
			BigInteger bigInteger4 = new BigInteger();
			BigInteger bigInteger5 = new BigInteger();
			uint num = DblHi(dbl);
			uint num2 = DblLo(dbl);
			bigInteger2.InitFromDigits(1u, 0u, 1);
			bigInteger3.InitFromDigits(1u, 0u, 1);
			int num3 = (int)(((num & 0x7FF00000) >> 20) - 1075);
			uint num4 = num & 0xFFFFFu;
			uint num5 = num2;
			int num6 = 2;
			bool flag = false;
			double num7;
			int num8;
			if (num3 == -1075)
			{
				if (num4 == 0)
				{
					num6 = 1;
				}
				num7 = BitConverter.Int64BitsToDouble(5760103923406864384L);
				num7 *= dbl;
				num8 = (int)(((DblHi(num7) & 0x7FF00000) >> 20) - 1279);
				num = DblHi(num7);
				num &= 0xFFFFFu;
				num |= 0x3FF00000u;
				num7 = BitConverter.UInt64BitsToDouble(((ulong)num << 32) | DblLo(num7));
				num3++;
			}
			else
			{
				num &= 0xFFFFFu;
				num |= 0x3FF00000u;
				num7 = BitConverter.UInt64BitsToDouble(((ulong)num << 32) | num2);
				num8 = num3 + 52;
				if (num5 == 0 && num4 == 0 && num3 > -1074)
				{
					num4 = 2097152u;
					num3--;
					flag = true;
				}
				else
				{
					num4 |= 0x100000u;
				}
			}
			num7 = (num7 - 1.5) * 0.289529654602168 + 0.1760912590558 + (double)num8 * 0.301029995663981;
			int num9 = (int)num7;
			if (num7 < 0.0 && num7 != (double)num9)
			{
				num9--;
			}
			int num10;
			int num11;
			if (num3 >= 0)
			{
				num10 = num3;
				num11 = 0;
			}
			else
			{
				num10 = 0;
				num11 = -num3;
			}
			int num12;
			int num13;
			if (num9 >= 0)
			{
				num12 = 0;
				num13 = num9;
				num11 += num9;
			}
			else
			{
				num10 -= num9;
				num12 = -num9;
				num13 = 0;
			}
			if (num10 > 0 && num11 > 0)
			{
				num8 = ((num10 < num11) ? num10 : num11);
				num10 -= num8;
				num11 -= num8;
			}
			num10++;
			num11++;
			if (num12 > 0)
			{
				bigInteger3.MulPow5(num12);
				bigInteger.InitFromBigint(bigInteger3);
				if (1 == num6)
				{
					bigInteger.MulAdd(num5, 0u);
				}
				else
				{
					bigInteger.MulAdd(num4, 0u);
					bigInteger.ShiftLeft(32);
					if (num5 != 0)
					{
						bigInteger5.InitFromBigint(bigInteger3);
						bigInteger5.MulAdd(num5, 0u);
						bigInteger.Add(bigInteger5);
					}
				}
			}
			else
			{
				bigInteger.InitFromDigits(num5, num4, num6);
				if (num13 > 0)
				{
					bigInteger2.MulPow5(num13);
				}
			}
			num8 = CbitZeroLeft(bigInteger2[bigInteger2.Length - 1]);
			num8 = (num8 + 28 - num11) & 0x1F;
			num10 += num8;
			num11 += num8;
			bigInteger.ShiftLeft(num10);
			if (num10 > 1)
			{
				bigInteger3.ShiftLeft(num10 - 1);
			}
			bigInteger2.ShiftLeft(num11);
			BigInteger bigInteger6;
			if (flag)
			{
				bigInteger6 = bigInteger4;
				bigInteger6.InitFromBigint(bigInteger3);
				bigInteger3.ShiftLeft(1);
			}
			else
			{
				bigInteger6 = bigInteger3;
			}
			int num14 = 0;
			while (true)
			{
				byte b = (byte)bigInteger.DivRem(bigInteger2);
				if (num14 == 0 && b == 0)
				{
					num9--;
					goto IL_03c7;
				}
				num8 = bigInteger.CompareTo(bigInteger6);
				int num15;
				if (bigInteger2.CompareTo(bigInteger3) < 0)
				{
					num15 = 1;
				}
				else
				{
					bigInteger5.InitFromBigint(bigInteger2);
					bigInteger5.Subtract(bigInteger3);
					num15 = bigInteger.CompareTo(bigInteger5);
				}
				if (num15 == 0 && (num2 & 1) == 0)
				{
					if (b != 9)
					{
						if (num8 > 0)
						{
							b++;
						}
						mantissa[num14++] = b;
						break;
					}
				}
				else
				{
					if (num8 < 0 || (num8 == 0 && (num2 & 1) == 0))
					{
						if (num15 > 0)
						{
							bigInteger.ShiftLeft(1);
							num15 = bigInteger.CompareTo(bigInteger2);
							if ((num15 > 0 || (num15 == 0 && ((uint)b & (true ? 1u : 0u)) != 0)) && b++ == 9)
							{
								goto IL_0412;
							}
						}
						mantissa[num14++] = b;
						break;
					}
					if (num15 <= 0)
					{
						mantissa[num14++] = b;
						goto IL_03c7;
					}
					if (b != 9)
					{
						mantissa[num14++] = (byte)(b + 1);
						break;
					}
				}
				goto IL_0412;
				IL_0412:
				while (true)
				{
					if (num14 > 0)
					{
						if (mantissa[--num14] != 9)
						{
							mantissa[num14++]++;
							break;
						}
						continue;
					}
					num9++;
					mantissa[num14++] = 1;
					break;
				}
				break;
				IL_03c7:
				bigInteger.MulAdd(10u, 0u);
				bigInteger3.MulAdd(10u, 0u);
				if (bigInteger6 != bigInteger3)
				{
					bigInteger6.MulAdd(10u, 0u);
				}
			}
			exponent = num9 + 1;
			mantissaSize = num14;
		}
	}

	private sealed class BigInteger : IComparable
	{
		private int _capacity;

		private int _length;

		private uint[] _digits;

		public int Length => _length;

		public uint this[int idx] => _digits[idx];

		public BigInteger()
		{
			_capacity = 30;
			_length = 0;
			_digits = new uint[30];
		}

		private void Ensure(int cu)
		{
			if (cu > _capacity)
			{
				cu += cu;
				uint[] array = new uint[cu];
				_digits.CopyTo(array, 0);
				_digits = array;
				_capacity = cu;
			}
		}

		public void InitFromRgu(uint[] rgu, int cu)
		{
			Ensure(cu);
			_length = cu;
			for (int i = 0; i < cu; i++)
			{
				_digits[i] = rgu[i];
			}
		}

		public void InitFromDigits(uint u0, uint u1, int cu)
		{
			_length = cu;
			_digits[0] = u0;
			_digits[1] = u1;
		}

		public void InitFromBigint(BigInteger biSrc)
		{
			InitFromRgu(biSrc._digits, biSrc._length);
		}

		public void InitFromFloatingDecimal(FloatingDecimal dec)
		{
			int cu = (dec.MantissaSize + 8) / 9;
			int mantissaSize = dec.MantissaSize;
			Ensure(cu);
			_length = 0;
			uint num = 0u;
			uint num2 = 1u;
			for (int i = 0; i < mantissaSize; i++)
			{
				if (1000000000 == num2)
				{
					MulAdd(num2, num);
					num2 = 1u;
					num = 0u;
				}
				num2 *= 10;
				num = num * 10 + dec[i];
			}
			MulAdd(num2, num);
		}

		public void MulAdd(uint uMul, uint uAdd)
		{
			for (int i = 0; i < _length; i++)
			{
				uint uHi;
				uint u = MulU(_digits[i], uMul, out uHi);
				if (uAdd != 0)
				{
					uHi += AddU(ref u, uAdd);
				}
				_digits[i] = u;
				uAdd = uHi;
			}
			if (uAdd != 0)
			{
				Ensure(_length + 1);
				_digits[_length++] = uAdd;
			}
		}

		public void MulPow5(int c5)
		{
			int num = (c5 + 12) / 13;
			if (_length == 0 || c5 == 0)
			{
				return;
			}
			Ensure(_length + num);
			while (c5 >= 13)
			{
				MulAdd(1220703125u, 0u);
				c5 -= 13;
			}
			if (c5 > 0)
			{
				uint num2 = 5u;
				while (--c5 > 0)
				{
					num2 *= 5;
				}
				MulAdd(num2, 0u);
			}
		}

		public void ShiftLeft(int cbit)
		{
			if (cbit == 0 || _length == 0)
			{
				return;
			}
			int num = cbit >> 5;
			cbit &= 0x1F;
			uint num3;
			int num2;
			if (cbit > 0)
			{
				num2 = _length - 1;
				num3 = _digits[num2] >> 32 - cbit;
				while (true)
				{
					_digits[num2] <<= cbit;
					if (num2 == 0)
					{
						break;
					}
					_digits[num2] |= _digits[num2 - 1] >> 32 - cbit;
					num2--;
				}
			}
			else
			{
				num3 = 0u;
			}
			if (num <= 0 && num3 == 0)
			{
				return;
			}
			num2 = _length + ((num3 != 0) ? 1 : 0) + num;
			Ensure(num2);
			if (num > 0)
			{
				int length = _length;
				while (length-- != 0)
				{
					_digits[num + length] = _digits[length];
				}
				for (int i = 0; i < num; i++)
				{
					_digits[i] = 0u;
				}
				_length += num;
			}
			if (num3 != 0)
			{
				_digits[_length++] = num3;
			}
		}

		public void ShiftUsRight(int cu)
		{
			if (cu >= _length)
			{
				_length = 0;
			}
			else if (cu > 0)
			{
				for (int i = 0; i < _length - cu; i++)
				{
					_digits[i] = _digits[cu + i];
				}
				_length -= cu;
			}
		}

		public void ShiftRight(int cbit)
		{
			int num = cbit >> 5;
			cbit &= 0x1F;
			if (num > 0)
			{
				ShiftUsRight(num);
			}
			if (cbit == 0 || _length == 0)
			{
				return;
			}
			int num2 = 0;
			while (true)
			{
				_digits[num2] >>= cbit;
				if (++num2 >= _length)
				{
					break;
				}
				_digits[num2 - 1] |= _digits[num2] << 32 - cbit;
			}
			if (_digits[num2 - 1] == 0)
			{
				_length--;
			}
		}

		public int CompareTo(object obj)
		{
			BigInteger bigInteger = (BigInteger)obj;
			if (_length > bigInteger._length)
			{
				return 1;
			}
			if (_length < bigInteger._length)
			{
				return -1;
			}
			if (_length == 0)
			{
				return 0;
			}
			int num = _length - 1;
			while (_digits[num] == bigInteger._digits[num])
			{
				if (num == 0)
				{
					return 0;
				}
				num--;
			}
			if (_digits[num] <= bigInteger._digits[num])
			{
				return -1;
			}
			return 1;
		}

		public void Add(BigInteger bi)
		{
			int length;
			int length2;
			if ((length = _length) < (length2 = bi._length))
			{
				length = bi._length;
				length2 = _length;
				Ensure(length + 1);
			}
			uint num = 0u;
			int i;
			for (i = 0; i < length2; i++)
			{
				if (num != 0)
				{
					num = AddU(ref _digits[i], num);
				}
				num += AddU(ref _digits[i], bi._digits[i]);
			}
			if (_length < bi._length)
			{
				for (; i < length; i++)
				{
					_digits[i] = bi._digits[i];
					if (num != 0)
					{
						num = AddU(ref _digits[i], num);
					}
				}
				_length = length;
			}
			else
			{
				while (num != 0 && i < length)
				{
					num = AddU(ref _digits[i], num);
					i++;
				}
			}
			if (num != 0)
			{
				Ensure(_length + 1);
				_digits[_length++] = num;
			}
		}

		public void Subtract(BigInteger bi)
		{
			if (_length >= bi._length)
			{
				uint num = 1u;
				int i;
				for (i = 0; i < bi._length; i++)
				{
					uint num2 = bi._digits[i];
					if (num2 != 0 || num == 0)
					{
						num = AddU(ref _digits[i], ~num2 + num);
					}
				}
				while (num == 0 && i < _length)
				{
					num = AddU(ref _digits[i], uint.MaxValue);
				}
				if (num != 0)
				{
					if (i == _length)
					{
						while (--i >= 0 && _digits[i] == 0)
						{
						}
						_length = i + 1;
					}
					return;
				}
			}
			_length = 0;
		}

		public uint DivRem(BigInteger bi)
		{
			int length = bi._length;
			if (_length < length)
			{
				return 0u;
			}
			uint num = _digits[length - 1] / (bi._digits[length - 1] + 1);
			switch (num)
			{
			case 1u:
				Subtract(bi);
				break;
			default:
			{
				uint u = 0u;
				uint num2 = 1u;
				int i;
				for (i = 0; i < length; i++)
				{
					uint uHi;
					uint u2 = MulU(num, bi._digits[i], out uHi);
					u = uHi + AddU(ref u2, u);
					if (u2 != 0 || num2 == 0)
					{
						num2 = AddU(ref _digits[i], ~u2 + num2);
					}
				}
				while (--i >= 0 && _digits[i] == 0)
				{
				}
				_length = i + 1;
				break;
			}
			case 0u:
				break;
			}
			int num3;
			if (num < 9 && (num3 = CompareTo(bi)) >= 0)
			{
				num++;
				if (num3 == 0)
				{
					_length = 0;
				}
				else
				{
					Subtract(bi);
				}
			}
			return num;
		}
	}

	private sealed class FloatingDecimal
	{
		private int _exponent;

		private int _sign;

		private int _mantissaSize;

		private readonly byte[] _mantissa = new byte[50];

		public int Exponent
		{
			get
			{
				return _exponent;
			}
			set
			{
				_exponent = value;
			}
		}

		public int Sign
		{
			get
			{
				return _sign;
			}
			set
			{
				_sign = value;
			}
		}

		public byte[] Mantissa => _mantissa;

		public int MantissaSize
		{
			get
			{
				return _mantissaSize;
			}
			set
			{
				_mantissaSize = value;
			}
		}

		public byte this[int ib] => _mantissa[ib];

		public FloatingDecimal()
		{
			_exponent = 0;
			_sign = 1;
			_mantissaSize = 0;
		}

		public FloatingDecimal(double dbl)
		{
			InitFromDouble(dbl);
		}

		public static explicit operator double(FloatingDecimal dec)
		{
			int mantissaSize = dec._mantissaSize;
			int num = dec._exponent - mantissaSize;
			double num3;
			if (mantissaSize <= 15 && num >= -22 && dec._exponent <= 37)
			{
				if (mantissaSize <= 9)
				{
					uint num2 = 0u;
					for (int i = 0; i < mantissaSize; i++)
					{
						num2 = num2 * 10 + dec[i];
					}
					num3 = num2;
				}
				else
				{
					num3 = 0.0;
					for (int j = 0; j < mantissaSize; j++)
					{
						num3 = num3 * 10.0 + (double)(int)dec[j];
					}
				}
				if (num > 0)
				{
					if (num > 22)
					{
						num3 *= C10toN[num - 22];
						num3 *= C10toN[22];
					}
					else
					{
						num3 *= C10toN[num];
					}
				}
				else if (num < 0)
				{
					num3 /= C10toN[-num];
				}
			}
			else if (dec._exponent >= 310)
			{
				num3 = double.PositiveInfinity;
			}
			else if (dec._exponent <= -325)
			{
				num3 = 0.0;
			}
			else
			{
				BigNumber bigNumber = new BigNumber(dec);
				if (bigNumber.Error == 0)
				{
					num3 = (double)bigNumber;
				}
				else
				{
					BigNumber bigNumber2 = bigNumber;
					bigNumber2.MakeUpperBound();
					BigNumber bigNumber3 = bigNumber;
					bigNumber3.MakeLowerBound();
					num3 = (double)bigNumber2;
					double num4 = (double)bigNumber3;
					if (num3 != num4)
					{
						num3 = dec.AdjustDbl((double)bigNumber);
					}
				}
			}
			if (dec._sign >= 0)
			{
				return num3;
			}
			return 0.0 - num3;
		}

		private double AdjustDbl(double dbl)
		{
			BigInteger bigInteger = new BigInteger();
			BigInteger bigInteger2 = new BigInteger();
			bigInteger.InitFromFloatingDecimal(this);
			int num = _exponent - _mantissaSize;
			int num3;
			int num5;
			int num4;
			int num2;
			if (num >= 0)
			{
				num3 = (num2 = num);
				num5 = (num4 = 0);
			}
			else
			{
				num3 = (num2 = 0);
				num5 = (num4 = -num);
			}
			uint num6 = DblHi(dbl);
			uint num7 = DblLo(dbl);
			int num8 = (int)((num6 >> 20) & 0x7FF);
			num6 &= 0xFFFFFu;
			uint u = 1u;
			if (num8 != 0)
			{
				if (num6 == 0 && num7 == 0 && 1 != num8)
				{
					u = 2u;
					num6 = 2097152u;
					num8--;
				}
				else
				{
					num6 |= 0x100000u;
				}
				num8 -= 1076;
			}
			else
			{
				num8 = -1075;
			}
			num6 = (num6 << 1) | (num7 >> 31);
			num7 <<= 1;
			int cu = ((num7 != 0 || num6 != 0) ? ((num6 == 0) ? 1 : 2) : 0);
			bigInteger2.InitFromDigits(num7, num6, cu);
			if (num8 >= 0)
			{
				num4 += num8;
			}
			else
			{
				num2 += -num8;
			}
			if (num4 > num2)
			{
				num4 -= num2;
				num2 = 0;
				int num9 = 0;
				while (num4 >= 32 && bigInteger[num9] == 0)
				{
					num4 -= 32;
					num9++;
				}
				if (num9 > 0)
				{
					bigInteger.ShiftUsRight(num9);
				}
				uint num10 = bigInteger[0];
				for (num9 = 0; num9 < num4 && (num10 & (1L << num9)) == 0L; num9++)
				{
				}
				if (num9 > 0)
				{
					num4 -= num9;
					bigInteger.ShiftRight(num9);
				}
			}
			else
			{
				num2 -= num4;
				num4 = 0;
			}
			if (num5 > 0)
			{
				bigInteger2.MulPow5(num5);
			}
			else if (num3 > 0)
			{
				bigInteger.MulPow5(num3);
			}
			if (num4 > 0)
			{
				bigInteger2.ShiftLeft(num4);
			}
			else if (num2 > 0)
			{
				bigInteger.ShiftLeft(num2);
			}
			int num11 = bigInteger2.CompareTo(bigInteger);
			if (num11 == 0)
			{
				return dbl;
			}
			if (num11 > 0)
			{
				if (AddU(ref num7, uint.MaxValue) == 0)
				{
					AddU(ref num6, uint.MaxValue);
				}
				bigInteger2.InitFromDigits(num7, num6, 1 + ((num6 != 0) ? 1 : 0));
				if (num5 > 0)
				{
					bigInteger2.MulPow5(num5);
				}
				if (num4 > 0)
				{
					bigInteger2.ShiftLeft(num4);
				}
				num11 = bigInteger2.CompareTo(bigInteger);
				if (num11 > 0 || (num11 == 0 && (DblLo(dbl) & (true ? 1u : 0u)) != 0))
				{
					dbl = BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(dbl) - 1);
				}
			}
			else
			{
				if (AddU(ref num7, u) != 0)
				{
					AddU(ref num6, 1u);
				}
				bigInteger2.InitFromDigits(num7, num6, 1 + ((num6 != 0) ? 1 : 0));
				if (num5 > 0)
				{
					bigInteger2.MulPow5(num5);
				}
				if (num4 > 0)
				{
					bigInteger2.ShiftLeft(num4);
				}
				num11 = bigInteger2.CompareTo(bigInteger);
				if (num11 < 0 || (num11 == 0 && (DblLo(dbl) & (true ? 1u : 0u)) != 0))
				{
					dbl = BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(dbl) + 1);
				}
			}
			return dbl;
		}

		private void InitFromDouble(double dbl)
		{
			if (0.0 == dbl || IsSpecial(dbl))
			{
				_exponent = 0;
				_sign = 1;
				_mantissaSize = 0;
				return;
			}
			if (dbl < 0.0)
			{
				_sign = -1;
				dbl = 0.0 - dbl;
			}
			else
			{
				_sign = 1;
			}
			if (!BigNumber.DblToRgbFast(dbl, _mantissa, out _exponent, out _mantissaSize))
			{
				BigNumber.DblToRgbPrecise(dbl, _mantissa, out _exponent, out _mantissaSize);
			}
		}
	}

	public static readonly double[] C10toN = new double[23]
	{
		1.0, 10.0, 100.0, 1000.0, 10000.0, 100000.0, 1000000.0, 10000000.0, 100000000.0, 1000000000.0,
		10000000000.0, 100000000000.0, 1000000000000.0, 10000000000000.0, 100000000000000.0, 1000000000000000.0, 10000000000000000.0, 1E+17, 1E+18, 1E+19,
		1E+20, 1E+21, 1E+22
	};

	public static uint DblHi(double dbl)
	{
		return (uint)(BitConverter.DoubleToUInt64Bits(dbl) >> 32);
	}

	public static uint DblLo(double dbl)
	{
		return (uint)BitConverter.DoubleToUInt64Bits(dbl);
	}

	public static bool IsSpecial(double dbl)
	{
		return (~DblHi(dbl) & 0x7FF00000) == 0;
	}

	public static uint NotZero(uint u)
	{
		if (u == 0)
		{
			return 0u;
		}
		return 1u;
	}

	public static uint AddU(ref uint u1, uint u2)
	{
		u1 += u2;
		if (u1 >= u2)
		{
			return 0u;
		}
		return 1u;
	}

	public static uint MulU(uint u1, uint u2, out uint uHi)
	{
		ulong num = (ulong)u1 * (ulong)u2;
		uHi = (uint)(num >> 32);
		return (uint)num;
	}

	public static int CbitZeroLeft(uint u)
	{
		int num = 0;
		if ((u & 0xFFFF0000u) == 0)
		{
			num += 16;
			u <<= 16;
		}
		if ((u & 0xFF000000u) == 0)
		{
			num += 8;
			u <<= 8;
		}
		if ((u & 0xF0000000u) == 0)
		{
			num += 4;
			u <<= 4;
		}
		if ((u & 0xC0000000u) == 0)
		{
			num += 2;
			u <<= 2;
		}
		if ((u & 0x80000000u) == 0)
		{
			num++;
			u <<= 1;
		}
		return num;
	}

	public static bool IsInteger(double dbl, out int value)
	{
		if (!IsSpecial(dbl))
		{
			int num = (int)dbl;
			double num2 = num;
			if (dbl == num2)
			{
				value = num;
				return true;
			}
		}
		value = 0;
		return false;
	}

	public unsafe static string DoubleToString(double dbl)
	{
		if (IsInteger(dbl, out var value))
		{
			return value.ToString(CultureInfo.InvariantCulture);
		}
		if (IsSpecial(dbl))
		{
			if (double.IsNaN(dbl))
			{
				return "NaN";
			}
			if (!(dbl < 0.0))
			{
				return "Infinity";
			}
			return "-Infinity";
		}
		FloatingDecimal floatingDecimal = new FloatingDecimal(dbl);
		int num = floatingDecimal.MantissaSize - floatingDecimal.Exponent;
		int num2;
		if (num > 0)
		{
			num2 = ((floatingDecimal.Exponent > 0) ? floatingDecimal.Exponent : 0);
		}
		else
		{
			num2 = floatingDecimal.Exponent;
			num = 0;
		}
		int num3 = num2 + num + 4;
		char* ptr = stackalloc char[num3];
		char* ptr2 = ptr;
		if (floatingDecimal.Sign < 0)
		{
			*(ptr2++) = '-';
		}
		int num4 = floatingDecimal.MantissaSize;
		int num5 = 0;
		if (num2 != 0)
		{
			do
			{
				if (num4 != 0)
				{
					*(ptr2++) = (char)(floatingDecimal[num5++] | 0x30u);
					num4--;
				}
				else
				{
					*(ptr2++) = '0';
				}
			}
			while (--num2 != 0);
		}
		else
		{
			*(ptr2++) = '0';
		}
		if (num != 0)
		{
			*(ptr2++) = '.';
			while (num > num4)
			{
				*(ptr2++) = '0';
				num--;
			}
			while (num4 != 0)
			{
				*(ptr2++) = (char)(floatingDecimal[num5++] | 0x30u);
				num4--;
			}
		}
		return new string(ptr, 0, (int)(ptr2 - ptr));
	}

	private static bool IsAsciiDigit(char ch)
	{
		return (uint)(ch - 48) <= 9u;
	}

	private static bool IsWhitespace(char ch)
	{
		if (ch != ' ' && ch != '\t' && ch != '\n')
		{
			return ch == '\r';
		}
		return true;
	}

	private unsafe static char* SkipWhitespace(char* pch)
	{
		while (IsWhitespace(*pch))
		{
			pch++;
		}
		return pch;
	}

	public unsafe static double StringToDouble(string s)
	{
		fixed (char* ptr = s)
		{
			int num = 0;
			char* ptr2 = ptr;
			char* ptr3 = null;
			int num2 = 1;
			int num3 = 0;
			while (true)
			{
				char c = *(ptr2++);
				if (!IsAsciiDigit(c))
				{
					if (c != '-')
					{
						if (c == '.')
						{
							if (IsAsciiDigit(*ptr2))
							{
								goto IL_00b8;
							}
						}
						else if (IsWhitespace(c) && num2 > 0)
						{
							ptr2 = SkipWhitespace(ptr2);
							continue;
						}
					}
					else if (num2 >= 0)
					{
						num2 = -1;
						continue;
					}
					return double.NaN;
				}
				if (c == '0')
				{
					do
					{
						c = *(ptr2++);
					}
					while (c == '0');
					if (!IsAsciiDigit(c))
					{
						goto IL_00b2;
					}
				}
				ptr3 = ptr2 - 1;
				do
				{
					c = *(ptr2++);
				}
				while (IsAsciiDigit(c));
				num = (int)(ptr2 - ptr3) - 1;
				goto IL_00b2;
				IL_00b8:
				c = *(ptr2++);
				if (ptr3 == null)
				{
					while (c == '0')
					{
						num3--;
						c = *(ptr2++);
					}
					ptr3 = ptr2 - 1;
				}
				while (IsAsciiDigit(c))
				{
					num3--;
					num++;
					c = *(ptr2++);
				}
				break;
				IL_00b2:
				if (c != '.')
				{
					break;
				}
				goto IL_00b8;
			}
			ptr2--;
			char* ptr4 = ptr + s.Length;
			if (ptr2 < ptr4 && SkipWhitespace(ptr2) < ptr4)
			{
				return double.NaN;
			}
			if (num == 0)
			{
				return 0.0;
			}
			if (num3 == 0 && num <= 9)
			{
				int num4 = *ptr3 & 0xF;
				while (--num != 0)
				{
					ptr3++;
					num4 = num4 * 10 + (*ptr3 & 0xF);
				}
				return (num2 < 0) ? (-num4) : num4;
			}
			if (num > 50)
			{
				ptr2 -= num - 50;
				num3 += num - 50;
				num = 50;
			}
			while (true)
			{
				if (*(--ptr2) == '0')
				{
					num--;
					num3++;
				}
				else if (*ptr2 != '.')
				{
					break;
				}
			}
			ptr2++;
			FloatingDecimal floatingDecimal = new FloatingDecimal();
			floatingDecimal.Exponent = num3 + num;
			floatingDecimal.Sign = num2;
			floatingDecimal.MantissaSize = num;
			fixed (byte* ptr5 = &floatingDecimal.Mantissa[0])
			{
				byte* ptr6 = ptr5;
				for (; ptr3 < ptr2; ptr3++)
				{
					if (*ptr3 != '.')
					{
						*ptr6 = (byte)(*ptr3 & 0xFu);
						ptr6++;
					}
				}
			}
			return (double)floatingDecimal;
		}
	}
}
