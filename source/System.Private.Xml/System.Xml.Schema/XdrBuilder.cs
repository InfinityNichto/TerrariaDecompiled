using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;

namespace System.Xml.Schema;

internal sealed class XdrBuilder : SchemaBuilder
{
	private sealed class DeclBaseInfo
	{
		internal XmlQualifiedName _Name;

		internal string _Prefix;

		internal XmlQualifiedName _TypeName;

		internal string _TypePrefix;

		internal object _Default;

		internal object _Revises;

		internal uint _MaxOccurs;

		internal uint _MinOccurs;

		internal bool _Checking;

		internal SchemaElementDecl _ElementDecl;

		internal SchemaAttDef _Attdef;

		internal DeclBaseInfo _Next;

		internal DeclBaseInfo()
		{
			Reset();
		}

		[MemberNotNull("_Name")]
		[MemberNotNull("_TypeName")]
		internal void Reset()
		{
			_Name = XmlQualifiedName.Empty;
			_Prefix = null;
			_TypeName = XmlQualifiedName.Empty;
			_TypePrefix = null;
			_Default = null;
			_Revises = null;
			_MaxOccurs = 1u;
			_MinOccurs = 1u;
			_Checking = false;
			_ElementDecl = null;
			_Next = null;
			_Attdef = null;
		}
	}

	private sealed class GroupContent
	{
		internal uint _MinVal;

		internal uint _MaxVal;

		internal bool _HasMaxAttr;

		internal bool _HasMinAttr;

		internal int _Order;

		internal static void Copy(GroupContent from, GroupContent to)
		{
			to._MinVal = from._MinVal;
			to._MaxVal = from._MaxVal;
			to._Order = from._Order;
		}

		internal static GroupContent Copy(GroupContent other)
		{
			GroupContent groupContent = new GroupContent();
			Copy(other, groupContent);
			return groupContent;
		}
	}

	private sealed class ElementContent
	{
		internal SchemaElementDecl _ElementDecl;

		internal int _ContentAttr;

		internal int _OrderAttr;

		internal bool _MasterGroupRequired;

		internal bool _ExistTerminal;

		internal bool _AllowDataType;

		internal bool _HasDataType;

		internal bool _HasType;

		internal bool _EnumerationRequired;

		internal uint _MinVal;

		internal uint _MaxVal;

		internal uint _MaxLength;

		internal uint _MinLength;

		internal Hashtable _AttDefList;
	}

	private sealed class AttributeContent
	{
		internal SchemaAttDef _AttDef;

		internal XmlQualifiedName _Name;

		internal string _Prefix;

		internal bool _Required;

		internal uint _MinVal;

		internal uint _MaxVal;

		internal uint _MaxLength;

		internal uint _MinLength;

		internal bool _EnumerationRequired;

		internal bool _HasDataType;

		internal bool _Global;

		internal object _Default;
	}

	private delegate void XdrBuildFunction(XdrBuilder builder, object obj, string prefix);

	private delegate void XdrInitFunction(XdrBuilder builder, object obj);

	private delegate void XdrBeginChildFunction(XdrBuilder builder);

	private delegate void XdrEndChildFunction(XdrBuilder builder);

	private sealed class XdrAttributeEntry
	{
		internal SchemaNames.Token _Attribute;

		internal int _SchemaFlags;

		internal XmlSchemaDatatype _Datatype;

		internal XdrBuildFunction _BuildFunc;

		internal XdrAttributeEntry(SchemaNames.Token a, XmlTokenizedType ttype, XdrBuildFunction build)
		{
			_Attribute = a;
			_Datatype = XmlSchemaDatatype.FromXmlTokenizedType(ttype);
			_SchemaFlags = 0;
			_BuildFunc = build;
		}

		internal XdrAttributeEntry(SchemaNames.Token a, XmlTokenizedType ttype, int schemaFlags, XdrBuildFunction build)
		{
			_Attribute = a;
			_Datatype = XmlSchemaDatatype.FromXmlTokenizedType(ttype);
			_SchemaFlags = schemaFlags;
			_BuildFunc = build;
		}
	}

	private sealed class XdrEntry
	{
		internal SchemaNames.Token _Name;

		internal int[] _NextStates;

		internal XdrAttributeEntry[] _Attributes;

		internal XdrInitFunction _InitFunc;

		internal XdrBeginChildFunction _BeginChildFunc;

		internal XdrEndChildFunction _EndChildFunc;

		internal bool _AllowText;

		internal XdrEntry(SchemaNames.Token n, int[] states, XdrAttributeEntry[] attributes, XdrInitFunction init, XdrBeginChildFunction begin, XdrEndChildFunction end, bool fText)
		{
			_Name = n;
			_NextStates = states;
			_Attributes = attributes;
			_InitFunc = init;
			_BeginChildFunc = begin;
			_EndChildFunc = end;
			_AllowText = fText;
		}
	}

	private static readonly int[] s_XDR_Root_Element = new int[1] { 1 };

	private static readonly int[] s_XDR_Root_SubElements = new int[2] { 2, 3 };

	private static readonly int[] s_XDR_ElementType_SubElements = new int[5] { 4, 6, 3, 5, 7 };

	private static readonly int[] s_XDR_AttributeType_SubElements = new int[1] { 8 };

	private static readonly int[] s_XDR_Group_SubElements = new int[2] { 4, 6 };

	private static readonly XdrAttributeEntry[] s_XDR_Root_Attributes = new XdrAttributeEntry[2]
	{
		new XdrAttributeEntry(SchemaNames.Token.SchemaName, XmlTokenizedType.CDATA, XDR_BuildRoot_Name),
		new XdrAttributeEntry(SchemaNames.Token.SchemaId, XmlTokenizedType.QName, XDR_BuildRoot_ID)
	};

	private static readonly XdrAttributeEntry[] s_XDR_ElementType_Attributes = new XdrAttributeEntry[8]
	{
		new XdrAttributeEntry(SchemaNames.Token.SchemaName, XmlTokenizedType.QName, 256, XDR_BuildElementType_Name),
		new XdrAttributeEntry(SchemaNames.Token.SchemaContent, XmlTokenizedType.QName, XDR_BuildElementType_Content),
		new XdrAttributeEntry(SchemaNames.Token.SchemaModel, XmlTokenizedType.QName, XDR_BuildElementType_Model),
		new XdrAttributeEntry(SchemaNames.Token.SchemaOrder, XmlTokenizedType.QName, XDR_BuildElementType_Order),
		new XdrAttributeEntry(SchemaNames.Token.SchemaDtType, XmlTokenizedType.CDATA, XDR_BuildElementType_DtType),
		new XdrAttributeEntry(SchemaNames.Token.SchemaDtValues, XmlTokenizedType.NMTOKENS, XDR_BuildElementType_DtValues),
		new XdrAttributeEntry(SchemaNames.Token.SchemaDtMaxLength, XmlTokenizedType.CDATA, XDR_BuildElementType_DtMaxLength),
		new XdrAttributeEntry(SchemaNames.Token.SchemaDtMinLength, XmlTokenizedType.CDATA, XDR_BuildElementType_DtMinLength)
	};

