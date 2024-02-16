using System.Resources;
using FxResources.System.Collections.Concurrent;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string BlockingCollection_Add_ConcurrentCompleteAdd => GetResourceString("BlockingCollection_Add_ConcurrentCompleteAdd");

	internal static string BlockingCollection_Add_Failed => GetResourceString("BlockingCollection_Add_Failed");

	internal static string BlockingCollection_CantAddAnyWhenCompleted => GetResourceString("BlockingCollection_CantAddAnyWhenCompleted");

	internal static string BlockingCollection_CantTakeAnyWhenAllDone => GetResourceString("BlockingCollection_CantTakeAnyWhenAllDone");

	internal static string BlockingCollection_CantTakeWhenDone => GetResourceString("BlockingCollection_CantTakeWhenDone");

	internal static string BlockingCollection_Completed => GetResourceString("BlockingCollection_Completed");

	internal static string BlockingCollection_CopyTo_IncorrectType => GetResourceString("BlockingCollection_CopyTo_IncorrectType");

	internal static string BlockingCollection_CopyTo_MultiDim => GetResourceString("BlockingCollection_CopyTo_MultiDim");

	internal static string BlockingCollection_CopyTo_NonNegative => GetResourceString("BlockingCollection_CopyTo_NonNegative");

	internal static string Collection_CopyTo_TooManyElems => GetResourceString("Collection_CopyTo_TooManyElems");

	internal static string BlockingCollection_ctor_BoundedCapacityRange => GetResourceString("BlockingCollection_ctor_BoundedCapacityRange");

	internal static string BlockingCollection_ctor_CountMoreThanCapacity => GetResourceString("BlockingCollection_ctor_CountMoreThanCapacity");

	internal static string BlockingCollection_Disposed => GetResourceString("BlockingCollection_Disposed");

	internal static string BlockingCollection_Take_CollectionModified => GetResourceString("BlockingCollection_Take_CollectionModified");

	internal static string BlockingCollection_TimeoutInvalid => GetResourceString("BlockingCollection_TimeoutInvalid");

	internal static string BlockingCollection_ValidateCollectionsArray_DispElems => GetResourceString("BlockingCollection_ValidateCollectionsArray_DispElems");

	internal static string BlockingCollection_ValidateCollectionsArray_LargeSize => GetResourceString("BlockingCollection_ValidateCollectionsArray_LargeSize");

	internal static string BlockingCollection_ValidateCollectionsArray_NullElems => GetResourceString("BlockingCollection_ValidateCollectionsArray_NullElems");

	internal static string BlockingCollection_ValidateCollectionsArray_ZeroSize => GetResourceString("BlockingCollection_ValidateCollectionsArray_ZeroSize");

	internal static string ConcurrentBag_Ctor_ArgumentNullException => GetResourceString("ConcurrentBag_Ctor_ArgumentNullException");

	internal static string ConcurrentBag_CopyTo_ArgumentNullException => GetResourceString("ConcurrentBag_CopyTo_ArgumentNullException");

	internal static string Collection_CopyTo_ArgumentOutOfRangeException => GetResourceString("Collection_CopyTo_ArgumentOutOfRangeException");

	internal static string ConcurrentCollection_SyncRoot_NotSupported => GetResourceString("ConcurrentCollection_SyncRoot_NotSupported");

	internal static string ConcurrentDictionary_ArrayIncorrectType => GetResourceString("ConcurrentDictionary_ArrayIncorrectType");

	internal static string ConcurrentDictionary_SourceContainsDuplicateKeys => GetResourceString("ConcurrentDictionary_SourceContainsDuplicateKeys");

	internal static string ConcurrentDictionary_ConcurrencyLevelMustBePositive => GetResourceString("ConcurrentDictionary_ConcurrencyLevelMustBePositive");

	internal static string ConcurrentDictionary_CapacityMustNotBeNegative => GetResourceString("ConcurrentDictionary_CapacityMustNotBeNegative");

	internal static string ConcurrentDictionary_IndexIsNegative => GetResourceString("ConcurrentDictionary_IndexIsNegative");

	internal static string ConcurrentDictionary_ArrayNotLargeEnough => GetResourceString("ConcurrentDictionary_ArrayNotLargeEnough");

	internal static string ConcurrentDictionary_KeyAlreadyExisted => GetResourceString("ConcurrentDictionary_KeyAlreadyExisted");

	internal static string ConcurrentDictionary_ItemKeyIsNull => GetResourceString("ConcurrentDictionary_ItemKeyIsNull");

	internal static string ConcurrentDictionary_TypeOfKeyIncorrect => GetResourceString("ConcurrentDictionary_TypeOfKeyIncorrect");

	internal static string ConcurrentDictionary_TypeOfValueIncorrect => GetResourceString("ConcurrentDictionary_TypeOfValueIncorrect");

	internal static string ConcurrentStack_PushPopRange_CountOutOfRange => GetResourceString("ConcurrentStack_PushPopRange_CountOutOfRange");

	internal static string ConcurrentStack_PushPopRange_InvalidCount => GetResourceString("ConcurrentStack_PushPopRange_InvalidCount");

	internal static string ConcurrentStack_PushPopRange_StartOutOfRange => GetResourceString("ConcurrentStack_PushPopRange_StartOutOfRange");

	internal static string Partitioner_DynamicPartitionsNotSupported => GetResourceString("Partitioner_DynamicPartitionsNotSupported");

	internal static string PartitionerStatic_CanNotCallGetEnumeratorAfterSourceHasBeenDisposed => GetResourceString("PartitionerStatic_CanNotCallGetEnumeratorAfterSourceHasBeenDisposed");

	internal static string PartitionerStatic_CurrentCalledBeforeMoveNext => GetResourceString("PartitionerStatic_CurrentCalledBeforeMoveNext");

	internal static string ConcurrentBag_Enumerator_EnumerationNotStartedOrAlreadyFinished => GetResourceString("ConcurrentBag_Enumerator_EnumerationNotStartedOrAlreadyFinished");

	internal static string Arg_KeyNotFoundWithKey => GetResourceString("Arg_KeyNotFoundWithKey");

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

	internal static string Format(string resourceFormat, object p1)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1);
		}
		return string.Format(resourceFormat, p1);
	}

	internal static string Format(IFormatProvider provider, string resourceFormat, object p1)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1);
		}
		return string.Format(provider, resourceFormat, p1);
	}
}
