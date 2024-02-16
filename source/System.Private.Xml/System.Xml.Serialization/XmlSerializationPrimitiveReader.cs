using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization;

internal sealed class XmlSerializationPrimitiveReader : XmlSerializationReader
{
	private string _id4_boolean;

	private string _id14_unsignedInt;

	private string _id15_unsignedLong;

	private string _id7_float;

	private string _id10_dateTime;

	private string _id20_dateTimeOffset;

	private string _id6_long;

	private string _id9_decimal;

	private string _id8_double;

	private string _id17_guid;

	private string _id19_TimeSpan;

	private string _id2_Item;

	private string _id13_unsignedShort;

	private string _id18_char;

	private string _id3_int;

	private string _id12_byte;

	private string _id16_base64Binary;

	private string _id11_unsignedByte;

	private string _id5_short;

	private string _id1_string;

	private string _id1_QName;

	internal object Read_string()
	{
		object result = null;
		base.Reader.MoveToContent();
		if (base.Reader.NodeType == XmlNodeType.Element)
		{
			if ((object)base.Reader.LocalName != _id1_string || (object)base.Reader.NamespaceURI != _id2_Item)
			{
				throw CreateUnknownNodeException();
			}
			result = ((!ReadNull()) ? base.Reader.ReadElementString() : null);
		}
		else
		{
			UnknownNode(null);
		}
		return result;
	}

	internal object Read_int()
	{
		object result = null;
		base.Reader.MoveToContent();
		if (base.Reader.NodeType == XmlNodeType.Element)
		{
			if ((object)base.Reader.LocalName != _id3_int || (object)base.Reader.NamespaceURI != _id2_Item)
			{
				throw CreateUnknownNodeException();
			}
			result = XmlConvert.ToInt32(base.Reader.ReadElementString());
		}
		else
		{
			UnknownNode(null);
		}
		return result;
	}

	internal object Read_boolean()
	{
		object result = null;
		base.Reader.MoveToContent();
		if (base.Reader.NodeType == XmlNodeType.Element)
		{
			if ((object)base.Reader.LocalName != _id4_boolean || (object)base.Reader.NamespaceURI != _id2_Item)
			{
				throw CreateUnknownNodeException();
			}
			result = XmlConvert.ToBoolean(base.Reader.ReadElementString());
		}
		else
		{
			UnknownNode(null);
		}
		return result;
	}

	internal object Read_short()
	{
		object result = null;
		base.Reader.MoveToContent();
		if (base.Reader.NodeType == XmlNodeType.Element)
		{
			if ((object)base.Reader.LocalName != _id5_short || (object)base.Reader.NamespaceURI != _id2_Item)
			{
				throw CreateUnknownNodeException();
			}
			result = XmlConvert.ToInt16(base.Reader.ReadElementString());
		}
		else
		{
			UnknownNode(null);
		}
		return result;
	}

	internal object Read_long()
	{
		object result = null;
		base.Reader.MoveToContent();
		if (base.Reader.NodeType == XmlNodeType.Element)
		{
			if ((object)base.Reader.LocalName != _id6_long || (object)base.Reader.NamespaceURI != _id2_Item)
			{
				throw CreateUnknownNodeException();
			}
			result = XmlConvert.ToInt64(base.Reader.ReadElementString());
		}
		else
		{
			UnknownNode(null);
		}
		return result;
	}

	internal object Read_float()
	{
		object result = null;
		base.Reader.MoveToContent();
		if (base.Reader.NodeType == XmlNodeType.Element)
		{
			if ((object)base.Reader.LocalName != _id7_float || (object)base.Reader.NamespaceURI != _id2_Item)
			{
				throw CreateUnknownNodeException();
			}
			result = XmlConvert.ToSingle(base.Reader.ReadElementString());
		}
		else
		{
			UnknownNode(null);
		}
		return result;
	}

	internal object Read_double()
	{
		object result = null;
		base.Reader.MoveToContent();
		if (base.Reader.NodeType == XmlNodeType.Element)
		{
			if ((object)base.Reader.LocalName != _id8_double || (object)base.Reader.NamespaceURI != _id2_Item)
			{
				throw CreateUnknownNodeException();
			}
			result = XmlConvert.ToDouble(base.Reader.ReadElementString());
		}
		else
		{
			UnknownNode(null);
		}
		return result;
	}

