using System.Runtime.Versioning;

namespace System;

[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
public interface ISpanParseable<TSelf> : IParseable<TSelf> where TSelf : ISpanParseable<TSelf>
{
	static abstract TSelf Parse(ReadOnlySpan<char> s, IFormatProvider? provider);

	static abstract bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out TSelf result);
}
