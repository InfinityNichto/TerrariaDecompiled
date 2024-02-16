using System.Collections.Generic;

namespace System.Text.Json.Serialization;

internal sealed class PreserveReferenceResolver : ReferenceResolver
{
	private uint _referenceCount;

	private readonly Dictionary<string, object> _referenceIdToObjectMap;

	private readonly Dictionary<object, string> _objectToReferenceIdMap;

	public PreserveReferenceResolver(bool writing)
	{
		if (writing)
		{
			_objectToReferenceIdMap = new Dictionary<object, string>(ReferenceEqualityComparer.Instance);
		}
		else
		{
			_referenceIdToObjectMap = new Dictionary<string, object>();
		}
	}

	public override void AddReference(string referenceId, object value)
	{
		if (!_referenceIdToObjectMap.TryAdd(in referenceId, in value))
		{
			ThrowHelper.ThrowJsonException_MetadataDuplicateIdFound(referenceId);
		}
	}

	public override string GetReference(object value, out bool alreadyExists)
	{
		if (_objectToReferenceIdMap.TryGetValue(value, out var value2))
		{
			alreadyExists = true;
		}
		else
		{
			_referenceCount++;
			value2 = _referenceCount.ToString();
			_objectToReferenceIdMap.Add(value, value2);
			alreadyExists = false;
		}
		return value2;
	}

	public override object ResolveReference(string referenceId)
	{
		if (!_referenceIdToObjectMap.TryGetValue(referenceId, out var value))
		{
			ThrowHelper.ThrowJsonException_MetadataReferenceNotFound(referenceId);
		}
		return value;
	}
}
