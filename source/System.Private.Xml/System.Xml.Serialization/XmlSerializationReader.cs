using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace System.Xml.Serialization;

public abstract class XmlSerializationReader : XmlSerializationGeneratedCode
{
	private struct SoapArrayInfo
	{
		public string qname;

		public int dimensions;

		public int length;

		public int jaggedDimensions;
	}

	protected class Fixup
	{
		private readonly XmlSerializationFixupCallback _callback;

		private object _source;

		private readonly string[] _ids;

		public XmlSerializationFixupCallback Callback => _callback;

		public object? Source
		{
			get
			{
				return _source;
			}
			set
			{
				_source = value;
			}
		}

		public string?[]? Ids => _ids;

		public Fixup(object? o, XmlSerializationFixupCallback callback, int count)
			: this(o, callback, new string[count])
		{
		}

		public Fixup(object? o, XmlSerializationFixupCallback callback, string?[]? ids)
		{
			_callback = callback;
			Source = o;
			_ids = ids;
		}
	}

	protected class CollectionFixup
	{
		private readonly XmlSerializationCollectionFixupCallback _callback;

		private readonly object _collection;

		private readonly object _collectionItems;

		public XmlSerializationCollectionFixupCallback Callback => _callback;

		public object? Collection => _collection;

		public object CollectionItems => _collectionItems;

		public CollectionFixup(object? collection, XmlSerializationCollectionFixupCallback callback, object collectionItems)
		{
			_callback = callback;
			_collection = collection;
			_collectionItems = collectionItems;
		}
	}

	private XmlReader _r;

	private XmlDocument _d;

	private Hashtable _callbacks;

	private Hashtable _types;

	private Hashtable _typesReverse;

	private XmlDeserializationEvents _events;

	private Hashtable _targets;

	private Hashtable _referencedTargets;

	private ArrayList _targetsWithoutIds;

	private ArrayList _fixups;

	private ArrayList _collectionFixups;

	private bool _soap12;

	private bool _isReturnValue;

	private bool _decodeName = true;

	private string _schemaNsID;

	private string _schemaNs1999ID;

	private string _schemaNs2000ID;

	private string _schemaNonXsdTypesNsID;

	private string _instanceNsID;

	private string _instanceNs2000ID;

	private string _instanceNs1999ID;

	private string _soapNsID;

	private string _soap12NsID;

	private string _schemaID;

	private string _wsdlNsID;

	private string _wsdlArrayTypeID;

	private string _nullID;

	private string _nilID;

	private string _typeID;

	private string _arrayTypeID;

	private string _itemTypeID;

	private string _arraySizeID;

	private string _arrayID;

	private string _urTypeID;

	private string _stringID;

	private string _intID;

	private string _booleanID;

	private string _shortID;

	private string _longID;

	private string _floatID;

	private string _doubleID;

	private string _decimalID;

	private string _dateTimeID;

	private string _qnameID;

	private string _dateID;

	private string _timeID;

	private string _hexBinaryID;

	private string _base64BinaryID;

	private string _base64ID;

	private string _unsignedByteID;

	private string _byteID;

	private string _unsignedShortID;

	private string _unsignedIntID;

	private string _unsignedLongID;

	private string _oldDecimalID;

	private string _oldTimeInstantID;

	private string _anyURIID;

	private string _durationID;

	private string _ENTITYID;

	private string _ENTITIESID;

	private string _gDayID;

	private string _gMonthID;

	private string _gMonthDayID;

	private string _gYearID;

	private string _gYearMonthID;

	private string _IDID;

	private string _IDREFID;

	private string _IDREFSID;

	private string _integerID;

	private string _languageID;

	private string _nameID;

	private string _NCNameID;

	private string _NMTOKENID;

	private string _NMTOKENSID;

	private string _negativeIntegerID;

	private string _nonPositiveIntegerID;

	private string _nonNegativeIntegerID;

	private string _normalizedStringID;

	private string _NOTATIONID;

	private string _positiveIntegerID;

	private string _tokenID;

	private string _charID;

	private string _guidID;

	private string _timeSpanID;

	private string _dateTimeOffsetID;

	protected bool DecodeName
	{
		get
		{
			return _decodeName;
		}
		set
		{
			_decodeName = value;
		}
	}

	protected XmlReader Reader => _r;

	protected int ReaderCount => 0;

	protected XmlDocument Document
	{
		get
		{
			if (_d == null)
			{
				_d = new XmlDocument(_r.NameTable);
				_d.SetBaseURI(_r.BaseURI);
			}
			return _d;
		}
	}

	protected bool IsReturnValue
	{
		get
		{
			if (_isReturnValue)
			{
				return !_soap12;
			}
			return false;
		}
		set
		{
			_isReturnValue = value;
		}
	}

	protected abstract void InitIDs();

	internal void Init(XmlReader r, XmlDeserializationEvents events, string encodingStyle, TempAssembly tempAssembly)
	{
		_events = events;
		_r = r;
		_d = null;
		_soap12 = encodingStyle == "http://www.w3.org/2003/05/soap-encoding";
		Init(tempAssembly);
		_schemaNsID = r.NameTable.Add("http://www.w3.org/2001/XMLSchema");
		_schemaNs2000ID = r.NameTable.Add("http://www.w3.org/2000/10/XMLSchema");
		_schemaNs1999ID = r.NameTable.Add("http://www.w3.org/1999/XMLSchema");
		_schemaNonXsdTypesNsID = r.NameTable.Add("http://microsoft.com/wsdl/types/");
		_instanceNsID = r.NameTable.Add("http://www.w3.org/2001/XMLSchema-instance");
		_instanceNs2000ID = r.NameTable.Add("http://www.w3.org/2000/10/XMLSchema-instance");
		_instanceNs1999ID = r.NameTable.Add("http://www.w3.org/1999/XMLSchema-instance");
		_soapNsID = r.NameTable.Add("http://schemas.xmlsoap.org/soap/encoding/");
		_soap12NsID = r.NameTable.Add("http://www.w3.org/2003/05/soap-encoding");
		_schemaID = r.NameTable.Add("schema");
		_wsdlNsID = r.NameTable.Add("http://schemas.xmlsoap.org/wsdl/");
		_wsdlArrayTypeID = r.NameTable.Add("arrayType");
		_nullID = r.NameTable.Add("null");
		_nilID = r.NameTable.Add("nil");
		_typeID = r.NameTable.Add("type");
		_arrayTypeID = r.NameTable.Add("arrayType");
		_itemTypeID = r.NameTable.Add("itemType");
		_arraySizeID = r.NameTable.Add("arraySize");
		_arrayID = r.NameTable.Add("Array");
		_urTypeID = r.NameTable.Add("anyType");
		InitIDs();
	}

	protected static Assembly? ResolveDynamicAssembly(string assemblyFullName)
	{
		return DynamicAssemblies.Get(assemblyFullName);
	}