	internal object Read_decimal()
	{
		object result = null;
		base.Reader.MoveToContent();
		if (base.Reader.NodeType == XmlNodeType.Element)
		{
			if ((object)base.Reader.LocalName != _id9_decimal || (object)base.Reader.NamespaceURI != _id2_Item)
			{
				throw CreateUnknownNodeException();
			}
			result = XmlConvert.ToDecimal(base.Reader.ReadElementString());
		}
		else
		{
			UnknownNode(null);
		}
		return result;
	}

	internal object Read_dateTime()
	{
		object result = null;
		base.Reader.MoveToContent();
		if (base.Reader.NodeType == XmlNodeType.Element)
		{
			if ((object)base.Reader.LocalName != _id10_dateTime || (object)base.Reader.NamespaceURI != _id2_Item)
			{
				throw CreateUnknownNodeException();
			}
			result = XmlSerializationReader.ToDateTime(base.Reader.ReadElementString());
		}
		else
		{
			UnknownNode(null);
		}
		return result;
	}

	internal object Read_dateTimeOffset()
	{
		object result = null;
		base.Reader.MoveToContent();
		if (base.Reader.NodeType == XmlNodeType.Element)
		{
			if ((object)base.Reader.LocalName != _id20_dateTimeOffset || (object)base.Reader.NamespaceURI != _id2_Item)
			{
				throw CreateUnknownNodeException();
			}
			if (base.Reader.IsEmptyElement)
			{
				base.Reader.Skip();
				result = default(DateTimeOffset);
			}
			else
			{
				result = XmlConvert.ToDateTimeOffset(base.Reader.ReadElementString());
			}
		}
		else
		{
			UnknownNode(null);
		}
		return result;
	}

	internal object Read_unsignedByte()
	{
		object result = null;
		base.Reader.MoveToContent();
		if (base.Reader.NodeType == XmlNodeType.Element)
		{
			if ((object)base.Reader.LocalName != _id11_unsignedByte || (object)base.Reader.NamespaceURI != _id2_Item)
			{
				throw CreateUnknownNodeException();
			}
			result = XmlConvert.ToByte(base.Reader.ReadElementString());
		}
		else
		{
			UnknownNode(null);
		}
		return result;
	}

	internal object Read_byte()
	{
		object result = null;
		base.Reader.MoveToContent();
		if (base.Reader.NodeType == XmlNodeType.Element)
		{
			if ((object)base.Reader.LocalName != _id12_byte || (object)base.Reader.NamespaceURI != _id2_Item)
			{
				throw CreateUnknownNodeException();
			}
			result = XmlConvert.ToSByte(base.Reader.ReadElementString());
		}
		else
		{
			UnknownNode(null);
		}
		return result;
	}

	internal object Read_unsignedShort()
	{
		object result = null;
		base.Reader.MoveToContent();
		if (base.Reader.NodeType == XmlNodeType.Element)
		{
			if ((object)base.Reader.LocalName != _id13_unsignedShort || (object)base.Reader.NamespaceURI != _id2_Item)
			{
				throw CreateUnknownNodeException();
			}
			result = XmlConvert.ToUInt16(base.Reader.ReadElementString());
		}
		else
		{
			UnknownNode(null);
		}
		return result;
	}

	internal object Read_unsignedInt()
	{
		object result = null;
		base.Reader.MoveToContent();
		if (base.Reader.NodeType == XmlNodeType.Element)
		{
			if ((object)base.Reader.LocalName != _id14_unsignedInt || (object)base.Reader.NamespaceURI != _id2_Item)
			{
				throw CreateUnknownNodeException();
			}
			result = XmlConvert.ToUInt32(base.Reader.ReadElementString());
		}
		else
		{
			UnknownNode(null);
		}
		return result;
	}

	internal object Read_unsignedLong()
	{
		object result = null;
		base.Reader.MoveToContent();
		if (base.Reader.NodeType == XmlNodeType.Element)
		{
			if ((object)base.Reader.LocalName != _id15_unsignedLong || (object)base.Reader.NamespaceURI != _id2_Item)
			{
				throw CreateUnknownNodeException();
			}
			result = XmlConvert.ToUInt64(base.Reader.ReadElementString());
		}
		else
		{
			UnknownNode(null);
		}
		return result;
	}

