using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Runtime.CompilerServices;

[StructLayout(LayoutKind.Auto)]
public readonly struct ConfiguredCancelableAsyncEnumerable<T>
{
	[StructLayout(LayoutKind.Auto)]
	public readonly struct Enumerator
	{
		private readonly IAsyncEnumerator<T> _enumerator;

		private readonly bool _continueOnCapturedContext;

		public T Current => _enumerator.Current;

		internal Enumerator(IAsyncEnumerator<T> enumerator, bool continueOnCapturedContext)
		{
			_enumerator = enumerator;
			_continueOnCapturedContext = continueOnCapturedContext;
		}

		public ConfiguredValueTaskAwaitable<bool> MoveNextAsync()
		{
			return _enumerator.MoveNextAsync().ConfigureAwait(_continueOnCapturedContext);
		}

		public ConfiguredValueTaskAwaitable DisposeAsync()
		{
			return _enumerator.DisposeAsync().ConfigureAwait(_continueOnCapturedContext);
		}
	}

	private readonly IAsyncEnumerable<T> _enumerable;

	private readonly CancellationToken _cancellationToken;

	private readonly bool _continueOnCapturedContext;

	internal ConfiguredCancelableAsyncEnumerable(IAsyncEnumerable<T> enumerable, bool continueOnCapturedContext, CancellationToken cancellationToken)
	{
		_enumerable = enumerable;
		_continueOnCapturedContext = continueOnCapturedContext;
		_cancellationToken = cancellationToken;
	}

	public ConfiguredCancelableAsyncEnumerable<T> ConfigureAwait(bool continueOnCapturedContext)
	{
		return new ConfiguredCancelableAsyncEnumerable<T>(_enumerable, continueOnCapturedContext, _cancellationToken);
	}

	public ConfiguredCancelableAsyncEnumerable<T> WithCancellation(CancellationToken cancellationToken)
	{
		return new ConfiguredCancelableAsyncEnumerable<T>(_enumerable, _continueOnCapturedContext, cancellationToken);
	}

	public Enumerator GetAsyncEnumerator()
	{
		return new Enumerator(_enumerable.GetAsyncEnumerator(_cancellationToken), _continueOnCapturedContext);
	}
}