	private void InitPrimitiveIDs()
	{
		if (_tokenID == null)
		{
			_r.NameTable.Add("http://www.w3.org/2001/XMLSchema");
			_r.NameTable.Add("http://microsoft.com/wsdl/types/");
			_stringID = _r.NameTable.Add("string");
			_intID = _r.NameTable.Add("int");
			_booleanID = _r.NameTable.Add("boolean");
			_shortID = _r.NameTable.Add("short");
			_longID = _r.NameTable.Add("long");
			_floatID = _r.NameTable.Add("float");
			_doubleID = _r.NameTable.Add("double");
			_decimalID = _r.NameTable.Add("decimal");
			_dateTimeID = _r.NameTable.Add("dateTime");
			_qnameID = _r.NameTable.Add("QName");
			_dateID = _r.NameTable.Add("date");
			_timeID = _r.NameTable.Add("time");
			_hexBinaryID = _r.NameTable.Add("hexBinary");
			_base64BinaryID = _r.NameTable.Add("base64Binary");
			_unsignedByteID = _r.NameTable.Add("unsignedByte");
			_byteID = _r.NameTable.Add("byte");
			_unsignedShortID = _r.NameTable.Add("unsignedShort");
			_unsignedIntID = _r.NameTable.Add("unsignedInt");
			_unsignedLongID = _r.NameTable.Add("unsignedLong");
			_oldDecimalID = _r.NameTable.Add("decimal");
			_oldTimeInstantID = _r.NameTable.Add("timeInstant");
			_charID = _r.NameTable.Add("char");
			_guidID = _r.NameTable.Add("guid");
			_timeSpanID = _r.NameTable.Add("TimeSpan");
			_dateTimeOffsetID = _r.NameTable.Add("dateTimeOffset");
			_base64ID = _r.NameTable.Add("base64");
			_anyURIID = _r.NameTable.Add("anyURI");
			_durationID = _r.NameTable.Add("duration");
			_ENTITYID = _r.NameTable.Add("ENTITY");
			_ENTITIESID = _r.NameTable.Add("ENTITIES");
			_gDayID = _r.NameTable.Add("gDay");
			_gMonthID = _r.NameTable.Add("gMonth");
			_gMonthDayID = _r.NameTable.Add("gMonthDay");
			_gYearID = _r.NameTable.Add("gYear");
			_gYearMonthID = _r.NameTable.Add("gYearMonth");
			_IDID = _r.NameTable.Add("ID");
			_IDREFID = _r.NameTable.Add("IDREF");
			_IDREFSID = _r.NameTable.Add("IDREFS");
			_integerID = _r.NameTable.Add("integer");
			_languageID = _r.NameTable.Add("language");
			_nameID = _r.NameTable.Add("Name");
			_NCNameID = _r.NameTable.Add("NCName");
			_NMTOKENID = _r.NameTable.Add("NMTOKEN");
			_NMTOKENSID = _r.NameTable.Add("NMTOKENS");
			_negativeIntegerID = _r.NameTable.Add("negativeInteger");
			_nonNegativeIntegerID = _r.NameTable.Add("nonNegativeInteger");
			_nonPositiveIntegerID = _r.NameTable.Add("nonPositiveInteger");
			_normalizedStringID = _r.NameTable.Add("normalizedString");
			_NOTATIONID = _r.NameTable.Add("NOTATION");
			_positiveIntegerID = _r.NameTable.Add("positiveInteger");
			_tokenID = _r.NameTable.Add("token");
		}
	}

	protected XmlQualifiedName? GetXsiType()
	{
		string attribute = _r.GetAttribute(_typeID, _instanceNsID);
		if (attribute == null)
		{
			attribute = _r.GetAttribute(_typeID, _instanceNs2000ID);
			if (attribute == null)
			{
				attribute = _r.GetAttribute(_typeID, _instanceNs1999ID);
				if (attribute == null)
				{
					return null;
				}
			}
		}
		return ToXmlQualifiedName(attribute, decodeName: false);
	}

	private Type GetPrimitiveType(XmlQualifiedName typeName, bool throwOnUnknown)
	{
		InitPrimitiveIDs();
		if ((object)typeName.Namespace == _schemaNsID || (object)typeName.Namespace == _soapNsID || (object)typeName.Namespace == _soap12NsID)
		{
			if ((object)typeName.Name == _stringID || (object)typeName.Name == _anyURIID || (object)typeName.Name == _durationID || (object)typeName.Name == _ENTITYID || (object)typeName.Name == _ENTITIESID || (object)typeName.Name == _gDayID || (object)typeName.Name == _gMonthID || (object)typeName.Name == _gMonthDayID || (object)typeName.Name == _gYearID || (object)typeName.Name == _gYearMonthID || (object)typeName.Name == _IDID || (object)typeName.Name == _IDREFID || (object)typeName.Name == _IDREFSID || (object)typeName.Name == _integerID || (object)typeName.Name == _languageID || (object)typeName.Name == _nameID || (object)typeName.Name == _NCNameID || (object)typeName.Name == _NMTOKENID || (object)typeName.Name == _NMTOKENSID || (object)typeName.Name == _negativeIntegerID || (object)typeName.Name == _nonPositiveIntegerID || (object)typeName.Name == _nonNegativeIntegerID || (object)typeName.Name == _normalizedStringID || (object)typeName.Name == _NOTATIONID || (object)typeName.Name == _positiveIntegerID || (object)typeName.Name == _tokenID)
			{
				return typeof(string);
			}
			if ((object)typeName.Name == _intID)
			{
				return typeof(int);
			}
			if ((object)typeName.Name == _booleanID)
			{
				return typeof(bool);
			}
			if ((object)typeName.Name == _shortID)
			{
				return typeof(short);
			}
			if ((object)typeName.Name == _longID)
			{
				return typeof(long);
			}
			if ((object)typeName.Name == _floatID)
			{
				return typeof(float);
			}
			if ((object)typeName.Name == _doubleID)
			{
				return typeof(double);
			}
			if ((object)typeName.Name == _decimalID)
			{
				return typeof(decimal);
			}
			if ((object)typeName.Name == _dateTimeID)
			{
				return typeof(DateTime);
			}
			if ((object)typeName.Name == _qnameID)
			{
				return typeof(XmlQualifiedName);
			}
			if ((object)typeName.Name == _dateID)
			{
				return typeof(DateTime);
			}
			if ((object)typeName.Name == _timeID)
			{
				return typeof(DateTime);
			}
			if ((object)typeName.Name == _hexBinaryID)
			{
				return typeof(byte[]);
			}
			if ((object)typeName.Name == _base64BinaryID)
			{
				return typeof(byte[]);
			}
			if ((object)typeName.Name == _unsignedByteID)
			{
				return typeof(byte);
			}
			if ((object)typeName.Name == _byteID)
			{
				return typeof(sbyte);
			}
			if ((object)typeName.Name == _unsignedShortID)
			{
				return typeof(ushort);
			}
			if ((object)typeName.Name == _unsignedIntID)
			{
				return typeof(uint);
			}
			if ((object)typeName.Name == _unsignedLongID)
			{
				return typeof(ulong);
			}
			throw CreateUnknownTypeException(typeName);
		}
		if ((object)typeName.Namespace == _schemaNs2000ID || (object)typeName.Namespace == _schemaNs1999ID)
		{
			if ((object)typeName.Name == _stringID || (object)typeName.Name == _anyURIID || (object)typeName.Name == _durationID || (object)typeName.Name == _ENTITYID || (object)typeName.Name == _ENTITIESID || (object)typeName.Name == _gDayID || (object)typeName.Name == _gMonthID || (object)typeName.Name == _gMonthDayID || (object)typeName.Name == _gYearID || (object)typeName.Name == _gYearMonthID || (object)typeName.Name == _IDID || (object)typeName.Name == _IDREFID || (object)typeName.Name == _IDREFSID || (object)typeName.Name == _integerID || (object)typeName.Name == _languageID || (object)typeName.Name == _nameID || (object)typeName.Name == _NCNameID || (object)typeName.Name == _NMTOKENID || (object)typeName.Name == _NMTOKENSID || (object)typeName.Name == _negativeIntegerID || (object)typeName.Name == _nonPositiveIntegerID || (object)typeName.Name == _nonNegativeIntegerID || (object)typeName.Name == _normalizedStringID || (object)typeName.Name == _NOTATIONID || (object)typeName.Name == _positiveIntegerID || (object)typeName.Name == _tokenID)
			{
				return typeof(string);
			}
			if ((object)typeName.Name == _intID)
			{
				return typeof(int);
			}
			if ((object)typeName.Name == _booleanID)
			{
				return typeof(bool);
			}
			if ((object)typeName.Name == _shortID)
			{
				return typeof(short);
			}
			if ((object)typeName.Name == _longID)
			{
				return typeof(long);
			}
			if ((object)typeName.Name == _floatID)
			{
				return typeof(float);
			}
			if ((object)typeName.Name == _doubleID)
			{
				return typeof(double);
			}
			if ((object)typeName.Name == _oldDecimalID)
			{
				return typeof(decimal);
			}
			if ((object)typeName.Name == _oldTimeInstantID)
			{
				return typeof(DateTime);
			}
			if ((object)typeName.Name == _qnameID)
			{
				return typeof(XmlQualifiedName);
			}
			if ((object)typeName.Name == _dateID)
			{
				return typeof(DateTime);
			}
			if ((object)typeName.Name == _timeID)
			{
				return typeof(DateTime);
			}
			if ((object)typeName.Name == _hexBinaryID)
			{
				return typeof(byte[]);
			}
			if ((object)typeName.Name == _byteID)
			{
				return typeof(sbyte);
			}
			if ((object)typeName.Name == _unsignedShortID)
			{
				return typeof(ushort);
			}
			if ((object)typeName.Name == _unsignedIntID)
			{
				return typeof(uint);
			}
			if ((object)typeName.Name == _unsignedLongID)
			{
				return typeof(ulong);
			}
			throw CreateUnknownTypeException(typeName);
		}
		if ((object)typeName.Namespace == _schemaNonXsdTypesNsID)
		{
			if ((object)typeName.Name == _charID)
			{
				return typeof(char);
			}
			if ((object)typeName.Name == _guidID)
			{
				return typeof(Guid);
			}
			throw CreateUnknownTypeException(typeName);
		}
		if (throwOnUnknown)
		{
			throw CreateUnknownTypeException(typeName);
		}
		return null;
	}

