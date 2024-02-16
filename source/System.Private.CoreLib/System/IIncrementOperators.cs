using System.Runtime.Versioning;

namespace System;

[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
public interface IIncrementOperators<TSelf> where TSelf : IIncrementOperators<TSelf>
{
	static abstract TSelf operator ++(TSelf value);
}