	internal object Read_base64Binary()
	{
		object result = null;
		base.Reader.MoveToContent();
		if (base.Reader.NodeType == XmlNodeType.Element)
		{
			if ((object)base.Reader.LocalName != _id16_base64Binary || (object)base.Reader.NamespaceURI != _id2_Item)
			{
				throw CreateUnknownNodeException();
			}
			result = ((!ReadNull()) ? ToByteArrayBase64(isNull: false) : null);
		}
		else
		{
			UnknownNode(null);
		}
		return result;
	}

	internal object Read_guid()
	{
		object result = null;
		base.Reader.MoveToContent();
		if (base.Reader.NodeType == XmlNodeType.Element)
		{
			if ((object)base.Reader.LocalName != _id17_guid || (object)base.Reader.NamespaceURI != _id2_Item)
			{
				throw CreateUnknownNodeException();
			}
			result = XmlConvert.ToGuid(base.Reader.ReadElementString());
		}
		else
		{
			UnknownNode(null);
		}
		return result;
	}

	internal object Read_TimeSpan()
	{
		object result = null;
		base.Reader.MoveToContent();
		if (base.Reader.NodeType == XmlNodeType.Element)
		{
			if ((object)base.Reader.LocalName != _id19_TimeSpan || (object)base.Reader.NamespaceURI != _id2_Item)
			{
				throw CreateUnknownNodeException();
			}
			if (base.Reader.IsEmptyElement)
			{
				base.Reader.Skip();
				result = default(TimeSpan);
			}
			else
			{
				result = XmlConvert.ToTimeSpan(base.Reader.ReadElementString());
			}
		}
		else
		{
			UnknownNode(null);
		}
		return result;
	}

	internal object Read_char()
	{
		object result = null;
		base.Reader.MoveToContent();
		if (base.Reader.NodeType == XmlNodeType.Element)
		{
			if ((object)base.Reader.LocalName != _id18_char || (object)base.Reader.NamespaceURI != _id2_Item)
			{
				throw CreateUnknownNodeException();
			}
			result = XmlSerializationReader.ToChar(base.Reader.ReadElementString());
		}
		else
		{
			UnknownNode(null);
		}
		return result;
	}

	internal object Read_QName()
	{
		object result = null;
		base.Reader.MoveToContent();
		if (base.Reader.NodeType == XmlNodeType.Element)
		{
			if ((object)base.Reader.LocalName != _id1_QName || (object)base.Reader.NamespaceURI != _id2_Item)
			{
				throw CreateUnknownNodeException();
			}
			result = ((!ReadNull()) ? ReadElementQualifiedName() : null);
		}
		else
		{
			UnknownNode(null);
		}
		return result;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	protected override void InitCallbacks()
	{
	}

	protected override void InitIDs()
	{
		_id4_boolean = base.Reader.NameTable.Add("boolean");
		_id14_unsignedInt = base.Reader.NameTable.Add("unsignedInt");
		_id15_unsignedLong = base.Reader.NameTable.Add("unsignedLong");
		_id7_float = base.Reader.NameTable.Add("float");
		_id10_dateTime = base.Reader.NameTable.Add("dateTime");
		_id20_dateTimeOffset = base.Reader.NameTable.Add("dateTimeOffset");
		_id6_long = base.Reader.NameTable.Add("long");
		_id9_decimal = base.Reader.NameTable.Add("decimal");
		_id8_double = base.Reader.NameTable.Add("double");
		_id17_guid = base.Reader.NameTable.Add("guid");
		_id19_TimeSpan = base.Reader.NameTable.Add("TimeSpan");
		_id2_Item = base.Reader.NameTable.Add("");
		_id13_unsignedShort = base.Reader.NameTable.Add("unsignedShort");
		_id18_char = base.Reader.NameTable.Add("char");
		_id3_int = base.Reader.NameTable.Add("int");
		_id12_byte = base.Reader.NameTable.Add("byte");
		_id16_base64Binary = base.Reader.NameTable.Add("base64Binary");
		_id11_unsignedByte = base.Reader.NameTable.Add("unsignedByte");
		_id5_short = base.Reader.NameTable.Add("short");
		_id1_string = base.Reader.NameTable.Add("string");
		_id1_QName = base.Reader.NameTable.Add("QName");
	}
}