	private static readonly XdrAttributeEntry[] s_XDR_AttributeType_Attributes = new XdrAttributeEntry[7]
	{
		new XdrAttributeEntry(SchemaNames.Token.SchemaName, XmlTokenizedType.QName, XDR_BuildAttributeType_Name),
		new XdrAttributeEntry(SchemaNames.Token.SchemaRequired, XmlTokenizedType.QName, XDR_BuildAttributeType_Required),
		new XdrAttributeEntry(SchemaNames.Token.SchemaDefault, XmlTokenizedType.CDATA, XDR_BuildAttributeType_Default),
		new XdrAttributeEntry(SchemaNames.Token.SchemaDtType, XmlTokenizedType.QName, XDR_BuildAttributeType_DtType),
		new XdrAttributeEntry(SchemaNames.Token.SchemaDtValues, XmlTokenizedType.NMTOKENS, XDR_BuildAttributeType_DtValues),
		new XdrAttributeEntry(SchemaNames.Token.SchemaDtMaxLength, XmlTokenizedType.CDATA, XDR_BuildAttributeType_DtMaxLength),
		new XdrAttributeEntry(SchemaNames.Token.SchemaDtMinLength, XmlTokenizedType.CDATA, XDR_BuildAttributeType_DtMinLength)
	};

	private static readonly XdrAttributeEntry[] s_XDR_Element_Attributes = new XdrAttributeEntry[3]
	{
		new XdrAttributeEntry(SchemaNames.Token.SchemaType, XmlTokenizedType.QName, 256, XDR_BuildElement_Type),
		new XdrAttributeEntry(SchemaNames.Token.SchemaMinOccurs, XmlTokenizedType.CDATA, XDR_BuildElement_MinOccurs),
		new XdrAttributeEntry(SchemaNames.Token.SchemaMaxOccurs, XmlTokenizedType.CDATA, XDR_BuildElement_MaxOccurs)
	};

	private static readonly XdrAttributeEntry[] s_XDR_Attribute_Attributes = new XdrAttributeEntry[3]
	{
		new XdrAttributeEntry(SchemaNames.Token.SchemaType, XmlTokenizedType.QName, XDR_BuildAttribute_Type),
		new XdrAttributeEntry(SchemaNames.Token.SchemaRequired, XmlTokenizedType.QName, XDR_BuildAttribute_Required),
		new XdrAttributeEntry(SchemaNames.Token.SchemaDefault, XmlTokenizedType.CDATA, XDR_BuildAttribute_Default)
	};

	private static readonly XdrAttributeEntry[] s_XDR_Group_Attributes = new XdrAttributeEntry[3]
	{
		new XdrAttributeEntry(SchemaNames.Token.SchemaOrder, XmlTokenizedType.QName, XDR_BuildGroup_Order),
		new XdrAttributeEntry(SchemaNames.Token.SchemaMinOccurs, XmlTokenizedType.CDATA, XDR_BuildGroup_MinOccurs),
		new XdrAttributeEntry(SchemaNames.Token.SchemaMaxOccurs, XmlTokenizedType.CDATA, XDR_BuildGroup_MaxOccurs)
	};

	private static readonly XdrAttributeEntry[] s_XDR_ElementDataType_Attributes = new XdrAttributeEntry[4]
	{
		new XdrAttributeEntry(SchemaNames.Token.SchemaDtType, XmlTokenizedType.CDATA, XDR_BuildElementType_DtType),
		new XdrAttributeEntry(SchemaNames.Token.SchemaDtValues, XmlTokenizedType.NMTOKENS, XDR_BuildElementType_DtValues),
		new XdrAttributeEntry(SchemaNames.Token.SchemaDtMaxLength, XmlTokenizedType.CDATA, XDR_BuildElementType_DtMaxLength),
		new XdrAttributeEntry(SchemaNames.Token.SchemaDtMinLength, XmlTokenizedType.CDATA, XDR_BuildElementType_DtMinLength)
	};

	private static readonly XdrAttributeEntry[] s_XDR_AttributeDataType_Attributes = new XdrAttributeEntry[4]
	{
		new XdrAttributeEntry(SchemaNames.Token.SchemaDtType, XmlTokenizedType.QName, XDR_BuildAttributeType_DtType),
		new XdrAttributeEntry(SchemaNames.Token.SchemaDtValues, XmlTokenizedType.NMTOKENS, XDR_BuildAttributeType_DtValues),
		new XdrAttributeEntry(SchemaNames.Token.SchemaDtMaxLength, XmlTokenizedType.CDATA, XDR_BuildAttributeType_DtMaxLength),
		new XdrAttributeEntry(SchemaNames.Token.SchemaDtMinLength, XmlTokenizedType.CDATA, XDR_BuildAttributeType_DtMinLength)
	};

	private static readonly XdrEntry[] s_schemaEntries = new XdrEntry[9]
	{
		new XdrEntry(SchemaNames.Token.Empty, s_XDR_Root_Element, null, null, null, null, fText: false),
		new XdrEntry(SchemaNames.Token.XdrRoot, s_XDR_Root_SubElements, s_XDR_Root_Attributes, XDR_InitRoot, XDR_BeginRoot, XDR_EndRoot, fText: false),
		new XdrEntry(SchemaNames.Token.XdrElementType, s_XDR_ElementType_SubElements, s_XDR_ElementType_Attributes, XDR_InitElementType, XDR_BeginElementType, XDR_EndElementType, fText: false),
		new XdrEntry(SchemaNames.Token.XdrAttributeType, s_XDR_AttributeType_SubElements, s_XDR_AttributeType_Attributes, XDR_InitAttributeType, XDR_BeginAttributeType, XDR_EndAttributeType, fText: false),
		new XdrEntry(SchemaNames.Token.XdrElement, null, s_XDR_Element_Attributes, XDR_InitElement, null, XDR_EndElement, fText: false),
		new XdrEntry(SchemaNames.Token.XdrAttribute, null, s_XDR_Attribute_Attributes, XDR_InitAttribute, XDR_BeginAttribute, XDR_EndAttribute, fText: false),
		new XdrEntry(SchemaNames.Token.XdrGroup, s_XDR_Group_SubElements, s_XDR_Group_Attributes, XDR_InitGroup, null, XDR_EndGroup, fText: false),
		new XdrEntry(SchemaNames.Token.XdrDatatype, null, s_XDR_ElementDataType_Attributes, XDR_InitElementDtType, null, XDR_EndElementDtType, fText: true),
		new XdrEntry(SchemaNames.Token.XdrDatatype, null, s_XDR_AttributeDataType_Attributes, XDR_InitAttributeDtType, null, XDR_EndAttributeDtType, fText: true)
	};

	private readonly SchemaInfo _SchemaInfo;

	private string _TargetNamespace;

	private readonly XmlReader _reader;

	private readonly PositionInfo _positionInfo;

	private ParticleContentValidator _contentValidator;

	private XdrEntry _CurState;

	private XdrEntry _NextState;

	private readonly HWStack _StateHistory;

	private readonly HWStack _GroupStack;

	private string _XdrName;

	private string _XdrPrefix;

	private readonly ElementContent _ElementDef;

	private GroupContent _GroupDef;

	private readonly AttributeContent _AttributeDef;

	private DeclBaseInfo _UndefinedAttributeTypes;

	private DeclBaseInfo _BaseDecl;

	private readonly XmlNameTable _NameTable;

	private readonly SchemaNames _SchemaNames;

	private readonly XmlNamespaceManager _CurNsMgr;

	private string _Text;

	private readonly ValidationEventHandler _validationEventHandler;

	private readonly Hashtable _UndeclaredElements = new Hashtable();

	private XmlResolver _xmlResolver;

	internal XmlResolver XmlResolver
	{
		set
		{
			_xmlResolver = value;
		}
	}

