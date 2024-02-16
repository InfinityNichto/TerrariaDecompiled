using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices;

[StructLayout(LayoutKind.Auto)]
public readonly struct ConfiguredAsyncDisposable
{
	private readonly IAsyncDisposable _source;

	private readonly bool _continueOnCapturedContext;

	internal ConfiguredAsyncDisposable(IAsyncDisposable source, bool continueOnCapturedContext)
	{
		_source = source;
		_continueOnCapturedContext = continueOnCapturedContext;
	}

	public ConfiguredValueTaskAwaitable DisposeAsync()
	{
		return _source.DisposeAsync().ConfigureAwait(_continueOnCapturedContext);
	}
}
