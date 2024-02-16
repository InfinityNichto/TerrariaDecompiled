using System.Collections.Generic;
using System.Diagnostics;

namespace System.Threading.Tasks.Dataflow.Internal;

[DebuggerDisplay("{DebuggerDisplayContent,nq}")]
[DebuggerTypeProxy(typeof(BatchedJoinBlockTarget<>.DebugView))]
internal sealed class BatchedJoinBlockTarget<T> : ITargetBlock<T>, IDataflowBlock, IDebuggerDisplay
{
	private sealed class DebugView
	{
		private readonly BatchedJoinBlockTarget<T> _batchedJoinBlockTarget;

		public IEnumerable<T> InputQueue => _batchedJoinBlockTarget._messages;

		public bool IsDecliningPermanently
		{
			get
			{
				if (!_batchedJoinBlockTarget._decliningPermanently)
				{
					return _batchedJoinBlockTarget._sharedResources._decliningPermanently;
				}
				return true;
			}
		}

		public DebugView(BatchedJoinBlockTarget<T> batchedJoinBlockTarget)
		{
			_batchedJoinBlockTarget = batchedJoinBlockTarget;
		}
	}

	private readonly BatchedJoinBlockTargetSharedResources _sharedResources;

	private bool _decliningPermanently;

	private IList<T> _messages = new List<T>();

	internal int Count => _messages.Count;

	Task IDataflowBlock.Completion
	{
		get
		{
			throw new NotSupportedException(System.SR.NotSupported_MemberNotNeeded);
		}
	}

	private object DebuggerDisplayContent => $"{Common.GetNameForDebugger(this)} InputCount={_messages.Count}";

	object IDebuggerDisplay.Content => DebuggerDisplayContent;

	internal BatchedJoinBlockTarget(BatchedJoinBlockTargetSharedResources sharedResources)
	{
		_sharedResources = sharedResources;
		sharedResources._remainingAliveTargets++;
	}

	internal IList<T> GetAndEmptyMessages()
	{
		IList<T> messages = _messages;
		_messages = new List<T>();
		return messages;
	}

	public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T> source, bool consumeToAccept)
	{
		if (!messageHeader.IsValid)
		{
			throw new ArgumentException(System.SR.Argument_InvalidMessageHeader, "messageHeader");
		}
		if (source == null && consumeToAccept)
		{
			throw new ArgumentException(System.SR.Argument_CantConsumeFromANullSource, "consumeToAccept");
		}
		lock (_sharedResources._incomingLock)
		{
			if (_decliningPermanently || _sharedResources._decliningPermanently)
			{
				return DataflowMessageStatus.DecliningPermanently;
			}
			if (consumeToAccept)
			{
				messageValue = source.ConsumeMessage(messageHeader, this, out var messageConsumed);
				if (!messageConsumed)
				{
					return DataflowMessageStatus.NotAvailable;
				}
			}
			_messages.Add(messageValue);
			if (--_sharedResources._remainingItemsInBatch == 0)
			{
				_sharedResources._batchSizeReachedAction();
			}
			return DataflowMessageStatus.Accepted;
		}
	}

	public void Complete()
	{
		lock (_sharedResources._incomingLock)
		{
			if (!_decliningPermanently)
			{
				_decliningPermanently = true;
				if (--_sharedResources._remainingAliveTargets == 0)
				{
					_sharedResources._allTargetsDecliningPermanentlyAction();
				}
			}
		}
	}

	void IDataflowBlock.Fault(Exception exception)
	{
		if (exception == null)
		{
			throw new ArgumentNullException("exception");
		}
		lock (_sharedResources._incomingLock)
		{
			if (!_decliningPermanently && !_sharedResources._decliningPermanently)
			{
				_sharedResources._exceptionAction(exception);
			}
		}
		_sharedResources._completionAction();
	}
}