	internal XdrBuilder(XmlReader reader, XmlNamespaceManager curmgr, SchemaInfo sinfo, string targetNamspace, XmlNameTable nameTable, SchemaNames schemaNames, ValidationEventHandler eventhandler)
	{
		_SchemaInfo = sinfo;
		_TargetNamespace = targetNamspace;
		_reader = reader;
		_CurNsMgr = curmgr;
		_validationEventHandler = eventhandler;
		_StateHistory = new HWStack(10);
		_ElementDef = new ElementContent();
		_AttributeDef = new AttributeContent();
		_GroupStack = new HWStack(10);
		_GroupDef = new GroupContent();
		_NameTable = nameTable;
		_SchemaNames = schemaNames;
		_CurState = s_schemaEntries[0];
		_positionInfo = PositionInfo.GetPositionInfo(_reader);
		_xmlResolver = null;
	}

	internal override bool ProcessElement(string prefix, string name, string ns)
	{
		XmlQualifiedName xmlQualifiedName = new XmlQualifiedName(name, XmlSchemaDatatype.XdrCanonizeUri(ns, _NameTable, _SchemaNames));
		if (GetNextState(xmlQualifiedName))
		{
			Push();
			if (_CurState._InitFunc != null)
			{
				_CurState._InitFunc(this, xmlQualifiedName);
			}
			return true;
		}
		if (!IsSkipableElement(xmlQualifiedName))
		{
			SendValidationEvent(System.SR.Sch_UnsupportedElement, XmlQualifiedName.ToString(name, prefix));
		}
		return false;
	}

	internal override void ProcessAttribute(string prefix, string name, string ns, string value)
	{
		XmlQualifiedName xmlQualifiedName = new XmlQualifiedName(name, XmlSchemaDatatype.XdrCanonizeUri(ns, _NameTable, _SchemaNames));
		for (int i = 0; i < _CurState._Attributes.Length; i++)
		{
			XdrAttributeEntry xdrAttributeEntry = _CurState._Attributes[i];
			if (!_SchemaNames.TokenToQName[(int)xdrAttributeEntry._Attribute].Equals(xmlQualifiedName))
			{
				continue;
			}
			XdrBuildFunction buildFunc = xdrAttributeEntry._BuildFunc;
			if (xdrAttributeEntry._Datatype.TokenizedType == XmlTokenizedType.QName)
			{
				string prefix2;
				XmlQualifiedName xmlQualifiedName2 = XmlQualifiedName.Parse(value, _CurNsMgr, out prefix2);
				xmlQualifiedName2.Atomize(_NameTable);
				if (prefix2.Length == 0)
				{
					xmlQualifiedName2 = ((!IsGlobal(xdrAttributeEntry._SchemaFlags)) ? new XmlQualifiedName(xmlQualifiedName2.Name) : new XmlQualifiedName(xmlQualifiedName2.Name, _TargetNamespace));
				}
				else if (xdrAttributeEntry._Attribute != SchemaNames.Token.SchemaType)
				{
					throw new XmlException(System.SR.Xml_UnexpectedToken, "NAME");
				}
				buildFunc(this, xmlQualifiedName2, prefix2);
			}
			else
			{
				buildFunc(this, xdrAttributeEntry._Datatype.ParseValue(value, _NameTable, _CurNsMgr), string.Empty);
			}
			return;
		}
		if ((object)ns == _SchemaNames.NsXmlNs && IsXdrSchema(value))
		{
			LoadSchema(value);
		}
		else if (!IsSkipableAttribute(xmlQualifiedName))
		{
			SendValidationEvent(System.SR.Sch_UnsupportedAttribute, XmlQualifiedName.ToString(xmlQualifiedName.Name, prefix));
		}
	}

	private bool LoadSchema(string uri)
	{
		if (_xmlResolver == null)
		{
			return false;
		}
		uri = _NameTable.Add(uri);
		if (_SchemaInfo.TargetNamespaces.ContainsKey(uri))
		{
			return false;
		}
		SchemaInfo schemaInfo = null;
		Uri baseUri = _xmlResolver.ResolveUri(null, _reader.BaseURI);
		XmlReader xmlReader = null;
		try
		{
			Uri uri2 = _xmlResolver.ResolveUri(baseUri, uri.Substring("x-schema:".Length));
			Stream input = (Stream)_xmlResolver.GetEntity(uri2, null, null);
			xmlReader = new XmlTextReader(uri2.ToString(), input, _NameTable);
			schemaInfo = new SchemaInfo();
			Parser parser = new Parser(SchemaType.XDR, _NameTable, _SchemaNames, _validationEventHandler);
			parser.XmlResolver = _xmlResolver;
			parser.Parse(xmlReader, uri);
			schemaInfo = parser.XdrSchema;
		}
		catch (XmlException ex)
		{
			SendValidationEvent(System.SR.Sch_CannotLoadSchema, new string[2] { uri, ex.Message }, XmlSeverityType.Warning);
			schemaInfo = null;
		}
		finally
		{
			xmlReader?.Close();
		}
		if (schemaInfo != null && schemaInfo.ErrorCount == 0)
		{
			_SchemaInfo.Add(schemaInfo, _validationEventHandler);
			return true;
		}
		return false;
	}

	internal static bool IsXdrSchema(string uri)
	{
		if (uri.Length >= "x-schema:".Length && string.Compare(uri, 0, "x-schema:", 0, "x-schema:".Length, StringComparison.Ordinal) == 0)
		{
			return !uri.StartsWith("x-schema:#", StringComparison.Ordinal);
		}
		return false;
	}

	internal override bool IsContentParsed()
	{
		return true;
	}

	internal override void ProcessMarkup(XmlNode[] markup)
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	internal override void ProcessCData(string value)
	{
		if (_CurState._AllowText)
		{
			_Text = value;
		}
		else
		{
			SendValidationEvent(System.SR.Sch_TextNotAllowed, value);
		}
	}

	internal override void StartChildren()
	{
		if (_CurState._BeginChildFunc != null)
		{
			_CurState._BeginChildFunc(this);
		}
	}

	internal override void EndChildren()
	{
		if (_CurState._EndChildFunc != null)
		{
			_CurState._EndChildFunc(this);
		}
		Pop();
	}

	private void Push()
	{
		_StateHistory.Push();
		_StateHistory[_StateHistory.Length - 1] = _CurState;
		_CurState = _NextState;
	}

	private void Pop()
	{
		_CurState = (XdrEntry)_StateHistory.Pop();
	}

	private void PushGroupInfo()
	{
		_GroupStack.Push();
		_GroupStack[_GroupStack.Length - 1] = GroupContent.Copy(_GroupDef);
	}

	private void PopGroupInfo()
	{
		_GroupDef = (GroupContent)_GroupStack.Pop();
	}

	private static void XDR_InitRoot(XdrBuilder builder, object obj)
	{
		builder._SchemaInfo.SchemaType = SchemaType.XDR;
		builder._ElementDef._ElementDecl = null;
		builder._ElementDef._AttDefList = null;
		builder._AttributeDef._AttDef = null;
	}

	private static void XDR_BuildRoot_Name(XdrBuilder builder, object obj, string prefix)
	{
		builder._XdrName = (string)obj;
		builder._XdrPrefix = prefix;
	}

	private static void XDR_BuildRoot_ID(XdrBuilder builder, object obj, string prefix)
	{
	}

