using System.Resources;
using FxResources.System.Linq.Parallel;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string MoreThanOneMatch => GetResourceString("MoreThanOneMatch");

	internal static string NoElements => GetResourceString("NoElements");

	internal static string ParallelPartitionable_NullReturn => GetResourceString("ParallelPartitionable_NullReturn");

	internal static string ParallelPartitionable_IncorretElementCount => GetResourceString("ParallelPartitionable_IncorretElementCount");

	internal static string ParallelPartitionable_NullElement => GetResourceString("ParallelPartitionable_NullElement");

	internal static string PLINQ_CommonEnumerator_Current_NotStarted => GetResourceString("PLINQ_CommonEnumerator_Current_NotStarted");

	internal static string PLINQ_ExternalCancellationRequested => GetResourceString("PLINQ_ExternalCancellationRequested");

	internal static string PLINQ_DisposeRequested => GetResourceString("PLINQ_DisposeRequested");

	internal static string ParallelQuery_DuplicateTaskScheduler => GetResourceString("ParallelQuery_DuplicateTaskScheduler");

	internal static string ParallelQuery_DuplicateDOP => GetResourceString("ParallelQuery_DuplicateDOP");

	internal static string ParallelQuery_DuplicateExecutionMode => GetResourceString("ParallelQuery_DuplicateExecutionMode");

	internal static string PartitionerQueryOperator_NullPartitionList => GetResourceString("PartitionerQueryOperator_NullPartitionList");

	internal static string PartitionerQueryOperator_WrongNumberOfPartitions => GetResourceString("PartitionerQueryOperator_WrongNumberOfPartitions");

	internal static string PartitionerQueryOperator_NullPartition => GetResourceString("PartitionerQueryOperator_NullPartition");

	internal static string ParallelQuery_DuplicateWithCancellation => GetResourceString("ParallelQuery_DuplicateWithCancellation");

	internal static string ParallelQuery_DuplicateMergeOptions => GetResourceString("ParallelQuery_DuplicateMergeOptions");

	internal static string PLINQ_EnumerationPreviouslyFailed => GetResourceString("PLINQ_EnumerationPreviouslyFailed");

	internal static string ParallelQuery_PartitionerNotOrderable => GetResourceString("ParallelQuery_PartitionerNotOrderable");

	internal static string ParallelQuery_InvalidAsOrderedCall => GetResourceString("ParallelQuery_InvalidAsOrderedCall");

	internal static string ParallelQuery_InvalidNonGenericAsOrderedCall => GetResourceString("ParallelQuery_InvalidNonGenericAsOrderedCall");

	internal static string ParallelEnumerable_BinaryOpMustUseAsParallel => GetResourceString("ParallelEnumerable_BinaryOpMustUseAsParallel");

	internal static string ParallelEnumerable_WithQueryExecutionMode_InvalidMode => GetResourceString("ParallelEnumerable_WithQueryExecutionMode_InvalidMode");

	internal static string ParallelEnumerable_WithMergeOptions_InvalidOptions => GetResourceString("ParallelEnumerable_WithMergeOptions_InvalidOptions");

	private static bool UsingResourceKeys()
	{
		return s_usingResourceKeys;
	}

	internal static string GetResourceString(string resourceKey)
	{
		if (UsingResourceKeys())
		{
			return resourceKey;
		}
		string result = null;
		try
		{
			result = ResourceManager.GetString(resourceKey);
		}
		catch (MissingManifestResourceException)
		{
		}
		return result;
	}
}
