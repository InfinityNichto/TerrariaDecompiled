using System.Collections.Generic;

namespace System.Runtime.Serialization;

internal sealed class ClassDataNode : DataNode<object>
{
	private IList<ExtensionDataMember> _members;

	internal IList<ExtensionDataMember> Members
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

	internal ClassDataNode()
	{
		dataType = Globals.TypeOfClassDataNode;
	}

	public override void Clear()
	{
		base.Clear();
		_members = null;
	}
}