	private static void XDR_BeginRoot(XdrBuilder builder)
	{
		if (builder._TargetNamespace == null)
		{
			if (builder._XdrName != null)
			{
				builder._TargetNamespace = builder._NameTable.Add("x-schema:#" + builder._XdrName);
			}
			else
			{
				builder._TargetNamespace = string.Empty;
			}
		}
		builder._SchemaInfo.TargetNamespaces.Add(builder._TargetNamespace, value: true);
	}

	private static void XDR_EndRoot(XdrBuilder builder)
	{
		while (builder._UndefinedAttributeTypes != null)
		{
			XmlQualifiedName xmlQualifiedName = builder._UndefinedAttributeTypes._TypeName;
			if (xmlQualifiedName.Namespace.Length == 0)
			{
				xmlQualifiedName = new XmlQualifiedName(xmlQualifiedName.Name, builder._TargetNamespace);
			}
			if (builder._SchemaInfo.AttributeDecls.TryGetValue(xmlQualifiedName, out var value))
			{
				builder._UndefinedAttributeTypes._Attdef = value.Clone();
				builder._UndefinedAttributeTypes._Attdef.Name = xmlQualifiedName;
				builder.XDR_CheckAttributeDefault(builder._UndefinedAttributeTypes, builder._UndefinedAttributeTypes._Attdef);
			}
			else
			{
				builder.SendValidationEvent(System.SR.Sch_UndeclaredAttribute, xmlQualifiedName.Name);
			}
			builder._UndefinedAttributeTypes = builder._UndefinedAttributeTypes._Next;
		}
		foreach (SchemaElementDecl value2 in builder._UndeclaredElements.Values)
		{
			builder.SendValidationEvent(System.SR.Sch_UndeclaredElement, XmlQualifiedName.ToString(value2.Name.Name, value2.Prefix));
		}
	}

	private static void XDR_InitElementType(XdrBuilder builder, object obj)
	{
		builder._ElementDef._ElementDecl = new SchemaElementDecl();
		builder._contentValidator = new ParticleContentValidator(XmlSchemaContentType.Mixed);
		builder._contentValidator.IsOpen = true;
		builder._ElementDef._ContentAttr = 0;
		builder._ElementDef._OrderAttr = 0;
		builder._ElementDef._MasterGroupRequired = false;
		builder._ElementDef._ExistTerminal = false;
		builder._ElementDef._AllowDataType = true;
		builder._ElementDef._HasDataType = false;
		builder._ElementDef._EnumerationRequired = false;
		builder._ElementDef._AttDefList = new Hashtable();
		builder._ElementDef._MaxLength = uint.MaxValue;
		builder._ElementDef._MinLength = uint.MaxValue;
	}

	private static void XDR_BuildElementType_Name(XdrBuilder builder, object obj, string prefix)
	{
		XmlQualifiedName xmlQualifiedName = (XmlQualifiedName)obj;
		if (builder._SchemaInfo.ElementDecls.ContainsKey(xmlQualifiedName))
		{
			builder.SendValidationEvent(System.SR.Sch_DupElementDecl, XmlQualifiedName.ToString(xmlQualifiedName.Name, prefix));
		}
		builder._ElementDef._ElementDecl.Name = xmlQualifiedName;
		builder._ElementDef._ElementDecl.Prefix = prefix;
		builder._SchemaInfo.ElementDecls.Add(xmlQualifiedName, builder._ElementDef._ElementDecl);
		if (builder._UndeclaredElements[xmlQualifiedName] != null)
		{
			builder._UndeclaredElements.Remove(xmlQualifiedName);
		}
	}

	private static void XDR_BuildElementType_Content(XdrBuilder builder, object obj, string prefix)
	{
		builder._ElementDef._ContentAttr = builder.GetContent((XmlQualifiedName)obj);
	}

	private static void XDR_BuildElementType_Model(XdrBuilder builder, object obj, string prefix)
	{
		builder._contentValidator.IsOpen = builder.GetModel((XmlQualifiedName)obj);
	}

	private static void XDR_BuildElementType_Order(XdrBuilder builder, object obj, string prefix)
	{
		builder._ElementDef._OrderAttr = (builder._GroupDef._Order = builder.GetOrder((XmlQualifiedName)obj));
	}

	private static void XDR_BuildElementType_DtType(XdrBuilder builder, object obj, string prefix)
	{
		builder._ElementDef._HasDataType = true;
		string text = ((string)obj).Trim();
		if (text.Length == 0)
		{
			builder.SendValidationEvent(System.SR.Sch_MissDtvalue);
			return;
		}
		XmlSchemaDatatype xmlSchemaDatatype = XmlSchemaDatatype.FromXdrName(text);
		if (xmlSchemaDatatype == null)
		{
			builder.SendValidationEvent(System.SR.Sch_UnknownDtType, text);
		}
		builder._ElementDef._ElementDecl.Datatype = xmlSchemaDatatype;
	}

	private static void XDR_BuildElementType_DtValues(XdrBuilder builder, object obj, string prefix)
	{
		builder._ElementDef._EnumerationRequired = true;
		builder._ElementDef._ElementDecl.Values = new List<string>((string[])obj);
	}

	private static void XDR_BuildElementType_DtMaxLength(XdrBuilder builder, object obj, string prefix)
	{
		ParseDtMaxLength(ref builder._ElementDef._MaxLength, obj, builder);
	}

	private static void XDR_BuildElementType_DtMinLength(XdrBuilder builder, object obj, string prefix)
	{
		ParseDtMinLength(ref builder._ElementDef._MinLength, obj, builder);
	}

	private static void XDR_BeginElementType(XdrBuilder builder)
	{
		string text = null;
		string msg = null;
		if (builder._ElementDef._ElementDecl.Name.IsEmpty)
		{
			text = System.SR.Sch_MissAttribute;
			msg = "name";
		}
		else
		{
			if (builder._ElementDef._HasDataType)
			{
				if (!builder._ElementDef._AllowDataType)
				{
					text = System.SR.Sch_DataTypeTextOnly;
					goto IL_01f4;
				}
				builder._ElementDef._ContentAttr = 2;
			}
			else if (builder._ElementDef._ContentAttr == 0)
			{
				switch (builder._ElementDef._OrderAttr)
				{
				case 0:
					builder._ElementDef._ContentAttr = 3;
					builder._ElementDef._OrderAttr = 1;
					break;
				case 2:
					builder._ElementDef._ContentAttr = 4;
					break;
				case 3:
					builder._ElementDef._ContentAttr = 4;
					break;
				case 1:
					builder._ElementDef._ContentAttr = 3;
					break;
				}
			}
			bool isOpen = builder._contentValidator.IsOpen;
			ElementContent elementDef = builder._ElementDef;
			switch (builder._ElementDef._ContentAttr)
			{
			case 2:
				builder._ElementDef._ElementDecl.ContentValidator = ContentValidator.TextOnly;
				builder._GroupDef._Order = 1;
				builder._contentValidator = null;
				goto default;
			case 4:
				builder._contentValidator = new ParticleContentValidator(XmlSchemaContentType.ElementOnly);
				if (elementDef._OrderAttr == 0)
				{
					builder._GroupDef._Order = 2;
				}
				elementDef._MasterGroupRequired = true;
				builder._contentValidator.IsOpen = isOpen;
				goto default;
			case 1:
				builder._ElementDef._ElementDecl.ContentValidator = ContentValidator.Empty;
				builder._contentValidator = null;
				goto default;
			case 3:
				if (elementDef._OrderAttr == 0 || elementDef._OrderAttr == 1)
				{
					builder._GroupDef._Order = 1;
					elementDef._MasterGroupRequired = true;
					builder._contentValidator.IsOpen = isOpen;
					goto default;
				}
				text = System.SR.Sch_MixedMany;
				break;
			default:
				if (elementDef._ContentAttr == 3 || elementDef._ContentAttr == 4)
				{
					builder._contentValidator.Start();
					builder._contentValidator.OpenGroup();
				}
				break;
			}
		}
		goto IL_01f4;
		IL_01f4:
		if (text != null)
		{
			builder.SendValidationEvent(text, msg);
		}
	}

