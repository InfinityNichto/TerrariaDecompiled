using System.Runtime.Versioning;

namespace System;

[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
public interface IEqualityOperators<TSelf, TOther> : IEquatable<TOther> where TSelf : IEqualityOperators<TSelf, TOther>
{
	static abstract bool operator ==(TSelf left, TOther right);

	static abstract bool operator !=(TSelf left, TOther right);
}