	private bool IsPrimitiveNamespace(string ns)
	{
		if ((object)ns != _schemaNsID && (object)ns != _schemaNonXsdTypesNsID && (object)ns != _soapNsID && (object)ns != _soap12NsID && (object)ns != _schemaNs2000ID)
		{
			return (object)ns == _schemaNs1999ID;
		}
		return true;
	}

	private string ReadStringValue()
	{
		if (_r.IsEmptyElement)
		{
			_r.Skip();
			return string.Empty;
		}
		_r.ReadStartElement();
		string result = _r.ReadString();
		ReadEndElement();
		return result;
	}

	private XmlQualifiedName ReadXmlQualifiedName()
	{
		bool flag = false;
		string value;
		if (_r.IsEmptyElement)
		{
			value = string.Empty;
			flag = true;
		}
		else
		{
			_r.ReadStartElement();
			value = _r.ReadString();
		}
		XmlQualifiedName result = ToXmlQualifiedName(value);
		if (flag)
		{
			_r.Skip();
		}
		else
		{
			ReadEndElement();
		}
		return result;
	}

	private byte[] ReadByteArray(bool isBase64)
	{
		ArrayList arrayList = new ArrayList();
		int num = 1024;
		int num2 = -1;
		int num3 = 0;
		int num4 = 0;
		byte[] array = new byte[num];
		arrayList.Add(array);
		while (num2 != 0)
		{
			if (num3 == array.Length)
			{
				num = Math.Min(num * 2, 65536);
				array = new byte[num];
				num3 = 0;
				arrayList.Add(array);
			}
			num2 = ((!isBase64) ? _r.ReadElementContentAsBinHex(array, num3, array.Length - num3) : _r.ReadElementContentAsBase64(array, num3, array.Length - num3));
			num3 += num2;
			num4 += num2;
		}
		byte[] array2 = new byte[num4];
		num3 = 0;
		foreach (byte[] item in arrayList)
		{
			num = Math.Min(item.Length, num4);
			if (num > 0)
			{
				Buffer.BlockCopy(item, 0, array2, num3, num);
				num3 += num;
				num4 -= num;
			}
		}
		arrayList.Clear();
		return array2;
	}

	protected object? ReadTypedPrimitive(XmlQualifiedName type)
	{
		return ReadTypedPrimitive(type, elementCanBeType: false);
	}

	private object ReadTypedPrimitive(XmlQualifiedName type, bool elementCanBeType)
	{
		InitPrimitiveIDs();
		object obj = null;
		if (!IsPrimitiveNamespace(type.Namespace) || (object)type.Name == _urTypeID)
		{
			return ReadXmlNodes(elementCanBeType);
		}
		if ((object)type.Namespace == _schemaNsID || (object)type.Namespace == _soapNsID || (object)type.Namespace == _soap12NsID)
		{
			if ((object)type.Name == _stringID || (object)type.Name == _normalizedStringID)
			{
				return ReadStringValue();
			}
			if ((object)type.Name == _anyURIID || (object)type.Name == _durationID || (object)type.Name == _ENTITYID || (object)type.Name == _ENTITIESID || (object)type.Name == _gDayID || (object)type.Name == _gMonthID || (object)type.Name == _gMonthDayID || (object)type.Name == _gYearID || (object)type.Name == _gYearMonthID || (object)type.Name == _IDID || (object)type.Name == _IDREFID || (object)type.Name == _IDREFSID || (object)type.Name == _integerID || (object)type.Name == _languageID || (object)type.Name == _nameID || (object)type.Name == _NCNameID || (object)type.Name == _NMTOKENID || (object)type.Name == _NMTOKENSID || (object)type.Name == _negativeIntegerID || (object)type.Name == _nonPositiveIntegerID || (object)type.Name == _nonNegativeIntegerID || (object)type.Name == _NOTATIONID || (object)type.Name == _positiveIntegerID || (object)type.Name == _tokenID)
			{
				return CollapseWhitespace(ReadStringValue());
			}
			if ((object)type.Name == _intID)
			{
				return XmlConvert.ToInt32(ReadStringValue());
			}
			if ((object)type.Name == _booleanID)
			{
				return XmlConvert.ToBoolean(ReadStringValue());
			}
			if ((object)type.Name == _shortID)
			{
				return XmlConvert.ToInt16(ReadStringValue());
			}
			if ((object)type.Name == _longID)
			{
				return XmlConvert.ToInt64(ReadStringValue());
			}
			if ((object)type.Name == _floatID)
			{
				return XmlConvert.ToSingle(ReadStringValue());
			}
			if ((object)type.Name == _doubleID)
			{
				return XmlConvert.ToDouble(ReadStringValue());
			}
			if ((object)type.Name == _decimalID)
			{
				return XmlConvert.ToDecimal(ReadStringValue());
			}
			if ((object)type.Name == _dateTimeID)
			{
				return ToDateTime(ReadStringValue());
			}
			if ((object)type.Name == _qnameID)
			{
				return ReadXmlQualifiedName();
			}
			if ((object)type.Name == _dateID)
			{
				return ToDate(ReadStringValue());
			}
			if ((object)type.Name == _timeID)
			{
				return ToTime(ReadStringValue());
			}
			if ((object)type.Name == _unsignedByteID)
			{
				return XmlConvert.ToByte(ReadStringValue());
			}
			if ((object)type.Name == _byteID)
			{
				return XmlConvert.ToSByte(ReadStringValue());
			}
			if ((object)type.Name == _unsignedShortID)
			{
				return XmlConvert.ToUInt16(ReadStringValue());
			}
			if ((object)type.Name == _unsignedIntID)
			{
				return XmlConvert.ToUInt32(ReadStringValue());
			}
			if ((object)type.Name == _unsignedLongID)
			{
				return XmlConvert.ToUInt64(ReadStringValue());
			}
			if ((object)type.Name == _hexBinaryID)
			{
				return ToByteArrayHex(isNull: false);
			}
			if ((object)type.Name == _base64BinaryID)
			{
				return ToByteArrayBase64(isNull: false);
			}
			if ((object)type.Name == _base64ID && ((object)type.Namespace == _soapNsID || (object)type.Namespace == _soap12NsID))
			{
				return ToByteArrayBase64(isNull: false);
			}
			return ReadXmlNodes(elementCanBeType);
		}
		if ((object)type.Namespace == _schemaNs2000ID || (object)type.Namespace == _schemaNs1999ID)
		{
			if ((object)type.Name == _stringID || (object)type.Name == _normalizedStringID)
			{
				return ReadStringValue();
			}
			if ((object)type.Name == _anyURIID || (object)type.Name == _anyURIID || (object)type.Name == _durationID || (object)type.Name == _ENTITYID || (object)type.Name == _ENTITIESID || (object)type.Name == _gDayID || (object)type.Name == _gMonthID || (object)type.Name == _gMonthDayID || (object)type.Name == _gYearID || (object)type.Name == _gYearMonthID || (object)type.Name == _IDID || (object)type.Name == _IDREFID || (object)type.Name == _IDREFSID || (object)type.Name == _integerID || (object)type.Name == _languageID || (object)type.Name == _nameID || (object)type.Name == _NCNameID || (object)type.Name == _NMTOKENID || (object)type.Name == _NMTOKENSID || (object)type.Name == _negativeIntegerID || (object)type.Name == _nonPositiveIntegerID || (object)type.Name == _nonNegativeIntegerID || (object)type.Name == _NOTATIONID || (object)type.Name == _positiveIntegerID || (object)type.Name == _tokenID)
			{
				return CollapseWhitespace(ReadStringValue());
			}
			if ((object)type.Name == _intID)
			{
				return XmlConvert.ToInt32(ReadStringValue());
			}
			if ((object)type.Name == _booleanID)
			{
				return XmlConvert.ToBoolean(ReadStringValue());
			}
			if ((object)type.Name == _shortID)
			{
				return XmlConvert.ToInt16(ReadStringValue());
			}
			if ((object)type.Name == _longID)
			{
				return XmlConvert.ToInt64(ReadStringValue());
			}
			if ((object)type.Name == _floatID)
			{
				return XmlConvert.ToSingle(ReadStringValue());
			}
			if ((object)type.Name == _doubleID)
			{
				return XmlConvert.ToDouble(ReadStringValue());
			}
			if ((object)type.Name == _oldDecimalID)
			{
				return XmlConvert.ToDecimal(ReadStringValue());
			}
			if ((object)type.Name == _oldTimeInstantID)
			{
				return ToDateTime(ReadStringValue());
			}
			if ((object)type.Name == _qnameID)
			{
				return ReadXmlQualifiedName();
			}
			if ((object)type.Name == _dateID)
			{
				return ToDate(ReadStringValue());
			}
			if ((object)type.Name == _timeID)
			{
				return ToTime(ReadStringValue());
			}
			if ((object)type.Name == _unsignedByteID)
			{
				return XmlConvert.ToByte(ReadStringValue());
			}
			if ((object)type.Name == _byteID)
			{
				return XmlConvert.ToSByte(ReadStringValue());
			}
			if ((object)type.Name == _unsignedShortID)
			{
				return XmlConvert.ToUInt16(ReadStringValue());
			}
			if ((object)type.Name == _unsignedIntID)
			{
				return XmlConvert.ToUInt32(ReadStringValue());
			}
			if ((object)type.Name == _unsignedLongID)
			{
				return XmlConvert.ToUInt64(ReadStringValue());
			}
			return ReadXmlNodes(elementCanBeType);
		}
		if ((object)type.Namespace == _schemaNonXsdTypesNsID)
		{
			if ((object)type.Name == _charID)
			{
				return ToChar(ReadStringValue());
			}
			if ((object)type.Name == _guidID)
			{
				return new Guid(CollapseWhitespace(ReadStringValue()));
			}
			if ((object)type.Name == _timeSpanID)
			{
				return XmlConvert.ToTimeSpan(ReadStringValue());
			}
			if ((object)type.Name == _dateTimeOffsetID)
			{
				return XmlConvert.ToDateTimeOffset(ReadStringValue());
			}
			return ReadXmlNodes(elementCanBeType);
		}
		return ReadXmlNodes(elementCanBeType);
	}

