using System.Collections.Generic;

namespace System.Runtime.Serialization;

public sealed class ExtensionDataObject
{
	private IList<ExtensionDataMember> _members;

	internal IList<ExtensionDataMember>? Members
	{
		get
		{
			return _members;
		}
		set
		{
			_members = value;
		}
	}

	internal ExtensionDataObject()
	{
	}
}
