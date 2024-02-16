namespace System.ComponentModel.Design.Serialization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
[Obsolete("RootDesignerSerializerAttribute has been deprecated. Use DesignerSerializerAttribute instead. For example, to specify a root designer for CodeDom, use DesignerSerializerAttribute(...,typeof(TypeCodeDomSerializer)).")]
public sealed class RootDesignerSerializerAttribute : Attribute
{
	private string _typeId;

	public bool Reloadable { get; }

	public string? SerializerTypeName { get; }

	public string? SerializerBaseTypeName { get; }

	public override object TypeId
	{
		get
		{
			if (_typeId == null)
			{
				string text = SerializerBaseTypeName ?? string.Empty;
				int num = text.IndexOf(',');
				if (num != -1)
				{
					text = text.Substring(0, num);
				}
				_typeId = GetType().FullName + text;
			}
			return _typeId;
		}
	}

	public RootDesignerSerializerAttribute(Type serializerType, Type baseSerializerType, bool reloadable)
	{
		if (serializerType == null)
		{
			throw new ArgumentNullException("serializerType");
		}
		if (baseSerializerType == null)
		{
			throw new ArgumentNullException("baseSerializerType");
		}
		SerializerTypeName = serializerType.AssemblyQualifiedName;
		SerializerBaseTypeName = baseSerializerType.AssemblyQualifiedName;
		Reloadable = reloadable;
	}

	public RootDesignerSerializerAttribute(string serializerTypeName, Type baseSerializerType, bool reloadable)
	{
		if (baseSerializerType == null)
		{
			throw new ArgumentNullException("baseSerializerType");
		}
		SerializerTypeName = serializerTypeName;
		SerializerBaseTypeName = baseSerializerType.AssemblyQualifiedName;
		Reloadable = reloadable;
	}

	public RootDesignerSerializerAttribute(string? serializerTypeName, string? baseSerializerTypeName, bool reloadable)
	{
		SerializerTypeName = serializerTypeName;
		SerializerBaseTypeName = baseSerializerTypeName;
		Reloadable = reloadable;
	}
}
