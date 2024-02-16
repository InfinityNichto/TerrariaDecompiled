using System.Runtime.Versioning;

namespace System;

[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
public interface IBinaryInteger<TSelf> : IBinaryNumber<TSelf>, IBitwiseOperators<TSelf, TSelf, TSelf>, INumber<TSelf>, IAdditionOperators<TSelf, TSelf, TSelf>, IAdditiveIdentity<TSelf, TSelf>, IComparisonOperators<TSelf, TSelf>, IComparable, IComparable<TSelf>, IEqualityOperators<TSelf, TSelf>, IEquatable<TSelf>, IDecrementOperators<TSelf>, IDivisionOperators<TSelf, TSelf, TSelf>, IIncrementOperators<TSelf>, IModulusOperators<TSelf, TSelf, TSelf>, IMultiplicativeIdentity<TSelf, TSelf>, IMultiplyOperators<TSelf, TSelf, TSelf>, ISpanFormattable, IFormattable, ISpanParseable<TSelf>, IParseable<TSelf>, ISubtractionOperators<TSelf, TSelf, TSelf>, IUnaryNegationOperators<TSelf, TSelf>, IUnaryPlusOperators<TSelf, TSelf>, IShiftOperators<TSelf, TSelf> where TSelf : IBinaryInteger<TSelf>
{
	static abstract TSelf LeadingZeroCount(TSelf value);

	static abstract TSelf PopCount(TSelf value);

	static abstract TSelf RotateLeft(TSelf value, int rotateAmount);

	static abstract TSelf RotateRight(TSelf value, int rotateAmount);

	static abstract TSelf TrailingZeroCount(TSelf value);
}
