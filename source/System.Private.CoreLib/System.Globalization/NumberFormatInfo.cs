namespace System.Globalization;

public sealed class NumberFormatInfo : IFormatProvider, ICloneable
{
	private static volatile NumberFormatInfo s_invariantInfo;

	internal int[] _numberGroupSizes = new int[1] { 3 };

	internal int[] _currencyGroupSizes = new int[1] { 3 };

	internal int[] _percentGroupSizes = new int[1] { 3 };

	internal string _positiveSign = "+";

	internal string _negativeSign = "-";

	internal string _numberDecimalSeparator = ".";

	internal string _numberGroupSeparator = ",";

	internal string _currencyGroupSeparator = ",";

	internal string _currencyDecimalSeparator = ".";

	internal string _currencySymbol = "¤";

	internal string _nanSymbol = "NaN";

	internal string _positiveInfinitySymbol = "Infinity";

	internal string _negativeInfinitySymbol = "-Infinity";

	internal string _percentDecimalSeparator = ".";

	internal string _percentGroupSeparator = ",";

	internal string _percentSymbol = "%";

	internal string _perMilleSymbol = "‰";

	internal string[] _nativeDigits = new string[10] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

	internal int _numberDecimalDigits = 2;

	internal int _currencyDecimalDigits = 2;

	internal int _currencyPositivePattern;

	internal int _currencyNegativePattern;

	internal int _numberNegativePattern = 1;

	internal int _percentPositivePattern;

	internal int _percentNegativePattern;

	internal int _percentDecimalDigits = 2;

	internal int _digitSubstitution = 1;

	internal bool _isReadOnly;

	private bool _hasInvariantNumberSigns = true;

	private bool _allowHyphenDuringParsing;

	internal bool HasInvariantNumberSigns => _hasInvariantNumberSigns;

	internal bool AllowHyphenDuringParsing => _allowHyphenDuringParsing;

	public static NumberFormatInfo InvariantInfo
	{
		get
		{
			object obj = s_invariantInfo;
			if (obj == null)
			{
				obj = new NumberFormatInfo
				{
					_isReadOnly = true
				};
				s_invariantInfo = (NumberFormatInfo)obj;
			}
			return (NumberFormatInfo)obj;
		}
	}

	public int CurrencyDecimalDigits
	{
		get
		{
			return _currencyDecimalDigits;
		}
		set
		{
			if (value < 0 || value > 99)
			{
				throw new ArgumentOutOfRangeException("value", value, SR.Format(SR.ArgumentOutOfRange_Range, 0, 99));
			}
			VerifyWritable();
			_currencyDecimalDigits = value;
		}
	}

	public string CurrencyDecimalSeparator
	{
		get
		{
			return _currencyDecimalSeparator;
		}
		set
		{
			VerifyWritable();
			VerifyDecimalSeparator(value, "value");
			_currencyDecimalSeparator = value;
		}
	}

	public bool IsReadOnly => _isReadOnly;

	public int[] CurrencyGroupSizes
	{
		get
		{
			return (int[])_currencyGroupSizes.Clone();
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			VerifyWritable();
			int[] array = (int[])value.Clone();
			CheckGroupSize("value", array);
			_currencyGroupSizes = array;
		}
	}

	public int[] NumberGroupSizes
	{
		get
		{
			return (int[])_numberGroupSizes.Clone();
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			VerifyWritable();
			int[] array = (int[])value.Clone();
			CheckGroupSize("value", array);
			_numberGroupSizes = array;
		}
	}

	public int[] PercentGroupSizes
	{
		get
		{
			return (int[])_percentGroupSizes.Clone();
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			VerifyWritable();
			int[] array = (int[])value.Clone();
			CheckGroupSize("value", array);
			_percentGroupSizes = array;
		}
	}

	public string CurrencyGroupSeparator
	{
		get
		{
			return _currencyGroupSeparator;
		}
		set
		{
			VerifyWritable();
			VerifyGroupSeparator(value, "value");
			_currencyGroupSeparator = value;
		}
	}

