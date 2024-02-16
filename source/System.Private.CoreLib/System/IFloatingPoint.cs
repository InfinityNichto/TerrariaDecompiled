using System.Runtime.Versioning;

namespace System;

[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
public interface IFloatingPoint<TSelf> : ISignedNumber<TSelf>, INumber<TSelf>, IAdditionOperators<TSelf, TSelf, TSelf>, IAdditiveIdentity<TSelf, TSelf>, IComparisonOperators<TSelf, TSelf>, IComparable, IComparable<TSelf>, IEqualityOperators<TSelf, TSelf>, IEquatable<TSelf>, IDecrementOperators<TSelf>, IDivisionOperators<TSelf, TSelf, TSelf>, IIncrementOperators<TSelf>, IModulusOperators<TSelf, TSelf, TSelf>, IMultiplicativeIdentity<TSelf, TSelf>, IMultiplyOperators<TSelf, TSelf, TSelf>, ISpanFormattable, IFormattable, ISpanParseable<TSelf>, IParseable<TSelf>, ISubtractionOperators<TSelf, TSelf, TSelf>, IUnaryNegationOperators<TSelf, TSelf>, IUnaryPlusOperators<TSelf, TSelf> where TSelf : IFloatingPoint<TSelf>
{
	static abstract TSelf E { get; }

	static abstract TSelf Epsilon { get; }

	static abstract TSelf NaN { get; }

	static abstract TSelf NegativeInfinity { get; }

	static abstract TSelf NegativeZero { get; }

	static abstract TSelf Pi { get; }

	static abstract TSelf PositiveInfinity { get; }

	static abstract TSelf Tau { get; }

	static abstract TSelf Acos(TSelf x);

	static abstract TSelf Acosh(TSelf x);

	static abstract TSelf Asin(TSelf x);

	static abstract TSelf Asinh(TSelf x);

	static abstract TSelf Atan(TSelf x);

	static abstract TSelf Atan2(TSelf y, TSelf x);

	static abstract TSelf Atanh(TSelf x);

	static abstract TSelf BitDecrement(TSelf x);

	static abstract TSelf BitIncrement(TSelf x);

	static abstract TSelf Cbrt(TSelf x);

	static abstract TSelf Ceiling(TSelf x);

	static abstract TSelf CopySign(TSelf x, TSelf y);

	static abstract TSelf Cos(TSelf x);

	static abstract TSelf Cosh(TSelf x);

	static abstract TSelf Exp(TSelf x);

	static abstract TSelf Floor(TSelf x);

	static abstract TSelf FusedMultiplyAdd(TSelf left, TSelf right, TSelf addend);

	static abstract TSelf IEEERemainder(TSelf left, TSelf right);

	static abstract TInteger ILogB<TInteger>(TSelf x) where TInteger : IBinaryInteger<TInteger>;

	static abstract bool IsFinite(TSelf value);

	static abstract bool IsInfinity(TSelf value);

	static abstract bool IsNaN(TSelf value);

	static abstract bool IsNegative(TSelf value);

	static abstract bool IsNegativeInfinity(TSelf value);

	static abstract bool IsNormal(TSelf value);

	static abstract bool IsPositiveInfinity(TSelf value);

	static abstract bool IsSubnormal(TSelf value);

	static abstract TSelf Log(TSelf x);

	static abstract TSelf Log(TSelf x, TSelf newBase);

	static abstract TSelf Log2(TSelf x);

	static abstract TSelf Log10(TSelf x);

	static abstract TSelf MaxMagnitude(TSelf x, TSelf y);

	static abstract TSelf MinMagnitude(TSelf x, TSelf y);

	static abstract TSelf Pow(TSelf x, TSelf y);

	static abstract TSelf Round(TSelf x);

	static abstract TSelf Round<TInteger>(TSelf x, TInteger digits) where TInteger : IBinaryInteger<TInteger>;

	static abstract TSelf Round(TSelf x, MidpointRounding mode);

	static abstract TSelf Round<TInteger>(TSelf x, TInteger digits, MidpointRounding mode) where TInteger : IBinaryInteger<TInteger>;

	static abstract TSelf ScaleB<TInteger>(TSelf x, TInteger n) where TInteger : IBinaryInteger<TInteger>;

	static abstract TSelf Sin(TSelf x);

	static abstract TSelf Sinh(TSelf x);

	static abstract TSelf Sqrt(TSelf x);

	static abstract TSelf Tan(TSelf x);

	static abstract TSelf Tanh(TSelf x);

	static abstract TSelf Truncate(TSelf x);
}
