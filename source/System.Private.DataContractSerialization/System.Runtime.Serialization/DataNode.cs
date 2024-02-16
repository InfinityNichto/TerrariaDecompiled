namespace System.Runtime.Serialization;

internal class DataNode<T> : IDataNode
{
	protected Type dataType;

	private T _value;

	private string _dataContractName;

	private string _dataContractNamespace;

	private string _clrTypeName;

	private string _clrAssemblyName;

	private string _id = Globals.NewObjectId;

	private bool _isFinalValue;

	public Type DataType => dataType;

	public object Value
	{
		get
		{
			return _value;
		}
		set
		{
			_value = (T)value;
		}
	}

	bool IDataNode.IsFinalValue
	{
		get
		{
			return _isFinalValue;
		}
		set
		{
			_isFinalValue = value;
		}
	}

	public string DataContractName
	{
		get
		{
			return _dataContractName;
		}
		set
		{
			_dataContractName = value;
		}
	}

	public string DataContractNamespace
	{
		get
		{
			return _dataContractNamespace;
		}
		set
		{
			_dataContractNamespace = value;
		}
	}

	public string ClrTypeName
	{
		get
		{
			return _clrTypeName;
		}
		set
		{
			_clrTypeName = value;
		}
	}

	public string ClrAssemblyName
	{
		get
		{
			return _clrAssemblyName;
		}
		set
		{
			_clrAssemblyName = value;
		}
	}

	public bool PreservesReferences => Id != Globals.NewObjectId;

	public string Id
	{
		get
		{
			return _id;
		}
		set
		{
			_id = value;
		}
	}

	internal DataNode()
	{
		dataType = typeof(T);
		_isFinalValue = true;
	}

	internal DataNode(T value)
		: this()
	{
		_value = value;
	}

	public T GetValue()
	{
		return _value;
	}

	public virtual void GetData(ElementData element)
	{
		element.dataNode = this;
		element.attributeCount = 0;
		element.childElementIndex = 0;
		if (DataContractName != null)
		{
			AddQualifiedNameAttribute(element, "i", "type", "http://www.w3.org/2001/XMLSchema-instance", DataContractName, DataContractNamespace);
		}
		if (ClrTypeName != null)
		{
			element.AddAttribute("z", "http://schemas.microsoft.com/2003/10/Serialization/", "Type", ClrTypeName);
		}
		if (ClrAssemblyName != null)
		{
			element.AddAttribute("z", "http://schemas.microsoft.com/2003/10/Serialization/", "Assembly", ClrAssemblyName);
		}
	}

	public virtual void Clear()
	{
		_clrTypeName = (_clrAssemblyName = null);
	}

	internal void AddQualifiedNameAttribute(ElementData element, string elementPrefix, string elementName, string elementNs, string valueName, string valueNs)
	{
		string prefix = ExtensionDataReader.GetPrefix(valueNs);
		element.AddAttribute(elementPrefix, elementNs, elementName, prefix + ":" + valueName);
		bool flag = false;
		if (element.attributes != null)
		{
			for (int i = 0; i < element.attributes.Length; i++)
			{
				AttributeData attributeData = element.attributes[i];
				if (attributeData != null && attributeData.prefix == "xmlns" && attributeData.localName == prefix)
				{
					flag = true;
					break;
				}
			}
		}
		if (!flag)
		{
			element.AddAttribute("xmlns", "http://www.w3.org/2000/xmlns/", prefix, valueNs);
		}
	}
}