	private static void XDR_EndElementType(XdrBuilder builder)
	{
		SchemaElementDecl elementDecl = builder._ElementDef._ElementDecl;
		if (builder._UndefinedAttributeTypes != null && builder._ElementDef._AttDefList != null)
		{
			DeclBaseInfo declBaseInfo = builder._UndefinedAttributeTypes;
			DeclBaseInfo declBaseInfo2 = declBaseInfo;
			while (declBaseInfo != null)
			{
				SchemaAttDef schemaAttDef = null;
				if (declBaseInfo._ElementDecl == elementDecl)
				{
					XmlQualifiedName typeName = declBaseInfo._TypeName;
					schemaAttDef = (SchemaAttDef)builder._ElementDef._AttDefList[typeName];
					if (schemaAttDef != null)
					{
						declBaseInfo._Attdef = schemaAttDef.Clone();
						declBaseInfo._Attdef.Name = typeName;
						builder.XDR_CheckAttributeDefault(declBaseInfo, schemaAttDef);
						if (declBaseInfo == builder._UndefinedAttributeTypes)
						{
							declBaseInfo = (builder._UndefinedAttributeTypes = declBaseInfo._Next);
							declBaseInfo2 = declBaseInfo;
						}
						else
						{
							declBaseInfo2._Next = declBaseInfo._Next;
							declBaseInfo = declBaseInfo2._Next;
						}
					}
				}
				if (schemaAttDef == null)
				{
					if (declBaseInfo != builder._UndefinedAttributeTypes)
					{
						declBaseInfo2 = declBaseInfo2._Next;
					}
					declBaseInfo = declBaseInfo._Next;
				}
			}
		}
		if (builder._ElementDef._MasterGroupRequired)
		{
			builder._contentValidator.CloseGroup();
			if (!builder._ElementDef._ExistTerminal)
			{
				if (builder._contentValidator.IsOpen)
				{
					builder._ElementDef._ElementDecl.ContentValidator = ContentValidator.Any;
					builder._contentValidator = null;
				}
				else if (builder._ElementDef._ContentAttr != 3)
				{
					builder.SendValidationEvent(System.SR.Sch_ElementMissing);
				}
			}
			else if (builder._GroupDef._Order == 1)
			{
				builder._contentValidator.AddStar();
			}
		}
		if (elementDecl.Datatype != null)
		{
			XmlTokenizedType tokenizedType = elementDecl.Datatype.TokenizedType;
			if (tokenizedType == XmlTokenizedType.ENUMERATION && !builder._ElementDef._EnumerationRequired)
			{
				builder.SendValidationEvent(System.SR.Sch_MissDtvaluesAttribute);
			}
			if (tokenizedType != XmlTokenizedType.ENUMERATION && builder._ElementDef._EnumerationRequired)
			{
				builder.SendValidationEvent(System.SR.Sch_RequireEnumeration);
			}
		}
		CompareMinMaxLength(builder._ElementDef._MinLength, builder._ElementDef._MaxLength, builder);
		elementDecl.MaxLength = builder._ElementDef._MaxLength;
		elementDecl.MinLength = builder._ElementDef._MinLength;
		if (builder._contentValidator != null)
		{
			builder._ElementDef._ElementDecl.ContentValidator = builder._contentValidator.Finish(useDFA: true);
			builder._contentValidator = null;
		}
		builder._ElementDef._ElementDecl = null;
		builder._ElementDef._AttDefList = null;
	}

	private static void XDR_InitAttributeType(XdrBuilder builder, object obj)
	{
		AttributeContent attributeDef = builder._AttributeDef;
		attributeDef._AttDef = new SchemaAttDef(XmlQualifiedName.Empty, null);
		attributeDef._Required = false;
		attributeDef._Prefix = null;
		attributeDef._Default = null;
		attributeDef._MinVal = 0u;
		attributeDef._MaxVal = 1u;
		attributeDef._EnumerationRequired = false;
		attributeDef._HasDataType = false;
		attributeDef._Global = builder._StateHistory.Length == 2;
		attributeDef._MaxLength = uint.MaxValue;
		attributeDef._MinLength = uint.MaxValue;
	}

	private static void XDR_BuildAttributeType_Name(XdrBuilder builder, object obj, string prefix)
	{
		XmlQualifiedName xmlQualifiedName = (XmlQualifiedName)obj;
		builder._AttributeDef._Name = xmlQualifiedName;
		builder._AttributeDef._Prefix = prefix;
		builder._AttributeDef._AttDef.Name = xmlQualifiedName;
		if (builder._ElementDef._ElementDecl != null)
		{
			if (builder._ElementDef._AttDefList[xmlQualifiedName] == null)
			{
				builder._ElementDef._AttDefList.Add(xmlQualifiedName, builder._AttributeDef._AttDef);
			}
			else
			{
				builder.SendValidationEvent(System.SR.Sch_DupAttribute, XmlQualifiedName.ToString(xmlQualifiedName.Name, prefix));
			}
			return;
		}
		xmlQualifiedName = new XmlQualifiedName(xmlQualifiedName.Name, builder._TargetNamespace);
		builder._AttributeDef._AttDef.Name = xmlQualifiedName;
		if (!builder._SchemaInfo.AttributeDecls.ContainsKey(xmlQualifiedName))
		{
			builder._SchemaInfo.AttributeDecls.Add(xmlQualifiedName, builder._AttributeDef._AttDef);
		}
		else
		{
			builder.SendValidationEvent(System.SR.Sch_DupAttribute, XmlQualifiedName.ToString(xmlQualifiedName.Name, prefix));
		}
	}

	private static void XDR_BuildAttributeType_Required(XdrBuilder builder, object obj, string prefix)
	{
		builder._AttributeDef._Required = IsYes(obj, builder);
	}

	private static void XDR_BuildAttributeType_Default(XdrBuilder builder, object obj, string prefix)
	{
		builder._AttributeDef._Default = obj;
	}

	private static void XDR_BuildAttributeType_DtType(XdrBuilder builder, object obj, string prefix)
	{
		XmlQualifiedName xmlQualifiedName = (XmlQualifiedName)obj;
		builder._AttributeDef._HasDataType = true;
		builder._AttributeDef._AttDef.Datatype = builder.CheckDatatype(xmlQualifiedName.Name);
	}

	private static void XDR_BuildAttributeType_DtValues(XdrBuilder builder, object obj, string prefix)
	{
		builder._AttributeDef._EnumerationRequired = true;
		builder._AttributeDef._AttDef.Values = new List<string>((string[])obj);
	}

	private static void XDR_BuildAttributeType_DtMaxLength(XdrBuilder builder, object obj, string prefix)
	{
		ParseDtMaxLength(ref builder._AttributeDef._MaxLength, obj, builder);
	}