	protected object? ReadTypedNull(XmlQualifiedName type)
	{
		InitPrimitiveIDs();
		object obj = null;
		if (!IsPrimitiveNamespace(type.Namespace) || (object)type.Name == _urTypeID)
		{
			return null;
		}
		if ((object)type.Namespace == _schemaNsID || (object)type.Namespace == _soapNsID || (object)type.Namespace == _soap12NsID)
		{
			if ((object)type.Name == _stringID || (object)type.Name == _anyURIID || (object)type.Name == _durationID || (object)type.Name == _ENTITYID || (object)type.Name == _ENTITIESID || (object)type.Name == _gDayID || (object)type.Name == _gMonthID || (object)type.Name == _gMonthDayID || (object)type.Name == _gYearID || (object)type.Name == _gYearMonthID || (object)type.Name == _IDID || (object)type.Name == _IDREFID || (object)type.Name == _IDREFSID || (object)type.Name == _integerID || (object)type.Name == _languageID || (object)type.Name == _nameID || (object)type.Name == _NCNameID || (object)type.Name == _NMTOKENID || (object)type.Name == _NMTOKENSID || (object)type.Name == _negativeIntegerID || (object)type.Name == _nonPositiveIntegerID || (object)type.Name == _nonNegativeIntegerID || (object)type.Name == _normalizedStringID || (object)type.Name == _NOTATIONID || (object)type.Name == _positiveIntegerID || (object)type.Name == _tokenID)
			{
				return null;
			}
			if ((object)type.Name == _intID)
			{
				return null;
			}
			if ((object)type.Name == _booleanID)
			{
				return null;
			}
			if ((object)type.Name == _shortID)
			{
				return null;
			}
			if ((object)type.Name == _longID)
			{
				return null;
			}
			if ((object)type.Name == _floatID)
			{
				return null;
			}
			if ((object)type.Name == _doubleID)
			{
				return null;
			}
			if ((object)type.Name == _decimalID)
			{
				return null;
			}
			if ((object)type.Name == _dateTimeID)
			{
				return null;
			}
			if ((object)type.Name == _qnameID)
			{
				return null;
			}
			if ((object)type.Name == _dateID)
			{
				return null;
			}
			if ((object)type.Name == _timeID)
			{
				return null;
			}
			if ((object)type.Name == _unsignedByteID)
			{
				return null;
			}
			if ((object)type.Name == _byteID)
			{
				return null;
			}
			if ((object)type.Name == _unsignedShortID)
			{
				return null;
			}
			if ((object)type.Name == _unsignedIntID)
			{
				return null;
			}
			if ((object)type.Name == _unsignedLongID)
			{
				return null;
			}
			if ((object)type.Name == _hexBinaryID)
			{
				return null;
			}
			if ((object)type.Name == _base64BinaryID)
			{
				return null;
			}
			if ((object)type.Name == _base64ID && ((object)type.Namespace == _soapNsID || (object)type.Namespace == _soap12NsID))
			{
				return null;
			}
			return null;
		}
		if ((object)type.Namespace == _schemaNonXsdTypesNsID)
		{
			if ((object)type.Name == _charID)
			{
				return null;
			}
			if ((object)type.Name == _guidID)
			{
				return null;
			}
			if ((object)type.Name == _timeSpanID)
			{
				return null;
			}
			if ((object)type.Name == _dateTimeOffsetID)
			{
				return null;
			}
			return null;
		}
		return null;
	}

	protected bool IsXmlnsAttribute(string name)
	{
		if (!name.StartsWith("xmlns", StringComparison.Ordinal))
		{
			return false;
		}
		if (name.Length == 5)
		{
			return true;
		}
		return name[5] == ':';
	}

	protected void ParseWsdlArrayType(XmlAttribute attr)
	{
		if ((object)attr.LocalName == _wsdlArrayTypeID && (object)attr.NamespaceURI == _wsdlNsID)
		{
			int num = attr.Value.LastIndexOf(':');
			if (num < 0)
			{
				attr.Value = _r.LookupNamespace("") + ":" + attr.Value;
			}
			else
			{
				attr.Value = _r.LookupNamespace(attr.Value.Substring(0, num)) + ":" + attr.Value.AsSpan(num + 1);
			}
		}
	}

	protected bool ReadNull()
	{
		if (!GetNullAttr())
		{
			return false;
		}
		if (_r.IsEmptyElement)
		{
			_r.Skip();
			return true;
		}
		_r.ReadStartElement();
		while (_r.NodeType != XmlNodeType.EndElement)
		{
			UnknownNode(null);
		}
		ReadEndElement();
		return true;
	}

	protected bool GetNullAttr()
	{
		string attribute = _r.GetAttribute(_nilID, _instanceNsID);
		if (attribute == null)
		{
			attribute = _r.GetAttribute(_nullID, _instanceNsID);
		}
		if (attribute == null)
		{
			attribute = _r.GetAttribute(_nullID, _instanceNs2000ID);
			if (attribute == null)
			{
				attribute = _r.GetAttribute(_nullID, _instanceNs1999ID);
			}
		}
		if (attribute == null || !XmlConvert.ToBoolean(attribute))
		{
			return false;
		}
		return true;
	}

	protected string? ReadNullableString()
	{
		if (ReadNull())
		{
			return null;
		}
		return _r.ReadElementString();
	}

	protected XmlQualifiedName? ReadNullableQualifiedName()
	{
		if (ReadNull())
		{
			return null;
		}
		return ReadElementQualifiedName();
	}

