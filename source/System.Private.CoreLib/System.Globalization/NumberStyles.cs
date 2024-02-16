namespace System.Globalization;

[Flags]
public enum NumberStyles
{
	None = 0,
	AllowLeadingWhite = 1,
	AllowTrailingWhite = 2,
	AllowLeadingSign = 4,
	AllowTrailingSign = 8,
	AllowParentheses = 0x10,
	AllowDecimalPoint = 0x20,
	AllowThousands = 0x40,
	AllowExponent = 0x80,
	AllowCurrencySymbol = 0x100,
	AllowHexSpecifier = 0x200,
	Integer = 7,
	HexNumber = 0x203,
	Number = 0x6F,
	Float = 0xA7,
	Currency = 0x17F,
	Any = 0x1FF
}
