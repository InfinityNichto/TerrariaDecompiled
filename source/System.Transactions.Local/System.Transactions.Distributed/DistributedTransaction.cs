using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Transactions.Distributed;

internal class DistributedTransaction : ISerializable, IObjectReference
{
	internal sealed class RealDistributedTransaction
	{
		[CompilerGenerated]
		private InternalTransaction _003CInternalTransaction_003Ek__BackingField;

		internal InternalTransaction InternalTransaction
		{
			[CompilerGenerated]
			set
			{
				_003CInternalTransaction_003Ek__BackingField = value;
			}
		}
	}

	[CompilerGenerated]
	private Transaction _003CSavedLtmPromotedTransaction_003Ek__BackingField;

	internal Exception InnerException { get; }

	internal Guid Identifier { get; }

	internal RealDistributedTransaction RealTransaction { get; }

	internal TransactionTraceIdentifier TransactionTraceId { get; }

	internal IsolationLevel IsolationLevel { get; }

	internal Transaction SavedLtmPromotedTransaction
	{
		[CompilerGenerated]
		set
		{
			_003CSavedLtmPromotedTransaction_003Ek__BackingField = value;
		}
	}

	protected DistributedTransaction(SerializationInfo serializationInfo, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	internal void Dispose()
	{
	}

	internal IPromotedEnlistment EnlistVolatile(InternalEnlistment internalEnlistment, EnlistmentOptions enlistmentOptions)
	{
		throw NotSupported();
	}

	internal IPromotedEnlistment EnlistDurable(Guid resourceManagerIdentifier, DurableInternalEnlistment internalEnlistment, bool v, EnlistmentOptions enlistmentOptions)
	{
		throw NotSupported();
	}

	internal void Rollback()
	{
		throw NotSupported();
	}

	internal DistributedDependentTransaction DependentClone(bool v)
	{
		throw NotSupported();
	}

	internal IPromotedEnlistment EnlistVolatile(VolatileDemultiplexer volatileDemux, EnlistmentOptions enlistmentOptions)
	{
		throw NotSupported();
	}

	internal byte[] GetExportCookie(byte[] whereaboutsCopy)
	{
		throw NotSupported();
	}

	public object GetRealObject(StreamingContext context)
	{
		throw NotSupported();
	}

	internal byte[] GetTransmitterPropagationToken()
	{
		throw NotSupported();
	}

	internal IDtcTransaction GetDtcTransaction()
	{
		throw NotSupported();
	}

	void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	internal static Exception NotSupported()
	{
		return new PlatformNotSupportedException(System.SR.DistributedNotSupported);
	}
}