	public string CurrencySymbol
	{
		get
		{
			return _currencySymbol;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			VerifyWritable();
			_currencySymbol = value;
		}
	}

	public static NumberFormatInfo CurrentInfo
	{
		get
		{
			CultureInfo currentCulture = CultureInfo.CurrentCulture;
			if (!currentCulture._isInherited)
			{
				NumberFormatInfo numInfo = currentCulture._numInfo;
				if (numInfo != null)
				{
					return numInfo;
				}
			}
			return (NumberFormatInfo)currentCulture.GetFormat(typeof(NumberFormatInfo));
		}
	}

	public string NaNSymbol
	{
		get
		{
			return _nanSymbol;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			VerifyWritable();
			_nanSymbol = value;
		}
	}

	public int CurrencyNegativePattern
	{
		get
		{
			return _currencyNegativePattern;
		}
		set
		{
			if (value < 0 || value > 15)
			{
				throw new ArgumentOutOfRangeException("value", value, SR.Format(SR.ArgumentOutOfRange_Range, 0, 15));
			}
			VerifyWritable();
			_currencyNegativePattern = value;
		}
	}

	public int NumberNegativePattern
	{
		get
		{
			return _numberNegativePattern;
		}
		set
		{
			if (value < 0 || value > 4)
			{
				throw new ArgumentOutOfRangeException("value", value, SR.Format(SR.ArgumentOutOfRange_Range, 0, 4));
			}
			VerifyWritable();
			_numberNegativePattern = value;
		}
	}

	public int PercentPositivePattern
	{
		get
		{
			return _percentPositivePattern;
		}
		set
		{
			if (value < 0 || value > 3)
			{
				throw new ArgumentOutOfRangeException("value", value, SR.Format(SR.ArgumentOutOfRange_Range, 0, 3));
			}
			VerifyWritable();
			_percentPositivePattern = value;
		}
	}

	public int PercentNegativePattern
	{
		get
		{
			return _percentNegativePattern;
		}
		set
		{
			if (value < 0 || value > 11)
			{
				throw new ArgumentOutOfRangeException("value", value, SR.Format(SR.ArgumentOutOfRange_Range, 0, 11));
			}
			VerifyWritable();
			_percentNegativePattern = value;
		}
	}

	public string NegativeInfinitySymbol
	{
		get
		{
			return _negativeInfinitySymbol;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			VerifyWritable();
			_negativeInfinitySymbol = value;
		}
	}

	public string NegativeSign
	{
		get
		{
			return _negativeSign;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			VerifyWritable();
			_negativeSign = value;
			InitializeInvariantAndNegativeSignFlags();
		}
	}

	public int NumberDecimalDigits
	{
		get
		{
			return _numberDecimalDigits;
		}
		set
		{
			if (value < 0 || value > 99)
			{
				throw new ArgumentOutOfRangeException("value", value, SR.Format(SR.ArgumentOutOfRange_Range, 0, 99));
			}
			VerifyWritable();
			_numberDecimalDigits = value;
		}
	}

	public string NumberDecimalSeparator
	{
		get
		{
			return _numberDecimalSeparator;
		}
		set
		{
			VerifyWritable();
			VerifyDecimalSeparator(value, "value");
			_numberDecimalSeparator = value;
		}
	}

	public string NumberGroupSeparator
	{
		get
		{
			return _numberGroupSeparator;
		}
		set
		{
			VerifyWritable();
			VerifyGroupSeparator(value, "value");
			_numberGroupSeparator = value;
		}
	}

	public int CurrencyPositivePattern
	{
		get
		{
			return _currencyPositivePattern;
		}
		set
		{
			if (value < 0 || value > 3)
			{
				throw new ArgumentOutOfRangeException("value", value, SR.Format(SR.ArgumentOutOfRange_Range, 0, 3));
			}
			VerifyWritable();
			_currencyPositivePattern = value;
		}
	}

	public string PositiveInfinitySymbol
	{
		get
		{
			return _positiveInfinitySymbol;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			VerifyWritable();
			_positiveInfinitySymbol = value;
		}
	}

