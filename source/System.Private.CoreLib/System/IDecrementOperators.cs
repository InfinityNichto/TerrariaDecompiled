using System.Runtime.Versioning;

namespace System;

[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
public interface IDecrementOperators<TSelf> where TSelf : IDecrementOperators<TSelf>
{
	static abstract TSelf operator --(TSelf value);
}
