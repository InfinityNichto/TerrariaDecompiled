using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Versioning;
using System.Text;

namespace System.ComponentModel;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
public class MaskedTextProvider : ICloneable
{
	private enum CaseConversion
	{
		None,
		ToLower,
		ToUpper
	}

	[Flags]
	private enum CharType
	{
		EditOptional = 1,
		EditRequired = 2,
		Separator = 4,
		Literal = 8,
		Modifier = 0x10
	}

	private sealed class CharDescriptor
	{
		public int MaskPosition;

		public CaseConversion CaseConversion;

		public CharType CharType;

		public bool IsAssigned;

		public CharDescriptor(int maskPos, CharType charType)
		{
			MaskPosition = maskPos;
			CharType = charType;
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "MaskPosition[{0}] <CaseConversion.{1}><CharType.{2}><IsAssigned: {3}", MaskPosition, CaseConversion, CharType, IsAssigned);
		}
	}

	private static readonly int s_ASCII_ONLY = BitVector32.CreateMask();

	private static readonly int s_ALLOW_PROMPT_AS_INPUT = BitVector32.CreateMask(s_ASCII_ONLY);

	private static readonly int s_INCLUDE_PROMPT = BitVector32.CreateMask(s_ALLOW_PROMPT_AS_INPUT);

	private static readonly int s_INCLUDE_LITERALS = BitVector32.CreateMask(s_INCLUDE_PROMPT);

	private static readonly int s_RESET_ON_PROMPT = BitVector32.CreateMask(s_INCLUDE_LITERALS);

	private static readonly int s_RESET_ON_LITERALS = BitVector32.CreateMask(s_RESET_ON_PROMPT);

	private static readonly int s_SKIP_SPACE = BitVector32.CreateMask(s_RESET_ON_LITERALS);

	private static readonly Type s_maskTextProviderType = typeof(MaskedTextProvider);

	private BitVector32 _flagState;

	private StringBuilder _testString;

	private int _requiredCharCount;

	private int _requiredEditChars;

	private int _optionalEditChars;

	private char _passwordChar;

	private char _promptChar;

	private List<CharDescriptor> _stringDescriptor;

	public bool AllowPromptAsInput => _flagState[s_ALLOW_PROMPT_AS_INPUT];

	public int AssignedEditPositionCount { get; private set; }

	public int AvailableEditPositionCount => EditPositionCount - AssignedEditPositionCount;

	public CultureInfo Culture { get; }

	public static char DefaultPasswordChar => '*';

	public int EditPositionCount => _optionalEditChars + _requiredEditChars;

	public IEnumerator EditPositions
	{
		get
		{
			List<int> list = new List<int>();
			int num = 0;
			foreach (CharDescriptor item in _stringDescriptor)
			{
				if (IsEditPosition(item))
				{
					list.Add(num);
				}
				num++;
			}
			return ((IEnumerable)list).GetEnumerator();
		}
	}

	public bool IncludeLiterals
	{
		get
		{
			return _flagState[s_INCLUDE_LITERALS];
		}
		set
		{
			_flagState[s_INCLUDE_LITERALS] = value;
		}
	}

	public bool IncludePrompt
	{
		get
		{
			return _flagState[s_INCLUDE_PROMPT];
		}
		set
		{
			_flagState[s_INCLUDE_PROMPT] = value;
		}
	}

	public bool AsciiOnly => _flagState[s_ASCII_ONLY];

	public bool IsPassword
	{
		get
		{
			return _passwordChar != '\0';
		}
		set
		{
			if (IsPassword != value)
			{
				_passwordChar = (value ? DefaultPasswordChar : '\0');
			}
		}
	}

	public static int InvalidIndex => -1;

	public int LastAssignedPosition => FindAssignedEditPositionFrom(_testString.Length - 1, direction: false);

	public int Length => _testString.Length;

	public string Mask { get; }

	public bool MaskCompleted => _requiredCharCount == _requiredEditChars;

	public bool MaskFull => AssignedEditPositionCount == EditPositionCount;

	public char PasswordChar
	{
		get
		{
			return _passwordChar;
		}
		set
		{
			if (value == _promptChar)
			{
				throw new InvalidOperationException(System.SR.MaskedTextProviderPasswordAndPromptCharError);
			}
			if (!IsValidPasswordChar(value) && value != 0)
			{
				throw new ArgumentException(System.SR.MaskedTextProviderInvalidCharError);
			}
			if (value != _passwordChar)
			{
				_passwordChar = value;
			}
		}
	}

	public char PromptChar
	{
		get
		{
			return _promptChar;
		}
		set
		{
			if (value == _passwordChar)
			{
				throw new InvalidOperationException(System.SR.MaskedTextProviderPasswordAndPromptCharError);
			}
			if (!IsPrintableChar(value))
			{
				throw new ArgumentException(System.SR.MaskedTextProviderInvalidCharError);
			}
			if (value == _promptChar)
			{
				return;
			}
			_promptChar = value;
			for (int i = 0; i < _testString.Length; i++)
			{
				CharDescriptor charDescriptor = _stringDescriptor[i];
				if (IsEditPosition(i) && !charDescriptor.IsAssigned)
				{
					_testString[i] = _promptChar;
				}
			}
		}
	}

	public bool ResetOnPrompt
	{
		get
		{
			return _flagState[s_RESET_ON_PROMPT];
		}
		set
		{
			_flagState[s_RESET_ON_PROMPT] = value;
		}
	}

	public bool ResetOnSpace
	{
		get
		{
			return _flagState[s_SKIP_SPACE];
		}
		set
		{
			_flagState[s_SKIP_SPACE] = value;
		}
	}

	public bool SkipLiterals
	{
		get
		{
			return _flagState[s_RESET_ON_LITERALS];
		}
		set
		{
			_flagState[s_RESET_ON_LITERALS] = value;
		}
	}

	public char this[int index]
	{
		get
		{
			if (index < 0 || index >= _testString.Length)
			{
				throw new IndexOutOfRangeException(index.ToString(CultureInfo.CurrentCulture));
			}
			return _testString[index];
		}
	}

	public MaskedTextProvider(string mask)
		: this(mask, null, allowPromptAsInput: true, '_', '\0', restrictToAscii: false)
	{
	}

	public MaskedTextProvider(string mask, bool restrictToAscii)
		: this(mask, null, allowPromptAsInput: true, '_', '\0', restrictToAscii)
	{
	}

	public MaskedTextProvider(string mask, CultureInfo? culture)
		: this(mask, culture, allowPromptAsInput: true, '_', '\0', restrictToAscii: false)
	{
	}

	public MaskedTextProvider(string mask, CultureInfo? culture, bool restrictToAscii)
		: this(mask, culture, allowPromptAsInput: true, '_', '\0', restrictToAscii)
	{
	}

	public MaskedTextProvider(string mask, char passwordChar, bool allowPromptAsInput)
		: this(mask, null, allowPromptAsInput, '_', passwordChar, restrictToAscii: false)
	{
	}

	public MaskedTextProvider(string mask, CultureInfo? culture, char passwordChar, bool allowPromptAsInput)
		: this(mask, culture, allowPromptAsInput, '_', passwordChar, restrictToAscii: false)
	{
	}

	public MaskedTextProvider(string mask, CultureInfo? culture, bool allowPromptAsInput, char promptChar, char passwordChar, bool restrictToAscii)
	{
		if (string.IsNullOrEmpty(mask))
		{
			throw new ArgumentException(System.SR.MaskedTextProviderMaskNullOrEmpty, "mask");
		}
		foreach (char c in mask)
		{
			if (!IsPrintableChar(c))
			{
				throw new ArgumentException(System.SR.MaskedTextProviderMaskInvalidChar);
			}
		}
		if (culture == null)
		{
			culture = CultureInfo.CurrentCulture;
		}
		_flagState = default(BitVector32);
		Mask = mask;
		_promptChar = promptChar;
		_passwordChar = passwordChar;
		if (culture.IsNeutralCulture)
		{
			CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
			foreach (CultureInfo cultureInfo in cultures)
			{
				if (culture.Equals(cultureInfo.Parent))
				{
					Culture = cultureInfo;
					break;
				}
			}
			if (Culture == null)
			{
				Culture = CultureInfo.InvariantCulture;
			}
		}
		else
		{
			Culture = culture;
		}
		if (!Culture.IsReadOnly)
		{
			Culture = CultureInfo.ReadOnly(Culture);
		}
		_flagState[s_ALLOW_PROMPT_AS_INPUT] = allowPromptAsInput;
		_flagState[s_ASCII_ONLY] = restrictToAscii;
		_flagState[s_INCLUDE_PROMPT] = false;
		_flagState[s_INCLUDE_LITERALS] = true;
		_flagState[s_RESET_ON_PROMPT] = true;
		_flagState[s_SKIP_SPACE] = true;
		_flagState[s_RESET_ON_LITERALS] = true;
		Initialize();
	}

	[MemberNotNull("_testString")]
	[MemberNotNull("_stringDescriptor")]
	private void Initialize()
	{
		_testString = new StringBuilder();
		_stringDescriptor = new List<CharDescriptor>();
		CaseConversion caseConversion = CaseConversion.None;
		bool flag = false;
		int num = 0;
		CharType charType = CharType.Literal;
		string text = string.Empty;
		for (int i = 0; i < Mask.Length; i++)
		{
			char c = Mask[i];
			if (!flag)
			{
				switch (c)
				{
				case '.':
					text = Culture.NumberFormat.NumberDecimalSeparator;
					charType = CharType.Separator;
					break;
				case ',':
					text = Culture.NumberFormat.NumberGroupSeparator;
					charType = CharType.Separator;
					break;
				case ':':
					text = Culture.DateTimeFormat.TimeSeparator;
					charType = CharType.Separator;
					break;
				case '/':
					text = Culture.DateTimeFormat.DateSeparator;
					charType = CharType.Separator;
					break;
				case '$':
					text = Culture.NumberFormat.CurrencySymbol;
					charType = CharType.Separator;
					break;
				case '<':
					caseConversion = CaseConversion.ToLower;
					continue;
				case '>':
					caseConversion = CaseConversion.ToUpper;
					continue;
				case '|':
					caseConversion = CaseConversion.None;
					continue;
				case '\\':
					flag = true;
					charType = CharType.Literal;
					continue;
				case '&':
				case '0':
				case 'A':
				case 'L':
					_requiredEditChars++;
					c = _promptChar;
					charType = CharType.EditRequired;
					break;
				case '#':
				case '9':
				case '?':
				case 'C':
				case 'a':
					_optionalEditChars++;
					c = _promptChar;
					charType = CharType.EditOptional;
					break;
				default:
					charType = CharType.Literal;
					break;
				}
			}
			else
			{
				flag = false;
			}
			CharDescriptor charDescriptor = new CharDescriptor(i, charType);
			if (IsEditPosition(charDescriptor))
			{
				charDescriptor.CaseConversion = caseConversion;
			}
			if (charType != CharType.Separator)
			{
				text = c.ToString();
			}
			string text2 = text;
			foreach (char value in text2)
			{
				_testString.Append(value);
				_stringDescriptor.Add(charDescriptor);
				num++;
			}
		}
		_testString.Capacity = _testString.Length;
	}

	[UnsupportedOSPlatform("browser")]
	public object Clone()
	{
		Type type = GetType();
		MaskedTextProvider maskedTextProvider;
		if (type == s_maskTextProviderType)
		{
			maskedTextProvider = new MaskedTextProvider(Mask, Culture, AllowPromptAsInput, PromptChar, PasswordChar, AsciiOnly);
		}
		else
		{
			object[] args = new object[6] { Mask, Culture, AllowPromptAsInput, PromptChar, PasswordChar, AsciiOnly };
			maskedTextProvider = Activator.CreateInstance(type, args) as MaskedTextProvider;
		}
		maskedTextProvider.ResetOnPrompt = false;
		maskedTextProvider.ResetOnSpace = false;
		maskedTextProvider.SkipLiterals = false;
		for (int i = 0; i < _testString.Length; i++)
		{
			CharDescriptor charDescriptor = _stringDescriptor[i];
			if (IsEditPosition(charDescriptor) && charDescriptor.IsAssigned)
			{
				maskedTextProvider.Replace(_testString[i], i);
			}
		}
		maskedTextProvider.ResetOnPrompt = ResetOnPrompt;
		maskedTextProvider.ResetOnSpace = ResetOnSpace;
		maskedTextProvider.SkipLiterals = SkipLiterals;
		maskedTextProvider.IncludeLiterals = IncludeLiterals;
		maskedTextProvider.IncludePrompt = IncludePrompt;
		return maskedTextProvider;
	}

	public bool Add(char input)
	{
		int testPosition;
		MaskedTextResultHint resultHint;
		return Add(input, out testPosition, out resultHint);
	}

	public bool Add(char input, out int testPosition, out MaskedTextResultHint resultHint)
	{
		int lastAssignedPosition = LastAssignedPosition;
		if (lastAssignedPosition == _testString.Length - 1)
		{
			testPosition = _testString.Length;
			resultHint = MaskedTextResultHint.UnavailableEditPosition;
			return false;
		}
		testPosition = lastAssignedPosition + 1;
		testPosition = FindEditPositionFrom(testPosition, direction: true);
		if (testPosition == -1)
		{
			resultHint = MaskedTextResultHint.UnavailableEditPosition;
			testPosition = _testString.Length;
			return false;
		}
		if (!TestSetChar(input, testPosition, out resultHint))
		{
			return false;
		}
		return true;
	}

	public bool Add(string input)
	{
		int testPosition;
		MaskedTextResultHint resultHint;
		return Add(input, out testPosition, out resultHint);
	}

	public bool Add(string input, out int testPosition, out MaskedTextResultHint resultHint)
	{
		if (input == null)
		{
			throw new ArgumentNullException("input");
		}
		testPosition = LastAssignedPosition + 1;
		if (input.Length == 0)
		{
			resultHint = MaskedTextResultHint.NoEffect;
			return true;
		}
		return TestSetString(input, testPosition, out testPosition, out resultHint);
	}

	public void Clear()
	{
		Clear(out var _);
	}

	public void Clear(out MaskedTextResultHint resultHint)
	{
		if (AssignedEditPositionCount == 0)
		{
			resultHint = MaskedTextResultHint.NoEffect;
			return;
		}
		resultHint = MaskedTextResultHint.Success;
		for (int i = 0; i < _testString.Length; i++)
		{
			ResetChar(i);
		}
	}

	public int FindAssignedEditPositionFrom(int position, bool direction)
	{
		if (AssignedEditPositionCount == 0)
		{
			return -1;
		}
		int startPosition;
		int endPosition;
		if (direction)
		{
			startPosition = position;
			endPosition = _testString.Length - 1;
		}
		else
		{
			startPosition = 0;
			endPosition = position;
		}
		return FindAssignedEditPositionInRange(startPosition, endPosition, direction);
	}

	public int FindAssignedEditPositionInRange(int startPosition, int endPosition, bool direction)
	{
		if (AssignedEditPositionCount == 0)
		{
			return -1;
		}
		return FindEditPositionInRange(startPosition, endPosition, direction, 2);
	}

	public int FindEditPositionFrom(int position, bool direction)
	{
		int startPosition;
		int endPosition;
		if (direction)
		{
			startPosition = position;
			endPosition = _testString.Length - 1;
		}
		else
		{
			startPosition = 0;
			endPosition = position;
		}
		return FindEditPositionInRange(startPosition, endPosition, direction);
	}

	public int FindEditPositionInRange(int startPosition, int endPosition, bool direction)
	{
		CharType charTypeFlags = CharType.EditOptional | CharType.EditRequired;
		return FindPositionInRange(startPosition, endPosition, direction, charTypeFlags);
	}

	private int FindEditPositionInRange(int startPosition, int endPosition, bool direction, byte assignedStatus)
	{
		do
		{
			int num = FindEditPositionInRange(startPosition, endPosition, direction);
			if (num == -1)
			{
				break;
			}
			CharDescriptor charDescriptor = _stringDescriptor[num];
			switch (assignedStatus)
			{
			case 1:
				if (!charDescriptor.IsAssigned)
				{
					return num;
				}
				break;
			case 2:
				if (charDescriptor.IsAssigned)
				{
					return num;
				}
				break;
			default:
				return num;
			}
			if (direction)
			{
				startPosition++;
			}
			else
			{
				endPosition--;
			}
		}
		while (startPosition <= endPosition);
		return -1;
	}

	public int FindNonEditPositionFrom(int position, bool direction)
	{
		int startPosition;
		int endPosition;
		if (direction)
		{
			startPosition = position;
			endPosition = _testString.Length - 1;
		}
		else
		{
			startPosition = 0;
			endPosition = position;
		}
		return FindNonEditPositionInRange(startPosition, endPosition, direction);
	}

	public int FindNonEditPositionInRange(int startPosition, int endPosition, bool direction)
	{
		CharType charTypeFlags = CharType.Separator | CharType.Literal;
		return FindPositionInRange(startPosition, endPosition, direction, charTypeFlags);
	}

	private int FindPositionInRange(int startPosition, int endPosition, bool direction, CharType charTypeFlags)
	{
		if (startPosition < 0)
		{
			startPosition = 0;
		}
		if (endPosition >= _testString.Length)
		{
			endPosition = _testString.Length - 1;
		}
		if (startPosition > endPosition)
		{
			return -1;
		}
		while (startPosition <= endPosition)
		{
			int num = (direction ? startPosition++ : endPosition--);
			CharDescriptor charDescriptor = _stringDescriptor[num];
			if ((charDescriptor.CharType & charTypeFlags) == charDescriptor.CharType)
			{
				return num;
			}
		}
		return -1;
	}

	public int FindUnassignedEditPositionFrom(int position, bool direction)
	{
		int startPosition;
		int endPosition;
		if (direction)
		{
			startPosition = position;
			endPosition = _testString.Length - 1;
		}
		else
		{
			startPosition = 0;
			endPosition = position;
		}
		return FindEditPositionInRange(startPosition, endPosition, direction, 1);
	}

	public int FindUnassignedEditPositionInRange(int startPosition, int endPosition, bool direction)
	{
		int num;
		while (true)
		{
			num = FindEditPositionInRange(startPosition, endPosition, direction, 0);
			if (num == -1)
			{
				return -1;
			}
			CharDescriptor charDescriptor = _stringDescriptor[num];
			if (!charDescriptor.IsAssigned)
			{
				break;
			}
			if (direction)
			{
				startPosition++;
			}
			else
			{
				endPosition--;
			}
		}
		return num;
	}

	public static bool GetOperationResultFromHint(MaskedTextResultHint hint)
	{
		return hint > MaskedTextResultHint.Unknown;
	}

	public bool InsertAt(char input, int position)
	{
		if (position < 0 || position >= _testString.Length)
		{
			return false;
		}
		return InsertAt(input.ToString(), position);
	}

	public bool InsertAt(char input, int position, out int testPosition, out MaskedTextResultHint resultHint)
	{
		return InsertAt(input.ToString(), position, out testPosition, out resultHint);
	}

	public bool InsertAt(string input, int position)
	{
		int testPosition;
		MaskedTextResultHint resultHint;
		return InsertAt(input, position, out testPosition, out resultHint);
	}

	public bool InsertAt(string input, int position, out int testPosition, out MaskedTextResultHint resultHint)
	{
		if (input == null)
		{
			throw new ArgumentNullException("input");
		}
		if (position < 0 || position >= _testString.Length)
		{
			testPosition = position;
			resultHint = MaskedTextResultHint.PositionOutOfRange;
			return false;
		}
		return InsertAtInt(input, position, out testPosition, out resultHint, testOnly: false);
	}

	private bool InsertAtInt(string input, int position, out int testPosition, out MaskedTextResultHint resultHint, bool testOnly)
	{
		if (input.Length == 0)
		{
			testPosition = position;
			resultHint = MaskedTextResultHint.NoEffect;
			return true;
		}
		if (!TestString(input, position, out testPosition, out resultHint))
		{
			return false;
		}
		int num = FindEditPositionFrom(position, direction: true);
		bool flag = FindAssignedEditPositionInRange(num, testPosition, direction: true) != -1;
		int lastAssignedPosition = LastAssignedPosition;
		if (flag && testPosition == _testString.Length - 1)
		{
			resultHint = MaskedTextResultHint.UnavailableEditPosition;
			testPosition = _testString.Length;
			return false;
		}
		int num2 = FindEditPositionFrom(testPosition + 1, direction: true);
		if (flag)
		{
			MaskedTextResultHint resultHint2 = MaskedTextResultHint.Unknown;
			while (true)
			{
				if (num2 == -1)
				{
					resultHint = MaskedTextResultHint.UnavailableEditPosition;
					testPosition = _testString.Length;
					return false;
				}
				CharDescriptor charDescriptor = _stringDescriptor[num];
				if (charDescriptor.IsAssigned && !TestChar(_testString[num], num2, out resultHint2))
				{
					resultHint = resultHint2;
					testPosition = num2;
					return false;
				}
				if (num == lastAssignedPosition)
				{
					break;
				}
				num = FindEditPositionFrom(num + 1, direction: true);
				num2 = FindEditPositionFrom(num2 + 1, direction: true);
			}
			if (resultHint2 > resultHint)
			{
				resultHint = resultHint2;
			}
		}
		if (testOnly)
		{
			return true;
		}
		if (flag)
		{
			while (num >= position)
			{
				CharDescriptor charDescriptor2 = _stringDescriptor[num];
				if (charDescriptor2.IsAssigned)
				{
					SetChar(_testString[num], num2);
				}
				else
				{
					ResetChar(num2);
				}
				num2 = FindEditPositionFrom(num2 - 1, direction: false);
				num = FindEditPositionFrom(num - 1, direction: false);
			}
		}
		SetString(input, position);
		return true;
	}

	private static bool IsAscii(char c)
	{
		if (c >= '!')
		{
			return c <= '~';
		}
		return false;
	}

	private static bool IsAciiAlphanumeric(char c)
	{
		if ((c < '0' || c > '9') && (c < 'A' || c > 'Z'))
		{
			if (c >= 'a')
			{
				return c <= 'z';
			}
			return false;
		}
		return true;
	}

	private static bool IsAlphanumeric(char c)
	{
		if (!char.IsLetter(c))
		{
			return char.IsDigit(c);
		}
		return true;
	}

	private static bool IsAsciiLetter(char c)
	{
		if (c < 'A' || c > 'Z')
		{
			if (c >= 'a')
			{
				return c <= 'z';
			}
			return false;
		}
		return true;
	}

	public bool IsAvailablePosition(int position)
	{
		if (position < 0 || position >= _testString.Length)
		{
			return false;
		}
		CharDescriptor charDescriptor = _stringDescriptor[position];
		if (IsEditPosition(charDescriptor))
		{
			return !charDescriptor.IsAssigned;
		}
		return false;
	}

	public bool IsEditPosition(int position)
	{
		if (position < 0 || position >= _testString.Length)
		{
			return false;
		}
		CharDescriptor charDescriptor = _stringDescriptor[position];
		return IsEditPosition(charDescriptor);
	}

	private static bool IsEditPosition(CharDescriptor charDescriptor)
	{
		if (charDescriptor.CharType != CharType.EditRequired)
		{
			return charDescriptor.CharType == CharType.EditOptional;
		}
		return true;
	}

	private static bool IsLiteralPosition(CharDescriptor charDescriptor)
	{
		if (charDescriptor.CharType != CharType.Literal)
		{
			return charDescriptor.CharType == CharType.Separator;
		}
		return true;
	}

	private static bool IsPrintableChar(char c)
	{
		if (!char.IsLetterOrDigit(c) && !char.IsPunctuation(c) && !char.IsSymbol(c))
		{
			return c == ' ';
		}
		return true;
	}

	public static bool IsValidInputChar(char c)
	{
		return IsPrintableChar(c);
	}

	public static bool IsValidMaskChar(char c)
	{
		return IsPrintableChar(c);
	}

	public static bool IsValidPasswordChar(char c)
	{
		if (!IsPrintableChar(c))
		{
			return c == '\0';
		}
		return true;
	}

	public bool Remove()
	{
		int testPosition;
		MaskedTextResultHint resultHint;
		return Remove(out testPosition, out resultHint);
	}

	public bool Remove(out int testPosition, out MaskedTextResultHint resultHint)
	{
		int lastAssignedPosition = LastAssignedPosition;
		if (lastAssignedPosition == -1)
		{
			testPosition = 0;
			resultHint = MaskedTextResultHint.NoEffect;
			return true;
		}
		ResetChar(lastAssignedPosition);
		testPosition = lastAssignedPosition;
		resultHint = MaskedTextResultHint.Success;
		return true;
	}

	public bool RemoveAt(int position)
	{
		return RemoveAt(position, position);
	}

	public bool RemoveAt(int startPosition, int endPosition)
	{
		int testPosition;
		MaskedTextResultHint resultHint;
		return RemoveAt(startPosition, endPosition, out testPosition, out resultHint);
	}

	public bool RemoveAt(int startPosition, int endPosition, out int testPosition, out MaskedTextResultHint resultHint)
	{
		if (endPosition >= _testString.Length)
		{
			testPosition = endPosition;
			resultHint = MaskedTextResultHint.PositionOutOfRange;
			return false;
		}
		if (startPosition < 0 || startPosition > endPosition)
		{
			testPosition = startPosition;
			resultHint = MaskedTextResultHint.PositionOutOfRange;
			return false;
		}
		return RemoveAtInt(startPosition, endPosition, out testPosition, out resultHint, testOnly: false);
	}

	private bool RemoveAtInt(int startPosition, int endPosition, out int testPosition, out MaskedTextResultHint resultHint, bool testOnly)
	{
		int lastAssignedPosition = LastAssignedPosition;
		int num = FindEditPositionInRange(startPosition, endPosition, direction: true);
		resultHint = MaskedTextResultHint.NoEffect;
		if (num == -1 || num > lastAssignedPosition)
		{
			testPosition = startPosition;
			return true;
		}
		testPosition = startPosition;
		bool flag = endPosition < lastAssignedPosition;
		if (FindAssignedEditPositionInRange(startPosition, endPosition, direction: true) != -1)
		{
			resultHint = MaskedTextResultHint.Success;
		}
		if (flag)
		{
			int num2 = FindEditPositionFrom(endPosition + 1, direction: true);
			int num3 = num2;
			startPosition = num;
			while (true)
			{
				char c = _testString[num2];
				CharDescriptor charDescriptor = _stringDescriptor[num2];
				if ((c != PromptChar || charDescriptor.IsAssigned) && !TestChar(c, num, out var resultHint2))
				{
					resultHint = resultHint2;
					testPosition = num;
					return false;
				}
				if (num2 == lastAssignedPosition)
				{
					break;
				}
				num2 = FindEditPositionFrom(num2 + 1, direction: true);
				num = FindEditPositionFrom(num + 1, direction: true);
			}
			if (MaskedTextResultHint.SideEffect > resultHint)
			{
				resultHint = MaskedTextResultHint.SideEffect;
			}
			if (testOnly)
			{
				return true;
			}
			num2 = num3;
			num = startPosition;
			while (true)
			{
				char c2 = _testString[num2];
				CharDescriptor charDescriptor2 = _stringDescriptor[num2];
				if (c2 == PromptChar && !charDescriptor2.IsAssigned)
				{
					ResetChar(num);
				}
				else
				{
					SetChar(c2, num);
					ResetChar(num2);
				}
				if (num2 == lastAssignedPosition)
				{
					break;
				}
				num2 = FindEditPositionFrom(num2 + 1, direction: true);
				num = FindEditPositionFrom(num + 1, direction: true);
			}
			startPosition = num + 1;
		}
		if (startPosition <= endPosition)
		{
			ResetString(startPosition, endPosition);
		}
		return true;
	}

	public bool Replace(char input, int position)
	{
		int testPosition;
		MaskedTextResultHint resultHint;
		return Replace(input, position, out testPosition, out resultHint);
	}

	public bool Replace(char input, int position, out int testPosition, out MaskedTextResultHint resultHint)
	{
		if (position < 0 || position >= _testString.Length)
		{
			testPosition = position;
			resultHint = MaskedTextResultHint.PositionOutOfRange;
			return false;
		}
		testPosition = position;
		if (!TestEscapeChar(input, testPosition))
		{
			testPosition = FindEditPositionFrom(testPosition, direction: true);
		}
		if (testPosition == -1)
		{
			resultHint = MaskedTextResultHint.UnavailableEditPosition;
			testPosition = position;
			return false;
		}
		if (!TestSetChar(input, testPosition, out resultHint))
		{
			return false;
		}
		return true;
	}

	public bool Replace(char input, int startPosition, int endPosition, out int testPosition, out MaskedTextResultHint resultHint)
	{
		if (endPosition >= _testString.Length)
		{
			testPosition = endPosition;
			resultHint = MaskedTextResultHint.PositionOutOfRange;
			return false;
		}
		if (startPosition < 0 || startPosition > endPosition)
		{
			testPosition = startPosition;
			resultHint = MaskedTextResultHint.PositionOutOfRange;
			return false;
		}
		if (startPosition == endPosition)
		{
			testPosition = startPosition;
			return TestSetChar(input, startPosition, out resultHint);
		}
		return Replace(input.ToString(), startPosition, endPosition, out testPosition, out resultHint);
	}

	public bool Replace(string input, int position)
	{
		int testPosition;
		MaskedTextResultHint resultHint;
		return Replace(input, position, out testPosition, out resultHint);
	}

	public bool Replace(string input, int position, out int testPosition, out MaskedTextResultHint resultHint)
	{
		if (input == null)
		{
			throw new ArgumentNullException("input");
		}
		if (position < 0 || position >= _testString.Length)
		{
			testPosition = position;
			resultHint = MaskedTextResultHint.PositionOutOfRange;
			return false;
		}
		if (input.Length == 0)
		{
			return RemoveAt(position, position, out testPosition, out resultHint);
		}
		if (!TestSetString(input, position, out testPosition, out resultHint))
		{
			return false;
		}
		return true;
	}

	public bool Replace(string input, int startPosition, int endPosition, out int testPosition, out MaskedTextResultHint resultHint)
	{
		if (input == null)
		{
			throw new ArgumentNullException("input");
		}
		if (endPosition >= _testString.Length)
		{
			testPosition = endPosition;
			resultHint = MaskedTextResultHint.PositionOutOfRange;
			return false;
		}
		if (startPosition < 0 || startPosition > endPosition)
		{
			testPosition = startPosition;
			resultHint = MaskedTextResultHint.PositionOutOfRange;
			return false;
		}
		if (input.Length == 0)
		{
			return RemoveAt(startPosition, endPosition, out testPosition, out resultHint);
		}
		if (!TestString(input, startPosition, out testPosition, out resultHint))
		{
			return false;
		}
		if (AssignedEditPositionCount > 0)
		{
			MaskedTextResultHint resultHint2;
			if (testPosition < endPosition)
			{
				if (!RemoveAtInt(testPosition + 1, endPosition, out var testPosition2, out resultHint2, testOnly: false))
				{
					testPosition = testPosition2;
					resultHint = resultHint2;
					return false;
				}
				if (resultHint2 == MaskedTextResultHint.Success && resultHint != resultHint2)
				{
					resultHint = MaskedTextResultHint.SideEffect;
				}
			}
			else if (testPosition > endPosition)
			{
				int lastAssignedPosition = LastAssignedPosition;
				int position = testPosition + 1;
				int position2 = endPosition + 1;
				while (true)
				{
					position2 = FindEditPositionFrom(position2, direction: true);
					position = FindEditPositionFrom(position, direction: true);
					if (position == -1)
					{
						testPosition = _testString.Length;
						resultHint = MaskedTextResultHint.UnavailableEditPosition;
						return false;
					}
					if (!TestChar(_testString[position2], position, out resultHint2))
					{
						testPosition = position;
						resultHint = resultHint2;
						return false;
					}
					if (resultHint2 == MaskedTextResultHint.Success && resultHint != resultHint2)
					{
						resultHint = MaskedTextResultHint.Success;
					}
					if (position2 == lastAssignedPosition)
					{
						break;
					}
					position2++;
					position++;
				}
				while (position > testPosition)
				{
					SetChar(_testString[position2], position);
					position2 = FindEditPositionFrom(position2 - 1, direction: false);
					position = FindEditPositionFrom(position - 1, direction: false);
				}
			}
		}
		SetString(input, startPosition);
		return true;
	}

	private void ResetChar(int testPosition)
	{
		CharDescriptor charDescriptor = _stringDescriptor[testPosition];
		if (IsEditPosition(testPosition) && charDescriptor.IsAssigned)
		{
			charDescriptor.IsAssigned = false;
			_testString[testPosition] = _promptChar;
			AssignedEditPositionCount--;
			if (charDescriptor.CharType == CharType.EditRequired)
			{
				_requiredCharCount--;
			}
		}
	}

	private void ResetString(int startPosition, int endPosition)
	{
		startPosition = FindAssignedEditPositionFrom(startPosition, direction: true);
		if (startPosition != -1)
		{
			endPosition = FindAssignedEditPositionFrom(endPosition, direction: false);
			while (startPosition <= endPosition)
			{
				startPosition = FindAssignedEditPositionFrom(startPosition, direction: true);
				ResetChar(startPosition);
				startPosition++;
			}
		}
	}

	public bool Set(string input)
	{
		int testPosition;
		MaskedTextResultHint resultHint;
		return Set(input, out testPosition, out resultHint);
	}

	public bool Set(string input, out int testPosition, out MaskedTextResultHint resultHint)
	{
		if (input == null)
		{
			throw new ArgumentNullException("input");
		}
		resultHint = MaskedTextResultHint.Unknown;
		testPosition = 0;
		if (input.Length == 0)
		{
			Clear(out resultHint);
			return true;
		}
		if (!TestSetString(input, testPosition, out testPosition, out resultHint))
		{
			return false;
		}
		int num = FindAssignedEditPositionFrom(testPosition + 1, direction: true);
		if (num != -1)
		{
			ResetString(num, _testString.Length - 1);
		}
		return true;
	}

	private void SetChar(char input, int position)
	{
		CharDescriptor charDescriptor = _stringDescriptor[position];
		SetChar(input, position, charDescriptor);
	}

	private void SetChar(char input, int position, CharDescriptor charDescriptor)
	{
		CharDescriptor charDescriptor2 = _stringDescriptor[position];
		if (TestEscapeChar(input, position, charDescriptor))
		{
			ResetChar(position);
			return;
		}
		if (char.IsLetter(input))
		{
			if (char.IsUpper(input))
			{
				if (charDescriptor.CaseConversion == CaseConversion.ToLower)
				{
					input = Culture.TextInfo.ToLower(input);
				}
			}
			else if (charDescriptor.CaseConversion == CaseConversion.ToUpper)
			{
				input = Culture.TextInfo.ToUpper(input);
			}
		}
		_testString[position] = input;
		if (!charDescriptor.IsAssigned)
		{
			charDescriptor.IsAssigned = true;
			AssignedEditPositionCount++;
			if (charDescriptor.CharType == CharType.EditRequired)
			{
				_requiredCharCount++;
			}
		}
	}

	private void SetString(string input, int testPosition)
	{
		foreach (char input2 in input)
		{
			if (!TestEscapeChar(input2, testPosition))
			{
				testPosition = FindEditPositionFrom(testPosition, direction: true);
			}
			SetChar(input2, testPosition);
			testPosition++;
		}
	}

	private bool TestChar(char input, int position, out MaskedTextResultHint resultHint)
	{
		if (!IsPrintableChar(input))
		{
			resultHint = MaskedTextResultHint.InvalidInput;
			return false;
		}
		CharDescriptor charDescriptor = _stringDescriptor[position];
		if (IsLiteralPosition(charDescriptor))
		{
			if (SkipLiterals && input == _testString[position])
			{
				resultHint = MaskedTextResultHint.CharacterEscaped;
				return true;
			}
			resultHint = MaskedTextResultHint.NonEditPosition;
			return false;
		}
		if (input == _promptChar)
		{
			if (ResetOnPrompt)
			{
				if (IsEditPosition(charDescriptor) && charDescriptor.IsAssigned)
				{
					resultHint = MaskedTextResultHint.SideEffect;
				}
				else
				{
					resultHint = MaskedTextResultHint.CharacterEscaped;
				}
				return true;
			}
			if (!AllowPromptAsInput)
			{
				resultHint = MaskedTextResultHint.PromptCharNotAllowed;
				return false;
			}
		}
		if (input == ' ' && ResetOnSpace)
		{
			if (IsEditPosition(charDescriptor) && charDescriptor.IsAssigned)
			{
				resultHint = MaskedTextResultHint.SideEffect;
			}
			else
			{
				resultHint = MaskedTextResultHint.CharacterEscaped;
			}
			return true;
		}
		switch (Mask[charDescriptor.MaskPosition])
		{
		case '#':
			if (!char.IsDigit(input) && input != '-' && input != '+' && input != ' ')
			{
				resultHint = MaskedTextResultHint.DigitExpected;
				return false;
			}
			break;
		case '0':
			if (!char.IsDigit(input))
			{
				resultHint = MaskedTextResultHint.DigitExpected;
				return false;
			}
			break;
		case '9':
			if (!char.IsDigit(input) && input != ' ')
			{
				resultHint = MaskedTextResultHint.DigitExpected;
				return false;
			}
			break;
		case 'L':
			if (!char.IsLetter(input))
			{
				resultHint = MaskedTextResultHint.LetterExpected;
				return false;
			}
			if (!IsAsciiLetter(input) && AsciiOnly)
			{
				resultHint = MaskedTextResultHint.AsciiCharacterExpected;
				return false;
			}
			break;
		case '?':
			if (!char.IsLetter(input) && input != ' ')
			{
				resultHint = MaskedTextResultHint.LetterExpected;
				return false;
			}
			if (!IsAsciiLetter(input) && AsciiOnly)
			{
				resultHint = MaskedTextResultHint.AsciiCharacterExpected;
				return false;
			}
			break;
		case '&':
			if (!IsAscii(input) && AsciiOnly)
			{
				resultHint = MaskedTextResultHint.AsciiCharacterExpected;
				return false;
			}
			break;
		case 'C':
			if (!IsAscii(input) && AsciiOnly && input != ' ')
			{
				resultHint = MaskedTextResultHint.AsciiCharacterExpected;
				return false;
			}
			break;
		case 'A':
			if (!IsAlphanumeric(input))
			{
				resultHint = MaskedTextResultHint.AlphanumericCharacterExpected;
				return false;
			}
			if (!IsAciiAlphanumeric(input) && AsciiOnly)
			{
				resultHint = MaskedTextResultHint.AsciiCharacterExpected;
				return false;
			}
			break;
		case 'a':
			if (!IsAlphanumeric(input) && input != ' ')
			{
				resultHint = MaskedTextResultHint.AlphanumericCharacterExpected;
				return false;
			}
			if (!IsAciiAlphanumeric(input) && AsciiOnly)
			{
				resultHint = MaskedTextResultHint.AsciiCharacterExpected;
				return false;
			}
			break;
		}
		if (input == _testString[position] && charDescriptor.IsAssigned)
		{
			resultHint = MaskedTextResultHint.NoEffect;
		}
		else
		{
			resultHint = MaskedTextResultHint.Success;
		}
		return true;
	}

	private bool TestEscapeChar(char input, int position)
	{
		CharDescriptor charDex = _stringDescriptor[position];
		return TestEscapeChar(input, position, charDex);
	}

	private bool TestEscapeChar(char input, int position, CharDescriptor charDex)
	{
		if (IsLiteralPosition(charDex))
		{
			if (SkipLiterals)
			{
				return input == _testString[position];
			}
			return false;
		}
		if ((ResetOnPrompt && input == _promptChar) || (ResetOnSpace && input == ' '))
		{
			return true;
		}
		return false;
	}

	private bool TestSetChar(char input, int position, out MaskedTextResultHint resultHint)
	{
		if (TestChar(input, position, out resultHint))
		{
			if (resultHint == MaskedTextResultHint.Success || resultHint == MaskedTextResultHint.SideEffect)
			{
				SetChar(input, position);
			}
			return true;
		}
		return false;
	}

	private bool TestSetString(string input, int position, out int testPosition, out MaskedTextResultHint resultHint)
	{
		if (TestString(input, position, out testPosition, out resultHint))
		{
			SetString(input, position);
			return true;
		}
		return false;
	}

	private bool TestString(string input, int position, out int testPosition, out MaskedTextResultHint resultHint)
	{
		resultHint = MaskedTextResultHint.Unknown;
		testPosition = position;
		if (input.Length == 0)
		{
			return true;
		}
		MaskedTextResultHint resultHint2 = resultHint;
		foreach (char input2 in input)
		{
			if (testPosition >= _testString.Length)
			{
				resultHint = MaskedTextResultHint.UnavailableEditPosition;
				return false;
			}
			if (!TestEscapeChar(input2, testPosition))
			{
				testPosition = FindEditPositionFrom(testPosition, direction: true);
				if (testPosition == -1)
				{
					testPosition = _testString.Length;
					resultHint = MaskedTextResultHint.UnavailableEditPosition;
					return false;
				}
			}
			if (!TestChar(input2, testPosition, out resultHint2))
			{
				resultHint = resultHint2;
				return false;
			}
			if (resultHint2 > resultHint)
			{
				resultHint = resultHint2;
			}
			testPosition++;
		}
		testPosition--;
		return true;
	}

	public string ToDisplayString()
	{
		if (!IsPassword || AssignedEditPositionCount == 0)
		{
			return _testString.ToString();
		}
		StringBuilder stringBuilder = new StringBuilder(_testString.Length);
		for (int i = 0; i < _testString.Length; i++)
		{
			CharDescriptor charDescriptor = _stringDescriptor[i];
			stringBuilder.Append((IsEditPosition(charDescriptor) && charDescriptor.IsAssigned) ? _passwordChar : _testString[i]);
		}
		return stringBuilder.ToString();
	}

	public override string ToString()
	{
		return ToString(ignorePasswordChar: true, IncludePrompt, IncludeLiterals, 0, _testString.Length);
	}

	public string ToString(bool ignorePasswordChar)
	{
		return ToString(ignorePasswordChar, IncludePrompt, IncludeLiterals, 0, _testString.Length);
	}

	public string ToString(int startPosition, int length)
	{
		return ToString(ignorePasswordChar: true, IncludePrompt, IncludeLiterals, startPosition, length);
	}

	public string ToString(bool ignorePasswordChar, int startPosition, int length)
	{
		return ToString(ignorePasswordChar, IncludePrompt, IncludeLiterals, startPosition, length);
	}

	public string ToString(bool includePrompt, bool includeLiterals)
	{
		return ToString(ignorePasswordChar: true, includePrompt, includeLiterals, 0, _testString.Length);
	}

	public string ToString(bool includePrompt, bool includeLiterals, int startPosition, int length)
	{
		return ToString(ignorePasswordChar: true, includePrompt, includeLiterals, startPosition, length);
	}

	public string ToString(bool ignorePasswordChar, bool includePrompt, bool includeLiterals, int startPosition, int length)
	{
		if (length <= 0)
		{
			return string.Empty;
		}
		if (startPosition < 0)
		{
			startPosition = 0;
		}
		if (startPosition >= _testString.Length)
		{
			return string.Empty;
		}
		int num = _testString.Length - startPosition;
		if (length > num)
		{
			length = num;
		}
		if ((!IsPassword || ignorePasswordChar) && includePrompt && includeLiterals)
		{
			return _testString.ToString(startPosition, length);
		}
		StringBuilder stringBuilder = new StringBuilder();
		int num2 = startPosition + length - 1;
		if (!includePrompt)
		{
			int num3 = (includeLiterals ? FindNonEditPositionInRange(startPosition, num2, direction: false) : InvalidIndex);
			int num4 = FindAssignedEditPositionInRange((num3 == InvalidIndex) ? startPosition : num3, num2, direction: false);
			num2 = ((num4 != InvalidIndex) ? num4 : num3);
			if (num2 == InvalidIndex)
			{
				return string.Empty;
			}
		}
		for (int i = startPosition; i <= num2; i++)
		{
			char value = _testString[i];
			CharDescriptor charDescriptor = _stringDescriptor[i];
			switch (charDescriptor.CharType)
			{
			case CharType.EditOptional:
			case CharType.EditRequired:
				if (charDescriptor.IsAssigned)
				{
					if (IsPassword && !ignorePasswordChar)
					{
						stringBuilder.Append(_passwordChar);
						continue;
					}
				}
				else if (!includePrompt)
				{
					stringBuilder.Append(' ');
					continue;
				}
				break;
			case CharType.Separator:
			case CharType.Literal:
				if (!includeLiterals)
				{
					continue;
				}
				break;
			}
			stringBuilder.Append(value);
		}
		return stringBuilder.ToString();
	}

	public bool VerifyChar(char input, int position, out MaskedTextResultHint hint)
	{
		hint = MaskedTextResultHint.NoEffect;
		if (position < 0 || position >= _testString.Length)
		{
			hint = MaskedTextResultHint.PositionOutOfRange;
			return false;
		}
		return TestChar(input, position, out hint);
	}

	public bool VerifyEscapeChar(char input, int position)
	{
		if (position < 0 || position >= _testString.Length)
		{
			return false;
		}
		return TestEscapeChar(input, position);
	}

	public bool VerifyString(string input)
	{
		int testPosition;
		MaskedTextResultHint resultHint;
		return VerifyString(input, out testPosition, out resultHint);
	}

	public bool VerifyString(string input, out int testPosition, out MaskedTextResultHint resultHint)
	{
		testPosition = 0;
		if (input == null || input.Length == 0)
		{
			resultHint = MaskedTextResultHint.NoEffect;
			return true;
		}
		return TestString(input, 0, out testPosition, out resultHint);
	}
}