	public string PositiveSign
	{
		get
		{
			return _positiveSign;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			VerifyWritable();
			_positiveSign = value;
			InitializeInvariantAndNegativeSignFlags();
		}
	}

	public int PercentDecimalDigits
	{
		get
		{
			return _percentDecimalDigits;
		}
		set
		{
			if (value < 0 || value > 99)
			{
				throw new ArgumentOutOfRangeException("value", value, SR.Format(SR.ArgumentOutOfRange_Range, 0, 99));
			}
			VerifyWritable();
			_percentDecimalDigits = value;
		}
	}

	public string PercentDecimalSeparator
	{
		get
		{
			return _percentDecimalSeparator;
		}
		set
		{
			VerifyWritable();
			VerifyDecimalSeparator(value, "value");
			_percentDecimalSeparator = value;
		}
	}

	public string PercentGroupSeparator
	{
		get
		{
			return _percentGroupSeparator;
		}
		set
		{
			VerifyWritable();
			VerifyGroupSeparator(value, "value");
			_percentGroupSeparator = value;
		}
	}

	public string PercentSymbol
	{
		get
		{
			return _percentSymbol;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			VerifyWritable();
			_percentSymbol = value;
		}
	}

	public string PerMilleSymbol
	{
		get
		{
			return _perMilleSymbol;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			VerifyWritable();
			_perMilleSymbol = value;
		}
	}

	public string[] NativeDigits
	{
		get
		{
			return (string[])_nativeDigits.Clone();
		}
		set
		{
			VerifyWritable();
			VerifyNativeDigits(value, "value");
			_nativeDigits = value;
		}
	}

	public DigitShapes DigitSubstitution
	{
		get
		{
			return (DigitShapes)_digitSubstitution;
		}
		set
		{
			VerifyWritable();
			VerifyDigitSubstitution(value, "value");
			_digitSubstitution = (int)value;
		}
	}

	public NumberFormatInfo()
	{
	}

