using System.Collections.Generic;

namespace System.Runtime.Serialization;

internal sealed class ISerializableDataNode : DataNode<object>
{
	private string _factoryTypeName;

	private string _factoryTypeNamespace;

	private IList<ISerializableDataMember> _members;

	internal string FactoryTypeName
	{
		get
		{
			return _factoryTypeName;
		}
		set
		{
			_factoryTypeName = value;
		}
	}

	internal string FactoryTypeNamespace
	{
		get
		{
			return _factoryTypeNamespace;
		}
		set
		{
			_factoryTypeNamespace = value;
		}
	}

	internal IList<ISerializableDataMember> Members
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

	internal ISerializableDataNode()
	{
		dataType = Globals.TypeOfISerializableDataNode;
	}

	public override void GetData(ElementData element)
	{
		base.GetData(element);
		if (FactoryTypeName != null)
		{
			AddQualifiedNameAttribute(element, "z", "FactoryType", "http://schemas.microsoft.com/2003/10/Serialization/", FactoryTypeName, FactoryTypeNamespace);
		}
	}

	public override void Clear()
	{
		base.Clear();
		_members = null;
		_factoryTypeName = (_factoryTypeNamespace = null);
	}
}