	private static void XDR_BuildAttributeType_DtMinLength(XdrBuilder builder, object obj, string prefix)
	{
		ParseDtMinLength(ref builder._AttributeDef._MinLength, obj, builder);
	}

	private static void XDR_BeginAttributeType(XdrBuilder builder)
	{
		if (builder._AttributeDef._Name.IsEmpty)
		{
			builder.SendValidationEvent(System.SR.Sch_MissAttribute);
		}
	}

	private static void XDR_EndAttributeType(XdrBuilder builder)
	{
		string text = null;
		if (builder._AttributeDef._HasDataType && builder._AttributeDef._AttDef.Datatype != null)
		{
			XmlTokenizedType tokenizedType = builder._AttributeDef._AttDef.Datatype.TokenizedType;
			if (tokenizedType == XmlTokenizedType.ENUMERATION && !builder._AttributeDef._EnumerationRequired)
			{
				text = System.SR.Sch_MissDtvaluesAttribute;
			}
			else if (tokenizedType != XmlTokenizedType.ENUMERATION && builder._AttributeDef._EnumerationRequired)
			{
				text = System.SR.Sch_RequireEnumeration;
			}
			else
			{
				if (builder._AttributeDef._Default == null || tokenizedType != XmlTokenizedType.ID)
				{
					goto IL_00a3;
				}
				text = System.SR.Sch_DefaultIdValue;
			}
			goto IL_0164;
		}
		builder._AttributeDef._AttDef.Datatype = XmlSchemaDatatype.FromXmlTokenizedType(XmlTokenizedType.CDATA);
		goto IL_00a3;
		IL_00a3:
		CompareMinMaxLength(builder._AttributeDef._MinLength, builder._AttributeDef._MaxLength, builder);
		builder._AttributeDef._AttDef.MaxLength = builder._AttributeDef._MaxLength;
		builder._AttributeDef._AttDef.MinLength = builder._AttributeDef._MinLength;
		if (builder._AttributeDef._Default != null)
		{
			SchemaAttDef attDef = builder._AttributeDef._AttDef;
			string defaultValueRaw = (builder._AttributeDef._AttDef.DefaultValueExpanded = (string)builder._AttributeDef._Default);
			attDef.DefaultValueRaw = defaultValueRaw;
			builder.CheckDefaultAttValue(builder._AttributeDef._AttDef);
		}
		builder.SetAttributePresence(builder._AttributeDef._AttDef, builder._AttributeDef._Required);
		goto IL_0164;
		IL_0164:
		if (text != null)
		{
			builder.SendValidationEvent(text);
		}
	}

	private static void XDR_InitElement(XdrBuilder builder, object obj)
	{
		if (builder._ElementDef._HasDataType || builder._ElementDef._ContentAttr == 1 || builder._ElementDef._ContentAttr == 2)
		{
			builder.SendValidationEvent(System.SR.Sch_ElementNotAllowed);
		}
		builder._ElementDef._AllowDataType = false;
		builder._ElementDef._HasType = false;
		builder._ElementDef._MinVal = 1u;
		builder._ElementDef._MaxVal = 1u;
	}

	private static void XDR_BuildElement_Type(XdrBuilder builder, object obj, string prefix)
	{
		XmlQualifiedName xmlQualifiedName = (XmlQualifiedName)obj;
		if (!builder._SchemaInfo.ElementDecls.ContainsKey(xmlQualifiedName))
		{
			SchemaElementDecl schemaElementDecl = (SchemaElementDecl)builder._UndeclaredElements[xmlQualifiedName];
			if (schemaElementDecl == null)
			{
				schemaElementDecl = new SchemaElementDecl(xmlQualifiedName, prefix);
				builder._UndeclaredElements.Add(xmlQualifiedName, schemaElementDecl);
			}
		}
		builder._ElementDef._HasType = true;
		if (builder._ElementDef._ExistTerminal)
		{
			builder.AddOrder();
		}
		else
		{
			builder._ElementDef._ExistTerminal = true;
		}
		builder._contentValidator.AddName(xmlQualifiedName, null);
	}

	private static void XDR_BuildElement_MinOccurs(XdrBuilder builder, object obj, string prefix)
	{
		builder._ElementDef._MinVal = ParseMinOccurs(obj, builder);
	}

	private static void XDR_BuildElement_MaxOccurs(XdrBuilder builder, object obj, string prefix)
	{
		builder._ElementDef._MaxVal = ParseMaxOccurs(obj, builder);
	}

	private static void XDR_EndElement(XdrBuilder builder)
	{
		if (builder._ElementDef._HasType)
		{
			HandleMinMax(builder._contentValidator, builder._ElementDef._MinVal, builder._ElementDef._MaxVal);
		}
		else
		{
			builder.SendValidationEvent(System.SR.Sch_MissAttribute);
		}
	}

	private static void XDR_InitAttribute(XdrBuilder builder, object obj)
	{
		if (builder._BaseDecl == null)
		{
			builder._BaseDecl = new DeclBaseInfo();
		}
		builder._BaseDecl._MinOccurs = 0u;
	}

	private static void XDR_BuildAttribute_Type(XdrBuilder builder, object obj, string prefix)
	{
		builder._BaseDecl._TypeName = (XmlQualifiedName)obj;
		builder._BaseDecl._Prefix = prefix;
	}

	private static void XDR_BuildAttribute_Required(XdrBuilder builder, object obj, string prefix)
	{
		if (IsYes(obj, builder))
		{
			builder._BaseDecl._MinOccurs = 1u;
		}
	}

	private static void XDR_BuildAttribute_Default(XdrBuilder builder, object obj, string prefix)
	{
		builder._BaseDecl._Default = obj;
	}

	private static void XDR_BeginAttribute(XdrBuilder builder)
	{
		if (builder._BaseDecl._TypeName.IsEmpty)
		{
			builder.SendValidationEvent(System.SR.Sch_MissAttribute);
		}
		SchemaAttDef schemaAttDef = null;
		XmlQualifiedName typeName = builder._BaseDecl._TypeName;
		string prefix = builder._BaseDecl._Prefix;
		if (builder._ElementDef._AttDefList != null)
		{
			schemaAttDef = (SchemaAttDef)builder._ElementDef._AttDefList[typeName];
		}
		if (schemaAttDef == null)
		{
			XmlQualifiedName key = typeName;
			if (prefix.Length == 0)
			{
				key = new XmlQualifiedName(typeName.Name, builder._TargetNamespace);
			}
			if (builder._SchemaInfo.AttributeDecls.TryGetValue(key, out var value))
			{
				schemaAttDef = value.Clone();
				schemaAttDef.Name = typeName;
			}
			else if (prefix.Length != 0)
			{
				builder.SendValidationEvent(System.SR.Sch_UndeclaredAttribute, XmlQualifiedName.ToString(typeName.Name, prefix));
			}
		}
		if (schemaAttDef != null)
		{
			builder.XDR_CheckAttributeDefault(builder._BaseDecl, schemaAttDef);
		}
		else
		{
			schemaAttDef = new SchemaAttDef(typeName, prefix);
			DeclBaseInfo declBaseInfo = new DeclBaseInfo();
			declBaseInfo._Checking = true;
			declBaseInfo._Attdef = schemaAttDef;
			declBaseInfo._TypeName = builder._BaseDecl._TypeName;
			declBaseInfo._ElementDecl = builder._ElementDef._ElementDecl;
			declBaseInfo._MinOccurs = builder._BaseDecl._MinOccurs;
			declBaseInfo._Default = builder._BaseDecl._Default;
			declBaseInfo._Next = builder._UndefinedAttributeTypes;
			builder._UndefinedAttributeTypes = declBaseInfo;
		}
		builder._ElementDef._ElementDecl.AddAttDef(schemaAttDef);
	}

