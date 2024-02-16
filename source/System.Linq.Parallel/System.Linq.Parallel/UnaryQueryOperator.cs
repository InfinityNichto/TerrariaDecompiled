using System.Collections.Generic;

namespace System.Linq.Parallel;

internal abstract class UnaryQueryOperator<TInput, TOutput> : QueryOperator<TOutput>
{
	internal class UnaryQueryOperatorResults : QueryResults<TOutput>
	{
		private sealed class ChildResultsRecipient : IPartitionedStreamRecipient<TInput>
		{
			private readonly IPartitionedStreamRecipient<TOutput> _outputRecipient;

			private readonly UnaryQueryOperator<TInput, TOutput> _op;

			private readonly bool _preferStriping;

			private QuerySettings _settings;

			internal ChildResultsRecipient(IPartitionedStreamRecipient<TOutput> outputRecipient, UnaryQueryOperator<TInput, TOutput> op, bool preferStriping, QuerySettings settings)
			{
				_outputRecipient = outputRecipient;
				_op = op;
				_preferStriping = preferStriping;
				_settings = settings;
			}

			public void Receive<TKey>(PartitionedStream<TInput, TKey> inputStream)
			{
				_op.WrapPartitionedStream(inputStream, _outputRecipient, _preferStriping, _settings);
			}
		}

		protected QueryResults<TInput> _childQueryResults;

		private readonly UnaryQueryOperator<TInput, TOutput> _op;

		private QuerySettings _settings;

		private readonly bool _preferStriping;

		internal UnaryQueryOperatorResults(QueryResults<TInput> childQueryResults, UnaryQueryOperator<TInput, TOutput> op, QuerySettings settings, bool preferStriping)
		{
			_childQueryResults = childQueryResults;
			_op = op;
			_settings = settings;
			_preferStriping = preferStriping;
		}

		internal override void GivePartitionedStream(IPartitionedStreamRecipient<TOutput> recipient)
		{
			if (_settings.ExecutionMode.Value == ParallelExecutionMode.Default && _op.LimitsParallelism)
			{
				IEnumerable<TOutput> source = _op.AsSequentialQuery(_settings.CancellationState.ExternalCancellationToken);
				PartitionedStream<TOutput, int> partitionedStream = ExchangeUtilities.PartitionDataSource(source, _settings.DegreeOfParallelism.Value, _preferStriping);
				recipient.Receive(partitionedStream);
			}
			else if (IsIndexible)
			{
				PartitionedStream<TOutput, int> partitionedStream2 = ExchangeUtilities.PartitionDataSource(this, _settings.DegreeOfParallelism.Value, _preferStriping);
				recipient.Receive(partitionedStream2);
			}
			else
			{
				_childQueryResults.GivePartitionedStream(new ChildResultsRecipient(recipient, _op, _preferStriping, _settings));
			}
		}
	}

	private readonly QueryOperator<TInput> _child;

	private OrdinalIndexState _indexState = OrdinalIndexState.Shuffled;

	internal QueryOperator<TInput> Child => _child;

	internal sealed override OrdinalIndexState OrdinalIndexState => _indexState;

	internal UnaryQueryOperator(IEnumerable<TInput> child)
		: this(QueryOperator<TInput>.AsQueryOperator(child))
	{
	}

	internal UnaryQueryOperator(IEnumerable<TInput> child, bool outputOrdered)
		: this(QueryOperator<TInput>.AsQueryOperator(child), outputOrdered)
	{
	}

	private UnaryQueryOperator(QueryOperator<TInput> child)
		: this(child, child.OutputOrdered, child.SpecifiedQuerySettings)
	{
	}

	internal UnaryQueryOperator(QueryOperator<TInput> child, bool outputOrdered)
		: this(child, outputOrdered, child.SpecifiedQuerySettings)
	{
	}

	private UnaryQueryOperator(QueryOperator<TInput> child, bool outputOrdered, QuerySettings settings)
		: base(outputOrdered, settings)
	{
		_child = child;
	}

	protected void SetOrdinalIndexState(OrdinalIndexState indexState)
	{
		_indexState = indexState;
	}

	internal abstract void WrapPartitionedStream<TKey>(PartitionedStream<TInput, TKey> inputStream, IPartitionedStreamRecipient<TOutput> recipient, bool preferStriping, QuerySettings settings);
}
