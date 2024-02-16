using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Transactions.Configuration;
using System.Transactions.Distributed;

namespace System.Transactions;

public static class TransactionManager
{
	private static Hashtable s_promotedTransactionTable;

	private static TransactionTable s_transactionTable;

	private static TransactionStartedEventHandler s_distributedTransactionStartedDelegate;

	internal static HostCurrentTransactionCallback s_currentDelegate;

	internal static bool s_currentDelegateSet;

	private static object s_classSyncObject;

	private static DefaultSettingsSection s_defaultSettings;

	private static MachineSettingsSection s_machineSettings;

	private static bool s_defaultTimeoutValidated;

	private static TimeSpan s_defaultTimeout;

	private static bool s_cachedMaxTimeout;

	private static TimeSpan s_maximumTimeout;

	internal static DistributedTransactionManager distributedTransactionManager;

	public static HostCurrentTransactionCallback? HostCurrentCallback
	{
		get
		{
			return s_currentDelegate;
		}
		[param: DisallowNull]
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			lock (ClassSyncObject)
			{
				if (s_currentDelegateSet)
				{
					throw new InvalidOperationException(System.SR.CurrentDelegateSet);
				}
				s_currentDelegateSet = true;
			}
			s_currentDelegate = value;
		}
	}

	private static object ClassSyncObject => LazyInitializer.EnsureInitialized(ref s_classSyncObject);

	internal static IsolationLevel DefaultIsolationLevel
	{
		get
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.MethodEnter(TraceSourceType.TraceSourceBase, "TransactionManager.get_DefaultIsolationLevel");
				log.MethodExit(TraceSourceType.TraceSourceBase, "TransactionManager.get_DefaultIsolationLevel");
			}
			return IsolationLevel.Serializable;
		}
	}

	private static DefaultSettingsSection DefaultSettings
	{
		get
		{
			if (s_defaultSettings == null)
			{
				s_defaultSettings = DefaultSettingsSection.GetSection();
			}
			return s_defaultSettings;
		}
	}

	private static MachineSettingsSection MachineSettings
	{
		get
		{
			if (s_machineSettings == null)
			{
				s_machineSettings = MachineSettingsSection.GetSection();
			}
			return s_machineSettings;
		}
	}

	public static TimeSpan DefaultTimeout
	{
		get
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.MethodEnter(TraceSourceType.TraceSourceBase, "TransactionManager.get_DefaultTimeout");
			}
			if (!s_defaultTimeoutValidated)
			{
				s_defaultTimeout = ValidateTimeout(DefaultSettings.Timeout);
				if (s_defaultTimeout != DefaultSettings.Timeout && log.IsEnabled())
				{
					log.ConfiguredDefaultTimeoutAdjusted();
				}
				s_defaultTimeoutValidated = true;
			}
			if (log.IsEnabled())
			{
				log.MethodExit(TraceSourceType.TraceSourceBase, "TransactionManager.get_DefaultTimeout");
			}
			return s_defaultTimeout;
		}
	}

	public static TimeSpan MaximumTimeout
	{
		get
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.MethodEnter(TraceSourceType.TraceSourceBase, "TransactionManager.get_DefaultMaximumTimeout");
			}
			LazyInitializer.EnsureInitialized(ref s_maximumTimeout, ref s_cachedMaxTimeout, ref s_classSyncObject, () => MachineSettings.MaxTimeout);
			if (log.IsEnabled())
			{
				log.MethodExit(TraceSourceType.TraceSourceBase, "TransactionManager.get_DefaultMaximumTimeout");
			}
			return s_maximumTimeout;
		}
	}

	internal static Hashtable PromotedTransactionTable => LazyInitializer.EnsureInitialized(ref s_promotedTransactionTable, ref s_classSyncObject, () => new Hashtable(100));

	internal static TransactionTable TransactionTable => LazyInitializer.EnsureInitialized(ref s_transactionTable, ref s_classSyncObject, () => new TransactionTable());

	internal static DistributedTransactionManager DistributedTransactionManager => LazyInitializer.EnsureInitialized(ref distributedTransactionManager, ref s_classSyncObject, () => new DistributedTransactionManager());

	public static event TransactionStartedEventHandler? DistributedTransactionStarted
	{
		add
		{
			lock (ClassSyncObject)
			{
				s_distributedTransactionStartedDelegate = (TransactionStartedEventHandler)Delegate.Combine(s_distributedTransactionStartedDelegate, value);
				if (value != null)
				{
					ProcessExistingTransactions(value);
				}
			}
		}
		remove
		{
			lock (ClassSyncObject)
			{
				s_distributedTransactionStartedDelegate = (TransactionStartedEventHandler)Delegate.Remove(s_distributedTransactionStartedDelegate, value);
			}
		}
	}

	internal static void ProcessExistingTransactions(TransactionStartedEventHandler eventHandler)
	{
		lock (PromotedTransactionTable)
		{
			IDictionaryEnumerator enumerator = PromotedTransactionTable.GetEnumerator();
			while (enumerator.MoveNext())
			{
				WeakReference weakReference = (WeakReference)enumerator.Value;
				if (weakReference.Target is Transaction transaction)
				{
					TransactionEventArgs transactionEventArgs = new TransactionEventArgs();
					transactionEventArgs._transaction = transaction.InternalClone();
					eventHandler(transactionEventArgs._transaction, transactionEventArgs);
				}
			}
		}
	}

	internal static void FireDistributedTransactionStarted(Transaction transaction)
	{
		TransactionStartedEventHandler transactionStartedEventHandler = null;
		lock (ClassSyncObject)
		{
			transactionStartedEventHandler = s_distributedTransactionStartedDelegate;
		}
		if (transactionStartedEventHandler != null)
		{
			TransactionEventArgs transactionEventArgs = new TransactionEventArgs();
			transactionEventArgs._transaction = transaction.InternalClone();
			transactionStartedEventHandler(transactionEventArgs._transaction, transactionEventArgs);
		}
	}

	public static Enlistment Reenlist(Guid resourceManagerIdentifier, byte[] recoveryInformation, IEnlistmentNotification enlistmentNotification)
	{
		if (resourceManagerIdentifier == Guid.Empty)
		{
			throw new ArgumentException(System.SR.BadResourceManagerId, "resourceManagerIdentifier");
		}
		if (recoveryInformation == null)
		{
			throw new ArgumentNullException("recoveryInformation");
		}
		if (enlistmentNotification == null)
		{
			throw new ArgumentNullException("enlistmentNotification");
		}
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceBase, "TransactionManager.Reenlist");
			log.TransactionManagerReenlist(resourceManagerIdentifier);
		}
		MemoryStream memoryStream = new MemoryStream(recoveryInformation);
		int num = 0;
		string nodeName = null;
		byte[] resourceManagerRecoveryInformation = null;
		try
		{
			BinaryReader binaryReader = new BinaryReader(memoryStream);
			num = binaryReader.ReadInt32();
			if (num != 1)
			{
				if (log.IsEnabled())
				{
					log.TransactionExceptionTrace(TraceSourceType.TraceSourceBase, TransactionExceptionType.UnrecognizedRecoveryInformation, "recoveryInformation", string.Empty);
				}
				throw new ArgumentException(System.SR.UnrecognizedRecoveryInformation, "recoveryInformation");
			}
			nodeName = binaryReader.ReadString();
			resourceManagerRecoveryInformation = binaryReader.ReadBytes(recoveryInformation.Length - checked((int)memoryStream.Position));
		}
		catch (EndOfStreamException ex)
		{
			if (log.IsEnabled())
			{
				log.TransactionExceptionTrace(TraceSourceType.TraceSourceBase, TransactionExceptionType.UnrecognizedRecoveryInformation, "recoveryInformation", ex.ToString());
			}
			throw new ArgumentException(System.SR.UnrecognizedRecoveryInformation, "recoveryInformation", ex);
		}
		catch (FormatException ex2)
		{
			if (log.IsEnabled())
			{
				log.TransactionExceptionTrace(TraceSourceType.TraceSourceBase, TransactionExceptionType.UnrecognizedRecoveryInformation, "recoveryInformation", ex2.ToString());
			}
			throw new ArgumentException(System.SR.UnrecognizedRecoveryInformation, "recoveryInformation", ex2);
		}
		finally
		{
			memoryStream.Dispose();
		}
		DistributedTransactionManager distributedTransactionManager = CheckTransactionManager(nodeName);
		object syncRoot = new object();
		Enlistment enlistment = new Enlistment(enlistmentNotification, syncRoot);
		EnlistmentState.EnlistmentStatePromoted.EnterState(enlistment.InternalEnlistment);
		enlistment.InternalEnlistment.PromotedEnlistment = distributedTransactionManager.ReenlistTransaction(resourceManagerIdentifier, resourceManagerRecoveryInformation, (RecoveringInternalEnlistment)enlistment.InternalEnlistment);
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceBase, "TransactionManager.Reenlist");
		}
		return enlistment;
	}

	private static DistributedTransactionManager CheckTransactionManager(string nodeName)
	{
		DistributedTransactionManager distributedTransactionManager = DistributedTransactionManager;
		if ((distributedTransactionManager.NodeName != null || (nodeName != null && nodeName.Length != 0)) && (distributedTransactionManager.NodeName == null || !distributedTransactionManager.NodeName.Equals(nodeName)))
		{
			throw new ArgumentException(System.SR.InvalidRecoveryInformation, "recoveryInformation");
		}
		return distributedTransactionManager;
	}

	public static void RecoveryComplete(Guid resourceManagerIdentifier)
	{
		if (resourceManagerIdentifier == Guid.Empty)
		{
			throw new ArgumentException(System.SR.BadResourceManagerId, "resourceManagerIdentifier");
		}
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceBase, "TransactionManager.RecoveryComplete");
			log.TransactionManagerRecoveryComplete(resourceManagerIdentifier);
		}
		DistributedTransactionManager.ResourceManagerRecoveryComplete(resourceManagerIdentifier);
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceBase, "TransactionManager.RecoveryComplete");
		}
	}

	internal static void ValidateIsolationLevel(IsolationLevel transactionIsolationLevel)
	{
		if ((uint)transactionIsolationLevel > 6u)
		{
			throw new ArgumentOutOfRangeException("transactionIsolationLevel");
		}
	}

	internal static TimeSpan ValidateTimeout(TimeSpan transactionTimeout)
	{
		if (transactionTimeout < TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException("transactionTimeout");
		}
		if (MaximumTimeout != TimeSpan.Zero && (transactionTimeout > MaximumTimeout || transactionTimeout == TimeSpan.Zero))
		{
			return MaximumTimeout;
		}
		return transactionTimeout;
	}

	internal static Transaction FindPromotedTransaction(Guid transactionIdentifier)
	{
		Hashtable promotedTransactionTable = PromotedTransactionTable;
		WeakReference weakReference = (WeakReference)promotedTransactionTable[transactionIdentifier];
		if (weakReference != null)
		{
			if (weakReference.Target is Transaction transaction)
			{
				return transaction.InternalClone();
			}
			lock (promotedTransactionTable)
			{
				promotedTransactionTable.Remove(transactionIdentifier);
			}
		}
		return null;
	}

	internal static Transaction FindOrCreatePromotedTransaction(Guid transactionIdentifier, DistributedTransaction dtx)
	{
		Transaction transaction = null;
		Hashtable promotedTransactionTable = PromotedTransactionTable;
		lock (promotedTransactionTable)
		{
			WeakReference weakReference = (WeakReference)promotedTransactionTable[transactionIdentifier];
			if (weakReference != null)
			{
				transaction = weakReference.Target as Transaction;
				if (null != transaction)
				{
					dtx.Dispose();
					return transaction.InternalClone();
				}
				lock (promotedTransactionTable)
				{
					promotedTransactionTable.Remove(transactionIdentifier);
				}
			}
			transaction = new Transaction(dtx);
			transaction._internalTransaction._finalizedObject = new FinalizedObject(transaction._internalTransaction, dtx.Identifier);
			weakReference = new WeakReference(transaction, trackResurrection: false);
			promotedTransactionTable[dtx.Identifier] = weakReference;
		}
		dtx.SavedLtmPromotedTransaction = transaction;
		FireDistributedTransactionStarted(transaction);
		return transaction;
	}
}