	private static void XDR_EndAttribute(XdrBuilder builder)
	{
		builder._BaseDecl.Reset();
	}

	private static void XDR_InitGroup(XdrBuilder builder, object obj)
	{
		if (builder._ElementDef._ContentAttr == 1 || builder._ElementDef._ContentAttr == 2)
		{
			builder.SendValidationEvent(System.SR.Sch_GroupDisabled);
		}
		builder.PushGroupInfo();
		builder._GroupDef._MinVal = 1u;
		builder._GroupDef._MaxVal = 1u;
		builder._GroupDef._HasMaxAttr = false;
		builder._GroupDef._HasMinAttr = false;
		if (builder._ElementDef._ExistTerminal)
		{
			builder.AddOrder();
		}
		builder._ElementDef._ExistTerminal = false;
		builder._contentValidator.OpenGroup();
	}

	private static void XDR_BuildGroup_Order(XdrBuilder builder, object obj, string prefix)
	{
		builder._GroupDef._Order = builder.GetOrder((XmlQualifiedName)obj);
		if (builder._ElementDef._ContentAttr == 3 && builder._GroupDef._Order != 1)
		{
			builder.SendValidationEvent(System.SR.Sch_MixedMany);
		}
	}

	private static void XDR_BuildGroup_MinOccurs(XdrBuilder builder, object obj, string prefix)
	{
		builder._GroupDef._MinVal = ParseMinOccurs(obj, builder);
		builder._GroupDef._HasMinAttr = true;
	}

	private static void XDR_BuildGroup_MaxOccurs(XdrBuilder builder, object obj, string prefix)
	{
		builder._GroupDef._MaxVal = ParseMaxOccurs(obj, builder);
		builder._GroupDef._HasMaxAttr = true;
	}

	private static void XDR_EndGroup(XdrBuilder builder)
	{
		if (!builder._ElementDef._ExistTerminal)
		{
			builder.SendValidationEvent(System.SR.Sch_ElementMissing);
		}
		builder._contentValidator.CloseGroup();
		if (builder._GroupDef._Order == 1)
		{
			builder._contentValidator.AddStar();
		}
		if (1 == builder._GroupDef._Order && builder._GroupDef._HasMaxAttr && builder._GroupDef._MaxVal != uint.MaxValue)
		{
			builder.SendValidationEvent(System.SR.Sch_ManyMaxOccurs);
		}
		HandleMinMax(builder._contentValidator, builder._GroupDef._MinVal, builder._GroupDef._MaxVal);
		builder.PopGroupInfo();
	}

	private static void XDR_InitElementDtType(XdrBuilder builder, object obj)
	{
		if (builder._ElementDef._HasDataType)
		{
			builder.SendValidationEvent(System.SR.Sch_DupDtType);
		}
		if (!builder._ElementDef._AllowDataType)
		{
			builder.SendValidationEvent(System.SR.Sch_DataTypeTextOnly);
		}
	}

	private static void XDR_EndElementDtType(XdrBuilder builder)
	{
		if (!builder._ElementDef._HasDataType)
		{
			builder.SendValidationEvent(System.SR.Sch_MissAttribute);
		}
		builder._ElementDef._ElementDecl.ContentValidator = ContentValidator.TextOnly;
		builder._ElementDef._ContentAttr = 2;
		builder._ElementDef._MasterGroupRequired = false;
		builder._contentValidator = null;
	}

	private static void XDR_InitAttributeDtType(XdrBuilder builder, object obj)
	{
		if (builder._AttributeDef._HasDataType)
		{
			builder.SendValidationEvent(System.SR.Sch_DupDtType);
		}
	}

	private static void XDR_EndAttributeDtType(XdrBuilder builder)
	{
		string text = null;
		if (!builder._AttributeDef._HasDataType)
		{
			text = System.SR.Sch_MissAttribute;
		}
		else if (builder._AttributeDef._AttDef.Datatype != null)
		{
			XmlTokenizedType tokenizedType = builder._AttributeDef._AttDef.Datatype.TokenizedType;
			if (tokenizedType == XmlTokenizedType.ENUMERATION && !builder._AttributeDef._EnumerationRequired)
			{
				text = System.SR.Sch_MissDtvaluesAttribute;
			}
			else if (tokenizedType != XmlTokenizedType.ENUMERATION && builder._AttributeDef._EnumerationRequired)
			{
				text = System.SR.Sch_RequireEnumeration;
			}
		}
		if (text != null)
		{
			builder.SendValidationEvent(text);
		}
	}

	private bool GetNextState(XmlQualifiedName qname)
	{
		if (_CurState._NextStates != null)
		{
			for (int i = 0; i < _CurState._NextStates.Length; i++)
			{
				if (_SchemaNames.TokenToQName[(int)s_schemaEntries[_CurState._NextStates[i]]._Name].Equals(qname))
				{
					_NextState = s_schemaEntries[_CurState._NextStates[i]];
					return true;
				}
			}
		}
		return false;
	}

	private bool IsSkipableElement(XmlQualifiedName qname)
	{
		string @namespace = qname.Namespace;
		if (@namespace != null && !Ref.Equal(@namespace, _SchemaNames.NsXdr))
		{
			return true;
		}
		if (_SchemaNames.TokenToQName[38].Equals(qname) || _SchemaNames.TokenToQName[39].Equals(qname))
		{
			return true;
		}
		return false;
	}

	private bool IsSkipableAttribute(XmlQualifiedName qname)
	{
		string @namespace = qname.Namespace;
		if (@namespace.Length != 0 && !Ref.Equal(@namespace, _SchemaNames.NsXdr) && !Ref.Equal(@namespace, _SchemaNames.NsDataType))
		{
			return true;
		}
		if (Ref.Equal(@namespace, _SchemaNames.NsDataType) && _CurState._Name == SchemaNames.Token.XdrDatatype && (_SchemaNames.QnDtMax.Equals(qname) || _SchemaNames.QnDtMin.Equals(qname) || _SchemaNames.QnDtMaxExclusive.Equals(qname) || _SchemaNames.QnDtMinExclusive.Equals(qname)))
		{
			return true;
		}
		return false;
	}

	private int GetOrder(XmlQualifiedName qname)
	{
		int result = 0;
		if (_SchemaNames.TokenToQName[15].Equals(qname))
		{
			result = 2;
		}
		else if (_SchemaNames.TokenToQName[16].Equals(qname))
		{
			result = 3;
		}
		else if (_SchemaNames.TokenToQName[17].Equals(qname))
		{
			result = 1;
		}
		else
		{
			SendValidationEvent(System.SR.Sch_UnknownOrder, qname.Name);
		}
		return result;
	}

	private void AddOrder()
	{
		switch (_GroupDef._Order)
		{
		case 2:
			_contentValidator.AddSequence();
			break;
		case 1:
		case 3:
			_contentValidator.AddChoice();
			break;
		default:
			throw new XmlException(System.SR.Xml_UnexpectedToken, "NAME");
		}
	}