	protected XmlQualifiedName ReadElementQualifiedName()
	{
		if (_r.IsEmptyElement)
		{
			XmlQualifiedName result = new XmlQualifiedName(string.Empty, _r.LookupNamespace(""));
			_r.Skip();
			return result;
		}
		XmlQualifiedName result2 = ToXmlQualifiedName(CollapseWhitespace(_r.ReadString()));
		_r.ReadEndElement();
		return result2;
	}

	protected XmlDocument? ReadXmlDocument(bool wrapped)
	{
		XmlNode xmlNode = ReadXmlNode(wrapped);
		if (xmlNode == null)
		{
			return null;
		}
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.AppendChild(xmlDocument.ImportNode(xmlNode, deep: true));
		return xmlDocument;
	}

	[return: NotNullIfNotNull("value")]
	protected string? CollapseWhitespace(string? value)
	{
		return value?.Trim();
	}

	protected XmlNode? ReadXmlNode(bool wrapped)
	{
		XmlNode result = null;
		if (wrapped)
		{
			if (ReadNull())
			{
				return null;
			}
			_r.ReadStartElement();
			_r.MoveToContent();
			if (_r.NodeType != XmlNodeType.EndElement)
			{
				result = Document.ReadNode(_r);
			}
			while (_r.NodeType != XmlNodeType.EndElement)
			{
				UnknownNode(null);
			}
			_r.ReadEndElement();
		}
		else
		{
			result = Document.ReadNode(_r);
		}
		return result;
	}

	[return: NotNullIfNotNull("value")]
	protected static byte[]? ToByteArrayBase64(string? value)
	{
		return XmlCustomFormatter.ToByteArrayBase64(value);
	}

	protected byte[]? ToByteArrayBase64(bool isNull)
	{
		if (isNull)
		{
			return null;
		}
		return ReadByteArray(isBase64: true);
	}

	[return: NotNullIfNotNull("value")]
	protected static byte[]? ToByteArrayHex(string? value)
	{
		return XmlCustomFormatter.ToByteArrayHex(value);
	}

	protected byte[]? ToByteArrayHex(bool isNull)
	{
		if (isNull)
		{
			return null;
		}
		return ReadByteArray(isBase64: false);
	}

