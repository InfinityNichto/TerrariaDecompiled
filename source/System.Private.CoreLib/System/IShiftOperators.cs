using System.Runtime.Versioning;

namespace System;

[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
public interface IShiftOperators<TSelf, TResult> where TSelf : IShiftOperators<TSelf, TResult>
{
	static abstract TResult operator <<(TSelf value, int shiftAmount);

	static abstract TResult operator >>(TSelf value, int shiftAmount);
}