	private static bool IsYes(object obj, XdrBuilder builder)
	{
		XmlQualifiedName xmlQualifiedName = (XmlQualifiedName)obj;
		bool result = false;
		if (xmlQualifiedName.Name == "yes")
		{
			result = true;
		}
		else if (xmlQualifiedName.Name != "no")
		{
			builder.SendValidationEvent(System.SR.Sch_UnknownRequired);
		}
		return result;
	}

	private static uint ParseMinOccurs(object obj, XdrBuilder builder)
	{
		uint n = 1u;
		if (!ParseInteger((string)obj, ref n) || (n != 0 && n != 1))
		{
			builder.SendValidationEvent(System.SR.Sch_MinOccursInvalid);
		}
		return n;
	}

	private static uint ParseMaxOccurs(object obj, XdrBuilder builder)
	{
		uint n = uint.MaxValue;
		string text = (string)obj;
		if (!text.Equals("*") && (!ParseInteger(text, ref n) || (n != uint.MaxValue && n != 1)))
		{
			builder.SendValidationEvent(System.SR.Sch_MaxOccursInvalid);
		}
		return n;
	}

	private static void HandleMinMax(ParticleContentValidator pContent, uint cMin, uint cMax)
	{
		if (pContent == null)
		{
			return;
		}
		if (cMax == uint.MaxValue)
		{
			if (cMin == 0)
			{
				pContent.AddStar();
			}
			else
			{
				pContent.AddPlus();
			}
		}
		else if (cMin == 0)
		{
			pContent.AddQMark();
		}
	}

	private static void ParseDtMaxLength(ref uint cVal, object obj, XdrBuilder builder)
	{
		if (-1 != (int)cVal)
		{
			builder.SendValidationEvent(System.SR.Sch_DupDtMaxLength);
		}
		if (!ParseInteger((string)obj, ref cVal) || cVal < 0)
		{
			builder.SendValidationEvent(System.SR.Sch_DtMaxLengthInvalid, obj.ToString());
		}
	}

	private static void ParseDtMinLength(ref uint cVal, object obj, XdrBuilder builder)
	{
		if (-1 != (int)cVal)
		{
			builder.SendValidationEvent(System.SR.Sch_DupDtMinLength);
		}
		if (!ParseInteger((string)obj, ref cVal) || cVal < 0)
		{
			builder.SendValidationEvent(System.SR.Sch_DtMinLengthInvalid, obj.ToString());
		}
	}

	private static void CompareMinMaxLength(uint cMin, uint cMax, XdrBuilder builder)
	{
		if (cMin != uint.MaxValue && cMax != uint.MaxValue && cMin > cMax)
		{
			builder.SendValidationEvent(System.SR.Sch_DtMinMaxLength);
		}
	}

	private static bool ParseInteger(string str, ref uint n)
	{
		return uint.TryParse(str, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, NumberFormatInfo.InvariantInfo, out n);
	}

	private void XDR_CheckAttributeDefault(DeclBaseInfo decl, SchemaAttDef pAttdef)
	{
		if ((decl._Default != null || pAttdef.DefaultValueTyped != null) && decl._Default != null)
		{
			string defaultValueRaw = (pAttdef.DefaultValueExpanded = (string)decl._Default);
			pAttdef.DefaultValueRaw = defaultValueRaw;
			CheckDefaultAttValue(pAttdef);
		}
		SetAttributePresence(pAttdef, 1 == decl._MinOccurs);
	}

	private void SetAttributePresence(SchemaAttDef pAttdef, bool fRequired)
	{
		if (SchemaDeclBase.Use.Fixed == pAttdef.Presence)
		{
			return;
		}
		if (fRequired || SchemaDeclBase.Use.Required == pAttdef.Presence)
		{
			if (pAttdef.DefaultValueTyped != null)
			{
				pAttdef.Presence = SchemaDeclBase.Use.Fixed;
			}
			else
			{
				pAttdef.Presence = SchemaDeclBase.Use.Required;
			}
		}
		else if (pAttdef.DefaultValueTyped != null)
		{
			pAttdef.Presence = SchemaDeclBase.Use.Default;
		}
		else
		{
			pAttdef.Presence = SchemaDeclBase.Use.Implied;
		}
	}

	private int GetContent(XmlQualifiedName qname)
	{
		int result = 0;
		if (_SchemaNames.TokenToQName[11].Equals(qname))
		{
			result = 1;
			_ElementDef._AllowDataType = false;
		}
		else if (_SchemaNames.TokenToQName[12].Equals(qname))
		{
			result = 4;
			_ElementDef._AllowDataType = false;
		}
		else if (_SchemaNames.TokenToQName[10].Equals(qname))
		{
			result = 3;
			_ElementDef._AllowDataType = false;
		}
		else if (_SchemaNames.TokenToQName[13].Equals(qname))
		{
			result = 2;
		}
		else
		{
			SendValidationEvent(System.SR.Sch_UnknownContent, qname.Name);
		}
		return result;
	}

	private bool GetModel(XmlQualifiedName qname)
	{
		bool result = false;
		if (_SchemaNames.TokenToQName[7].Equals(qname))
		{
			result = true;
		}
		else if (_SchemaNames.TokenToQName[8].Equals(qname))
		{
			result = false;
		}
		else
		{
			SendValidationEvent(System.SR.Sch_UnknownModel, qname.Name);
		}
		return result;
	}

	private XmlSchemaDatatype CheckDatatype(string str)
	{
		XmlSchemaDatatype xmlSchemaDatatype = XmlSchemaDatatype.FromXdrName(str);
		if (xmlSchemaDatatype == null)
		{
			SendValidationEvent(System.SR.Sch_UnknownDtType, str);
		}
		else if (xmlSchemaDatatype.TokenizedType == XmlTokenizedType.ID && !_AttributeDef._Global)
		{
			if (_ElementDef._ElementDecl.IsIdDeclared)
			{
				SendValidationEvent(System.SR.Sch_IdAttrDeclared, XmlQualifiedName.ToString(_ElementDef._ElementDecl.Name.Name, _ElementDef._ElementDecl.Prefix));
			}
			_ElementDef._ElementDecl.IsIdDeclared = true;
		}
		return xmlSchemaDatatype;
	}

	private void CheckDefaultAttValue(SchemaAttDef attDef)
	{
		string value = attDef.DefaultValueRaw.Trim();
		XdrValidator.CheckDefaultValue(value, attDef, _SchemaInfo, _CurNsMgr, _NameTable, null, _validationEventHandler, _reader.BaseURI, _positionInfo.LineNumber, _positionInfo.LinePosition);
	}

	private bool IsGlobal(int flags)
	{
		return flags == 256;
	}

	private void SendValidationEvent(string code, string[] args, XmlSeverityType severity)
	{
		SendValidationEvent(new XmlSchemaException(code, args, _reader.BaseURI, _positionInfo.LineNumber, _positionInfo.LinePosition), severity);
	}

	private void SendValidationEvent(string code)
	{
		SendValidationEvent(code, string.Empty);
	}

	private void SendValidationEvent(string code, string msg)
	{
		SendValidationEvent(new XmlSchemaException(code, msg, _reader.BaseURI, _positionInfo.LineNumber, _positionInfo.LinePosition), XmlSeverityType.Error);
	}

	private void SendValidationEvent(XmlSchemaException e, XmlSeverityType severity)
	{
		_SchemaInfo.ErrorCount++;
		if (_validationEventHandler != null)
		{
			_validationEventHandler(this, new ValidationEventArgs(e, severity));
		}
		else if (severity == XmlSeverityType.Error)
		{
			throw e;
		}
	}
}