	private static void VerifyDecimalSeparator(string decSep, string propertyName)
	{
		if (decSep == null)
		{
			throw new ArgumentNullException(propertyName);
		}
		if (decSep.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyDecString, propertyName);
		}
	}

	private static void VerifyGroupSeparator(string groupSep, string propertyName)
	{
		if (groupSep == null)
		{
			throw new ArgumentNullException(propertyName);
		}
	}

	private static void VerifyNativeDigits(string[] nativeDig, string propertyName)
	{
		if (nativeDig == null)
		{
			throw new ArgumentNullException(propertyName, SR.ArgumentNull_Array);
		}
		if (nativeDig.Length != 10)
		{
			throw new ArgumentException(SR.Argument_InvalidNativeDigitCount, propertyName);
		}
		for (int i = 0; i < nativeDig.Length; i++)
		{
			if (nativeDig[i] == null)
			{
				throw new ArgumentNullException(propertyName, SR.ArgumentNull_ArrayValue);
			}
			if (nativeDig[i].Length != 1)
			{
				if (nativeDig[i].Length != 2)
				{
					throw new ArgumentException(SR.Argument_InvalidNativeDigitValue, propertyName);
				}
				if (!char.IsSurrogatePair(nativeDig[i][0], nativeDig[i][1]))
				{
					throw new ArgumentException(SR.Argument_InvalidNativeDigitValue, propertyName);
				}
			}
			if (CharUnicodeInfo.GetDecimalDigitValue(nativeDig[i], 0) != i && CharUnicodeInfo.GetUnicodeCategory(nativeDig[i], 0) != UnicodeCategory.PrivateUse)
			{
				throw new ArgumentException(SR.Argument_InvalidNativeDigitValue, propertyName);
			}
		}
	}

	private static void VerifyDigitSubstitution(DigitShapes digitSub, string propertyName)
	{
		if ((uint)digitSub > 2u)
		{
			throw new ArgumentException(SR.Argument_InvalidDigitSubstitution, propertyName);
		}
	}

	private void InitializeInvariantAndNegativeSignFlags()
	{
		_hasInvariantNumberSigns = _positiveSign == "+" && _negativeSign == "-";
		bool flag = _negativeSign.Length == 1;
		bool flag2 = flag;
		if (flag2)
		{
			bool flag3;
			switch (_negativeSign[0])
			{
			case '‒':
			case '⁻':
			case '₋':
			case '−':
			case '➖':
			case '﹣':
			case '－':
				flag3 = true;
				break;
			default:
				flag3 = false;
				break;
			}
			flag2 = flag3;
		}
		_allowHyphenDuringParsing = flag2;
	}

	internal NumberFormatInfo(CultureData cultureData)
	{
		if (cultureData != null)
		{
			cultureData.GetNFIValues(this);
			InitializeInvariantAndNegativeSignFlags();
		}
	}

	private void VerifyWritable()
	{
		if (_isReadOnly)
		{
			throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
		}
	}

	public static NumberFormatInfo GetInstance(IFormatProvider? formatProvider)
	{
		if (formatProvider != null)
		{
			return GetProviderNonNull(formatProvider);
		}
		return CurrentInfo;
		static NumberFormatInfo GetProviderNonNull(IFormatProvider provider)
		{
			if (provider is CultureInfo { _isInherited: false } cultureInfo)
			{
				return cultureInfo._numInfo ?? cultureInfo.NumberFormat;
			}
			return (provider as NumberFormatInfo) ?? (provider.GetFormat(typeof(NumberFormatInfo)) as NumberFormatInfo) ?? CurrentInfo;
		}
	}

	public object Clone()
	{
		NumberFormatInfo numberFormatInfo = (NumberFormatInfo)MemberwiseClone();
		numberFormatInfo._isReadOnly = false;
		return numberFormatInfo;
	}

	internal static void CheckGroupSize(string propName, int[] groupSize)
	{
		for (int i = 0; i < groupSize.Length; i++)
		{
			if (groupSize[i] < 1)
			{
				if (i == groupSize.Length - 1 && groupSize[i] == 0)
				{
					break;
				}
				throw new ArgumentException(SR.Argument_InvalidGroupSize, propName);
			}
			if (groupSize[i] > 9)
			{
				throw new ArgumentException(SR.Argument_InvalidGroupSize, propName);
			}
		}
	}

	public object? GetFormat(Type? formatType)
	{
		if (!(formatType == typeof(NumberFormatInfo)))
		{
			return null;
		}
		return this;
	}

	public static NumberFormatInfo ReadOnly(NumberFormatInfo nfi)
	{
		if (nfi == null)
		{
			throw new ArgumentNullException("nfi");
		}
		if (nfi.IsReadOnly)
		{
			return nfi;
		}
		NumberFormatInfo numberFormatInfo = (NumberFormatInfo)nfi.MemberwiseClone();
		numberFormatInfo._isReadOnly = true;
		return numberFormatInfo;
	}

	internal static void ValidateParseStyleInteger(NumberStyles style)
	{
		if (((uint)style & 0xFFFFFE00u) != 0 && ((uint)style & 0xFFFFFDFCu) != 0)
		{
			ThrowInvalid(style);
		}
		static void ThrowInvalid(NumberStyles value)
		{
			if (((uint)value & 0xFFFFFC00u) != 0)
			{
				throw new ArgumentException(SR.Argument_InvalidNumberStyles, "style");
			}
			throw new ArgumentException(SR.Arg_InvalidHexStyle);
		}
	}

	internal static void ValidateParseStyleFloatingPoint(NumberStyles style)
	{
		if (((uint)style & 0xFFFFFE00u) != 0)
		{
			ThrowInvalid(style);
		}
		static void ThrowInvalid(NumberStyles value)
		{
			if (((uint)value & 0xFFFFFC00u) != 0)
			{
				throw new ArgumentException(SR.Argument_InvalidNumberStyles, "style");
			}
			throw new ArgumentException(SR.Arg_HexStyleNotSupported);
		}
	}
}
