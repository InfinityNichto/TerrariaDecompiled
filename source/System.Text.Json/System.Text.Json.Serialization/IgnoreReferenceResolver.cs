using System.Collections.Generic;

namespace System.Text.Json.Serialization;

internal sealed class IgnoreReferenceResolver : ReferenceResolver
{
	private Stack<ReferenceEqualsWrapper> _stackForCycleDetection;

	internal override void PopReferenceForCycleDetection()
	{
		_stackForCycleDetection.Pop();
	}

	internal override bool ContainsReferenceForCycleDetection(object value)
	{
		return _stackForCycleDetection?.Contains(new ReferenceEqualsWrapper(value)) ?? false;
	}

	internal override void PushReferenceForCycleDetection(object value)
	{
		ReferenceEqualsWrapper item = new ReferenceEqualsWrapper(value);
		if (_stackForCycleDetection == null)
		{
			_stackForCycleDetection = new Stack<ReferenceEqualsWrapper>();
		}
		_stackForCycleDetection.Push(item);
	}

	public override void AddReference(string referenceId, object value)
	{
		throw new InvalidOperationException();
	}

	public override string GetReference(object value, out bool alreadyExists)
	{
		throw new InvalidOperationException();
	}

	public override object ResolveReference(string referenceId)
	{
		throw new InvalidOperationException();
	}
}
