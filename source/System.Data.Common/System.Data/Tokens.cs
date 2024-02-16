namespace System.Data;

internal enum Tokens
{
	None,
	Name,
	Numeric,
	Decimal,
	Float,
	BinaryConst,
	StringConst,
	Date,
	ListSeparator,
	LeftParen,
	RightParen,
	ZeroOp,
	UnaryOp,
	BinaryOp,
	Child,
	Parent,
	Dot,
	Unknown,
	EOS
}
