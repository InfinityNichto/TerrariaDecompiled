using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Versioning;

namespace System;

[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
public interface INumber<TSelf> : IAdditionOperators<TSelf, TSelf, TSelf>, IAdditiveIdentity<TSelf, TSelf>, IComparisonOperators<TSelf, TSelf>, IComparable, IComparable<TSelf>, IEqualityOperators<TSelf, TSelf>, IEquatable<TSelf>, IDecrementOperators<TSelf>, IDivisionOperators<TSelf, TSelf, TSelf>, IIncrementOperators<TSelf>, IModulusOperators<TSelf, TSelf, TSelf>, IMultiplicativeIdentity<TSelf, TSelf>, IMultiplyOperators<TSelf, TSelf, TSelf>, ISpanFormattable, IFormattable, ISpanParseable<TSelf>, IParseable<TSelf>, ISubtractionOperators<TSelf, TSelf, TSelf>, IUnaryNegationOperators<TSelf, TSelf>, IUnaryPlusOperators<TSelf, TSelf> where TSelf : INumber<TSelf>
{
	static abstract TSelf One { get; }

	static abstract TSelf Zero { get; }

	static abstract TSelf Abs(TSelf value);

	static abstract TSelf Clamp(TSelf value, TSelf min, TSelf max);

	static abstract TSelf Create<TOther>(TOther value) where TOther : INumber<TOther>;

	static abstract TSelf CreateSaturating<TOther>(TOther value) where TOther : INumber<TOther>;

	static abstract TSelf CreateTruncating<TOther>(TOther value) where TOther : INumber<TOther>;

	static abstract (TSelf Quotient, TSelf Remainder) DivRem(TSelf left, TSelf right);

	static abstract TSelf Max(TSelf x, TSelf y);

	static abstract TSelf Min(TSelf x, TSelf y);

	static abstract TSelf Parse(string s, NumberStyles style, IFormatProvider? provider);

	static abstract TSelf Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider);

	static abstract TSelf Sign(TSelf value);

	static abstract bool TryCreate<TOther>(TOther value, out TSelf result) where TOther : INumber<TOther>;

	static abstract bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out TSelf result);

	static abstract bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out TSelf result);
}