	protected int GetArrayLength(string name, string ns)
	{
		if (GetNullAttr())
		{
			return 0;
		}
		string attribute = _r.GetAttribute(_arrayTypeID, _soapNsID);
		SoapArrayInfo soapArrayInfo = ParseArrayType(attribute);
		if (soapArrayInfo.dimensions != 1)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidArrayDimentions, CurrentTag()));
		}
		XmlQualifiedName xmlQualifiedName = ToXmlQualifiedName(soapArrayInfo.qname, decodeName: false);
		if (xmlQualifiedName.Name != name)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidArrayTypeName, xmlQualifiedName.Name, name, CurrentTag()));
		}
		if (xmlQualifiedName.Namespace != ns)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidArrayTypeNamespace, xmlQualifiedName.Namespace, ns, CurrentTag()));
		}
		return soapArrayInfo.length;
	}

	private SoapArrayInfo ParseArrayType(string value)
	{
		if (value == null)
		{
			throw new ArgumentNullException(System.SR.Format(System.SR.XmlMissingArrayType, CurrentTag()));
		}
		if (value.Length == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.XmlEmptyArrayType, CurrentTag()), "value");
		}
		char[] array = value.ToCharArray();
		int num = array.Length;
		SoapArrayInfo result = default(SoapArrayInfo);
		int num2 = num - 1;
		if (array[num2] != ']')
		{
			throw new ArgumentException(System.SR.XmlInvalidArraySyntax, "value");
		}
		num2--;
		while (num2 != -1 && array[num2] != '[')
		{
			if (array[num2] == ',')
			{
				throw new ArgumentException(System.SR.Format(System.SR.XmlInvalidArrayDimentions, CurrentTag()), "value");
			}
			num2--;
		}
		if (num2 == -1)
		{
			throw new ArgumentException(System.SR.XmlMismatchedArrayBrackets, "value");
		}
		int num3 = num - num2 - 2;
		if (num3 > 0)
		{
			string text = new string(array, num2 + 1, num3);
			try
			{
				result.length = int.Parse(text, CultureInfo.InvariantCulture);
			}
			catch (Exception ex)
			{
				if (ex is OutOfMemoryException)
				{
					throw;
				}
				throw new ArgumentException(System.SR.Format(System.SR.XmlInvalidArrayLength, text), "value");
			}
		}
		else
		{
			result.length = -1;
		}
		num2--;
		result.jaggedDimensions = 0;
		while (num2 != -1 && array[num2] == ']')
		{
			num2--;
			if (num2 < 0)
			{
				throw new ArgumentException(System.SR.XmlMismatchedArrayBrackets, "value");
			}
			if (array[num2] == ',')
			{
				throw new ArgumentException(System.SR.Format(System.SR.XmlInvalidArrayDimentions, CurrentTag()), "value");
			}
			if (array[num2] != '[')
			{
				throw new ArgumentException(System.SR.XmlInvalidArraySyntax, "value");
			}
			num2--;
			result.jaggedDimensions++;
		}
		result.dimensions = 1;
		result.qname = new string(array, 0, num2 + 1);
		return result;
	}

	private SoapArrayInfo ParseSoap12ArrayType(string itemType, string arraySize)
	{
		SoapArrayInfo result = default(SoapArrayInfo);
		if (itemType != null && itemType.Length > 0)
		{
			result.qname = itemType;
		}
		else
		{
			result.qname = "";
		}
		string[] array = ((arraySize == null || arraySize.Length <= 0) ? Array.Empty<string>() : arraySize.Split((char[]?)null));
		result.dimensions = 0;
		result.length = -1;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].Length <= 0)
			{
				continue;
			}
			if (array[i] == "*")
			{
				result.dimensions++;
				continue;
			}
			try
			{
				result.length = int.Parse(array[i], CultureInfo.InvariantCulture);
				result.dimensions++;
			}
			catch (Exception ex)
			{
				if (ex is OutOfMemoryException)
				{
					throw;
				}
				throw new ArgumentException(System.SR.Format(System.SR.XmlInvalidArrayLength, array[i]), "value");
			}
		}
		if (result.dimensions == 0)
		{
			result.dimensions = 1;
		}
		return result;
	}

	protected static DateTime ToDateTime(string value)
	{
		return XmlCustomFormatter.ToDateTime(value);
	}

	protected static DateTime ToDate(string value)
	{
		return XmlCustomFormatter.ToDate(value);
	}

	protected static DateTime ToTime(string value)
	{
		return XmlCustomFormatter.ToTime(value);
	}

	protected static char ToChar(string value)
	{
		return XmlCustomFormatter.ToChar(value);
	}

	protected static long ToEnum(string value, Hashtable h, string typeName)
	{
		return XmlCustomFormatter.ToEnum(value, h, typeName, validate: true);
	}

	[return: NotNullIfNotNull("value")]
	protected static string? ToXmlName(string? value)
	{
		return XmlCustomFormatter.ToXmlName(value);
	}

	[return: NotNullIfNotNull("value")]
	protected static string? ToXmlNCName(string? value)
	{
		return XmlCustomFormatter.ToXmlNCName(value);
	}

	[return: NotNullIfNotNull("value")]
	protected static string? ToXmlNmToken(string? value)
	{
		return XmlCustomFormatter.ToXmlNmToken(value);
	}

	[return: NotNullIfNotNull("value")]
	protected static string? ToXmlNmTokens(string? value)
	{
		return XmlCustomFormatter.ToXmlNmTokens(value);
	}

	protected XmlQualifiedName ToXmlQualifiedName(string? value)
	{
		return ToXmlQualifiedName(value, DecodeName);
	}

	internal XmlQualifiedName ToXmlQualifiedName(string value, bool decodeName)
	{
		int num = value?.LastIndexOf(':') ?? (-1);
		string text = ((num < 0) ? null : value.Substring(0, num));
		string text2 = value.Substring(num + 1);
		if (decodeName)
		{
			text = XmlConvert.DecodeName(text);
			text2 = XmlConvert.DecodeName(text2);
		}
		if (text == null || text.Length == 0)
		{
			return new XmlQualifiedName(_r.NameTable.Add(value), _r.LookupNamespace(string.Empty));
		}
		string text3 = _r.LookupNamespace(text);
		if (text3 == null)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlUndefinedAlias, text));
		}
		return new XmlQualifiedName(_r.NameTable.Add(text2), text3);
	}

	protected void UnknownAttribute(object? o, XmlAttribute attr)
	{
		UnknownAttribute(o, attr, null);
	}

	protected void UnknownAttribute(object? o, XmlAttribute attr, string? qnames)
	{
		if (_events.OnUnknownAttribute != null)
		{
			GetCurrentPosition(out var lineNumber, out var linePosition);
			XmlAttributeEventArgs e = new XmlAttributeEventArgs(attr, lineNumber, linePosition, o, qnames);
			_events.OnUnknownAttribute(_events.sender, e);
		}
	}

	protected void UnknownElement(object? o, XmlElement elem)
	{
		UnknownElement(o, elem, null);
	}

	protected void UnknownElement(object? o, XmlElement elem, string? qnames)
	{
		if (_events.OnUnknownElement != null)
		{
			GetCurrentPosition(out var lineNumber, out var linePosition);
			XmlElementEventArgs e = new XmlElementEventArgs(elem, lineNumber, linePosition, o, qnames);
			_events.OnUnknownElement(_events.sender, e);
		}
	}

	protected void UnknownNode(object? o)
	{
		UnknownNode(o, null);
	}

	protected void UnknownNode(object? o, string? qnames)
	{
		if (_r.NodeType == XmlNodeType.None || _r.NodeType == XmlNodeType.Whitespace)
		{
			_r.Read();
		}
		else
		{
			if (_r.NodeType == XmlNodeType.EndElement)
			{
				return;
			}
			if (_events.OnUnknownNode != null)
			{
				UnknownNode(Document.ReadNode(_r), o, qnames);
			}
			else if (_r.NodeType != XmlNodeType.Attribute || _events.OnUnknownAttribute != null)
			{
				if (_r.NodeType == XmlNodeType.Element && _events.OnUnknownElement == null)
				{
					_r.Skip();
				}
				else
				{
					UnknownNode(Document.ReadNode(_r), o, qnames);
				}
			}
		}
	}

	private void UnknownNode(XmlNode unknownNode, object o, string qnames)
	{
		if (unknownNode != null)
		{
			if (unknownNode.NodeType != 0 && unknownNode.NodeType != XmlNodeType.Whitespace && _events.OnUnknownNode != null)
			{
				GetCurrentPosition(out var lineNumber, out var linePosition);
				XmlNodeEventArgs e = new XmlNodeEventArgs(unknownNode, lineNumber, linePosition, o);
				_events.OnUnknownNode(_events.sender, e);
			}
			if (unknownNode.NodeType == XmlNodeType.Attribute)
			{
				UnknownAttribute(o, (XmlAttribute)unknownNode, qnames);
			}
			else if (unknownNode.NodeType == XmlNodeType.Element)
			{
				UnknownElement(o, (XmlElement)unknownNode, qnames);
			}
		}
	}

	private void GetCurrentPosition(out int lineNumber, out int linePosition)
	{
		if (Reader is IXmlLineInfo)
		{
			IXmlLineInfo xmlLineInfo = (IXmlLineInfo)Reader;
			lineNumber = xmlLineInfo.LineNumber;
			linePosition = xmlLineInfo.LinePosition;
		}
		else
		{
			lineNumber = (linePosition = -1);
		}
	}

	protected void UnreferencedObject(string? id, object? o)
	{
		if (_events.OnUnreferencedObject != null)
		{
			UnreferencedObjectEventArgs e = new UnreferencedObjectEventArgs(o, id);
			_events.OnUnreferencedObject(_events.sender, e);
		}
	}

	private string CurrentTag()
	{
		return _r.NodeType switch
		{
			XmlNodeType.Element => "<" + _r.LocalName + " xmlns='" + _r.NamespaceURI + "'>", 
			XmlNodeType.EndElement => ">", 
			XmlNodeType.Text => _r.Value, 
			XmlNodeType.CDATA => "CDATA", 
			XmlNodeType.Comment => "<--", 
			XmlNodeType.ProcessingInstruction => "<?", 
			_ => "(unknown)", 
		};
	}

	protected Exception CreateUnknownTypeException(XmlQualifiedName type)
	{
		return new InvalidOperationException(System.SR.Format(System.SR.XmlUnknownType, type.Name, type.Namespace, CurrentTag()));
	}

	protected Exception CreateReadOnlyCollectionException(string name)
	{
		return new InvalidOperationException(System.SR.Format(System.SR.XmlReadOnlyCollection, name));
	}

	protected Exception CreateAbstractTypeException(string name, string? ns)
	{
		return new InvalidOperationException(System.SR.Format(System.SR.XmlAbstractType, name, ns, CurrentTag()));
	}

	protected Exception CreateInaccessibleConstructorException(string typeName)
	{
		return new InvalidOperationException(System.SR.Format(System.SR.XmlConstructorInaccessible, typeName));
	}

	protected Exception CreateCtorHasSecurityException(string typeName)
	{
		return new InvalidOperationException(System.SR.Format(System.SR.XmlConstructorHasSecurityAttributes, typeName));
	}

	protected Exception CreateUnknownNodeException()
	{
		return new InvalidOperationException(System.SR.Format(System.SR.XmlUnknownNode, CurrentTag()));
	}

	protected Exception CreateUnknownConstantException(string? value, Type enumType)
	{
		return new InvalidOperationException(System.SR.Format(System.SR.XmlUnknownConstant, value, enumType.Name));
	}

	protected Exception CreateInvalidCastException(Type type, object? value)
	{
		return CreateInvalidCastException(type, value, null);
	}

	protected Exception CreateInvalidCastException(Type type, object? value, string? id)
	{
		if (value == null)
		{
			return new InvalidCastException(System.SR.Format(System.SR.XmlInvalidNullCast, type.FullName));
		}
		if (id == null)
		{
			return new InvalidCastException(System.SR.Format(System.SR.XmlInvalidCast, value.GetType().FullName, type.FullName));
		}
		return new InvalidCastException(System.SR.Format(System.SR.XmlInvalidCastWithId, value.GetType().FullName, type.FullName, id));
	}

	protected Exception CreateBadDerivationException(string? xsdDerived, string? nsDerived, string? xsdBase, string? nsBase, string? clrDerived, string? clrBase)
	{
		return new InvalidOperationException(System.SR.Format(System.SR.XmlSerializableBadDerivation, xsdDerived, nsDerived, xsdBase, nsBase, clrDerived, clrBase));
	}

	protected Exception CreateMissingIXmlSerializableType(string? name, string? ns, string? clrType)
	{
		return new InvalidOperationException(System.SR.Format(System.SR.XmlSerializableMissingClrType, name, ns, "XmlIncludeAttribute", clrType));
	}

	protected Array EnsureArrayIndex(Array? a, int index, Type elementType)
	{
		if (a == null)
		{
			return Array.CreateInstance(elementType, 32);
		}
		if (index < a.Length)
		{
			return a;
		}
		Array array = Array.CreateInstance(elementType, a.Length * 2);
		Array.Copy(a, array, index);
		return array;
	}

	protected Array? ShrinkArray(Array? a, int length, Type elementType, bool isNullable)
	{
		if (a == null)
		{
			if (isNullable)
			{
				return null;
			}
			return Array.CreateInstance(elementType, 0);
		}
		if (a.Length == length)
		{
			return a;
		}
		Array array = Array.CreateInstance(elementType, length);
		Array.Copy(a, array, length);
		return array;
	}

	[return: NotNullIfNotNull("value")]
	protected string? ReadString(string? value)
	{
		return ReadString(value, trim: false);
	}

	[return: NotNullIfNotNull("value")]
	protected string? ReadString(string? value, bool trim)
	{
		string text = _r.ReadString();
		if (text != null && trim)
		{
			text = text.Trim();
		}
		if (value == null || value.Length == 0)
		{
			return text;
		}
		return value + text;
	}

	protected IXmlSerializable ReadSerializable(IXmlSerializable serializable)
	{
		return ReadSerializable(serializable, wrappedAny: false);
	}

	protected IXmlSerializable ReadSerializable(IXmlSerializable serializable, bool wrappedAny)
	{
		string text = null;
		string text2 = null;
		if (wrappedAny)
		{
			text = _r.LocalName;
			text2 = _r.NamespaceURI;
			_r.Read();
			_r.MoveToContent();
		}
		serializable.ReadXml(_r);
		if (wrappedAny)
		{
			while (_r.NodeType == XmlNodeType.Whitespace)
			{
				_r.Skip();
			}
			if (_r.NodeType == XmlNodeType.None)
			{
				_r.Skip();
			}
			if (_r.NodeType == XmlNodeType.EndElement && _r.LocalName == text && _r.NamespaceURI == text2)
			{
				Reader.Read();
			}
		}
		return serializable;
	}

	protected bool ReadReference([NotNullWhen(true)] out string? fixupReference)
	{
		string text = (_soap12 ? _r.GetAttribute("ref", "http://www.w3.org/2003/05/soap-encoding") : _r.GetAttribute("href"));
		if (text == null)
		{
			fixupReference = null;
			return false;
		}
		if (!_soap12)
		{
			if (!text.StartsWith('#'))
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlMissingHref, text));
			}
			fixupReference = text.Substring(1);
		}
		else
		{
			fixupReference = text;
		}
		if (_r.IsEmptyElement)
		{
			_r.Skip();
		}
		else
		{
			_r.ReadStartElement();
			ReadEndElement();
		}
		return true;
	}

	protected void AddTarget(string? id, object? o)
	{
		if (id == null)
		{
			if (_targetsWithoutIds == null)
			{
				_targetsWithoutIds = new ArrayList();
			}
			if (o != null)
			{
				_targetsWithoutIds.Add(o);
			}
		}
		else
		{
			if (_targets == null)
			{
				_targets = new Hashtable();
			}
			if (!_targets.Contains(id))
			{
				_targets.Add(id, o);
			}
		}
	}

	protected void AddFixup(Fixup? fixup)
	{
		if (_fixups == null)
		{
			_fixups = new ArrayList();
		}
		_fixups.Add(fixup);
	}

	protected void AddFixup(CollectionFixup? fixup)
	{
		if (_collectionFixups == null)
		{
			_collectionFixups = new ArrayList();
		}
		_collectionFixups.Add(fixup);
	}

	protected object GetTarget(string id)
	{
		object obj = ((_targets != null) ? _targets[id] : null);
		if (obj == null)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidHref, id));
		}
		Referenced(obj);
		return obj;
	}

	protected void Referenced(object? o)
	{
		if (o != null)
		{
			if (_referencedTargets == null)
			{
				_referencedTargets = new Hashtable();
			}
			_referencedTargets[o] = o;
		}
	}

	private void HandleUnreferencedObjects()
	{
		if (_targets != null)
		{
			foreach (DictionaryEntry target in _targets)
			{
				if (_referencedTargets == null || !_referencedTargets.Contains(target.Value))
				{
					UnreferencedObject((string)target.Key, target.Value);
				}
			}
		}
		if (_targetsWithoutIds == null)
		{
			return;
		}
		foreach (object targetsWithoutId in _targetsWithoutIds)
		{
			if (_referencedTargets == null || !_referencedTargets.Contains(targetsWithoutId))
			{
				UnreferencedObject(null, targetsWithoutId);
			}
		}
	}

	private void DoFixups()
	{
		if (_fixups == null)
		{
			return;
		}
		for (int i = 0; i < _fixups.Count; i++)
		{
			Fixup fixup = (Fixup)_fixups[i];
			fixup.Callback(fixup);
		}
		if (_collectionFixups != null)
		{
			for (int j = 0; j < _collectionFixups.Count; j++)
			{
				CollectionFixup collectionFixup = (CollectionFixup)_collectionFixups[j];
				collectionFixup.Callback(collectionFixup.Collection, collectionFixup.CollectionItems);
			}
		}
	}

	protected void FixupArrayRefs(object fixup)
	{
		Fixup fixup2 = (Fixup)fixup;
		Array array = (Array)fixup2.Source;
		for (int i = 0; i < array.Length; i++)
		{
			string text = fixup2.Ids[i];
			if (text != null)
			{
				object target = GetTarget(text);
				try
				{
					array.SetValue(target, i);
				}
				catch (InvalidCastException)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidArrayRef, text, target.GetType().FullName, i.ToString(CultureInfo.InvariantCulture)));
				}
			}
		}
	}

	[RequiresUnreferencedCode("calls GetArrayElementType")]
	private object ReadArray(string typeName, string typeNs)
	{
		Type type = null;
		SoapArrayInfo soapArrayInfo;
		if (_soap12)
		{
			string attribute = _r.GetAttribute(_itemTypeID, _soap12NsID);
			string attribute2 = _r.GetAttribute(_arraySizeID, _soap12NsID);
			Type type2 = (Type)_types[new XmlQualifiedName(typeName, typeNs)];
			if (attribute == null && attribute2 == null && (type2 == null || !type2.IsArray))
			{
				return null;
			}
			soapArrayInfo = ParseSoap12ArrayType(attribute, attribute2);
			if (type2 != null)
			{
				type = TypeScope.GetArrayElementType(type2, null);
			}
		}
		else
		{
			string attribute3 = _r.GetAttribute(_arrayTypeID, _soapNsID);
			if (attribute3 == null)
			{
				return null;
			}
			soapArrayInfo = ParseArrayType(attribute3);
		}
		if (soapArrayInfo.dimensions != 1)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidArrayDimentions, CurrentTag()));
		}
		Type type3 = null;
		XmlQualifiedName xmlQualifiedName = new XmlQualifiedName(_urTypeID, _schemaNsID);
		XmlQualifiedName xmlQualifiedName2;
		if (soapArrayInfo.qname.Length > 0)
		{
			xmlQualifiedName2 = ToXmlQualifiedName(soapArrayInfo.qname, decodeName: false);
			type3 = (Type)_types[xmlQualifiedName2];
		}
		else
		{
			xmlQualifiedName2 = xmlQualifiedName;
		}
		if (_soap12 && type3 == typeof(object))
		{
			type3 = null;
		}
		bool flag;
		if (type3 == null)
		{
			if (!_soap12)
			{
				type3 = GetPrimitiveType(xmlQualifiedName2, throwOnUnknown: true);
				flag = true;
			}
			else
			{
				if (xmlQualifiedName2 != xmlQualifiedName)
				{
					type3 = GetPrimitiveType(xmlQualifiedName2, throwOnUnknown: false);
				}
				if (type3 != null)
				{
					flag = true;
				}
				else if (type == null)
				{
					type3 = typeof(object);
					flag = false;
				}
				else
				{
					type3 = type;
					XmlQualifiedName xmlQualifiedName3 = (XmlQualifiedName)_typesReverse[type3];
					if (xmlQualifiedName3 == null)
					{
						xmlQualifiedName3 = XmlSerializationWriter.GetPrimitiveTypeNameInternal(type3);
						flag = true;
					}
					else
					{
						flag = type3.IsPrimitive;
					}
					if (xmlQualifiedName3 != null)
					{
						xmlQualifiedName2 = xmlQualifiedName3;
					}
				}
			}
		}
		else
		{
			flag = type3.IsPrimitive;
		}
		if (!_soap12 && soapArrayInfo.jaggedDimensions > 0)
		{
			for (int i = 0; i < soapArrayInfo.jaggedDimensions; i++)
			{
				type3 = type3.MakeArrayType();
			}
		}
		if (_r.IsEmptyElement)
		{
			_r.Skip();
			return Array.CreateInstance(type3, 0);
		}
		_r.ReadStartElement();
		_r.MoveToContent();
		int num = 0;
		Array array = null;
		if (type3.IsValueType)
		{
			if (!flag && !type3.IsEnum)
			{
				throw new NotSupportedException(System.SR.Format(System.SR.XmlRpcArrayOfValueTypes, type3.FullName));
			}
			while (_r.NodeType != XmlNodeType.EndElement)
			{
				array = EnsureArrayIndex(array, num, type3);
				array.SetValue(ReadReferencedElement(xmlQualifiedName2.Name, xmlQualifiedName2.Namespace), num);
				num++;
				_r.MoveToContent();
			}
			array = ShrinkArray(array, num, type3, isNullable: false);
		}
		else
		{
			string[] array2 = null;
			int num2 = 0;
			while (_r.NodeType != XmlNodeType.EndElement)
			{
				array = EnsureArrayIndex(array, num, type3);
				array2 = (string[])EnsureArrayIndex(array2, num2, typeof(string));
				string name;
				string ns;
				if (_r.NamespaceURI.Length != 0)
				{
					name = _r.LocalName;
					ns = (((object)_r.NamespaceURI != _soapNsID) ? _r.NamespaceURI : "http://www.w3.org/2001/XMLSchema");
				}
				else
				{
					name = xmlQualifiedName2.Name;
					ns = xmlQualifiedName2.Namespace;
				}
				array.SetValue(ReadReferencingElement(name, ns, out array2[num2]), num);
				num++;
				num2++;
				_r.MoveToContent();
			}
			if (_soap12 && type3 == typeof(object))
			{
				Type type4 = null;
				for (int j = 0; j < num; j++)
				{
					object value = array.GetValue(j);
					if (value != null)
					{
						Type type5 = value.GetType();
						if (type5.IsValueType)
						{
							type4 = null;
							break;
						}
						if (type4 == null || type5.IsAssignableFrom(type4))
						{
							type4 = type5;
						}
						else if (!type4.IsAssignableFrom(type5))
						{
							type4 = null;
							break;
						}
					}
				}
				if (type4 != null)
				{
					type3 = type4;
				}
			}
			array2 = (string[])ShrinkArray(array2, num2, typeof(string), isNullable: false);
			array = ShrinkArray(array, num, type3, isNullable: false);
			Fixup fixup = new Fixup(array, FixupArrayRefs, array2);
			AddFixup(fixup);
		}
		ReadEndElement();
		return array;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	protected abstract void InitCallbacks();

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	protected void ReadReferencedElements()
	{
		_r.MoveToContent();
		while (_r.NodeType != XmlNodeType.EndElement && _r.NodeType != 0)
		{
			ReadReferencingElement(null, null, elementCanBeType: true, out string _);
			_r.MoveToContent();
		}
		DoFixups();
		HandleUnreferencedObjects();
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	protected object? ReadReferencedElement()
	{
		return ReadReferencedElement(null, null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	protected object? ReadReferencedElement(string? name, string? ns)
	{
		string fixupReference;
		return ReadReferencingElement(name, ns, out fixupReference);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	protected object? ReadReferencingElement(out string? fixupReference)
	{
		return ReadReferencingElement(null, null, out fixupReference);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	protected object? ReadReferencingElement(string? name, string? ns, out string? fixupReference)
	{
		return ReadReferencingElement(name, ns, elementCanBeType: false, out fixupReference);
	}

	[MemberNotNull("_callbacks")]
	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	protected object? ReadReferencingElement(string? name, string? ns, bool elementCanBeType, out string? fixupReference)
	{
		object obj = null;
		EnsureCallbackTables();
		_r.MoveToContent();
		if (ReadReference(out fixupReference))
		{
			return null;
		}
		if (ReadNull())
		{
			return null;
		}
		string id = (_soap12 ? _r.GetAttribute("id", "http://www.w3.org/2003/05/soap-encoding") : _r.GetAttribute("id", null));
		if ((obj = ReadArray(name, ns)) == null)
		{
			XmlQualifiedName xmlQualifiedName = GetXsiType();
			if (xmlQualifiedName == null)
			{
				xmlQualifiedName = ((name != null) ? new XmlQualifiedName(_r.NameTable.Add(name), _r.NameTable.Add(ns)) : new XmlQualifiedName(_r.NameTable.Add(_r.LocalName), _r.NameTable.Add(_r.NamespaceURI)));
			}
			XmlSerializationReadCallback xmlSerializationReadCallback = (XmlSerializationReadCallback)_callbacks[xmlQualifiedName];
			obj = ((xmlSerializationReadCallback == null) ? ReadTypedPrimitive(xmlQualifiedName, elementCanBeType) : xmlSerializationReadCallback());
		}
		AddTarget(id, obj);
		return obj;
	}

	[MemberNotNull("_callbacks")]
	[RequiresUnreferencedCode("calls InitCallbacks")]
	internal void EnsureCallbackTables()
	{
		if (_callbacks == null)
		{
			_callbacks = new Hashtable();
			_types = new Hashtable();
			XmlQualifiedName xmlQualifiedName = new XmlQualifiedName(_urTypeID, _r.NameTable.Add("http://www.w3.org/2001/XMLSchema"));
			_types.Add(xmlQualifiedName, typeof(object));
			_typesReverse = new Hashtable();
			_typesReverse.Add(typeof(object), xmlQualifiedName);
			InitCallbacks();
		}
	}

	protected void AddReadCallback(string name, string ns, Type type, XmlSerializationReadCallback read)
	{
		XmlQualifiedName xmlQualifiedName = new XmlQualifiedName(_r.NameTable.Add(name), _r.NameTable.Add(ns));
		_callbacks[xmlQualifiedName] = read;
		_types[xmlQualifiedName] = type;
		_typesReverse[type] = xmlQualifiedName;
	}

	protected void ReadEndElement()
	{
		while (_r.NodeType == XmlNodeType.Whitespace)
		{
			_r.Skip();
		}
		if (_r.NodeType == XmlNodeType.None)
		{
			_r.Skip();
		}
		else
		{
			_r.ReadEndElement();
		}
	}

	private object ReadXmlNodes(bool elementCanBeType)
	{
		List<XmlNode> list = new List<XmlNode>();
		string localName = Reader.LocalName;
		string namespaceURI = Reader.NamespaceURI;
		string name = Reader.Name;
		string text = null;
		string text2 = null;
		int num = 0;
		int lineNumber = -1;
		int linePosition = -1;
		XmlNode xmlNode = null;
		if (Reader.NodeType == XmlNodeType.Attribute)
		{
			XmlAttribute xmlAttribute = Document.CreateAttribute(name, namespaceURI);
			xmlAttribute.Value = Reader.Value;
			xmlNode = xmlAttribute;
		}
		else
		{
			xmlNode = Document.CreateElement(name, namespaceURI);
		}
		GetCurrentPosition(out lineNumber, out linePosition);
		XmlElement xmlElement = xmlNode as XmlElement;
		while (Reader.MoveToNextAttribute())
		{
			if (IsXmlnsAttribute(Reader.Name) || (Reader.Name == "id" && (!_soap12 || Reader.NamespaceURI == "http://www.w3.org/2003/05/soap-encoding")))
			{
				num++;
			}
			if ((object)Reader.LocalName == _typeID && ((object)Reader.NamespaceURI == _instanceNsID || (object)Reader.NamespaceURI == _instanceNs2000ID || (object)Reader.NamespaceURI == _instanceNs1999ID))
			{
				string value = Reader.Value;
				int num2 = value.LastIndexOf(':');
				text = ((num2 >= 0) ? value.Substring(num2 + 1) : value);
				text2 = Reader.LookupNamespace((num2 >= 0) ? value.Substring(0, num2) : "");
			}
			XmlAttribute xmlAttribute2 = (XmlAttribute)Document.ReadNode(_r);
			list.Add(xmlAttribute2);
			xmlElement?.SetAttributeNode(xmlAttribute2);
		}
		if (elementCanBeType && text == null)
		{
			text = localName;
			text2 = namespaceURI;
			XmlAttribute xmlAttribute3 = Document.CreateAttribute(_typeID, _instanceNsID);
			xmlAttribute3.Value = name;
			list.Add(xmlAttribute3);
		}
		if (text == "anyType" && ((object)text2 == _schemaNsID || (object)text2 == _schemaNs1999ID || (object)text2 == _schemaNs2000ID))
		{
			num++;
		}
		Reader.MoveToElement();
		if (Reader.IsEmptyElement)
		{
			Reader.Skip();
		}
		else
		{
			Reader.ReadStartElement();
			Reader.MoveToContent();
			while (Reader.NodeType != XmlNodeType.EndElement)
			{
				XmlNode xmlNode2 = Document.ReadNode(_r);
				list.Add(xmlNode2);
				xmlElement?.AppendChild(xmlNode2);
				Reader.MoveToContent();
			}
			ReadEndElement();
		}
		if (list.Count <= num)
		{
			return new object();
		}
		XmlNode[] result = list.ToArray();
		UnknownNode(xmlNode, null, null);
		return result;
	}

	protected void CheckReaderCount(ref int whileIterations, ref int readerCount)
	{
	}
}
