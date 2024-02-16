using System.Collections.Generic;

namespace System.Linq.Parallel;

internal abstract class BinaryQueryOperator<TLeftInput, TRightInput, TOutput> : QueryOperator<TOutput>
{
	internal class BinaryQueryOperatorResults : QueryResults<TOutput>
	{
		private sealed class LeftChildResultsRecipient : IPartitionedStreamRecipient<TLeftInput>
		{
			private readonly IPartitionedStreamRecipient<TOutput> _outputRecipient;

			private readonly BinaryQueryOperatorResults _results;

			private readonly bool _preferStriping;

			private QuerySettings _settings;

			internal LeftChildResultsRecipient(IPartitionedStreamRecipient<TOutput> outputRecipient, BinaryQueryOperatorResults results, bool preferStriping, QuerySettings settings)
			{
				_outputRecipient = outputRecipient;
				_results = results;
				_preferStriping = preferStriping;
				_settings = settings;
			}

			public void Receive<TLeftKey>(PartitionedStream<TLeftInput, TLeftKey> source)
			{
				RightChildResultsRecipient<TLeftKey> recipient = new RightChildResultsRecipient<TLeftKey>(_outputRecipient, _results._op, source, _preferStriping, _settings);
				_results._rightChildQueryResults.GivePartitionedStream(recipient);
			}
		}

		private sealed class RightChildResultsRecipient<TLeftKey> : IPartitionedStreamRecipient<TRightInput>
		{
			private readonly IPartitionedStreamRecipient<TOutput> _outputRecipient;

			private readonly PartitionedStream<TLeftInput, TLeftKey> _leftPartitionedStream;

			private readonly BinaryQueryOperator<TLeftInput, TRightInput, TOutput> _op;

			private readonly bool _preferStriping;

			private QuerySettings _settings;

			internal RightChildResultsRecipient(IPartitionedStreamRecipient<TOutput> outputRecipient, BinaryQueryOperator<TLeftInput, TRightInput, TOutput> op, PartitionedStream<TLeftInput, TLeftKey> leftPartitionedStream, bool preferStriping, QuerySettings settings)
			{
				_outputRecipient = outputRecipient;
				_op = op;
				_preferStriping = preferStriping;
				_leftPartitionedStream = leftPartitionedStream;
				_settings = settings;
			}

			public void Receive<TRightKey>(PartitionedStream<TRightInput, TRightKey> rightPartitionedStream)
			{
				_op.WrapPartitionedStream(_leftPartitionedStream, rightPartitionedStream, _outputRecipient, _preferStriping, _settings);
			}
		}

		protected QueryResults<TLeftInput> _leftChildQueryResults;

		protected QueryResults<TRightInput> _rightChildQueryResults;

		private readonly BinaryQueryOperator<TLeftInput, TRightInput, TOutput> _op;

		private QuerySettings _settings;

		private readonly bool _preferStriping;

		internal BinaryQueryOperatorResults(QueryResults<TLeftInput> leftChildQueryResults, QueryResults<TRightInput> rightChildQueryResults, BinaryQueryOperator<TLeftInput, TRightInput, TOutput> op, QuerySettings settings, bool preferStriping)
		{
			_leftChildQueryResults = leftChildQueryResults;
			_rightChildQueryResults = rightChildQueryResults;
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
				_leftChildQueryResults.GivePartitionedStream(new LeftChildResultsRecipient(recipient, this, _preferStriping, _settings));
			}
		}
	}

	private readonly QueryOperator<TLeftInput> _leftChild;

	private readonly QueryOperator<TRightInput> _rightChild;

	private OrdinalIndexState _indexState = OrdinalIndexState.Shuffled;

	internal QueryOperator<TLeftInput> LeftChild => _leftChild;

	internal QueryOperator<TRightInput> RightChild => _rightChild;

	internal sealed override OrdinalIndexState OrdinalIndexState => _indexState;

	internal BinaryQueryOperator(ParallelQuery<TLeftInput> leftChild, ParallelQuery<TRightInput> rightChild)
		: this(QueryOperator<TLeftInput>.AsQueryOperator(leftChild), QueryOperator<TRightInput>.AsQueryOperator(rightChild))
	{
	}

	internal BinaryQueryOperator(QueryOperator<TLeftInput> leftChild, QueryOperator<TRightInput> rightChild)
		: base(isOrdered: false, leftChild.SpecifiedQuerySettings.Merge(rightChild.SpecifiedQuerySettings))
	{
		_leftChild = leftChild;
		_rightChild = rightChild;
	}

	protected void SetOrdinalIndex(OrdinalIndexState indexState)
	{
		_indexState = indexState;
	}

	public abstract void WrapPartitionedStream<TLeftKey, TRightKey>(PartitionedStream<TLeftInput, TLeftKey> leftPartitionedStream, PartitionedStream<TRightInput, TRightKey> rightPartitionedStream, IPartitionedStreamRecipient<TOutput> outputRecipient, bool preferStriping, QuerySettings settings);
}
