using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class QueryOpeningEnumerator<TOutput> : IEnumerator<TOutput>, IEnumerator, IDisposable
{
	private readonly QueryOperator<TOutput> _queryOperator;

	private IEnumerator<TOutput> _openedQueryEnumerator;

	private QuerySettings _querySettings;

	private readonly ParallelMergeOptions? _mergeOptions;

	private readonly bool _suppressOrderPreservation;

	private int _moveNextIteration;

	private bool _hasQueryOpeningFailed;

	private readonly Shared<bool> _topLevelDisposedFlag = new Shared<bool>(value: false);

	private readonly CancellationTokenSource _topLevelCancellationTokenSource = new CancellationTokenSource();

	public TOutput Current
	{
		get
		{
			if (_openedQueryEnumerator == null)
			{
				throw new InvalidOperationException(System.SR.PLINQ_CommonEnumerator_Current_NotStarted);
			}
			return _openedQueryEnumerator.Current;
		}
	}

	object IEnumerator.Current => ((IEnumerator<TOutput>)this).Current;

	internal QueryOpeningEnumerator(QueryOperator<TOutput> queryOperator, ParallelMergeOptions? mergeOptions, bool suppressOrderPreservation)
	{
		_queryOperator = queryOperator;
		_mergeOptions = mergeOptions;
		_suppressOrderPreservation = suppressOrderPreservation;
	}

	public void Dispose()
	{
		_topLevelDisposedFlag.Value = true;
		_topLevelCancellationTokenSource.Cancel();
		if (_openedQueryEnumerator != null)
		{
			_openedQueryEnumerator.Dispose();
			_querySettings.CleanStateAtQueryEnd();
		}
		QueryLifecycle.LogicalQueryExecutionEnd(_querySettings.QueryId);
	}

	public bool MoveNext()
	{
		if (_topLevelDisposedFlag.Value)
		{
			throw new ObjectDisposedException("enumerator", System.SR.PLINQ_DisposeRequested);
		}
		if (_openedQueryEnumerator == null)
		{
			OpenQuery();
		}
		bool result = _openedQueryEnumerator.MoveNext();
		if ((_moveNextIteration & 0x3F) == 0)
		{
			CancellationState.ThrowWithStandardMessageIfCanceled(_querySettings.CancellationState.ExternalCancellationToken);
		}
		_moveNextIteration++;
		return result;
	}

	private void OpenQuery()
	{
		if (_hasQueryOpeningFailed)
		{
			throw new InvalidOperationException(System.SR.PLINQ_EnumerationPreviouslyFailed);
		}
		try
		{
			_querySettings = _queryOperator.SpecifiedQuerySettings.WithPerExecutionSettings(_topLevelCancellationTokenSource, _topLevelDisposedFlag).WithDefaults();
			QueryLifecycle.LogicalQueryExecutionBegin(_querySettings.QueryId);
			_openedQueryEnumerator = _queryOperator.GetOpenedEnumerator(_mergeOptions, _suppressOrderPreservation, forEffect: false, _querySettings);
			CancellationState.ThrowWithStandardMessageIfCanceled(_querySettings.CancellationState.ExternalCancellationToken);
		}
		catch
		{
			_hasQueryOpeningFailed = true;
			throw;
		}
	}

	public void Reset()
	{
		throw new NotSupportedException();
	}
}
