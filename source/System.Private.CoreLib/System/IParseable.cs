using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace System;

[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
public interface IParseable<TSelf> where TSelf : IParseable<TSelf>
{
	static abstract TSelf Parse(string s, IFormatProvider? provider);

	static abstract bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out TSelf result);
}
