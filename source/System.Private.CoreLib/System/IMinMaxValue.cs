using System.Runtime.Versioning;

namespace System;

[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
public interface IMinMaxValue<TSelf> where TSelf : IMinMaxValue<TSelf>
{
	static abstract TSelf MinValue { get; }

	static abstract TSelf MaxValue { get; }
}
