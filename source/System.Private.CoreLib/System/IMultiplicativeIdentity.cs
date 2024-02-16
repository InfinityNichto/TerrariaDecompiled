using System.Runtime.Versioning;

namespace System;

[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
public interface IMultiplicativeIdentity<TSelf, TResult> where TSelf : IMultiplicativeIdentity<TSelf, TResult>
{
	static abstract TResult MultiplicativeIdentity { get; }
}
