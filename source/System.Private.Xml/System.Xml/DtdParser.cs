using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace System.Xml;

internal sealed class DtdParser : IDtdParser
{
	private enum Token
	{
		CDATA,
		ID,
		IDREF,
		IDREFS,
		ENTITY,
		ENTITIES,
		NMTOKEN,
		NMTOKENS,
		NOTATION,
		None,
		PERef,
		AttlistDecl,
		ElementDecl,
		EntityDecl,
		NotationDecl,
		Comment,
		PI,
		CondSectionStart,
		CondSectionEnd,
		Eof,
		REQUIRED,
		IMPLIED,
		FIXED,
		QName,
		Name,
		Nmtoken,
		Quote,
		LeftParen,
		RightParen,
		GreaterThan,
		Or,
		LeftBracket,
		RightBracket,
		PUBLIC,
		SYSTEM,
		Literal,
		DOCTYPE,
		NData,
		Percent,
		Star,
		QMark,
		Plus,
		PCDATA,
		Comma,
		ANY,
		EMPTY,
		IGNORE,
		INCLUDE
	}

	private enum ScanningFunction
	{
		SubsetContent,
		Name,
		QName,
		Nmtoken,
		Doctype1,
		Doctype2,
		Element1,
		Element2,
		Element3,
		Element4,
		Element5,
		Element6,
		Element7,
		Attlist1,
		Attlist2,
		Attlist3,
		Attlist4,
		Attlist5,
		Attlist6,
		Attlist7,
		Entity1,
		Entity2,
		Entity3,
		Notation1,
		CondSection1,
		CondSection2,
		CondSection3,
		Literal,
		SystemId,
		PublicId1,
		PublicId2,
		ClosingTag,
		ParamEntitySpace,
		None
	}

	private enum LiteralType
	{
		AttributeValue,
		EntityReplText,
		SystemOrPublicID
	}

	private sealed class UndeclaredNotation
	{
		internal string name;

		internal int lineNo;

		internal int linePos;

		internal UndeclaredNotation next;

		internal UndeclaredNotation(string name, int lineNo, int linePos)
		{
			this.name = name;
			this.lineNo = lineNo;
			this.linePos = linePos;
			next = null;
		}
	}

	private sealed class ParseElementOnlyContent_LocalFrame
	{
		public int startParenEntityId;

		public Token parsingSchema;

		public ParseElementOnlyContent_LocalFrame(int startParentEntityIdParam)
		{
			startParenEntityId = startParentEntityIdParam;
			parsingSchema = Token.None;
		}
	}

	private IDtdParserAdapter _readerAdapter;

	private IDtdParserAdapterWithValidation _readerAdapterWithValidation;

	private XmlNameTable _nameTable;

	private SchemaInfo _schemaInfo;

	private string _systemId = string.Empty;

	private string _publicId = string.Empty;

	private bool _normalize = true;

	private bool _validate;

	private bool _supportNamespaces = true;

	private bool _v1Compat;

	private char[] _chars;

	private int _charsUsed;

	private int _curPos;

	private ScanningFunction _scanningFunction;

	private ScanningFunction _nextScaningFunction;

	private ScanningFunction _savedScanningFunction;

	private bool _whitespaceSeen;

	private int _tokenStartPos;

	private int _colonPos;

	private StringBuilder _internalSubsetValueSb;

	private int _externalEntitiesDepth;

	private int _currentEntityId;

	private bool _freeFloatingDtd;

	private bool _hasFreeFloatingInternalSubset;

	private StringBuilder _stringBuilder;

	private int _condSectionDepth;

	private LineInfo _literalLineInfo = new LineInfo(0, 0);

	private char _literalQuoteChar = '"';

	private string _documentBaseUri = string.Empty;

	private string _externalDtdBaseUri = string.Empty;

	private Dictionary<string, UndeclaredNotation> _undeclaredNotations;

	private int[] _condSectionEntityIds;

	private bool ParsingInternalSubset => _externalEntitiesDepth == 0;

	private bool IgnoreEntityReferences => _scanningFunction == ScanningFunction.CondSection3;

	private bool SaveInternalSubsetValue
	{
		get
		{
			if (_readerAdapter.EntityStackLength == 0)
			{
				return _internalSubsetValueSb != null;
			}
			return false;
		}
	}

	private bool ParsingTopLevelMarkup
	{
		get
		{
			if (_scanningFunction != 0)
			{
				if (_scanningFunction == ScanningFunction.ParamEntitySpace)
				{
					return _savedScanningFunction == ScanningFunction.SubsetContent;
				}
				return false;
			}
			return true;
		}
	}

	private bool SupportNamespaces => _supportNamespaces;

	private bool Normalize => _normalize;

	private int LineNo => _readerAdapter.LineNo;

	private int LinePos => _curPos - _readerAdapter.LineStartPosition;

	private string BaseUriStr
	{
		get
		{
			Uri baseUri = _readerAdapter.BaseUri;
			if (!(baseUri != null))
			{
				return string.Empty;
			}
			return baseUri.ToString();
		}
	}

	private DtdParser()
	{
	}

	internal static IDtdParser Create()
	{
		return new DtdParser();
	}

	private void Initialize(IDtdParserAdapter readerAdapter)
	{
		_readerAdapter = readerAdapter;
		_readerAdapterWithValidation = readerAdapter as IDtdParserAdapterWithValidation;
		_nameTable = readerAdapter.NameTable;
		if (readerAdapter is IDtdParserAdapterWithValidation dtdParserAdapterWithValidation)
		{
			_validate = dtdParserAdapterWithValidation.DtdValidation;
		}
		if (readerAdapter is IDtdParserAdapterV1 dtdParserAdapterV)
		{
			_v1Compat = dtdParserAdapterV.V1CompatibilityMode;
			_normalize = dtdParserAdapterV.Normalization;
			_supportNamespaces = dtdParserAdapterV.Namespaces;
		}
		_schemaInfo = new SchemaInfo();
		_schemaInfo.SchemaType = SchemaType.DTD;
		_stringBuilder = new StringBuilder();
		Uri baseUri = readerAdapter.BaseUri;
		if (baseUri != null)
		{
			_documentBaseUri = baseUri.ToString();
		}
		_freeFloatingDtd = false;
	}

	private void InitializeFreeFloatingDtd(string baseUri, string docTypeName, string publicId, string systemId, string internalSubset, IDtdParserAdapter adapter)
	{
		Initialize(adapter);
		if (docTypeName == null || docTypeName.Length == 0)
		{
			throw XmlConvert.CreateInvalidNameArgumentException(docTypeName, "docTypeName");
		}
		XmlConvert.VerifyName(docTypeName);
		int num = docTypeName.IndexOf(':');
		if (num == -1)
		{
			_schemaInfo.DocTypeName = new XmlQualifiedName(_nameTable.Add(docTypeName));
		}
		else
		{
			_schemaInfo.DocTypeName = new XmlQualifiedName(_nameTable.Add(docTypeName.Substring(0, num)), _nameTable.Add(docTypeName.Substring(num + 1)));
		}
		if (systemId != null && systemId.Length > 0)
		{
			int invCharPos;
			if ((invCharPos = XmlCharType.IsOnlyCharData(systemId)) >= 0)
			{
				ThrowInvalidChar(_curPos, systemId, invCharPos);
			}
			_systemId = systemId;
		}
		if (publicId != null && publicId.Length > 0)
		{
			int invCharPos;
			if ((invCharPos = XmlCharType.IsPublicId(publicId)) >= 0)
			{
				ThrowInvalidChar(_curPos, publicId, invCharPos);
			}
			_publicId = publicId;
		}
		if (internalSubset != null && internalSubset.Length > 0)
		{
			_readerAdapter.PushInternalDtd(baseUri, internalSubset);
			_hasFreeFloatingInternalSubset = true;
		}
		Uri baseUri2 = _readerAdapter.BaseUri;
		if (baseUri2 != null)
		{
			_documentBaseUri = baseUri2.ToString();
		}
		_freeFloatingDtd = true;
	}

	IDtdInfo IDtdParser.ParseInternalDtd(IDtdParserAdapter adapter, bool saveInternalSubset)
	{
		Initialize(adapter);
		Parse(saveInternalSubset);
		return _schemaInfo;
	}

	IDtdInfo IDtdParser.ParseFreeFloatingDtd(string baseUri, string docTypeName, string publicId, string systemId, string internalSubset, IDtdParserAdapter adapter)
	{
		InitializeFreeFloatingDtd(baseUri, docTypeName, publicId, systemId, internalSubset, adapter);
		Parse(saveInternalSubset: false);
		return _schemaInfo;
	}

	private void Parse(bool saveInternalSubset)
	{
		if (_freeFloatingDtd)
		{
			ParseFreeFloatingDtd();
		}
		else
		{
			ParseInDocumentDtd(saveInternalSubset);
		}
		_schemaInfo.Finish();
		if (!_validate || _undeclaredNotations == null)
		{
			return;
		}
		foreach (UndeclaredNotation value in _undeclaredNotations.Values)
		{
			for (UndeclaredNotation undeclaredNotation = value; undeclaredNotation != null; undeclaredNotation = undeclaredNotation.next)
			{
				SendValidationEvent(XmlSeverityType.Error, new XmlSchemaException(System.SR.Sch_UndeclaredNotation, value.name, BaseUriStr, value.lineNo, value.linePos));
			}
		}
	}

	private void ParseInDocumentDtd(bool saveInternalSubset)
	{
		LoadParsingBuffer();
		_scanningFunction = ScanningFunction.QName;
		_nextScaningFunction = ScanningFunction.Doctype1;
		if (GetToken(needWhiteSpace: false) != Token.QName)
		{
			OnUnexpectedError();
		}
		_schemaInfo.DocTypeName = GetNameQualified(canHavePrefix: true);
		Token token = GetToken(needWhiteSpace: false);
		if (token == Token.SYSTEM || token == Token.PUBLIC)
		{
			ParseExternalId(token, Token.DOCTYPE, out _publicId, out _systemId);
			token = GetToken(needWhiteSpace: false);
		}
		switch (token)
		{
		case Token.LeftBracket:
			if (saveInternalSubset)
			{
				SaveParsingBuffer();
				_internalSubsetValueSb = new StringBuilder();
			}
			ParseInternalSubset();
			break;
		default:
			OnUnexpectedError();
			break;
		case Token.GreaterThan:
			break;
		}
		SaveParsingBuffer();
		if (_systemId != null && _systemId.Length > 0)
		{
			ParseExternalSubset();
		}
	}

	private void ParseFreeFloatingDtd()
	{
		if (_hasFreeFloatingInternalSubset)
		{
			LoadParsingBuffer();
			ParseInternalSubset();
			SaveParsingBuffer();
		}
		if (_systemId != null && _systemId.Length > 0)
		{
			ParseExternalSubset();
		}
	}

	private void ParseInternalSubset()
	{
		ParseSubset();
	}

	private void ParseExternalSubset()
	{
		if (_readerAdapter.PushExternalSubset(_systemId, _publicId))
		{
			Uri baseUri = _readerAdapter.BaseUri;
			if (baseUri != null)
			{
				_externalDtdBaseUri = baseUri.ToString();
			}
			_externalEntitiesDepth++;
			LoadParsingBuffer();
			ParseSubset();
		}
	}

	private void ParseSubset()
	{
		while (true)
		{
			Token token = GetToken(needWhiteSpace: false);
			int currentEntityId = _currentEntityId;
			switch (token)
			{
			case Token.AttlistDecl:
				ParseAttlistDecl();
				break;
			case Token.ElementDecl:
				ParseElementDecl();
				break;
			case Token.EntityDecl:
				ParseEntityDecl();
				break;
			case Token.NotationDecl:
				ParseNotationDecl();
				break;
			case Token.Comment:
				ParseComment();
				break;
			case Token.PI:
				ParsePI();
				break;
			case Token.CondSectionStart:
				if (ParsingInternalSubset)
				{
					Throw(_curPos - 3, System.SR.Xml_InvalidConditionalSection);
				}
				ParseCondSection();
				currentEntityId = _currentEntityId;
				break;
			case Token.CondSectionEnd:
				if (_condSectionDepth > 0)
				{
					_condSectionDepth--;
					if (_validate && _currentEntityId != _condSectionEntityIds[_condSectionDepth])
					{
						SendValidationEvent(_curPos, XmlSeverityType.Error, System.SR.Sch_ParEntityRefNesting, string.Empty);
					}
				}
				else
				{
					Throw(_curPos - 3, System.SR.Xml_UnexpectedCDataEnd);
				}
				break;
			case Token.RightBracket:
				if (ParsingInternalSubset)
				{
					if (_condSectionDepth != 0)
					{
						Throw(_curPos, System.SR.Xml_UnclosedConditionalSection);
					}
					if (_internalSubsetValueSb != null)
					{
						SaveParsingBuffer(_curPos - 1);
						_schemaInfo.InternalDtdSubset = _internalSubsetValueSb.ToString();
						_internalSubsetValueSb = null;
					}
					if (GetToken(needWhiteSpace: false) != Token.GreaterThan)
					{
						ThrowUnexpectedToken(_curPos, ">");
					}
				}
				else
				{
					Throw(_curPos, System.SR.Xml_ExpectDtdMarkup);
				}
				return;
			case Token.Eof:
				if (ParsingInternalSubset && !_freeFloatingDtd)
				{
					Throw(_curPos, System.SR.Xml_IncompleteDtdContent);
				}
				if (_condSectionDepth != 0)
				{
					Throw(_curPos, System.SR.Xml_UnclosedConditionalSection);
				}
				return;
			}
			if (_currentEntityId != currentEntityId)
			{
				if (_validate)
				{
					SendValidationEvent(_curPos, XmlSeverityType.Error, System.SR.Sch_ParEntityRefNesting, string.Empty);
				}
				else if (!_v1Compat)
				{
					Throw(_curPos, System.SR.Sch_ParEntityRefNesting);
				}
			}
		}
	}

	private void ParseAttlistDecl()
	{
		if (GetToken(needWhiteSpace: true) == Token.QName)
		{
			XmlQualifiedName nameQualified = GetNameQualified(canHavePrefix: true);
			if (!_schemaInfo.ElementDecls.TryGetValue(nameQualified, out var value) && !_schemaInfo.UndeclaredElementDecls.TryGetValue(nameQualified, out value))
			{
				value = new SchemaElementDecl(nameQualified, nameQualified.Namespace);
				_schemaInfo.UndeclaredElementDecls.Add(nameQualified, value);
			}
			SchemaAttDef schemaAttDef = null;
			while (true)
			{
				switch (GetToken(needWhiteSpace: false))
				{
				case Token.QName:
				{
					XmlQualifiedName nameQualified2 = GetNameQualified(canHavePrefix: true);
					schemaAttDef = new SchemaAttDef(nameQualified2, nameQualified2.Namespace);
					schemaAttDef.IsDeclaredInExternal = !ParsingInternalSubset;
					schemaAttDef.LineNumber = LineNo;
					schemaAttDef.LinePosition = LinePos - (_curPos - _tokenStartPos);
					bool flag = value.GetAttDef(schemaAttDef.Name) != null;
					ParseAttlistType(schemaAttDef, value, flag);
					ParseAttlistDefault(schemaAttDef, flag);
					if (schemaAttDef.Prefix.Length > 0 && schemaAttDef.Prefix.Equals("xml"))
					{
						if (schemaAttDef.Name.Name == "space")
						{
							if (_v1Compat)
							{
								string text = schemaAttDef.DefaultValueExpanded.Trim();
								if (text.Equals("preserve") || text.Equals("default"))
								{
									schemaAttDef.Reserved = SchemaAttDef.Reserve.XmlSpace;
								}
							}
							else
							{
								schemaAttDef.Reserved = SchemaAttDef.Reserve.XmlSpace;
								if (schemaAttDef.TokenizedType != XmlTokenizedType.ENUMERATION)
								{
									Throw(System.SR.Xml_EnumerationRequired, string.Empty, schemaAttDef.LineNumber, schemaAttDef.LinePosition);
								}
								if (_validate)
								{
									schemaAttDef.CheckXmlSpace(_readerAdapterWithValidation.ValidationEventHandling);
								}
							}
						}
						else if (schemaAttDef.Name.Name == "lang")
						{
							schemaAttDef.Reserved = SchemaAttDef.Reserve.XmlLang;
						}
					}
					if (!flag)
					{
						value.AddAttDef(schemaAttDef);
					}
					continue;
				}
				case Token.GreaterThan:
					if (_v1Compat && schemaAttDef != null && schemaAttDef.Prefix.Length > 0 && schemaAttDef.Prefix.Equals("xml") && schemaAttDef.Name.Name == "space")
					{
						schemaAttDef.Reserved = SchemaAttDef.Reserve.XmlSpace;
						if (schemaAttDef.Datatype.TokenizedType != XmlTokenizedType.ENUMERATION)
						{
							Throw(System.SR.Xml_EnumerationRequired, string.Empty, schemaAttDef.LineNumber, schemaAttDef.LinePosition);
						}
						if (_validate)
						{
							schemaAttDef.CheckXmlSpace(_readerAdapterWithValidation.ValidationEventHandling);
						}
					}
					return;
				}
				break;
			}
		}
		OnUnexpectedError();
	}

	private void ParseAttlistType(SchemaAttDef attrDef, SchemaElementDecl elementDecl, bool ignoreErrors)
	{
		Token token = GetToken(needWhiteSpace: true);
		if (token != 0)
		{
			elementDecl.HasNonCDataAttribute = true;
		}
		if (IsAttributeValueType(token))
		{
			attrDef.TokenizedType = (XmlTokenizedType)token;
			attrDef.SchemaType = XmlSchemaType.GetBuiltInSimpleType(attrDef.Datatype.TypeCode);
			switch (token)
			{
			default:
				return;
			case Token.ID:
				if (_validate && elementDecl.IsIdDeclared)
				{
					SchemaAttDef attDef = elementDecl.GetAttDef(attrDef.Name);
					if ((attDef == null || attDef.Datatype.TokenizedType != XmlTokenizedType.ID) && !ignoreErrors)
					{
						SendValidationEvent(XmlSeverityType.Error, System.SR.Sch_IdAttrDeclared, elementDecl.Name.ToString());
					}
				}
				elementDecl.IsIdDeclared = true;
				return;
			case Token.NOTATION:
				break;
			}
			if (_validate)
			{
				if (elementDecl.IsNotationDeclared && !ignoreErrors)
				{
					SendValidationEvent(_curPos - 8, XmlSeverityType.Error, System.SR.Sch_DupNotationAttribute, elementDecl.Name.ToString());
				}
				else
				{
					if (elementDecl.ContentValidator != null && elementDecl.ContentValidator.ContentType == XmlSchemaContentType.Empty && !ignoreErrors)
					{
						SendValidationEvent(_curPos - 8, XmlSeverityType.Error, System.SR.Sch_NotationAttributeOnEmptyElement, elementDecl.Name.ToString());
					}
					elementDecl.IsNotationDeclared = true;
				}
			}
			if (GetToken(needWhiteSpace: true) == Token.LeftParen && GetToken(needWhiteSpace: false) == Token.Name)
			{
				do
				{
					string nameString = GetNameString();
					if (!_schemaInfo.Notations.ContainsKey(nameString))
					{
						AddUndeclaredNotation(nameString);
					}
					if (_validate && !_v1Compat && attrDef.Values != null && attrDef.Values.Contains(nameString) && !ignoreErrors)
					{
						SendValidationEvent(XmlSeverityType.Error, new XmlSchemaException(System.SR.Xml_AttlistDuplNotationValue, nameString, BaseUriStr, LineNo, LinePos));
					}
					attrDef.AddValue(nameString);
					switch (GetToken(needWhiteSpace: false))
					{
					case Token.Or:
						continue;
					case Token.RightParen:
						return;
					}
					break;
				}
				while (GetToken(needWhiteSpace: false) == Token.Name);
			}
		}
		else if (token == Token.LeftParen)
		{
			attrDef.TokenizedType = XmlTokenizedType.ENUMERATION;
			attrDef.SchemaType = XmlSchemaType.GetBuiltInSimpleType(attrDef.Datatype.TypeCode);
			if (GetToken(needWhiteSpace: false) == Token.Nmtoken)
			{
				attrDef.AddValue(GetNameString());
				while (true)
				{
					string nmtokenString;
					switch (GetToken(needWhiteSpace: false))
					{
					case Token.Or:
						if (GetToken(needWhiteSpace: false) == Token.Nmtoken)
						{
							nmtokenString = GetNmtokenString();
							if (_validate && !_v1Compat && attrDef.Values != null && attrDef.Values.Contains(nmtokenString) && !ignoreErrors)
							{
								SendValidationEvent(XmlSeverityType.Error, new XmlSchemaException(System.SR.Xml_AttlistDuplEnumValue, nmtokenString, BaseUriStr, LineNo, LinePos));
							}
							goto IL_0278;
						}
						break;
					case Token.RightParen:
						return;
					}
					break;
					IL_0278:
					attrDef.AddValue(nmtokenString);
				}
			}
		}
		OnUnexpectedError();
	}

	private void ParseAttlistDefault(SchemaAttDef attrDef, bool ignoreErrors)
	{
		switch (GetToken(needWhiteSpace: true))
		{
		case Token.REQUIRED:
			attrDef.Presence = SchemaDeclBase.Use.Required;
			return;
		case Token.IMPLIED:
			attrDef.Presence = SchemaDeclBase.Use.Implied;
			return;
		case Token.FIXED:
			attrDef.Presence = SchemaDeclBase.Use.Fixed;
			if (GetToken(needWhiteSpace: true) != Token.Literal)
			{
				break;
			}
			goto case Token.Literal;
		case Token.Literal:
			if (_validate && attrDef.Datatype.TokenizedType == XmlTokenizedType.ID && !ignoreErrors)
			{
				SendValidationEvent(_curPos, XmlSeverityType.Error, System.SR.Sch_AttListPresence, string.Empty);
			}
			if (attrDef.TokenizedType != 0)
			{
				attrDef.DefaultValueExpanded = GetValueWithStrippedSpaces();
			}
			else
			{
				attrDef.DefaultValueExpanded = GetValue();
			}
			attrDef.ValueLineNumber = _literalLineInfo.lineNo;
			attrDef.ValueLinePosition = _literalLineInfo.linePos + 1;
			DtdValidator.SetDefaultTypedValue(attrDef, _readerAdapter);
			return;
		}
		OnUnexpectedError();
	}

	private void ParseElementDecl()
	{
		if (GetToken(needWhiteSpace: true) == Token.QName)
		{
			SchemaElementDecl value = null;
			XmlQualifiedName nameQualified = GetNameQualified(canHavePrefix: true);
			if (_schemaInfo.ElementDecls.TryGetValue(nameQualified, out value))
			{
				if (_validate)
				{
					SendValidationEvent(_curPos - nameQualified.Name.Length, XmlSeverityType.Error, System.SR.Sch_DupElementDecl, GetNameString());
				}
			}
			else
			{
				if (_schemaInfo.UndeclaredElementDecls.TryGetValue(nameQualified, out value))
				{
					_schemaInfo.UndeclaredElementDecls.Remove(nameQualified);
				}
				else
				{
					value = new SchemaElementDecl(nameQualified, nameQualified.Namespace);
				}
				_schemaInfo.ElementDecls.Add(nameQualified, value);
			}
			value.IsDeclaredInExternal = !ParsingInternalSubset;
			Token token = GetToken(needWhiteSpace: true);
			if (token != Token.LeftParen)
			{
				if (token != Token.ANY)
				{
					if (token != Token.EMPTY)
					{
						goto IL_0181;
					}
					value.ContentValidator = ContentValidator.Empty;
				}
				else
				{
					value.ContentValidator = ContentValidator.Any;
				}
			}
			else
			{
				int currentEntityId = _currentEntityId;
				Token token2 = GetToken(needWhiteSpace: false);
				if (token2 != Token.None)
				{
					if (token2 != Token.PCDATA)
					{
						goto IL_0181;
					}
					ParticleContentValidator particleContentValidator = new ParticleContentValidator(XmlSchemaContentType.Mixed);
					particleContentValidator.Start();
					particleContentValidator.OpenGroup();
					ParseElementMixedContent(particleContentValidator, currentEntityId);
					value.ContentValidator = particleContentValidator.Finish(useDFA: true);
				}
				else
				{
					ParticleContentValidator particleContentValidator2 = null;
					particleContentValidator2 = new ParticleContentValidator(XmlSchemaContentType.ElementOnly);
					particleContentValidator2.Start();
					particleContentValidator2.OpenGroup();
					ParseElementOnlyContent(particleContentValidator2, currentEntityId);
					value.ContentValidator = particleContentValidator2.Finish(useDFA: true);
				}
			}
			if (GetToken(needWhiteSpace: false) != Token.GreaterThan)
			{
				ThrowUnexpectedToken(_curPos, ">");
			}
			return;
		}
		goto IL_0181;
		IL_0181:
		OnUnexpectedError();
	}

	private void ParseElementOnlyContent(ParticleContentValidator pcv, int startParenEntityId)
	{
		Stack<ParseElementOnlyContent_LocalFrame> stack = new Stack<ParseElementOnlyContent_LocalFrame>();
		ParseElementOnlyContent_LocalFrame parseElementOnlyContent_LocalFrame = new ParseElementOnlyContent_LocalFrame(startParenEntityId);
		stack.Push(parseElementOnlyContent_LocalFrame);
		while (true)
		{
			Token token = GetToken(needWhiteSpace: false);
			if (token != Token.QName)
			{
				if (token != Token.LeftParen)
				{
					if (token != Token.GreaterThan)
					{
						goto IL_0148;
					}
					Throw(_curPos, System.SR.Xml_InvalidContentModel);
					goto IL_014e;
				}
				pcv.OpenGroup();
				parseElementOnlyContent_LocalFrame = new ParseElementOnlyContent_LocalFrame(_currentEntityId);
				stack.Push(parseElementOnlyContent_LocalFrame);
				continue;
			}
			pcv.AddName(GetNameQualified(canHavePrefix: true), null);
			ParseHowMany(pcv);
			goto IL_0078;
			IL_0148:
			OnUnexpectedError();
			goto IL_014e;
			IL_00f9:
			pcv.CloseGroup();
			if (_validate && _currentEntityId != parseElementOnlyContent_LocalFrame.startParenEntityId)
			{
				SendValidationEvent(_curPos, XmlSeverityType.Error, System.SR.Sch_ParEntityRefNesting, string.Empty);
			}
			ParseHowMany(pcv);
			goto IL_014e;
			IL_00cb:
			if (parseElementOnlyContent_LocalFrame.parsingSchema == Token.Comma)
			{
				Throw(_curPos, System.SR.Xml_InvalidContentModel);
			}
			pcv.AddChoice();
			parseElementOnlyContent_LocalFrame.parsingSchema = Token.Or;
			continue;
			IL_014e:
			stack.Pop();
			if (stack.Count > 0)
			{
				parseElementOnlyContent_LocalFrame = stack.Peek();
				goto IL_0078;
			}
			break;
			IL_0135:
			Throw(_curPos, System.SR.Xml_InvalidContentModel);
			goto IL_014e;
			IL_0078:
			switch (GetToken(needWhiteSpace: false))
			{
			case Token.Comma:
				break;
			case Token.Or:
				goto IL_00cb;
			case Token.RightParen:
				goto IL_00f9;
			case Token.GreaterThan:
				goto IL_0135;
			default:
				goto IL_0148;
			}
			if (parseElementOnlyContent_LocalFrame.parsingSchema == Token.Or)
			{
				Throw(_curPos, System.SR.Xml_InvalidContentModel);
			}
			pcv.AddSequence();
			parseElementOnlyContent_LocalFrame.parsingSchema = Token.Comma;
		}
	}

	private void ParseHowMany(ParticleContentValidator pcv)
	{
		switch (GetToken(needWhiteSpace: false))
		{
		case Token.Star:
			pcv.AddStar();
			break;
		case Token.QMark:
			pcv.AddQMark();
			break;
		case Token.Plus:
			pcv.AddPlus();
			break;
		}
	}

	private void ParseElementMixedContent(ParticleContentValidator pcv, int startParenEntityId)
	{
		bool flag = false;
		int num = -1;
		int currentEntityId = _currentEntityId;
		while (true)
		{
			switch (GetToken(needWhiteSpace: false))
			{
			case Token.RightParen:
				pcv.CloseGroup();
				if (_validate && _currentEntityId != startParenEntityId)
				{
					SendValidationEvent(_curPos, XmlSeverityType.Error, System.SR.Sch_ParEntityRefNesting, string.Empty);
				}
				if (GetToken(needWhiteSpace: false) == Token.Star && flag)
				{
					pcv.AddStar();
				}
				else if (flag)
				{
					ThrowUnexpectedToken(_curPos, "*");
				}
				return;
			case Token.Or:
			{
				if (!flag)
				{
					flag = true;
				}
				else
				{
					pcv.AddChoice();
				}
				if (_validate)
				{
					num = _currentEntityId;
					if (currentEntityId < num)
					{
						SendValidationEvent(_curPos, XmlSeverityType.Error, System.SR.Sch_ParEntityRefNesting, string.Empty);
					}
				}
				if (GetToken(needWhiteSpace: false) != Token.QName)
				{
					break;
				}
				XmlQualifiedName nameQualified = GetNameQualified(canHavePrefix: true);
				if (pcv.Exists(nameQualified) && _validate)
				{
					SendValidationEvent(XmlSeverityType.Error, System.SR.Sch_DupElement, nameQualified.ToString());
				}
				pcv.AddName(nameQualified, null);
				if (_validate)
				{
					currentEntityId = _currentEntityId;
					if (currentEntityId < num)
					{
						SendValidationEvent(_curPos, XmlSeverityType.Error, System.SR.Sch_ParEntityRefNesting, string.Empty);
					}
				}
				continue;
			}
			}
			OnUnexpectedError();
		}
	}

	private void ParseEntityDecl()
	{
		bool flag = false;
		SchemaEntity schemaEntity = null;
		Token token = GetToken(needWhiteSpace: true);
		if (token == Token.Name)
		{
			goto IL_002c;
		}
		if (token == Token.Percent)
		{
			flag = true;
			if (GetToken(needWhiteSpace: true) == Token.Name)
			{
				goto IL_002c;
			}
		}
		goto IL_01d6;
		IL_002c:
		XmlQualifiedName nameQualified = GetNameQualified(canHavePrefix: false);
		schemaEntity = new SchemaEntity(nameQualified, flag);
		schemaEntity.BaseURI = BaseUriStr;
		schemaEntity.DeclaredURI = ((_externalDtdBaseUri.Length == 0) ? _documentBaseUri : _externalDtdBaseUri);
		if (flag)
		{
			if (!_schemaInfo.ParameterEntities.ContainsKey(nameQualified))
			{
				_schemaInfo.ParameterEntities.Add(nameQualified, schemaEntity);
			}
		}
		else if (!_schemaInfo.GeneralEntities.ContainsKey(nameQualified))
		{
			_schemaInfo.GeneralEntities.Add(nameQualified, schemaEntity);
		}
		schemaEntity.DeclaredInExternal = !ParsingInternalSubset;
		schemaEntity.ParsingInProgress = true;
		Token token2 = GetToken(needWhiteSpace: true);
		if ((uint)(token2 - 33) > 1u)
		{
			if (token2 != Token.Literal)
			{
				goto IL_01d6;
			}
			schemaEntity.Text = GetValue();
			schemaEntity.Line = _literalLineInfo.lineNo;
			schemaEntity.Pos = _literalLineInfo.linePos;
		}
		else
		{
			ParseExternalId(token2, Token.EntityDecl, out var publicId, out var systemId);
			schemaEntity.IsExternal = true;
			schemaEntity.Url = systemId;
			schemaEntity.Pubid = publicId;
			if (GetToken(needWhiteSpace: false) == Token.NData)
			{
				if (flag)
				{
					ThrowUnexpectedToken(_curPos - 5, ">");
				}
				if (!_whitespaceSeen)
				{
					Throw(_curPos - 5, System.SR.Xml_ExpectingWhiteSpace, "NDATA");
				}
				if (GetToken(needWhiteSpace: true) != Token.Name)
				{
					goto IL_01d6;
				}
				schemaEntity.NData = GetNameQualified(canHavePrefix: false);
				string name = schemaEntity.NData.Name;
				if (!_schemaInfo.Notations.ContainsKey(name))
				{
					AddUndeclaredNotation(name);
				}
			}
		}
		if (GetToken(needWhiteSpace: false) == Token.GreaterThan)
		{
			schemaEntity.ParsingInProgress = false;
			return;
		}
		goto IL_01d6;
		IL_01d6:
		OnUnexpectedError();
	}

	private void ParseNotationDecl()
	{
		if (GetToken(needWhiteSpace: true) != Token.Name)
		{
			OnUnexpectedError();
		}
		XmlQualifiedName nameQualified = GetNameQualified(canHavePrefix: false);
		SchemaNotation schemaNotation = null;
		if (!_schemaInfo.Notations.ContainsKey(nameQualified.Name))
		{
			if (_undeclaredNotations != null)
			{
				_undeclaredNotations.Remove(nameQualified.Name);
			}
			schemaNotation = new SchemaNotation(nameQualified);
			_schemaInfo.Notations.Add(schemaNotation.Name.Name, schemaNotation);
		}
		else if (_validate)
		{
			SendValidationEvent(_curPos - nameQualified.Name.Length, XmlSeverityType.Error, System.SR.Sch_DupNotation, nameQualified.Name);
		}
		Token token = GetToken(needWhiteSpace: true);
		if (token == Token.SYSTEM || token == Token.PUBLIC)
		{
			ParseExternalId(token, Token.NOTATION, out var publicId, out var systemId);
			if (schemaNotation != null)
			{
				schemaNotation.SystemLiteral = systemId;
				schemaNotation.Pubid = publicId;
			}
		}
		else
		{
			OnUnexpectedError();
		}
		if (GetToken(needWhiteSpace: false) != Token.GreaterThan)
		{
			OnUnexpectedError();
		}
	}

	private void AddUndeclaredNotation(string notationName)
	{
		if (_undeclaredNotations == null)
		{
			_undeclaredNotations = new Dictionary<string, UndeclaredNotation>();
		}
		UndeclaredNotation undeclaredNotation = new UndeclaredNotation(notationName, LineNo, LinePos - notationName.Length);
		if (_undeclaredNotations.TryGetValue(notationName, out var value))
		{
			undeclaredNotation.next = value.next;
			value.next = undeclaredNotation;
		}
		else
		{
			_undeclaredNotations.Add(notationName, undeclaredNotation);
		}
	}

	private void ParseComment()
	{
		SaveParsingBuffer();
		try
		{
			if (SaveInternalSubsetValue)
			{
				_readerAdapter.ParseComment(_internalSubsetValueSb);
				_internalSubsetValueSb.Append("-->");
			}
			else
			{
				_readerAdapter.ParseComment(null);
			}
		}
		catch (XmlException ex)
		{
			if (!(ex.ResString == System.SR.Xml_UnexpectedEOF) || _currentEntityId == 0)
			{
				throw;
			}
			SendValidationEvent(XmlSeverityType.Error, System.SR.Sch_ParEntityRefNesting, null);
		}
		LoadParsingBuffer();
	}

	private void ParsePI()
	{
		SaveParsingBuffer();
		if (SaveInternalSubsetValue)
		{
			_readerAdapter.ParsePI(_internalSubsetValueSb);
			_internalSubsetValueSb.Append("?>");
		}
		else
		{
			_readerAdapter.ParsePI(null);
		}
		LoadParsingBuffer();
	}

	private void ParseCondSection()
	{
		int currentEntityId = _currentEntityId;
		switch (GetToken(needWhiteSpace: false))
		{
		case Token.INCLUDE:
			if (GetToken(needWhiteSpace: false) == Token.LeftBracket)
			{
				if (_validate && currentEntityId != _currentEntityId)
				{
					SendValidationEvent(_curPos, XmlSeverityType.Error, System.SR.Sch_ParEntityRefNesting, string.Empty);
				}
				if (_validate)
				{
					if (_condSectionEntityIds == null)
					{
						_condSectionEntityIds = new int[2];
					}
					else if (_condSectionEntityIds.Length == _condSectionDepth)
					{
						int[] array = new int[_condSectionEntityIds.Length * 2];
						Array.Copy(_condSectionEntityIds, array, _condSectionEntityIds.Length);
						_condSectionEntityIds = array;
					}
					_condSectionEntityIds[_condSectionDepth] = currentEntityId;
				}
				_condSectionDepth++;
				break;
			}
			goto default;
		case Token.IGNORE:
			if (GetToken(needWhiteSpace: false) == Token.LeftBracket)
			{
				if (_validate && currentEntityId != _currentEntityId)
				{
					SendValidationEvent(_curPos, XmlSeverityType.Error, System.SR.Sch_ParEntityRefNesting, string.Empty);
				}
				if (GetToken(needWhiteSpace: false) == Token.CondSectionEnd)
				{
					if (_validate && currentEntityId != _currentEntityId)
					{
						SendValidationEvent(_curPos, XmlSeverityType.Error, System.SR.Sch_ParEntityRefNesting, string.Empty);
					}
					break;
				}
			}
			goto default;
		default:
			OnUnexpectedError();
			break;
		}
	}

	private void ParseExternalId(Token idTokenType, Token declType, out string publicId, out string systemId)
	{
		LineInfo keywordLineInfo = new LineInfo(LineNo, LinePos - 6);
		publicId = null;
		systemId = null;
		if (GetToken(needWhiteSpace: true) != Token.Literal)
		{
			ThrowUnexpectedToken(_curPos, "\"", "'");
		}
		if (idTokenType == Token.SYSTEM)
		{
			systemId = GetValue();
			if (systemId.Contains('#'))
			{
				Throw(_curPos - systemId.Length - 1, System.SR.Xml_FragmentId, new string[2]
				{
					systemId.Substring(systemId.IndexOf('#')),
					systemId
				});
			}
			if (declType == Token.DOCTYPE && !_freeFloatingDtd)
			{
				_literalLineInfo.linePos++;
				_readerAdapter.OnSystemId(systemId, keywordLineInfo, _literalLineInfo);
			}
			return;
		}
		publicId = GetValue();
		int num;
		if ((num = XmlCharType.IsPublicId(publicId)) >= 0)
		{
			ThrowInvalidChar(_curPos - 1 - publicId.Length + num, publicId, num);
		}
		if (declType == Token.DOCTYPE && !_freeFloatingDtd)
		{
			_literalLineInfo.linePos++;
			_readerAdapter.OnPublicId(publicId, keywordLineInfo, _literalLineInfo);
			if (GetToken(needWhiteSpace: false) == Token.Literal)
			{
				if (!_whitespaceSeen)
				{
					Throw(System.SR.Xml_ExpectingWhiteSpace, char.ToString(_literalQuoteChar), _literalLineInfo.lineNo, _literalLineInfo.linePos);
				}
				systemId = GetValue();
				_literalLineInfo.linePos++;
				_readerAdapter.OnSystemId(systemId, keywordLineInfo, _literalLineInfo);
			}
			else
			{
				ThrowUnexpectedToken(_curPos, "\"", "'");
			}
		}
		else if (GetToken(needWhiteSpace: false) == Token.Literal)
		{
			if (!_whitespaceSeen)
			{
				Throw(System.SR.Xml_ExpectingWhiteSpace, char.ToString(_literalQuoteChar), _literalLineInfo.lineNo, _literalLineInfo.linePos);
			}
			systemId = GetValue();
		}
		else if (declType != Token.NOTATION)
		{
			ThrowUnexpectedToken(_curPos, "\"", "'");
		}
	}

	private Token GetToken(bool needWhiteSpace)
	{
		_whitespaceSeen = false;
		while (true)
		{
			switch (_chars[_curPos])
			{
			case '\0':
				if (_curPos != _charsUsed)
				{
					ThrowInvalidChar(_chars, _charsUsed, _curPos);
				}
				break;
			case '\n':
				_whitespaceSeen = true;
				_curPos++;
				_readerAdapter.OnNewLine(_curPos);
				continue;
			case '\r':
				_whitespaceSeen = true;
				if (_chars[_curPos + 1] == '\n')
				{
					if (Normalize)
					{
						SaveParsingBuffer();
						_readerAdapter.CurrentPosition++;
					}
					_curPos += 2;
				}
				else
				{
					if (_curPos + 1 >= _charsUsed && !_readerAdapter.IsEof)
					{
						break;
					}
					_chars[_curPos] = '\n';
					_curPos++;
				}
				_readerAdapter.OnNewLine(_curPos);
				continue;
			case '\t':
			case ' ':
				_whitespaceSeen = true;
				_curPos++;
				continue;
			case '%':
				if (_charsUsed - _curPos < 2)
				{
					break;
				}
				if (!XmlCharType.IsWhiteSpace(_chars[_curPos + 1]))
				{
					if (IgnoreEntityReferences)
					{
						_curPos++;
					}
					else
					{
						HandleEntityReference(paramEntity: true, inLiteral: false, inAttribute: false);
					}
					continue;
				}
				goto default;
			default:
				if (needWhiteSpace && !_whitespaceSeen && _scanningFunction != ScanningFunction.ParamEntitySpace)
				{
					Throw(_curPos, System.SR.Xml_ExpectingWhiteSpace, ParseUnexpectedToken(_curPos));
				}
				_tokenStartPos = _curPos;
				while (true)
				{
					switch (_scanningFunction)
					{
					case ScanningFunction.Name:
						return ScanNameExpected();
					case ScanningFunction.QName:
						return ScanQNameExpected();
					case ScanningFunction.Nmtoken:
						return ScanNmtokenExpected();
					case ScanningFunction.SubsetContent:
						return ScanSubsetContent();
					case ScanningFunction.Doctype1:
						return ScanDoctype1();
					case ScanningFunction.Doctype2:
						return ScanDoctype2();
					case ScanningFunction.Element1:
						return ScanElement1();
					case ScanningFunction.Element2:
						return ScanElement2();
					case ScanningFunction.Element3:
						return ScanElement3();
					case ScanningFunction.Element4:
						return ScanElement4();
					case ScanningFunction.Element5:
						return ScanElement5();
					case ScanningFunction.Element6:
						return ScanElement6();
					case ScanningFunction.Element7:
						return ScanElement7();
					case ScanningFunction.Attlist1:
						return ScanAttlist1();
					case ScanningFunction.Attlist2:
						return ScanAttlist2();
					case ScanningFunction.Attlist3:
						return ScanAttlist3();
					case ScanningFunction.Attlist4:
						return ScanAttlist4();
					case ScanningFunction.Attlist5:
						return ScanAttlist5();
					case ScanningFunction.Attlist6:
						return ScanAttlist6();
					case ScanningFunction.Attlist7:
						return ScanAttlist7();
					case ScanningFunction.Notation1:
						return ScanNotation1();
					case ScanningFunction.SystemId:
						return ScanSystemId();
					case ScanningFunction.PublicId1:
						return ScanPublicId1();
					case ScanningFunction.PublicId2:
						return ScanPublicId2();
					case ScanningFunction.Entity1:
						return ScanEntity1();
					case ScanningFunction.Entity2:
						return ScanEntity2();
					case ScanningFunction.Entity3:
						return ScanEntity3();
					case ScanningFunction.CondSection1:
						return ScanCondSection1();
					case ScanningFunction.CondSection2:
						return ScanCondSection2();
					case ScanningFunction.CondSection3:
						return ScanCondSection3();
					case ScanningFunction.ClosingTag:
						return ScanClosingTag();
					case ScanningFunction.ParamEntitySpace:
						break;
					default:
						return Token.None;
					}
					_whitespaceSeen = true;
					_scanningFunction = _savedScanningFunction;
				}
			}
			if ((_readerAdapter.IsEof || ReadData() == 0) && !HandleEntityEnd(inLiteral: false))
			{
				if (_scanningFunction == ScanningFunction.SubsetContent)
				{
					break;
				}
				Throw(_curPos, System.SR.Xml_IncompleteDtdContent);
			}
		}
		return Token.Eof;
	}

	private Token ScanSubsetContent()
	{
		while (true)
		{
			char c = _chars[_curPos];
			if (c != '<')
			{
				if (c != ']')
				{
					goto IL_04f3;
				}
				if (_charsUsed - _curPos >= 2 || _readerAdapter.IsEof)
				{
					if (_chars[_curPos + 1] != ']')
					{
						_curPos++;
						_scanningFunction = ScanningFunction.ClosingTag;
						return Token.RightBracket;
					}
					if (_charsUsed - _curPos >= 3 || _readerAdapter.IsEof)
					{
						if (_chars[_curPos + 1] == ']' && _chars[_curPos + 2] == '>')
						{
							break;
						}
						goto IL_04f3;
					}
				}
			}
			else
			{
				switch (_chars[_curPos + 1])
				{
				case '!':
					switch (_chars[_curPos + 2])
					{
					case 'E':
						if (_chars[_curPos + 3] == 'L')
						{
							if (_charsUsed - _curPos >= 9)
							{
								if (_chars[_curPos + 4] != 'E' || _chars[_curPos + 5] != 'M' || _chars[_curPos + 6] != 'E' || _chars[_curPos + 7] != 'N' || _chars[_curPos + 8] != 'T')
								{
									Throw(_curPos, System.SR.Xml_ExpectDtdMarkup);
								}
								_curPos += 9;
								_scanningFunction = ScanningFunction.QName;
								_nextScaningFunction = ScanningFunction.Element1;
								return Token.ElementDecl;
							}
						}
						else if (_chars[_curPos + 3] == 'N')
						{
							if (_charsUsed - _curPos >= 8)
							{
								if (_chars[_curPos + 4] != 'T' || _chars[_curPos + 5] != 'I' || _chars[_curPos + 6] != 'T' || _chars[_curPos + 7] != 'Y')
								{
									Throw(_curPos, System.SR.Xml_ExpectDtdMarkup);
								}
								_curPos += 8;
								_scanningFunction = ScanningFunction.Entity1;
								return Token.EntityDecl;
							}
						}
						else if (_charsUsed - _curPos >= 4)
						{
							Throw(_curPos, System.SR.Xml_ExpectDtdMarkup);
							return Token.None;
						}
						break;
					case 'A':
						if (_charsUsed - _curPos >= 9)
						{
							if (_chars[_curPos + 3] != 'T' || _chars[_curPos + 4] != 'T' || _chars[_curPos + 5] != 'L' || _chars[_curPos + 6] != 'I' || _chars[_curPos + 7] != 'S' || _chars[_curPos + 8] != 'T')
							{
								Throw(_curPos, System.SR.Xml_ExpectDtdMarkup);
							}
							_curPos += 9;
							_scanningFunction = ScanningFunction.QName;
							_nextScaningFunction = ScanningFunction.Attlist1;
							return Token.AttlistDecl;
						}
						break;
					case 'N':
						if (_charsUsed - _curPos >= 10)
						{
							if (_chars[_curPos + 3] != 'O' || _chars[_curPos + 4] != 'T' || _chars[_curPos + 5] != 'A' || _chars[_curPos + 6] != 'T' || _chars[_curPos + 7] != 'I' || _chars[_curPos + 8] != 'O' || _chars[_curPos + 9] != 'N')
							{
								Throw(_curPos, System.SR.Xml_ExpectDtdMarkup);
							}
							_curPos += 10;
							_scanningFunction = ScanningFunction.Name;
							_nextScaningFunction = ScanningFunction.Notation1;
							return Token.NotationDecl;
						}
						break;
					case '[':
						_curPos += 3;
						_scanningFunction = ScanningFunction.CondSection1;
						return Token.CondSectionStart;
					case '-':
						if (_chars[_curPos + 3] == '-')
						{
							_curPos += 4;
							return Token.Comment;
						}
						if (_charsUsed - _curPos >= 4)
						{
							Throw(_curPos, System.SR.Xml_ExpectDtdMarkup);
						}
						break;
					default:
						if (_charsUsed - _curPos >= 3)
						{
							Throw(_curPos + 2, System.SR.Xml_ExpectDtdMarkup);
						}
						break;
					}
					break;
				case '?':
					_curPos += 2;
					return Token.PI;
				default:
					if (_charsUsed - _curPos >= 2)
					{
						Throw(_curPos, System.SR.Xml_ExpectDtdMarkup);
						return Token.None;
					}
					break;
				}
			}
			goto IL_0513;
			IL_0513:
			if (ReadData() == 0)
			{
				Throw(_charsUsed, System.SR.Xml_IncompleteDtdContent);
			}
			continue;
			IL_04f3:
			if (_charsUsed - _curPos != 0)
			{
				Throw(_curPos, System.SR.Xml_ExpectDtdMarkup);
			}
			goto IL_0513;
		}
		_curPos += 3;
		return Token.CondSectionEnd;
	}

	private Token ScanNameExpected()
	{
		ScanName();
		_scanningFunction = _nextScaningFunction;
		return Token.Name;
	}

	private Token ScanQNameExpected()
	{
		ScanQName();
		_scanningFunction = _nextScaningFunction;
		return Token.QName;
	}

	private Token ScanNmtokenExpected()
	{
		ScanNmtoken();
		_scanningFunction = _nextScaningFunction;
		return Token.Nmtoken;
	}

	private Token ScanDoctype1()
	{
		switch (_chars[_curPos])
		{
		case 'P':
			if (!EatPublicKeyword())
			{
				Throw(_curPos, System.SR.Xml_ExpectExternalOrClose);
			}
			_nextScaningFunction = ScanningFunction.Doctype2;
			_scanningFunction = ScanningFunction.PublicId1;
			return Token.PUBLIC;
		case 'S':
			if (!EatSystemKeyword())
			{
				Throw(_curPos, System.SR.Xml_ExpectExternalOrClose);
			}
			_nextScaningFunction = ScanningFunction.Doctype2;
			_scanningFunction = ScanningFunction.SystemId;
			return Token.SYSTEM;
		case '[':
			_curPos++;
			_scanningFunction = ScanningFunction.SubsetContent;
			return Token.LeftBracket;
		case '>':
			_curPos++;
			_scanningFunction = ScanningFunction.SubsetContent;
			return Token.GreaterThan;
		default:
			Throw(_curPos, System.SR.Xml_ExpectExternalOrClose);
			return Token.None;
		}
	}

	private Token ScanDoctype2()
	{
		switch (_chars[_curPos])
		{
		case '[':
			_curPos++;
			_scanningFunction = ScanningFunction.SubsetContent;
			return Token.LeftBracket;
		case '>':
			_curPos++;
			_scanningFunction = ScanningFunction.SubsetContent;
			return Token.GreaterThan;
		default:
			Throw(_curPos, System.SR.Xml_ExpectSubOrClose);
			return Token.None;
		}
	}

	private Token ScanClosingTag()
	{
		if (_chars[_curPos] != '>')
		{
			ThrowUnexpectedToken(_curPos, ">");
		}
		_curPos++;
		_scanningFunction = ScanningFunction.SubsetContent;
		return Token.GreaterThan;
	}

	private Token ScanElement1()
	{
		while (true)
		{
			char c = _chars[_curPos];
			if (c != '(')
			{
				if (c != 'A')
				{
					if (c == 'E')
					{
						if (_charsUsed - _curPos < 5)
						{
							goto IL_011b;
						}
						if (_chars[_curPos + 1] == 'M' && _chars[_curPos + 2] == 'P' && _chars[_curPos + 3] == 'T' && _chars[_curPos + 4] == 'Y')
						{
							_curPos += 5;
							_scanningFunction = ScanningFunction.ClosingTag;
							return Token.EMPTY;
						}
					}
				}
				else
				{
					if (_charsUsed - _curPos < 3)
					{
						goto IL_011b;
					}
					if (_chars[_curPos + 1] == 'N' && _chars[_curPos + 2] == 'Y')
					{
						break;
					}
				}
				Throw(_curPos, System.SR.Xml_InvalidContentModel);
				goto IL_011b;
			}
			_scanningFunction = ScanningFunction.Element2;
			_curPos++;
			return Token.LeftParen;
			IL_011b:
			if (ReadData() == 0)
			{
				Throw(_curPos, System.SR.Xml_IncompleteDtdContent);
			}
		}
		_curPos += 3;
		_scanningFunction = ScanningFunction.ClosingTag;
		return Token.ANY;
	}

	private Token ScanElement2()
	{
		if (_chars[_curPos] == '#')
		{
			while (_charsUsed - _curPos < 7)
			{
				if (ReadData() == 0)
				{
					Throw(_curPos, System.SR.Xml_IncompleteDtdContent);
				}
			}
			if (_chars[_curPos + 1] == 'P' && _chars[_curPos + 2] == 'C' && _chars[_curPos + 3] == 'D' && _chars[_curPos + 4] == 'A' && _chars[_curPos + 5] == 'T' && _chars[_curPos + 6] == 'A')
			{
				_curPos += 7;
				_scanningFunction = ScanningFunction.Element6;
				return Token.PCDATA;
			}
			Throw(_curPos + 1, System.SR.Xml_ExpectPcData);
		}
		_scanningFunction = ScanningFunction.Element3;
		return Token.None;
	}

	private Token ScanElement3()
	{
		switch (_chars[_curPos])
		{
		case '(':
			_curPos++;
			return Token.LeftParen;
		case '>':
			_curPos++;
			_scanningFunction = ScanningFunction.SubsetContent;
			return Token.GreaterThan;
		default:
			ScanQName();
			_scanningFunction = ScanningFunction.Element4;
			return Token.QName;
		}
	}

	private Token ScanElement4()
	{
		_scanningFunction = ScanningFunction.Element5;
		Token result;
		switch (_chars[_curPos])
		{
		case '*':
			result = Token.Star;
			break;
		case '?':
			result = Token.QMark;
			break;
		case '+':
			result = Token.Plus;
			break;
		default:
			return Token.None;
		}
		if (_whitespaceSeen)
		{
			Throw(_curPos, System.SR.Xml_ExpectNoWhitespace);
		}
		_curPos++;
		return result;
	}

	private Token ScanElement5()
	{
		switch (_chars[_curPos])
		{
		case ',':
			_curPos++;
			_scanningFunction = ScanningFunction.Element3;
			return Token.Comma;
		case '|':
			_curPos++;
			_scanningFunction = ScanningFunction.Element3;
			return Token.Or;
		case ')':
			_curPos++;
			_scanningFunction = ScanningFunction.Element4;
			return Token.RightParen;
		case '>':
			_curPos++;
			_scanningFunction = ScanningFunction.SubsetContent;
			return Token.GreaterThan;
		default:
			Throw(_curPos, System.SR.Xml_ExpectOp);
			return Token.None;
		}
	}

	private Token ScanElement6()
	{
		switch (_chars[_curPos])
		{
		case ')':
			_curPos++;
			_scanningFunction = ScanningFunction.Element7;
			return Token.RightParen;
		case '|':
			_curPos++;
			_nextScaningFunction = ScanningFunction.Element6;
			_scanningFunction = ScanningFunction.QName;
			return Token.Or;
		default:
			ThrowUnexpectedToken(_curPos, ")", "|");
			return Token.None;
		}
	}

	private Token ScanElement7()
	{
		_scanningFunction = ScanningFunction.ClosingTag;
		if (_chars[_curPos] == '*' && !_whitespaceSeen)
		{
			_curPos++;
			return Token.Star;
		}
		return Token.None;
	}

	private Token ScanAttlist1()
	{
		char c = _chars[_curPos];
		if (c == '>')
		{
			_curPos++;
			_scanningFunction = ScanningFunction.SubsetContent;
			return Token.GreaterThan;
		}
		if (!_whitespaceSeen)
		{
			Throw(_curPos, System.SR.Xml_ExpectingWhiteSpace, ParseUnexpectedToken(_curPos));
		}
		ScanQName();
		_scanningFunction = ScanningFunction.Attlist2;
		return Token.QName;
	}

	private Token ScanAttlist2()
	{
		while (true)
		{
			switch (_chars[_curPos])
			{
			case '(':
				_curPos++;
				_scanningFunction = ScanningFunction.Nmtoken;
				_nextScaningFunction = ScanningFunction.Attlist5;
				return Token.LeftParen;
			case 'C':
				if (_charsUsed - _curPos >= 5)
				{
					if (_chars[_curPos + 1] != 'D' || _chars[_curPos + 2] != 'A' || _chars[_curPos + 3] != 'T' || _chars[_curPos + 4] != 'A')
					{
						Throw(_curPos, System.SR.Xml_InvalidAttributeType1);
					}
					_curPos += 5;
					_scanningFunction = ScanningFunction.Attlist6;
					return Token.CDATA;
				}
				break;
			case 'E':
				if (_charsUsed - _curPos < 9)
				{
					break;
				}
				_scanningFunction = ScanningFunction.Attlist6;
				if (_chars[_curPos + 1] != 'N' || _chars[_curPos + 2] != 'T' || _chars[_curPos + 3] != 'I' || _chars[_curPos + 4] != 'T')
				{
					Throw(_curPos, System.SR.Xml_InvalidAttributeType);
				}
				switch (_chars[_curPos + 5])
				{
				case 'I':
					if (_chars[_curPos + 6] != 'E' || _chars[_curPos + 7] != 'S')
					{
						Throw(_curPos, System.SR.Xml_InvalidAttributeType);
					}
					_curPos += 8;
					return Token.ENTITIES;
				case 'Y':
					_curPos += 6;
					return Token.ENTITY;
				}
				Throw(_curPos, System.SR.Xml_InvalidAttributeType);
				break;
			case 'I':
				if (_charsUsed - _curPos >= 6)
				{
					_scanningFunction = ScanningFunction.Attlist6;
					if (_chars[_curPos + 1] != 'D')
					{
						Throw(_curPos, System.SR.Xml_InvalidAttributeType);
					}
					if (_chars[_curPos + 2] != 'R')
					{
						_curPos += 2;
						return Token.ID;
					}
					if (_chars[_curPos + 3] != 'E' || _chars[_curPos + 4] != 'F')
					{
						Throw(_curPos, System.SR.Xml_InvalidAttributeType);
					}
					if (_chars[_curPos + 5] != 'S')
					{
						_curPos += 5;
						return Token.IDREF;
					}
					_curPos += 6;
					return Token.IDREFS;
				}
				break;
			case 'N':
				if (_charsUsed - _curPos < 8 && !_readerAdapter.IsEof)
				{
					break;
				}
				switch (_chars[_curPos + 1])
				{
				case 'O':
					if (_chars[_curPos + 2] != 'T' || _chars[_curPos + 3] != 'A' || _chars[_curPos + 4] != 'T' || _chars[_curPos + 5] != 'I' || _chars[_curPos + 6] != 'O' || _chars[_curPos + 7] != 'N')
					{
						Throw(_curPos, System.SR.Xml_InvalidAttributeType);
					}
					_curPos += 8;
					_scanningFunction = ScanningFunction.Attlist3;
					return Token.NOTATION;
				case 'M':
					if (_chars[_curPos + 2] != 'T' || _chars[_curPos + 3] != 'O' || _chars[_curPos + 4] != 'K' || _chars[_curPos + 5] != 'E' || _chars[_curPos + 6] != 'N')
					{
						Throw(_curPos, System.SR.Xml_InvalidAttributeType);
					}
					_scanningFunction = ScanningFunction.Attlist6;
					if (_chars[_curPos + 7] == 'S')
					{
						_curPos += 8;
						return Token.NMTOKENS;
					}
					_curPos += 7;
					return Token.NMTOKEN;
				}
				Throw(_curPos, System.SR.Xml_InvalidAttributeType);
				break;
			default:
				Throw(_curPos, System.SR.Xml_InvalidAttributeType);
				break;
			}
			if (ReadData() == 0)
			{
				Throw(_curPos, System.SR.Xml_IncompleteDtdContent);
			}
		}
	}

	private Token ScanAttlist3()
	{
		if (_chars[_curPos] == '(')
		{
			_curPos++;
			_scanningFunction = ScanningFunction.Name;
			_nextScaningFunction = ScanningFunction.Attlist4;
			return Token.LeftParen;
		}
		ThrowUnexpectedToken(_curPos, "(");
		return Token.None;
	}

	private Token ScanAttlist4()
	{
		switch (_chars[_curPos])
		{
		case ')':
			_curPos++;
			_scanningFunction = ScanningFunction.Attlist6;
			return Token.RightParen;
		case '|':
			_curPos++;
			_scanningFunction = ScanningFunction.Name;
			_nextScaningFunction = ScanningFunction.Attlist4;
			return Token.Or;
		default:
			ThrowUnexpectedToken(_curPos, ")", "|");
			return Token.None;
		}
	}

	private Token ScanAttlist5()
	{
		switch (_chars[_curPos])
		{
		case ')':
			_curPos++;
			_scanningFunction = ScanningFunction.Attlist6;
			return Token.RightParen;
		case '|':
			_curPos++;
			_scanningFunction = ScanningFunction.Nmtoken;
			_nextScaningFunction = ScanningFunction.Attlist5;
			return Token.Or;
		default:
			ThrowUnexpectedToken(_curPos, ")", "|");
			return Token.None;
		}
	}

	private Token ScanAttlist6()
	{
		while (true)
		{
			switch (_chars[_curPos])
			{
			case '"':
			case '\'':
				ScanLiteral(LiteralType.AttributeValue);
				_scanningFunction = ScanningFunction.Attlist1;
				return Token.Literal;
			case '#':
				if (_charsUsed - _curPos < 6)
				{
					break;
				}
				switch (_chars[_curPos + 1])
				{
				case 'R':
					if (_charsUsed - _curPos >= 9)
					{
						if (_chars[_curPos + 2] != 'E' || _chars[_curPos + 3] != 'Q' || _chars[_curPos + 4] != 'U' || _chars[_curPos + 5] != 'I' || _chars[_curPos + 6] != 'R' || _chars[_curPos + 7] != 'E' || _chars[_curPos + 8] != 'D')
						{
							Throw(_curPos, System.SR.Xml_ExpectAttType);
						}
						_curPos += 9;
						_scanningFunction = ScanningFunction.Attlist1;
						return Token.REQUIRED;
					}
					break;
				case 'I':
					if (_charsUsed - _curPos >= 8)
					{
						if (_chars[_curPos + 2] != 'M' || _chars[_curPos + 3] != 'P' || _chars[_curPos + 4] != 'L' || _chars[_curPos + 5] != 'I' || _chars[_curPos + 6] != 'E' || _chars[_curPos + 7] != 'D')
						{
							Throw(_curPos, System.SR.Xml_ExpectAttType);
						}
						_curPos += 8;
						_scanningFunction = ScanningFunction.Attlist1;
						return Token.IMPLIED;
					}
					break;
				case 'F':
					if (_chars[_curPos + 2] != 'I' || _chars[_curPos + 3] != 'X' || _chars[_curPos + 4] != 'E' || _chars[_curPos + 5] != 'D')
					{
						Throw(_curPos, System.SR.Xml_ExpectAttType);
					}
					_curPos += 6;
					_scanningFunction = ScanningFunction.Attlist7;
					return Token.FIXED;
				default:
					Throw(_curPos, System.SR.Xml_ExpectAttType);
					break;
				}
				break;
			default:
				Throw(_curPos, System.SR.Xml_ExpectAttType);
				break;
			}
			if (ReadData() == 0)
			{
				Throw(_curPos, System.SR.Xml_IncompleteDtdContent);
			}
		}
	}

	private Token ScanAttlist7()
	{
		char c = _chars[_curPos];
		if (c == '"' || c == '\'')
		{
			ScanLiteral(LiteralType.AttributeValue);
			_scanningFunction = ScanningFunction.Attlist1;
			return Token.Literal;
		}
		ThrowUnexpectedToken(_curPos, "\"", "'");
		return Token.None;
	}

	private Token ScanLiteral(LiteralType literalType)
	{
		char c = _chars[_curPos];
		char value = ((literalType == LiteralType.AttributeValue) ? ' ' : '\n');
		int currentEntityId = _currentEntityId;
		_literalLineInfo.Set(LineNo, LinePos);
		_curPos++;
		_tokenStartPos = _curPos;
		_stringBuilder.Length = 0;
		while (true)
		{
			if (XmlCharType.IsAttributeValueChar(_chars[_curPos]) && _chars[_curPos] != '%')
			{
				_curPos++;
				continue;
			}
			if (_chars[_curPos] == c && _currentEntityId == currentEntityId)
			{
				break;
			}
			int num = _curPos - _tokenStartPos;
			if (num > 0)
			{
				_stringBuilder.Append(_chars, _tokenStartPos, num);
				_tokenStartPos = _curPos;
			}
			switch (_chars[_curPos])
			{
			case '"':
			case '\'':
			case '>':
				_curPos++;
				continue;
			case '\n':
				_curPos++;
				if (Normalize)
				{
					_stringBuilder.Append(value);
					_tokenStartPos = _curPos;
				}
				_readerAdapter.OnNewLine(_curPos);
				continue;
			case '\r':
				if (_chars[_curPos + 1] == '\n')
				{
					if (Normalize)
					{
						if (literalType == LiteralType.AttributeValue)
						{
							_stringBuilder.Append(_readerAdapter.IsEntityEolNormalized ? "  " : " ");
						}
						else
						{
							_stringBuilder.Append(_readerAdapter.IsEntityEolNormalized ? "\r\n" : "\n");
						}
						_tokenStartPos = _curPos + 2;
						SaveParsingBuffer();
						_readerAdapter.CurrentPosition++;
					}
					_curPos += 2;
				}
				else
				{
					if (_curPos + 1 == _charsUsed)
					{
						break;
					}
					_curPos++;
					if (Normalize)
					{
						_stringBuilder.Append(value);
						_tokenStartPos = _curPos;
					}
				}
				_readerAdapter.OnNewLine(_curPos);
				continue;
			case '\t':
				if (literalType == LiteralType.AttributeValue && Normalize)
				{
					_stringBuilder.Append(' ');
					_tokenStartPos++;
				}
				_curPos++;
				continue;
			case '<':
				if (literalType == LiteralType.AttributeValue)
				{
					Throw(_curPos, System.SR.Xml_BadAttributeChar, XmlException.BuildCharExceptionArgs('<', '\0'));
				}
				_curPos++;
				continue;
			case '%':
				if (literalType != LiteralType.EntityReplText)
				{
					_curPos++;
					continue;
				}
				HandleEntityReference(paramEntity: true, inLiteral: true, literalType == LiteralType.AttributeValue);
				_tokenStartPos = _curPos;
				continue;
			case '&':
			{
				if (literalType == LiteralType.SystemOrPublicID)
				{
					_curPos++;
					continue;
				}
				if (_curPos + 1 == _charsUsed)
				{
					break;
				}
				if (_chars[_curPos + 1] == '#')
				{
					SaveParsingBuffer();
					int num2 = _readerAdapter.ParseNumericCharRef(SaveInternalSubsetValue ? _internalSubsetValueSb : null);
					LoadParsingBuffer();
					_stringBuilder.Append(_chars, _curPos, num2 - _curPos);
					_readerAdapter.CurrentPosition = num2;
					_tokenStartPos = num2;
					_curPos = num2;
					continue;
				}
				SaveParsingBuffer();
				if (literalType == LiteralType.AttributeValue)
				{
					int num3 = _readerAdapter.ParseNamedCharRef(expand: true, SaveInternalSubsetValue ? _internalSubsetValueSb : null);
					LoadParsingBuffer();
					if (num3 >= 0)
					{
						_stringBuilder.Append(_chars, _curPos, num3 - _curPos);
						_readerAdapter.CurrentPosition = num3;
						_tokenStartPos = num3;
						_curPos = num3;
					}
					else
					{
						HandleEntityReference(paramEntity: false, inLiteral: true, inAttribute: true);
						_tokenStartPos = _curPos;
					}
					continue;
				}
				int num4 = _readerAdapter.ParseNamedCharRef(expand: false, null);
				LoadParsingBuffer();
				if (num4 >= 0)
				{
					_tokenStartPos = _curPos;
					_curPos = num4;
					continue;
				}
				_stringBuilder.Append('&');
				_curPos++;
				_tokenStartPos = _curPos;
				XmlQualifiedName entityName = ScanEntityName();
				VerifyEntityReference(entityName, paramEntity: false, mustBeDeclared: false, inAttribute: false);
				continue;
			}
			default:
			{
				if (_curPos == _charsUsed)
				{
					break;
				}
				char ch = _chars[_curPos];
				if (XmlCharType.IsHighSurrogate(ch))
				{
					if (_curPos + 1 == _charsUsed)
					{
						break;
					}
					_curPos++;
					if (XmlCharType.IsLowSurrogate(_chars[_curPos]))
					{
						_curPos++;
						continue;
					}
				}
				ThrowInvalidChar(_chars, _charsUsed, _curPos);
				return Token.None;
			}
			}
			if ((_readerAdapter.IsEof || ReadData() == 0) && (literalType == LiteralType.SystemOrPublicID || !HandleEntityEnd(inLiteral: true)))
			{
				Throw(_curPos, System.SR.Xml_UnclosedQuote);
			}
			_tokenStartPos = _curPos;
		}
		if (_stringBuilder.Length > 0)
		{
			_stringBuilder.Append(_chars, _tokenStartPos, _curPos - _tokenStartPos);
		}
		_curPos++;
		_literalQuoteChar = c;
		return Token.Literal;
	}

	private XmlQualifiedName ScanEntityName()
	{
		try
		{
			ScanName();
		}
		catch (XmlException ex)
		{
			Throw(System.SR.Xml_ErrorParsingEntityName, string.Empty, ex.LineNumber, ex.LinePosition);
		}
		if (_chars[_curPos] != ';')
		{
			ThrowUnexpectedToken(_curPos, ";");
		}
		XmlQualifiedName nameQualified = GetNameQualified(canHavePrefix: false);
		_curPos++;
		return nameQualified;
	}

	private Token ScanNotation1()
	{
		switch (_chars[_curPos])
		{
		case 'P':
			if (!EatPublicKeyword())
			{
				Throw(_curPos, System.SR.Xml_ExpectExternalOrClose);
			}
			_nextScaningFunction = ScanningFunction.ClosingTag;
			_scanningFunction = ScanningFunction.PublicId1;
			return Token.PUBLIC;
		case 'S':
			if (!EatSystemKeyword())
			{
				Throw(_curPos, System.SR.Xml_ExpectExternalOrClose);
			}
			_nextScaningFunction = ScanningFunction.ClosingTag;
			_scanningFunction = ScanningFunction.SystemId;
			return Token.SYSTEM;
		default:
			Throw(_curPos, System.SR.Xml_ExpectExternalOrPublicId);
			return Token.None;
		}
	}

	private Token ScanSystemId()
	{
		if (_chars[_curPos] != '"' && _chars[_curPos] != '\'')
		{
			ThrowUnexpectedToken(_curPos, "\"", "'");
		}
		ScanLiteral(LiteralType.SystemOrPublicID);
		_scanningFunction = _nextScaningFunction;
		return Token.Literal;
	}

	private Token ScanEntity1()
	{
		if (_chars[_curPos] == '%')
		{
			_curPos++;
			_nextScaningFunction = ScanningFunction.Entity2;
			_scanningFunction = ScanningFunction.Name;
			return Token.Percent;
		}
		ScanName();
		_scanningFunction = ScanningFunction.Entity2;
		return Token.Name;
	}

	private Token ScanEntity2()
	{
		switch (_chars[_curPos])
		{
		case 'P':
			if (!EatPublicKeyword())
			{
				Throw(_curPos, System.SR.Xml_ExpectExternalOrClose);
			}
			_nextScaningFunction = ScanningFunction.Entity3;
			_scanningFunction = ScanningFunction.PublicId1;
			return Token.PUBLIC;
		case 'S':
			if (!EatSystemKeyword())
			{
				Throw(_curPos, System.SR.Xml_ExpectExternalOrClose);
			}
			_nextScaningFunction = ScanningFunction.Entity3;
			_scanningFunction = ScanningFunction.SystemId;
			return Token.SYSTEM;
		case '"':
		case '\'':
			ScanLiteral(LiteralType.EntityReplText);
			_scanningFunction = ScanningFunction.ClosingTag;
			return Token.Literal;
		default:
			Throw(_curPos, System.SR.Xml_ExpectExternalIdOrEntityValue);
			return Token.None;
		}
	}

	private Token ScanEntity3()
	{
		if (_chars[_curPos] == 'N')
		{
			do
			{
				if (_charsUsed - _curPos >= 5)
				{
					if (_chars[_curPos + 1] != 'D' || _chars[_curPos + 2] != 'A' || _chars[_curPos + 3] != 'T' || _chars[_curPos + 4] != 'A')
					{
						break;
					}
					_curPos += 5;
					_scanningFunction = ScanningFunction.Name;
					_nextScaningFunction = ScanningFunction.ClosingTag;
					return Token.NData;
				}
			}
			while (ReadData() != 0);
		}
		_scanningFunction = ScanningFunction.ClosingTag;
		return Token.None;
	}

	private Token ScanPublicId1()
	{
		if (_chars[_curPos] != '"' && _chars[_curPos] != '\'')
		{
			ThrowUnexpectedToken(_curPos, "\"", "'");
		}
		ScanLiteral(LiteralType.SystemOrPublicID);
		_scanningFunction = ScanningFunction.PublicId2;
		return Token.Literal;
	}

	private Token ScanPublicId2()
	{
		if (_chars[_curPos] != '"' && _chars[_curPos] != '\'')
		{
			_scanningFunction = _nextScaningFunction;
			return Token.None;
		}
		ScanLiteral(LiteralType.SystemOrPublicID);
		_scanningFunction = _nextScaningFunction;
		return Token.Literal;
	}

	private Token ScanCondSection1()
	{
		if (_chars[_curPos] != 'I')
		{
			Throw(_curPos, System.SR.Xml_ExpectIgnoreOrInclude);
		}
		_curPos++;
		while (true)
		{
			if (_charsUsed - _curPos >= 5)
			{
				char c = _chars[_curPos];
				if (c == 'G')
				{
					if (_chars[_curPos + 1] != 'N' || _chars[_curPos + 2] != 'O' || _chars[_curPos + 3] != 'R' || _chars[_curPos + 4] != 'E' || XmlCharType.IsNameSingleChar(_chars[_curPos + 5]))
					{
						break;
					}
					_nextScaningFunction = ScanningFunction.CondSection3;
					_scanningFunction = ScanningFunction.CondSection2;
					_curPos += 5;
					return Token.IGNORE;
				}
				if (c != 'N')
				{
					break;
				}
				if (_charsUsed - _curPos >= 6)
				{
					if (_chars[_curPos + 1] != 'C' || _chars[_curPos + 2] != 'L' || _chars[_curPos + 3] != 'U' || _chars[_curPos + 4] != 'D' || _chars[_curPos + 5] != 'E' || XmlCharType.IsNameSingleChar(_chars[_curPos + 6]))
					{
						break;
					}
					_nextScaningFunction = ScanningFunction.SubsetContent;
					_scanningFunction = ScanningFunction.CondSection2;
					_curPos += 6;
					return Token.INCLUDE;
				}
			}
			if (ReadData() == 0)
			{
				Throw(_curPos, System.SR.Xml_IncompleteDtdContent);
			}
		}
		Throw(_curPos - 1, System.SR.Xml_ExpectIgnoreOrInclude);
		return Token.None;
	}

	private Token ScanCondSection2()
	{
		if (_chars[_curPos] != '[')
		{
			ThrowUnexpectedToken(_curPos, "[");
		}
		_curPos++;
		_scanningFunction = _nextScaningFunction;
		return Token.LeftBracket;
	}

	private Token ScanCondSection3()
	{
		int num = 0;
		while (true)
		{
			if (XmlCharType.IsTextChar(_chars[_curPos]) && _chars[_curPos] != ']')
			{
				_curPos++;
				continue;
			}
			switch (_chars[_curPos])
			{
			case '\t':
			case '"':
			case '&':
			case '\'':
				_curPos++;
				continue;
			case '\n':
				_curPos++;
				_readerAdapter.OnNewLine(_curPos);
				continue;
			case '\r':
				if (_chars[_curPos + 1] == '\n')
				{
					_curPos += 2;
				}
				else
				{
					if (_curPos + 1 >= _charsUsed && !_readerAdapter.IsEof)
					{
						break;
					}
					_curPos++;
				}
				_readerAdapter.OnNewLine(_curPos);
				continue;
			case '<':
				if (_charsUsed - _curPos >= 3)
				{
					if (_chars[_curPos + 1] != '!' || _chars[_curPos + 2] != '[')
					{
						_curPos++;
						continue;
					}
					num++;
					_curPos += 3;
					continue;
				}
				break;
			case ']':
				if (_charsUsed - _curPos < 3)
				{
					break;
				}
				if (_chars[_curPos + 1] != ']' || _chars[_curPos + 2] != '>')
				{
					_curPos++;
					continue;
				}
				if (num > 0)
				{
					num--;
					_curPos += 3;
					continue;
				}
				_curPos += 3;
				_scanningFunction = ScanningFunction.SubsetContent;
				return Token.CondSectionEnd;
			default:
			{
				if (_curPos == _charsUsed)
				{
					break;
				}
				char ch = _chars[_curPos];
				if (XmlCharType.IsHighSurrogate(ch))
				{
					if (_curPos + 1 == _charsUsed)
					{
						break;
					}
					_curPos++;
					if (XmlCharType.IsLowSurrogate(_chars[_curPos]))
					{
						_curPos++;
						continue;
					}
				}
				ThrowInvalidChar(_chars, _charsUsed, _curPos);
				return Token.None;
			}
			}
			if (_readerAdapter.IsEof || ReadData() == 0)
			{
				if (HandleEntityEnd(inLiteral: false))
				{
					continue;
				}
				Throw(_curPos, System.SR.Xml_UnclosedConditionalSection);
			}
			_tokenStartPos = _curPos;
		}
	}

	private void ScanName()
	{
		ScanQName(isQName: false);
	}

	private void ScanQName()
	{
		ScanQName(SupportNamespaces);
	}

	private void ScanQName(bool isQName)
	{
		_tokenStartPos = _curPos;
		int num = -1;
		while (true)
		{
			if (XmlCharType.IsStartNCNameSingleChar(_chars[_curPos]) || _chars[_curPos] == ':')
			{
				_curPos++;
			}
			else if (_curPos + 1 >= _charsUsed)
			{
				if (ReadDataInName())
				{
					continue;
				}
				Throw(_curPos, System.SR.Xml_UnexpectedEOF, "Name");
			}
			else
			{
				Throw(_curPos, System.SR.Xml_BadStartNameChar, XmlException.BuildCharExceptionArgs(_chars, _charsUsed, _curPos));
			}
			while (true)
			{
				if (XmlCharType.IsNCNameSingleChar(_chars[_curPos]))
				{
					_curPos++;
					continue;
				}
				if (_chars[_curPos] == ':')
				{
					if (isQName)
					{
						break;
					}
					_curPos++;
					continue;
				}
				if (_curPos == _charsUsed)
				{
					if (ReadDataInName())
					{
						continue;
					}
					if (_tokenStartPos == _curPos)
					{
						Throw(_curPos, System.SR.Xml_UnexpectedEOF, "Name");
					}
				}
				_colonPos = ((num == -1) ? (-1) : (_tokenStartPos + num));
				return;
			}
			if (num != -1)
			{
				Throw(_curPos, System.SR.Xml_BadNameChar, XmlException.BuildCharExceptionArgs(':', '\0'));
			}
			num = _curPos - _tokenStartPos;
			_curPos++;
		}
	}

	private bool ReadDataInName()
	{
		int num = _curPos - _tokenStartPos;
		_curPos = _tokenStartPos;
		bool result = ReadData() != 0;
		_tokenStartPos = _curPos;
		_curPos += num;
		return result;
	}

	private void ScanNmtoken()
	{
		_tokenStartPos = _curPos;
		int num;
		while (true)
		{
			if (XmlCharType.IsNCNameSingleChar(_chars[_curPos]) || _chars[_curPos] == ':')
			{
				_curPos++;
				continue;
			}
			if (_curPos < _charsUsed)
			{
				if (_curPos - _tokenStartPos == 0)
				{
					Throw(_curPos, System.SR.Xml_BadNameChar, XmlException.BuildCharExceptionArgs(_chars, _charsUsed, _curPos));
				}
				return;
			}
			num = _curPos - _tokenStartPos;
			_curPos = _tokenStartPos;
			if (ReadData() == 0)
			{
				if (num > 0)
				{
					break;
				}
				Throw(_curPos, System.SR.Xml_UnexpectedEOF, "NmToken");
			}
			_tokenStartPos = _curPos;
			_curPos += num;
		}
		_tokenStartPos = _curPos;
		_curPos += num;
	}

	private bool EatPublicKeyword()
	{
		while (_charsUsed - _curPos < 6)
		{
			if (ReadData() == 0)
			{
				return false;
			}
		}
		if (_chars[_curPos + 1] != 'U' || _chars[_curPos + 2] != 'B' || _chars[_curPos + 3] != 'L' || _chars[_curPos + 4] != 'I' || _chars[_curPos + 5] != 'C')
		{
			return false;
		}
		_curPos += 6;
		return true;
	}

	private bool EatSystemKeyword()
	{
		while (_charsUsed - _curPos < 6)
		{
			if (ReadData() == 0)
			{
				return false;
			}
		}
		if (_chars[_curPos + 1] != 'Y' || _chars[_curPos + 2] != 'S' || _chars[_curPos + 3] != 'T' || _chars[_curPos + 4] != 'E' || _chars[_curPos + 5] != 'M')
		{
			return false;
		}
		_curPos += 6;
		return true;
	}

	private XmlQualifiedName GetNameQualified(bool canHavePrefix)
	{
		if (_colonPos == -1)
		{
			return new XmlQualifiedName(_nameTable.Add(_chars, _tokenStartPos, _curPos - _tokenStartPos));
		}
		if (canHavePrefix)
		{
			return new XmlQualifiedName(_nameTable.Add(_chars, _colonPos + 1, _curPos - _colonPos - 1), _nameTable.Add(_chars, _tokenStartPos, _colonPos - _tokenStartPos));
		}
		Throw(_tokenStartPos, System.SR.Xml_ColonInLocalName, GetNameString());
		return null;
	}

	private string GetNameString()
	{
		return new string(_chars, _tokenStartPos, _curPos - _tokenStartPos);
	}

	private string GetNmtokenString()
	{
		return GetNameString();
	}

	private string GetValue()
	{
		if (_stringBuilder.Length == 0)
		{
			return new string(_chars, _tokenStartPos, _curPos - _tokenStartPos - 1);
		}
		return _stringBuilder.ToString();
	}

	private string GetValueWithStrippedSpaces()
	{
		string value = ((_stringBuilder.Length == 0) ? new string(_chars, _tokenStartPos, _curPos - _tokenStartPos - 1) : _stringBuilder.ToString());
		return StripSpaces(value);
	}

	private int ReadData()
	{
		SaveParsingBuffer();
		int result = _readerAdapter.ReadData();
		LoadParsingBuffer();
		return result;
	}

	private void LoadParsingBuffer()
	{
		_chars = _readerAdapter.ParsingBuffer;
		_charsUsed = _readerAdapter.ParsingBufferLength;
		_curPos = _readerAdapter.CurrentPosition;
	}

	private void SaveParsingBuffer()
	{
		SaveParsingBuffer(_curPos);
	}

	private void SaveParsingBuffer(int internalSubsetValueEndPos)
	{
		if (SaveInternalSubsetValue)
		{
			int currentPosition = _readerAdapter.CurrentPosition;
			if (internalSubsetValueEndPos - currentPosition > 0)
			{
				_internalSubsetValueSb.Append(_chars, currentPosition, internalSubsetValueEndPos - currentPosition);
			}
		}
		_readerAdapter.CurrentPosition = _curPos;
	}

	private bool HandleEntityReference(bool paramEntity, bool inLiteral, bool inAttribute)
	{
		_curPos++;
		return HandleEntityReference(ScanEntityName(), paramEntity, inLiteral, inAttribute);
	}

	private bool HandleEntityReference(XmlQualifiedName entityName, bool paramEntity, bool inLiteral, bool inAttribute)
	{
		SaveParsingBuffer();
		if (paramEntity && ParsingInternalSubset && !ParsingTopLevelMarkup)
		{
			Throw(_curPos - entityName.Name.Length - 1, System.SR.Xml_InvalidParEntityRef);
		}
		SchemaEntity schemaEntity = VerifyEntityReference(entityName, paramEntity, mustBeDeclared: true, inAttribute);
		if (schemaEntity == null)
		{
			return false;
		}
		if (schemaEntity.ParsingInProgress)
		{
			Throw(_curPos - entityName.Name.Length - 1, paramEntity ? System.SR.Xml_RecursiveParEntity : System.SR.Xml_RecursiveGenEntity, entityName.Name);
		}
		int entityId;
		if (schemaEntity.IsExternal)
		{
			if (!_readerAdapter.PushEntity(schemaEntity, out entityId))
			{
				return false;
			}
			_externalEntitiesDepth++;
		}
		else
		{
			if (schemaEntity.Text.Length == 0)
			{
				return false;
			}
			if (!_readerAdapter.PushEntity(schemaEntity, out entityId))
			{
				return false;
			}
		}
		_currentEntityId = entityId;
		if (paramEntity && !inLiteral && _scanningFunction != ScanningFunction.ParamEntitySpace)
		{
			_savedScanningFunction = _scanningFunction;
			_scanningFunction = ScanningFunction.ParamEntitySpace;
		}
		LoadParsingBuffer();
		return true;
	}

	private bool HandleEntityEnd(bool inLiteral)
	{
		SaveParsingBuffer();
		if (!_readerAdapter.PopEntity(out var oldEntity, out _currentEntityId))
		{
			return false;
		}
		LoadParsingBuffer();
		if (oldEntity == null)
		{
			if (_scanningFunction == ScanningFunction.ParamEntitySpace)
			{
				_scanningFunction = _savedScanningFunction;
			}
			return false;
		}
		if (oldEntity.IsExternal)
		{
			_externalEntitiesDepth--;
		}
		if (!inLiteral && _scanningFunction != ScanningFunction.ParamEntitySpace)
		{
			_savedScanningFunction = _scanningFunction;
			_scanningFunction = ScanningFunction.ParamEntitySpace;
		}
		return true;
	}

	private SchemaEntity VerifyEntityReference(XmlQualifiedName entityName, bool paramEntity, bool mustBeDeclared, bool inAttribute)
	{
		SchemaEntity value;
		if (paramEntity)
		{
			_schemaInfo.ParameterEntities.TryGetValue(entityName, out value);
		}
		else
		{
			_schemaInfo.GeneralEntities.TryGetValue(entityName, out value);
		}
		if (value == null)
		{
			if (paramEntity)
			{
				if (_validate)
				{
					SendValidationEvent(_curPos - entityName.Name.Length - 1, XmlSeverityType.Error, System.SR.Xml_UndeclaredParEntity, entityName.Name);
				}
			}
			else if (mustBeDeclared)
			{
				if (!ParsingInternalSubset)
				{
					if (_validate)
					{
						SendValidationEvent(_curPos - entityName.Name.Length - 1, XmlSeverityType.Error, System.SR.Xml_UndeclaredEntity, entityName.Name);
					}
				}
				else
				{
					Throw(_curPos - entityName.Name.Length - 1, System.SR.Xml_UndeclaredEntity, entityName.Name);
				}
			}
			return null;
		}
		if (!value.NData.IsEmpty)
		{
			Throw(_curPos - entityName.Name.Length - 1, System.SR.Xml_UnparsedEntityRef, entityName.Name);
		}
		if (inAttribute && value.IsExternal)
		{
			Throw(_curPos - entityName.Name.Length - 1, System.SR.Xml_ExternalEntityInAttValue, entityName.Name);
		}
		return value;
	}

	private void SendValidationEvent(int pos, XmlSeverityType severity, string code, string arg)
	{
		SendValidationEvent(severity, new XmlSchemaException(code, arg, BaseUriStr, LineNo, LinePos + (pos - _curPos)));
	}

	private void SendValidationEvent(XmlSeverityType severity, string code, string arg)
	{
		SendValidationEvent(severity, new XmlSchemaException(code, arg, BaseUriStr, LineNo, LinePos));
	}

	private void SendValidationEvent(XmlSeverityType severity, XmlSchemaException e)
	{
		_readerAdapterWithValidation.ValidationEventHandling?.SendEvent(e, severity);
	}

	private bool IsAttributeValueType(Token token)
	{
		if (token >= Token.CDATA)
		{
			return token <= Token.NOTATION;
		}
		return false;
	}

	private void OnUnexpectedError()
	{
		Throw(_curPos, System.SR.Xml_InternalError);
	}

	private void Throw(int curPos, string res)
	{
		Throw(curPos, res, string.Empty);
	}

	[DoesNotReturn]
	private void Throw(int curPos, string res, string arg)
	{
		_curPos = curPos;
		Uri baseUri = _readerAdapter.BaseUri;
		_readerAdapter.Throw(new XmlException(res, arg, LineNo, LinePos, (baseUri == null) ? null : baseUri.ToString()));
	}

	[DoesNotReturn]
	private void Throw(int curPos, string res, string[] args)
	{
		_curPos = curPos;
		Uri baseUri = _readerAdapter.BaseUri;
		_readerAdapter.Throw(new XmlException(res, args, LineNo, LinePos, (baseUri == null) ? null : baseUri.ToString()));
	}

	[DoesNotReturn]
	private void Throw(string res, string arg, int lineNo, int linePos)
	{
		Uri baseUri = _readerAdapter.BaseUri;
		_readerAdapter.Throw(new XmlException(res, arg, lineNo, linePos, (baseUri == null) ? null : baseUri.ToString()));
	}

	private void ThrowInvalidChar(int pos, string data, int invCharPos)
	{
		Throw(pos, System.SR.Xml_InvalidCharacter, XmlException.BuildCharExceptionArgs(data, invCharPos));
	}

	private void ThrowInvalidChar(char[] data, int length, int invCharPos)
	{
		Throw(invCharPos, System.SR.Xml_InvalidCharacter, XmlException.BuildCharExceptionArgs(data, length, invCharPos));
	}

	private void ThrowUnexpectedToken(int pos, string expectedToken)
	{
		ThrowUnexpectedToken(pos, expectedToken, null);
	}

	private void ThrowUnexpectedToken(int pos, string expectedToken1, string expectedToken2)
	{
		string text = ParseUnexpectedToken(pos);
		if (expectedToken2 != null)
		{
			Throw(_curPos, System.SR.Xml_UnexpectedTokens2, new string[3] { text, expectedToken1, expectedToken2 });
		}
		else
		{
			Throw(_curPos, System.SR.Xml_UnexpectedTokenEx, new string[2] { text, expectedToken1 });
		}
	}

	private string ParseUnexpectedToken(int startPos)
	{
		if (XmlCharType.IsNCNameSingleChar(_chars[startPos]))
		{
			int i;
			for (i = startPos; XmlCharType.IsNCNameSingleChar(_chars[i]); i++)
			{
			}
			int num = i - startPos;
			return new string(_chars, startPos, (num <= 0) ? 1 : num);
		}
		return new string(_chars, startPos, 1);
	}

	internal static string StripSpaces(string value)
	{
		int length = value.Length;
		if (length <= 0)
		{
			return string.Empty;
		}
		int num = 0;
		StringBuilder stringBuilder = null;
		while (value[num] == ' ')
		{
			num++;
			if (num == length)
			{
				return " ";
			}
		}
		int i;
		for (i = num; i < length; i++)
		{
			if (value[i] != ' ')
			{
				continue;
			}
			int j;
			for (j = i + 1; j < length && value[j] == ' '; j++)
			{
			}
			if (j == length)
			{
				if (stringBuilder == null)
				{
					return value.Substring(num, i - num);
				}
				stringBuilder.Append(value, num, i - num);
				return stringBuilder.ToString();
			}
			if (j > i + 1)
			{
				if (stringBuilder == null)
				{
					stringBuilder = new StringBuilder(length);
				}
				stringBuilder.Append(value, num, i - num + 1);
				num = j;
				i = j - 1;
			}
		}
		if (stringBuilder == null)
		{
			if (num != 0)
			{
				return value.Substring(num, length - num);
			}
			return value;
		}
		if (i > num)
		{
			stringBuilder.Append(value, num, i - num);
		}
		return stringBuilder.ToString();
	}

	async Task<IDtdInfo> IDtdParser.ParseInternalDtdAsync(IDtdParserAdapter adapter, bool saveInternalSubset)
	{
		Initialize(adapter);
		await ParseAsync(saveInternalSubset).ConfigureAwait(continueOnCapturedContext: false);
		return _schemaInfo;
	}

	async Task<IDtdInfo> IDtdParser.ParseFreeFloatingDtdAsync(string baseUri, string docTypeName, string publicId, string systemId, string internalSubset, IDtdParserAdapter adapter)
	{
		InitializeFreeFloatingDtd(baseUri, docTypeName, publicId, systemId, internalSubset, adapter);
		await ParseAsync(saveInternalSubset: false).ConfigureAwait(continueOnCapturedContext: false);
		return _schemaInfo;
	}

	private async Task ParseAsync(bool saveInternalSubset)
	{
		if (!_freeFloatingDtd)
		{
			await ParseInDocumentDtdAsync(saveInternalSubset).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			await ParseFreeFloatingDtdAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		_schemaInfo.Finish();
		if (!_validate || _undeclaredNotations == null)
		{
			return;
		}
		foreach (UndeclaredNotation value in _undeclaredNotations.Values)
		{
			for (UndeclaredNotation undeclaredNotation = value; undeclaredNotation != null; undeclaredNotation = undeclaredNotation.next)
			{
				SendValidationEvent(XmlSeverityType.Error, new XmlSchemaException(System.SR.Sch_UndeclaredNotation, value.name, BaseUriStr, value.lineNo, value.linePos));
			}
		}
	}

	private async Task ParseInDocumentDtdAsync(bool saveInternalSubset)
	{
		LoadParsingBuffer();
		_scanningFunction = ScanningFunction.QName;
		_nextScaningFunction = ScanningFunction.Doctype1;
		if (await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false) != Token.QName)
		{
			OnUnexpectedError();
		}
		_schemaInfo.DocTypeName = GetNameQualified(canHavePrefix: true);
		Token token = await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false);
		if (token == Token.SYSTEM || token == Token.PUBLIC)
		{
			(string, string) tuple = await ParseExternalIdAsync(token, Token.DOCTYPE).ConfigureAwait(continueOnCapturedContext: false);
			_publicId = tuple.Item1;
			_systemId = tuple.Item2;
			token = await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false);
		}
		switch (token)
		{
		case Token.LeftBracket:
			if (saveInternalSubset)
			{
				SaveParsingBuffer();
				_internalSubsetValueSb = new StringBuilder();
			}
			await ParseInternalSubsetAsync().ConfigureAwait(continueOnCapturedContext: false);
			break;
		default:
			OnUnexpectedError();
			break;
		case Token.GreaterThan:
			break;
		}
		SaveParsingBuffer();
		if (_systemId != null && _systemId.Length > 0)
		{
			await ParseExternalSubsetAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private async Task ParseFreeFloatingDtdAsync()
	{
		if (_hasFreeFloatingInternalSubset)
		{
			LoadParsingBuffer();
			await ParseInternalSubsetAsync().ConfigureAwait(continueOnCapturedContext: false);
			SaveParsingBuffer();
		}
		if (_systemId != null && _systemId.Length > 0)
		{
			await ParseExternalSubsetAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private Task ParseInternalSubsetAsync()
	{
		return ParseSubsetAsync();
	}

	private async Task ParseExternalSubsetAsync()
	{
		if (await _readerAdapter.PushExternalSubsetAsync(_systemId, _publicId).ConfigureAwait(continueOnCapturedContext: false))
		{
			Uri baseUri = _readerAdapter.BaseUri;
			if (baseUri != null)
			{
				_externalDtdBaseUri = baseUri.ToString();
			}
			_externalEntitiesDepth++;
			LoadParsingBuffer();
			await ParseSubsetAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private async Task ParseSubsetAsync()
	{
		while (true)
		{
			Token token = await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false);
			int startTagEntityId = _currentEntityId;
			switch (token)
			{
			case Token.AttlistDecl:
				await ParseAttlistDeclAsync().ConfigureAwait(continueOnCapturedContext: false);
				break;
			case Token.ElementDecl:
				await ParseElementDeclAsync().ConfigureAwait(continueOnCapturedContext: false);
				break;
			case Token.EntityDecl:
				await ParseEntityDeclAsync().ConfigureAwait(continueOnCapturedContext: false);
				break;
			case Token.NotationDecl:
				await ParseNotationDeclAsync().ConfigureAwait(continueOnCapturedContext: false);
				break;
			case Token.Comment:
				await ParseCommentAsync().ConfigureAwait(continueOnCapturedContext: false);
				break;
			case Token.PI:
				await ParsePIAsync().ConfigureAwait(continueOnCapturedContext: false);
				break;
			case Token.CondSectionStart:
				if (ParsingInternalSubset)
				{
					Throw(_curPos - 3, System.SR.Xml_InvalidConditionalSection);
				}
				await ParseCondSectionAsync().ConfigureAwait(continueOnCapturedContext: false);
				startTagEntityId = _currentEntityId;
				break;
			case Token.CondSectionEnd:
				if (_condSectionDepth > 0)
				{
					_condSectionDepth--;
					if (_validate && _currentEntityId != _condSectionEntityIds[_condSectionDepth])
					{
						SendValidationEvent(_curPos, XmlSeverityType.Error, System.SR.Sch_ParEntityRefNesting, string.Empty);
					}
				}
				else
				{
					Throw(_curPos - 3, System.SR.Xml_UnexpectedCDataEnd);
				}
				break;
			case Token.RightBracket:
				if (ParsingInternalSubset)
				{
					if (_condSectionDepth != 0)
					{
						Throw(_curPos, System.SR.Xml_UnclosedConditionalSection);
					}
					if (_internalSubsetValueSb != null)
					{
						SaveParsingBuffer(_curPos - 1);
						_schemaInfo.InternalDtdSubset = _internalSubsetValueSb.ToString();
						_internalSubsetValueSb = null;
					}
					if (await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false) != Token.GreaterThan)
					{
						ThrowUnexpectedToken(_curPos, ">");
					}
				}
				else
				{
					Throw(_curPos, System.SR.Xml_ExpectDtdMarkup);
				}
				return;
			case Token.Eof:
				if (ParsingInternalSubset && !_freeFloatingDtd)
				{
					Throw(_curPos, System.SR.Xml_IncompleteDtdContent);
				}
				if (_condSectionDepth != 0)
				{
					Throw(_curPos, System.SR.Xml_UnclosedConditionalSection);
				}
				return;
			}
			if (_currentEntityId != startTagEntityId)
			{
				if (_validate)
				{
					SendValidationEvent(_curPos, XmlSeverityType.Error, System.SR.Sch_ParEntityRefNesting, string.Empty);
				}
				else if (!_v1Compat)
				{
					Throw(_curPos, System.SR.Sch_ParEntityRefNesting);
				}
			}
		}
	}

	private async Task ParseAttlistDeclAsync()
	{
		if (await GetTokenAsync(needWhiteSpace: true).ConfigureAwait(continueOnCapturedContext: false) == Token.QName)
		{
			XmlQualifiedName nameQualified = GetNameQualified(canHavePrefix: true);
			if (!_schemaInfo.ElementDecls.TryGetValue(nameQualified, out var elementDecl) && !_schemaInfo.UndeclaredElementDecls.TryGetValue(nameQualified, out elementDecl))
			{
				elementDecl = new SchemaElementDecl(nameQualified, nameQualified.Namespace);
				_schemaInfo.UndeclaredElementDecls.Add(nameQualified, elementDecl);
			}
			SchemaAttDef attrDef = null;
			while (true)
			{
				switch (await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false))
				{
				case Token.QName:
				{
					XmlQualifiedName nameQualified2 = GetNameQualified(canHavePrefix: true);
					attrDef = new SchemaAttDef(nameQualified2, nameQualified2.Namespace)
					{
						IsDeclaredInExternal = !ParsingInternalSubset,
						LineNumber = LineNo,
						LinePosition = LinePos - (_curPos - _tokenStartPos)
					};
					bool attrDefAlreadyExists = elementDecl.GetAttDef(attrDef.Name) != null;
					await ParseAttlistTypeAsync(attrDef, elementDecl, attrDefAlreadyExists).ConfigureAwait(continueOnCapturedContext: false);
					await ParseAttlistDefaultAsync(attrDef, attrDefAlreadyExists).ConfigureAwait(continueOnCapturedContext: false);
					if (attrDef.Prefix.Length > 0 && attrDef.Prefix.Equals("xml"))
					{
						if (attrDef.Name.Name == "space")
						{
							if (_v1Compat)
							{
								string text = attrDef.DefaultValueExpanded.Trim();
								if (text.Equals("preserve") || text.Equals("default"))
								{
									attrDef.Reserved = SchemaAttDef.Reserve.XmlSpace;
								}
							}
							else
							{
								attrDef.Reserved = SchemaAttDef.Reserve.XmlSpace;
								if (attrDef.TokenizedType != XmlTokenizedType.ENUMERATION)
								{
									Throw(System.SR.Xml_EnumerationRequired, string.Empty, attrDef.LineNumber, attrDef.LinePosition);
								}
								if (_validate)
								{
									attrDef.CheckXmlSpace(_readerAdapterWithValidation.ValidationEventHandling);
								}
							}
						}
						else if (attrDef.Name.Name == "lang")
						{
							attrDef.Reserved = SchemaAttDef.Reserve.XmlLang;
						}
					}
					if (!attrDefAlreadyExists)
					{
						elementDecl.AddAttDef(attrDef);
					}
					continue;
				}
				case Token.GreaterThan:
					if (_v1Compat && attrDef != null && attrDef.Prefix.Length > 0 && attrDef.Prefix.Equals("xml") && attrDef.Name.Name == "space")
					{
						attrDef.Reserved = SchemaAttDef.Reserve.XmlSpace;
						if (attrDef.Datatype.TokenizedType != XmlTokenizedType.ENUMERATION)
						{
							Throw(System.SR.Xml_EnumerationRequired, string.Empty, attrDef.LineNumber, attrDef.LinePosition);
						}
						if (_validate)
						{
							attrDef.CheckXmlSpace(_readerAdapterWithValidation.ValidationEventHandling);
						}
					}
					return;
				}
				break;
			}
		}
		OnUnexpectedError();
	}

	private async Task ParseAttlistTypeAsync(SchemaAttDef attrDef, SchemaElementDecl elementDecl, bool ignoreErrors)
	{
		Token token = await GetTokenAsync(needWhiteSpace: true).ConfigureAwait(continueOnCapturedContext: false);
		if (token != 0)
		{
			elementDecl.HasNonCDataAttribute = true;
		}
		if (IsAttributeValueType(token))
		{
			attrDef.TokenizedType = (XmlTokenizedType)token;
			attrDef.SchemaType = XmlSchemaType.GetBuiltInSimpleType(attrDef.Datatype.TypeCode);
			switch (token)
			{
			default:
				return;
			case Token.ID:
				if (_validate && elementDecl.IsIdDeclared)
				{
					SchemaAttDef attDef = elementDecl.GetAttDef(attrDef.Name);
					if ((attDef == null || attDef.Datatype.TokenizedType != XmlTokenizedType.ID) && !ignoreErrors)
					{
						SendValidationEvent(XmlSeverityType.Error, System.SR.Sch_IdAttrDeclared, elementDecl.Name.ToString());
					}
				}
				elementDecl.IsIdDeclared = true;
				return;
			case Token.NOTATION:
				break;
			}
			if (_validate)
			{
				if (elementDecl.IsNotationDeclared && !ignoreErrors)
				{
					SendValidationEvent(_curPos - 8, XmlSeverityType.Error, System.SR.Sch_DupNotationAttribute, elementDecl.Name.ToString());
				}
				else
				{
					if (elementDecl.ContentValidator != null && elementDecl.ContentValidator.ContentType == XmlSchemaContentType.Empty && !ignoreErrors)
					{
						SendValidationEvent(_curPos - 8, XmlSeverityType.Error, System.SR.Sch_NotationAttributeOnEmptyElement, elementDecl.Name.ToString());
					}
					elementDecl.IsNotationDeclared = true;
				}
			}
			if (await GetTokenAsync(needWhiteSpace: true).ConfigureAwait(continueOnCapturedContext: false) == Token.LeftParen && await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false) == Token.Name)
			{
				do
				{
					string nameString = GetNameString();
					if (!_schemaInfo.Notations.ContainsKey(nameString))
					{
						AddUndeclaredNotation(nameString);
					}
					if (_validate && !_v1Compat && attrDef.Values != null && attrDef.Values.Contains(nameString) && !ignoreErrors)
					{
						SendValidationEvent(XmlSeverityType.Error, new XmlSchemaException(System.SR.Xml_AttlistDuplNotationValue, nameString, BaseUriStr, LineNo, LinePos));
					}
					attrDef.AddValue(nameString);
					switch (await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false))
					{
					case Token.Or:
						continue;
					case Token.RightParen:
						return;
					}
					break;
				}
				while (await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false) == Token.Name);
			}
		}
		else if (token == Token.LeftParen)
		{
			attrDef.TokenizedType = XmlTokenizedType.ENUMERATION;
			attrDef.SchemaType = XmlSchemaType.GetBuiltInSimpleType(attrDef.Datatype.TypeCode);
			if (await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false) == Token.Nmtoken)
			{
				attrDef.AddValue(GetNameString());
				while (true)
				{
					string nmtokenString;
					switch (await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false))
					{
					case Token.Or:
						if (await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false) == Token.Nmtoken)
						{
							nmtokenString = GetNmtokenString();
							if (_validate && !_v1Compat && attrDef.Values != null && attrDef.Values.Contains(nmtokenString) && !ignoreErrors)
							{
								SendValidationEvent(XmlSeverityType.Error, new XmlSchemaException(System.SR.Xml_AttlistDuplEnumValue, nmtokenString, BaseUriStr, LineNo, LinePos));
							}
							goto IL_068c;
						}
						break;
					case Token.RightParen:
						return;
					}
					break;
					IL_068c:
					attrDef.AddValue(nmtokenString);
				}
			}
		}
		OnUnexpectedError();
	}

	private async Task ParseAttlistDefaultAsync(SchemaAttDef attrDef, bool ignoreErrors)
	{
		switch (await GetTokenAsync(needWhiteSpace: true).ConfigureAwait(continueOnCapturedContext: false))
		{
		case Token.REQUIRED:
			attrDef.Presence = SchemaDeclBase.Use.Required;
			return;
		case Token.IMPLIED:
			attrDef.Presence = SchemaDeclBase.Use.Implied;
			return;
		case Token.FIXED:
			attrDef.Presence = SchemaDeclBase.Use.Fixed;
			if (await GetTokenAsync(needWhiteSpace: true).ConfigureAwait(continueOnCapturedContext: false) != Token.Literal)
			{
				break;
			}
			goto case Token.Literal;
		case Token.Literal:
			if (_validate && attrDef.Datatype.TokenizedType == XmlTokenizedType.ID && !ignoreErrors)
			{
				SendValidationEvent(_curPos, XmlSeverityType.Error, System.SR.Sch_AttListPresence, string.Empty);
			}
			if (attrDef.TokenizedType != 0)
			{
				attrDef.DefaultValueExpanded = GetValueWithStrippedSpaces();
			}
			else
			{
				attrDef.DefaultValueExpanded = GetValue();
			}
			attrDef.ValueLineNumber = _literalLineInfo.lineNo;
			attrDef.ValueLinePosition = _literalLineInfo.linePos + 1;
			DtdValidator.SetDefaultTypedValue(attrDef, _readerAdapter);
			return;
		}
		OnUnexpectedError();
	}

	private async Task ParseElementDeclAsync()
	{
		if (await GetTokenAsync(needWhiteSpace: true).ConfigureAwait(continueOnCapturedContext: false) == Token.QName)
		{
			SchemaElementDecl elementDecl = null;
			XmlQualifiedName nameQualified = GetNameQualified(canHavePrefix: true);
			if (_schemaInfo.ElementDecls.TryGetValue(nameQualified, out elementDecl))
			{
				if (_validate)
				{
					SendValidationEvent(_curPos - nameQualified.Name.Length, XmlSeverityType.Error, System.SR.Sch_DupElementDecl, GetNameString());
				}
			}
			else
			{
				if (_schemaInfo.UndeclaredElementDecls.TryGetValue(nameQualified, out elementDecl))
				{
					_schemaInfo.UndeclaredElementDecls.Remove(nameQualified);
				}
				else
				{
					elementDecl = new SchemaElementDecl(nameQualified, nameQualified.Namespace);
				}
				_schemaInfo.ElementDecls.Add(nameQualified, elementDecl);
			}
			elementDecl.IsDeclaredInExternal = !ParsingInternalSubset;
			Token token = await GetTokenAsync(needWhiteSpace: true).ConfigureAwait(continueOnCapturedContext: false);
			if (token != Token.LeftParen)
			{
				if (token != Token.ANY)
				{
					if (token != Token.EMPTY)
					{
						goto IL_0483;
					}
					elementDecl.ContentValidator = ContentValidator.Empty;
				}
				else
				{
					elementDecl.ContentValidator = ContentValidator.Any;
				}
			}
			else
			{
				int startParenEntityId = _currentEntityId;
				Token token2 = await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false);
				if (token2 != Token.None)
				{
					if (token2 != Token.PCDATA)
					{
						goto IL_0483;
					}
					ParticleContentValidator pcv = new ParticleContentValidator(XmlSchemaContentType.Mixed);
					pcv.Start();
					pcv.OpenGroup();
					await ParseElementMixedContentAsync(pcv, startParenEntityId).ConfigureAwait(continueOnCapturedContext: false);
					elementDecl.ContentValidator = pcv.Finish(useDFA: true);
				}
				else
				{
					ParticleContentValidator pcv = new ParticleContentValidator(XmlSchemaContentType.ElementOnly);
					pcv.Start();
					pcv.OpenGroup();
					await ParseElementOnlyContentAsync(pcv, startParenEntityId).ConfigureAwait(continueOnCapturedContext: false);
					elementDecl.ContentValidator = pcv.Finish(useDFA: true);
				}
			}
			if (await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false) != Token.GreaterThan)
			{
				ThrowUnexpectedToken(_curPos, ">");
			}
			return;
		}
		goto IL_0483;
		IL_0483:
		OnUnexpectedError();
	}

	private async Task ParseElementOnlyContentAsync(ParticleContentValidator pcv, int startParenEntityId)
	{
		Stack<ParseElementOnlyContent_LocalFrame> localFrames = new Stack<ParseElementOnlyContent_LocalFrame>();
		ParseElementOnlyContent_LocalFrame currentFrame = new ParseElementOnlyContent_LocalFrame(startParenEntityId);
		localFrames.Push(currentFrame);
		while (true)
		{
			Token token = await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false);
			if (token != Token.QName)
			{
				if (token != Token.LeftParen)
				{
					if (token != Token.GreaterThan)
					{
						goto IL_036a;
					}
					Throw(_curPos, System.SR.Xml_InvalidContentModel);
					goto IL_0370;
				}
				pcv.OpenGroup();
				currentFrame = new ParseElementOnlyContent_LocalFrame(_currentEntityId);
				localFrames.Push(currentFrame);
				continue;
			}
			pcv.AddName(GetNameQualified(canHavePrefix: true), null);
			await ParseHowManyAsync(pcv).ConfigureAwait(continueOnCapturedContext: false);
			goto IL_01a2;
			IL_036a:
			OnUnexpectedError();
			goto IL_0370;
			IL_02aa:
			pcv.CloseGroup();
			if (_validate && _currentEntityId != currentFrame.startParenEntityId)
			{
				SendValidationEvent(_curPos, XmlSeverityType.Error, System.SR.Sch_ParEntityRefNesting, string.Empty);
			}
			await ParseHowManyAsync(pcv).ConfigureAwait(continueOnCapturedContext: false);
			goto IL_0370;
			IL_026d:
			if (currentFrame.parsingSchema == Token.Comma)
			{
				Throw(_curPos, System.SR.Xml_InvalidContentModel);
			}
			pcv.AddChoice();
			currentFrame.parsingSchema = Token.Or;
			continue;
			IL_0370:
			localFrames.Pop();
			if (localFrames.Count > 0)
			{
				currentFrame = localFrames.Peek();
				goto IL_01a2;
			}
			break;
			IL_0357:
			Throw(_curPos, System.SR.Xml_InvalidContentModel);
			goto IL_0370;
			IL_01a2:
			switch (await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false))
			{
			case Token.Comma:
				break;
			case Token.Or:
				goto IL_026d;
			case Token.RightParen:
				goto IL_02aa;
			case Token.GreaterThan:
				goto IL_0357;
			default:
				goto IL_036a;
			}
			if (currentFrame.parsingSchema == Token.Or)
			{
				Throw(_curPos, System.SR.Xml_InvalidContentModel);
			}
			pcv.AddSequence();
			currentFrame.parsingSchema = Token.Comma;
		}
	}

	private async Task ParseHowManyAsync(ParticleContentValidator pcv)
	{
		switch (await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false))
		{
		case Token.Star:
			pcv.AddStar();
			break;
		case Token.QMark:
			pcv.AddQMark();
			break;
		case Token.Plus:
			pcv.AddPlus();
			break;
		}
	}

	private async Task ParseElementMixedContentAsync(ParticleContentValidator pcv, int startParenEntityId)
	{
		bool hasNames = false;
		int connectorEntityId = -1;
		int contentEntityId = _currentEntityId;
		while (true)
		{
			switch (await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false))
			{
			case Token.RightParen:
				pcv.CloseGroup();
				if (_validate && _currentEntityId != startParenEntityId)
				{
					SendValidationEvent(_curPos, XmlSeverityType.Error, System.SR.Sch_ParEntityRefNesting, string.Empty);
				}
				if (await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false) == Token.Star && hasNames)
				{
					pcv.AddStar();
				}
				else if (hasNames)
				{
					ThrowUnexpectedToken(_curPos, "*");
				}
				return;
			case Token.Or:
			{
				if (!hasNames)
				{
					hasNames = true;
				}
				else
				{
					pcv.AddChoice();
				}
				if (_validate)
				{
					connectorEntityId = _currentEntityId;
					if (contentEntityId < connectorEntityId)
					{
						SendValidationEvent(_curPos, XmlSeverityType.Error, System.SR.Sch_ParEntityRefNesting, string.Empty);
					}
				}
				if (await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false) != Token.QName)
				{
					break;
				}
				XmlQualifiedName nameQualified = GetNameQualified(canHavePrefix: true);
				if (pcv.Exists(nameQualified) && _validate)
				{
					SendValidationEvent(XmlSeverityType.Error, System.SR.Sch_DupElement, nameQualified.ToString());
				}
				pcv.AddName(nameQualified, null);
				if (_validate)
				{
					contentEntityId = _currentEntityId;
					if (contentEntityId < connectorEntityId)
					{
						SendValidationEvent(_curPos, XmlSeverityType.Error, System.SR.Sch_ParEntityRefNesting, string.Empty);
					}
				}
				continue;
			}
			}
			OnUnexpectedError();
		}
	}

	private async Task ParseEntityDeclAsync()
	{
		bool isParamEntity = false;
		Token token = await GetTokenAsync(needWhiteSpace: true).ConfigureAwait(continueOnCapturedContext: false);
		if (token == Token.Name)
		{
			goto IL_013a;
		}
		if (token == Token.Percent)
		{
			isParamEntity = true;
			if (await GetTokenAsync(needWhiteSpace: true).ConfigureAwait(continueOnCapturedContext: false) == Token.Name)
			{
				goto IL_013a;
			}
		}
		goto IL_0552;
		IL_013a:
		XmlQualifiedName nameQualified = GetNameQualified(canHavePrefix: false);
		SchemaEntity entity = new SchemaEntity(nameQualified, isParamEntity)
		{
			BaseURI = BaseUriStr,
			DeclaredURI = ((_externalDtdBaseUri.Length == 0) ? _documentBaseUri : _externalDtdBaseUri)
		};
		if (isParamEntity)
		{
			if (!_schemaInfo.ParameterEntities.ContainsKey(nameQualified))
			{
				_schemaInfo.ParameterEntities.Add(nameQualified, entity);
			}
		}
		else if (!_schemaInfo.GeneralEntities.ContainsKey(nameQualified))
		{
			_schemaInfo.GeneralEntities.Add(nameQualified, entity);
		}
		entity.DeclaredInExternal = !ParsingInternalSubset;
		entity.ParsingInProgress = true;
		Token token2 = await GetTokenAsync(needWhiteSpace: true).ConfigureAwait(continueOnCapturedContext: false);
		if ((uint)(token2 - 33) > 1u)
		{
			if (token2 != Token.Literal)
			{
				goto IL_0552;
			}
			entity.Text = GetValue();
			entity.Line = _literalLineInfo.lineNo;
			entity.Pos = _literalLineInfo.linePos;
		}
		else
		{
			(string, string) tuple = await ParseExternalIdAsync(token2, Token.EntityDecl).ConfigureAwait(continueOnCapturedContext: false);
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			entity.IsExternal = true;
			entity.Url = item2;
			entity.Pubid = item;
			if (await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false) == Token.NData)
			{
				if (isParamEntity)
				{
					ThrowUnexpectedToken(_curPos - 5, ">");
				}
				if (!_whitespaceSeen)
				{
					Throw(_curPos - 5, System.SR.Xml_ExpectingWhiteSpace, "NDATA");
				}
				if (await GetTokenAsync(needWhiteSpace: true).ConfigureAwait(continueOnCapturedContext: false) != Token.Name)
				{
					goto IL_0552;
				}
				entity.NData = GetNameQualified(canHavePrefix: false);
				string name = entity.NData.Name;
				if (!_schemaInfo.Notations.ContainsKey(name))
				{
					AddUndeclaredNotation(name);
				}
			}
		}
		if (await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false) == Token.GreaterThan)
		{
			entity.ParsingInProgress = false;
			return;
		}
		goto IL_0552;
		IL_0552:
		OnUnexpectedError();
	}

	private async Task ParseNotationDeclAsync()
	{
		if (await GetTokenAsync(needWhiteSpace: true).ConfigureAwait(continueOnCapturedContext: false) != Token.Name)
		{
			OnUnexpectedError();
		}
		XmlQualifiedName nameQualified = GetNameQualified(canHavePrefix: false);
		SchemaNotation notation = null;
		if (!_schemaInfo.Notations.ContainsKey(nameQualified.Name))
		{
			if (_undeclaredNotations != null)
			{
				_undeclaredNotations.Remove(nameQualified.Name);
			}
			notation = new SchemaNotation(nameQualified);
			_schemaInfo.Notations.Add(notation.Name.Name, notation);
		}
		else if (_validate)
		{
			SendValidationEvent(_curPos - nameQualified.Name.Length, XmlSeverityType.Error, System.SR.Sch_DupNotation, nameQualified.Name);
		}
		Token token = await GetTokenAsync(needWhiteSpace: true).ConfigureAwait(continueOnCapturedContext: false);
		if (token == Token.SYSTEM || token == Token.PUBLIC)
		{
			var (pubid, systemLiteral) = await ParseExternalIdAsync(token, Token.NOTATION).ConfigureAwait(continueOnCapturedContext: false);
			if (notation != null)
			{
				notation.SystemLiteral = systemLiteral;
				notation.Pubid = pubid;
			}
		}
		else
		{
			OnUnexpectedError();
		}
		if (await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false) != Token.GreaterThan)
		{
			OnUnexpectedError();
		}
	}

	private async Task ParseCommentAsync()
	{
		SaveParsingBuffer();
		try
		{
			if (!SaveInternalSubsetValue)
			{
				await _readerAdapter.ParseCommentAsync(null).ConfigureAwait(continueOnCapturedContext: false);
			}
			else
			{
				await _readerAdapter.ParseCommentAsync(_internalSubsetValueSb).ConfigureAwait(continueOnCapturedContext: false);
				_internalSubsetValueSb.Append("-->");
			}
		}
		catch (XmlException ex)
		{
			if (!(ex.ResString == System.SR.Xml_UnexpectedEOF) || _currentEntityId == 0)
			{
				throw;
			}
			SendValidationEvent(XmlSeverityType.Error, System.SR.Sch_ParEntityRefNesting, null);
		}
		LoadParsingBuffer();
	}

	private async Task ParsePIAsync()
	{
		SaveParsingBuffer();
		if (!SaveInternalSubsetValue)
		{
			await _readerAdapter.ParsePIAsync(null).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			await _readerAdapter.ParsePIAsync(_internalSubsetValueSb).ConfigureAwait(continueOnCapturedContext: false);
			_internalSubsetValueSb.Append("?>");
		}
		LoadParsingBuffer();
	}

	private async Task ParseCondSectionAsync()
	{
		int csEntityId = _currentEntityId;
		switch (await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false))
		{
		case Token.INCLUDE:
			if (await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false) == Token.LeftBracket)
			{
				if (_validate && csEntityId != _currentEntityId)
				{
					SendValidationEvent(_curPos, XmlSeverityType.Error, System.SR.Sch_ParEntityRefNesting, string.Empty);
				}
				if (_validate)
				{
					if (_condSectionEntityIds == null)
					{
						_condSectionEntityIds = new int[2];
					}
					else if (_condSectionEntityIds.Length == _condSectionDepth)
					{
						int[] array = new int[_condSectionEntityIds.Length * 2];
						Array.Copy(_condSectionEntityIds, array, _condSectionEntityIds.Length);
						_condSectionEntityIds = array;
					}
					_condSectionEntityIds[_condSectionDepth] = csEntityId;
				}
				_condSectionDepth++;
				break;
			}
			goto default;
		case Token.IGNORE:
			if (await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false) == Token.LeftBracket)
			{
				if (_validate && csEntityId != _currentEntityId)
				{
					SendValidationEvent(_curPos, XmlSeverityType.Error, System.SR.Sch_ParEntityRefNesting, string.Empty);
				}
				if (await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false) == Token.CondSectionEnd)
				{
					if (_validate && csEntityId != _currentEntityId)
					{
						SendValidationEvent(_curPos, XmlSeverityType.Error, System.SR.Sch_ParEntityRefNesting, string.Empty);
					}
					break;
				}
			}
			goto default;
		default:
			OnUnexpectedError();
			break;
		}
	}

	private async Task<(string, string)> ParseExternalIdAsync(Token idTokenType, Token declType)
	{
		LineInfo keywordLineInfo = new LineInfo(LineNo, LinePos - 6);
		string publicId = null;
		string systemId = null;
		if (await GetTokenAsync(needWhiteSpace: true).ConfigureAwait(continueOnCapturedContext: false) != Token.Literal)
		{
			ThrowUnexpectedToken(_curPos, "\"", "'");
		}
		if (idTokenType == Token.SYSTEM)
		{
			systemId = GetValue();
			if (systemId.Contains('#'))
			{
				Throw(_curPos - systemId.Length - 1, System.SR.Xml_FragmentId, new string[2]
				{
					systemId.Substring(systemId.IndexOf('#')),
					systemId
				});
			}
			if (declType == Token.DOCTYPE && !_freeFloatingDtd)
			{
				_literalLineInfo.linePos++;
				_readerAdapter.OnSystemId(systemId, keywordLineInfo, _literalLineInfo);
			}
		}
		else
		{
			publicId = GetValue();
			int num;
			if ((num = XmlCharType.IsPublicId(publicId)) >= 0)
			{
				ThrowInvalidChar(_curPos - 1 - publicId.Length + num, publicId, num);
			}
			if (declType == Token.DOCTYPE && !_freeFloatingDtd)
			{
				_literalLineInfo.linePos++;
				_readerAdapter.OnPublicId(publicId, keywordLineInfo, _literalLineInfo);
				if (await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false) == Token.Literal)
				{
					if (!_whitespaceSeen)
					{
						Throw(System.SR.Xml_ExpectingWhiteSpace, char.ToString(_literalQuoteChar), _literalLineInfo.lineNo, _literalLineInfo.linePos);
					}
					systemId = GetValue();
					_literalLineInfo.linePos++;
					_readerAdapter.OnSystemId(systemId, keywordLineInfo, _literalLineInfo);
				}
				else
				{
					ThrowUnexpectedToken(_curPos, "\"", "'");
				}
			}
			else if (await GetTokenAsync(needWhiteSpace: false).ConfigureAwait(continueOnCapturedContext: false) == Token.Literal)
			{
				if (!_whitespaceSeen)
				{
					Throw(System.SR.Xml_ExpectingWhiteSpace, char.ToString(_literalQuoteChar), _literalLineInfo.lineNo, _literalLineInfo.linePos);
				}
				systemId = GetValue();
			}
			else if (declType != Token.NOTATION)
			{
				ThrowUnexpectedToken(_curPos, "\"", "'");
			}
		}
		return (publicId, systemId);
	}

	private async Task<Token> GetTokenAsync(bool needWhiteSpace)
	{
		_whitespaceSeen = false;
		while (true)
		{
			switch (_chars[_curPos])
			{
			case '\0':
				if (_curPos != _charsUsed)
				{
					ThrowInvalidChar(_chars, _charsUsed, _curPos);
				}
				break;
			case '\n':
				_whitespaceSeen = true;
				_curPos++;
				_readerAdapter.OnNewLine(_curPos);
				continue;
			case '\r':
				_whitespaceSeen = true;
				if (_chars[_curPos + 1] == '\n')
				{
					if (Normalize)
					{
						SaveParsingBuffer();
						_readerAdapter.CurrentPosition++;
					}
					_curPos += 2;
				}
				else
				{
					if (_curPos + 1 >= _charsUsed && !_readerAdapter.IsEof)
					{
						break;
					}
					_chars[_curPos] = '\n';
					_curPos++;
				}
				_readerAdapter.OnNewLine(_curPos);
				continue;
			case '\t':
			case ' ':
				_whitespaceSeen = true;
				_curPos++;
				continue;
			case '%':
				if (_charsUsed - _curPos < 2)
				{
					break;
				}
				if (!XmlCharType.IsWhiteSpace(_chars[_curPos + 1]))
				{
					if (IgnoreEntityReferences)
					{
						_curPos++;
					}
					else
					{
						await HandleEntityReferenceAsync(paramEntity: true, inLiteral: false, inAttribute: false).ConfigureAwait(continueOnCapturedContext: false);
					}
					continue;
				}
				goto default;
			default:
				if (needWhiteSpace && !_whitespaceSeen && _scanningFunction != ScanningFunction.ParamEntitySpace)
				{
					Throw(_curPos, System.SR.Xml_ExpectingWhiteSpace, ParseUnexpectedToken(_curPos));
				}
				_tokenStartPos = _curPos;
				while (true)
				{
					switch (_scanningFunction)
					{
					case ScanningFunction.Name:
						return await ScanNameExpectedAsync().ConfigureAwait(continueOnCapturedContext: false);
					case ScanningFunction.QName:
						return await ScanQNameExpectedAsync().ConfigureAwait(continueOnCapturedContext: false);
					case ScanningFunction.Nmtoken:
						return await ScanNmtokenExpectedAsync().ConfigureAwait(continueOnCapturedContext: false);
					case ScanningFunction.SubsetContent:
						return await ScanSubsetContentAsync().ConfigureAwait(continueOnCapturedContext: false);
					case ScanningFunction.Doctype1:
						return await ScanDoctype1Async().ConfigureAwait(continueOnCapturedContext: false);
					case ScanningFunction.Doctype2:
						return ScanDoctype2();
					case ScanningFunction.Element1:
						return await ScanElement1Async().ConfigureAwait(continueOnCapturedContext: false);
					case ScanningFunction.Element2:
						return await ScanElement2Async().ConfigureAwait(continueOnCapturedContext: false);
					case ScanningFunction.Element3:
						return await ScanElement3Async().ConfigureAwait(continueOnCapturedContext: false);
					case ScanningFunction.Element4:
						return ScanElement4();
					case ScanningFunction.Element5:
						return ScanElement5();
					case ScanningFunction.Element6:
						return ScanElement6();
					case ScanningFunction.Element7:
						return ScanElement7();
					case ScanningFunction.Attlist1:
						return await ScanAttlist1Async().ConfigureAwait(continueOnCapturedContext: false);
					case ScanningFunction.Attlist2:
						return await ScanAttlist2Async().ConfigureAwait(continueOnCapturedContext: false);
					case ScanningFunction.Attlist3:
						return ScanAttlist3();
					case ScanningFunction.Attlist4:
						return ScanAttlist4();
					case ScanningFunction.Attlist5:
						return ScanAttlist5();
					case ScanningFunction.Attlist6:
						return await ScanAttlist6Async().ConfigureAwait(continueOnCapturedContext: false);
					case ScanningFunction.Attlist7:
						return ScanAttlist7();
					case ScanningFunction.Notation1:
						return await ScanNotation1Async().ConfigureAwait(continueOnCapturedContext: false);
					case ScanningFunction.SystemId:
						return await ScanSystemIdAsync().ConfigureAwait(continueOnCapturedContext: false);
					case ScanningFunction.PublicId1:
						return await ScanPublicId1Async().ConfigureAwait(continueOnCapturedContext: false);
					case ScanningFunction.PublicId2:
						return await ScanPublicId2Async().ConfigureAwait(continueOnCapturedContext: false);
					case ScanningFunction.Entity1:
						return await ScanEntity1Async().ConfigureAwait(continueOnCapturedContext: false);
					case ScanningFunction.Entity2:
						return await ScanEntity2Async().ConfigureAwait(continueOnCapturedContext: false);
					case ScanningFunction.Entity3:
						return await ScanEntity3Async().ConfigureAwait(continueOnCapturedContext: false);
					case ScanningFunction.CondSection1:
						return await ScanCondSection1Async().ConfigureAwait(continueOnCapturedContext: false);
					case ScanningFunction.CondSection2:
						return ScanCondSection2();
					case ScanningFunction.CondSection3:
						return await ScanCondSection3Async().ConfigureAwait(continueOnCapturedContext: false);
					case ScanningFunction.ClosingTag:
						return ScanClosingTag();
					case ScanningFunction.ParamEntitySpace:
						break;
					default:
						return Token.None;
					}
					_whitespaceSeen = true;
					_scanningFunction = _savedScanningFunction;
				}
			}
			bool isEof = _readerAdapter.IsEof;
			bool flag = isEof;
			if (!flag)
			{
				flag = await ReadDataAsync().ConfigureAwait(continueOnCapturedContext: false) == 0;
			}
			if (flag && !HandleEntityEnd(inLiteral: false))
			{
				if (_scanningFunction == ScanningFunction.SubsetContent)
				{
					break;
				}
				Throw(_curPos, System.SR.Xml_IncompleteDtdContent);
			}
		}
		return Token.Eof;
	}

	private async Task<Token> ScanSubsetContentAsync()
	{
		while (true)
		{
			char c = _chars[_curPos];
			if (c != '<')
			{
				if (c != ']')
				{
					goto IL_0548;
				}
				if (_charsUsed - _curPos >= 2 || _readerAdapter.IsEof)
				{
					if (_chars[_curPos + 1] != ']')
					{
						_curPos++;
						_scanningFunction = ScanningFunction.ClosingTag;
						return Token.RightBracket;
					}
					if (_charsUsed - _curPos >= 3 || _readerAdapter.IsEof)
					{
						if (_chars[_curPos + 1] == ']' && _chars[_curPos + 2] == '>')
						{
							break;
						}
						goto IL_0548;
					}
				}
			}
			else
			{
				switch (_chars[_curPos + 1])
				{
				case '!':
					switch (_chars[_curPos + 2])
					{
					case 'E':
						if (_chars[_curPos + 3] == 'L')
						{
							if (_charsUsed - _curPos >= 9)
							{
								if (_chars[_curPos + 4] != 'E' || _chars[_curPos + 5] != 'M' || _chars[_curPos + 6] != 'E' || _chars[_curPos + 7] != 'N' || _chars[_curPos + 8] != 'T')
								{
									Throw(_curPos, System.SR.Xml_ExpectDtdMarkup);
								}
								_curPos += 9;
								_scanningFunction = ScanningFunction.QName;
								_nextScaningFunction = ScanningFunction.Element1;
								return Token.ElementDecl;
							}
						}
						else if (_chars[_curPos + 3] == 'N')
						{
							if (_charsUsed - _curPos >= 8)
							{
								if (_chars[_curPos + 4] != 'T' || _chars[_curPos + 5] != 'I' || _chars[_curPos + 6] != 'T' || _chars[_curPos + 7] != 'Y')
								{
									Throw(_curPos, System.SR.Xml_ExpectDtdMarkup);
								}
								_curPos += 8;
								_scanningFunction = ScanningFunction.Entity1;
								return Token.EntityDecl;
							}
						}
						else if (_charsUsed - _curPos >= 4)
						{
							Throw(_curPos, System.SR.Xml_ExpectDtdMarkup);
							return Token.None;
						}
						break;
					case 'A':
						if (_charsUsed - _curPos >= 9)
						{
							if (_chars[_curPos + 3] != 'T' || _chars[_curPos + 4] != 'T' || _chars[_curPos + 5] != 'L' || _chars[_curPos + 6] != 'I' || _chars[_curPos + 7] != 'S' || _chars[_curPos + 8] != 'T')
							{
								Throw(_curPos, System.SR.Xml_ExpectDtdMarkup);
							}
							_curPos += 9;
							_scanningFunction = ScanningFunction.QName;
							_nextScaningFunction = ScanningFunction.Attlist1;
							return Token.AttlistDecl;
						}
						break;
					case 'N':
						if (_charsUsed - _curPos >= 10)
						{
							if (_chars[_curPos + 3] != 'O' || _chars[_curPos + 4] != 'T' || _chars[_curPos + 5] != 'A' || _chars[_curPos + 6] != 'T' || _chars[_curPos + 7] != 'I' || _chars[_curPos + 8] != 'O' || _chars[_curPos + 9] != 'N')
							{
								Throw(_curPos, System.SR.Xml_ExpectDtdMarkup);
							}
							_curPos += 10;
							_scanningFunction = ScanningFunction.Name;
							_nextScaningFunction = ScanningFunction.Notation1;
							return Token.NotationDecl;
						}
						break;
					case '[':
						_curPos += 3;
						_scanningFunction = ScanningFunction.CondSection1;
						return Token.CondSectionStart;
					case '-':
						if (_chars[_curPos + 3] == '-')
						{
							_curPos += 4;
							return Token.Comment;
						}
						if (_charsUsed - _curPos >= 4)
						{
							Throw(_curPos, System.SR.Xml_ExpectDtdMarkup);
						}
						break;
					default:
						if (_charsUsed - _curPos >= 3)
						{
							Throw(_curPos + 2, System.SR.Xml_ExpectDtdMarkup);
						}
						break;
					}
					break;
				case '?':
					_curPos += 2;
					return Token.PI;
				default:
					if (_charsUsed - _curPos >= 2)
					{
						Throw(_curPos, System.SR.Xml_ExpectDtdMarkup);
						return Token.None;
					}
					break;
				}
			}
			goto IL_0568;
			IL_0568:
			if (await ReadDataAsync().ConfigureAwait(continueOnCapturedContext: false) == 0)
			{
				Throw(_charsUsed, System.SR.Xml_IncompleteDtdContent);
			}
			continue;
			IL_0548:
			if (_charsUsed - _curPos != 0)
			{
				Throw(_curPos, System.SR.Xml_ExpectDtdMarkup);
			}
			goto IL_0568;
		}
		_curPos += 3;
		return Token.CondSectionEnd;
	}

	private async Task<Token> ScanNameExpectedAsync()
	{
		await ScanNameAsync().ConfigureAwait(continueOnCapturedContext: false);
		_scanningFunction = _nextScaningFunction;
		return Token.Name;
	}

	private async Task<Token> ScanQNameExpectedAsync()
	{
		await ScanQNameAsync().ConfigureAwait(continueOnCapturedContext: false);
		_scanningFunction = _nextScaningFunction;
		return Token.QName;
	}

	private async Task<Token> ScanNmtokenExpectedAsync()
	{
		await ScanNmtokenAsync().ConfigureAwait(continueOnCapturedContext: false);
		_scanningFunction = _nextScaningFunction;
		return Token.Nmtoken;
	}

	private async Task<Token> ScanDoctype1Async()
	{
		switch (_chars[_curPos])
		{
		case 'P':
			if (!(await EatPublicKeywordAsync().ConfigureAwait(continueOnCapturedContext: false)))
			{
				Throw(_curPos, System.SR.Xml_ExpectExternalOrClose);
			}
			_nextScaningFunction = ScanningFunction.Doctype2;
			_scanningFunction = ScanningFunction.PublicId1;
			return Token.PUBLIC;
		case 'S':
			if (!(await EatSystemKeywordAsync().ConfigureAwait(continueOnCapturedContext: false)))
			{
				Throw(_curPos, System.SR.Xml_ExpectExternalOrClose);
			}
			_nextScaningFunction = ScanningFunction.Doctype2;
			_scanningFunction = ScanningFunction.SystemId;
			return Token.SYSTEM;
		case '[':
			_curPos++;
			_scanningFunction = ScanningFunction.SubsetContent;
			return Token.LeftBracket;
		case '>':
			_curPos++;
			_scanningFunction = ScanningFunction.SubsetContent;
			return Token.GreaterThan;
		default:
			Throw(_curPos, System.SR.Xml_ExpectExternalOrClose);
			return Token.None;
		}
	}

	private async Task<Token> ScanElement1Async()
	{
		while (true)
		{
			char c = _chars[_curPos];
			if (c != '(')
			{
				if (c != 'A')
				{
					if (c == 'E')
					{
						if (_charsUsed - _curPos < 5)
						{
							goto IL_0141;
						}
						if (_chars[_curPos + 1] == 'M' && _chars[_curPos + 2] == 'P' && _chars[_curPos + 3] == 'T' && _chars[_curPos + 4] == 'Y')
						{
							_curPos += 5;
							_scanningFunction = ScanningFunction.ClosingTag;
							return Token.EMPTY;
						}
					}
				}
				else
				{
					if (_charsUsed - _curPos < 3)
					{
						goto IL_0141;
					}
					if (_chars[_curPos + 1] == 'N' && _chars[_curPos + 2] == 'Y')
					{
						break;
					}
				}
				Throw(_curPos, System.SR.Xml_InvalidContentModel);
				goto IL_0141;
			}
			_scanningFunction = ScanningFunction.Element2;
			_curPos++;
			return Token.LeftParen;
			IL_0141:
			if (await ReadDataAsync().ConfigureAwait(continueOnCapturedContext: false) == 0)
			{
				Throw(_curPos, System.SR.Xml_IncompleteDtdContent);
			}
		}
		_curPos += 3;
		_scanningFunction = ScanningFunction.ClosingTag;
		return Token.ANY;
	}

	private async Task<Token> ScanElement2Async()
	{
		if (_chars[_curPos] == '#')
		{
			while (_charsUsed - _curPos < 7)
			{
				if (await ReadDataAsync().ConfigureAwait(continueOnCapturedContext: false) == 0)
				{
					Throw(_curPos, System.SR.Xml_IncompleteDtdContent);
				}
			}
			if (_chars[_curPos + 1] == 'P' && _chars[_curPos + 2] == 'C' && _chars[_curPos + 3] == 'D' && _chars[_curPos + 4] == 'A' && _chars[_curPos + 5] == 'T' && _chars[_curPos + 6] == 'A')
			{
				_curPos += 7;
				_scanningFunction = ScanningFunction.Element6;
				return Token.PCDATA;
			}
			Throw(_curPos + 1, System.SR.Xml_ExpectPcData);
		}
		_scanningFunction = ScanningFunction.Element3;
		return Token.None;
	}

	private async Task<Token> ScanElement3Async()
	{
		switch (_chars[_curPos])
		{
		case '(':
			_curPos++;
			return Token.LeftParen;
		case '>':
			_curPos++;
			_scanningFunction = ScanningFunction.SubsetContent;
			return Token.GreaterThan;
		default:
			await ScanQNameAsync().ConfigureAwait(continueOnCapturedContext: false);
			_scanningFunction = ScanningFunction.Element4;
			return Token.QName;
		}
	}

	private async Task<Token> ScanAttlist1Async()
	{
		char c = _chars[_curPos];
		if (c == '>')
		{
			_curPos++;
			_scanningFunction = ScanningFunction.SubsetContent;
			return Token.GreaterThan;
		}
		if (!_whitespaceSeen)
		{
			Throw(_curPos, System.SR.Xml_ExpectingWhiteSpace, ParseUnexpectedToken(_curPos));
		}
		await ScanQNameAsync().ConfigureAwait(continueOnCapturedContext: false);
		_scanningFunction = ScanningFunction.Attlist2;
		return Token.QName;
	}

	private async Task<Token> ScanAttlist2Async()
	{
		while (true)
		{
			switch (_chars[_curPos])
			{
			case '(':
				_curPos++;
				_scanningFunction = ScanningFunction.Nmtoken;
				_nextScaningFunction = ScanningFunction.Attlist5;
				return Token.LeftParen;
			case 'C':
				if (_charsUsed - _curPos >= 5)
				{
					if (_chars[_curPos + 1] != 'D' || _chars[_curPos + 2] != 'A' || _chars[_curPos + 3] != 'T' || _chars[_curPos + 4] != 'A')
					{
						Throw(_curPos, System.SR.Xml_InvalidAttributeType1);
					}
					_curPos += 5;
					_scanningFunction = ScanningFunction.Attlist6;
					return Token.CDATA;
				}
				break;
			case 'E':
				if (_charsUsed - _curPos < 9)
				{
					break;
				}
				_scanningFunction = ScanningFunction.Attlist6;
				if (_chars[_curPos + 1] != 'N' || _chars[_curPos + 2] != 'T' || _chars[_curPos + 3] != 'I' || _chars[_curPos + 4] != 'T')
				{
					Throw(_curPos, System.SR.Xml_InvalidAttributeType);
				}
				switch (_chars[_curPos + 5])
				{
				case 'I':
					if (_chars[_curPos + 6] != 'E' || _chars[_curPos + 7] != 'S')
					{
						Throw(_curPos, System.SR.Xml_InvalidAttributeType);
					}
					_curPos += 8;
					return Token.ENTITIES;
				case 'Y':
					_curPos += 6;
					return Token.ENTITY;
				}
				Throw(_curPos, System.SR.Xml_InvalidAttributeType);
				break;
			case 'I':
				if (_charsUsed - _curPos >= 6)
				{
					_scanningFunction = ScanningFunction.Attlist6;
					if (_chars[_curPos + 1] != 'D')
					{
						Throw(_curPos, System.SR.Xml_InvalidAttributeType);
					}
					if (_chars[_curPos + 2] != 'R')
					{
						_curPos += 2;
						return Token.ID;
					}
					if (_chars[_curPos + 3] != 'E' || _chars[_curPos + 4] != 'F')
					{
						Throw(_curPos, System.SR.Xml_InvalidAttributeType);
					}
					if (_chars[_curPos + 5] != 'S')
					{
						_curPos += 5;
						return Token.IDREF;
					}
					_curPos += 6;
					return Token.IDREFS;
				}
				break;
			case 'N':
				if (_charsUsed - _curPos < 8 && !_readerAdapter.IsEof)
				{
					break;
				}
				switch (_chars[_curPos + 1])
				{
				case 'O':
					if (_chars[_curPos + 2] != 'T' || _chars[_curPos + 3] != 'A' || _chars[_curPos + 4] != 'T' || _chars[_curPos + 5] != 'I' || _chars[_curPos + 6] != 'O' || _chars[_curPos + 7] != 'N')
					{
						Throw(_curPos, System.SR.Xml_InvalidAttributeType);
					}
					_curPos += 8;
					_scanningFunction = ScanningFunction.Attlist3;
					return Token.NOTATION;
				case 'M':
					if (_chars[_curPos + 2] != 'T' || _chars[_curPos + 3] != 'O' || _chars[_curPos + 4] != 'K' || _chars[_curPos + 5] != 'E' || _chars[_curPos + 6] != 'N')
					{
						Throw(_curPos, System.SR.Xml_InvalidAttributeType);
					}
					_scanningFunction = ScanningFunction.Attlist6;
					if (_chars[_curPos + 7] == 'S')
					{
						_curPos += 8;
						return Token.NMTOKENS;
					}
					_curPos += 7;
					return Token.NMTOKEN;
				}
				Throw(_curPos, System.SR.Xml_InvalidAttributeType);
				break;
			default:
				Throw(_curPos, System.SR.Xml_InvalidAttributeType);
				break;
			}
			if (await ReadDataAsync().ConfigureAwait(continueOnCapturedContext: false) == 0)
			{
				Throw(_curPos, System.SR.Xml_IncompleteDtdContent);
			}
		}
	}

	private async Task<Token> ScanAttlist6Async()
	{
		while (true)
		{
			switch (_chars[_curPos])
			{
			case '"':
			case '\'':
				await ScanLiteralAsync(LiteralType.AttributeValue).ConfigureAwait(continueOnCapturedContext: false);
				_scanningFunction = ScanningFunction.Attlist1;
				return Token.Literal;
			case '#':
				if (_charsUsed - _curPos < 6)
				{
					break;
				}
				switch (_chars[_curPos + 1])
				{
				case 'R':
					if (_charsUsed - _curPos >= 9)
					{
						if (_chars[_curPos + 2] != 'E' || _chars[_curPos + 3] != 'Q' || _chars[_curPos + 4] != 'U' || _chars[_curPos + 5] != 'I' || _chars[_curPos + 6] != 'R' || _chars[_curPos + 7] != 'E' || _chars[_curPos + 8] != 'D')
						{
							Throw(_curPos, System.SR.Xml_ExpectAttType);
						}
						_curPos += 9;
						_scanningFunction = ScanningFunction.Attlist1;
						return Token.REQUIRED;
					}
					break;
				case 'I':
					if (_charsUsed - _curPos >= 8)
					{
						if (_chars[_curPos + 2] != 'M' || _chars[_curPos + 3] != 'P' || _chars[_curPos + 4] != 'L' || _chars[_curPos + 5] != 'I' || _chars[_curPos + 6] != 'E' || _chars[_curPos + 7] != 'D')
						{
							Throw(_curPos, System.SR.Xml_ExpectAttType);
						}
						_curPos += 8;
						_scanningFunction = ScanningFunction.Attlist1;
						return Token.IMPLIED;
					}
					break;
				case 'F':
					if (_chars[_curPos + 2] != 'I' || _chars[_curPos + 3] != 'X' || _chars[_curPos + 4] != 'E' || _chars[_curPos + 5] != 'D')
					{
						Throw(_curPos, System.SR.Xml_ExpectAttType);
					}
					_curPos += 6;
					_scanningFunction = ScanningFunction.Attlist7;
					return Token.FIXED;
				default:
					Throw(_curPos, System.SR.Xml_ExpectAttType);
					break;
				}
				break;
			default:
				Throw(_curPos, System.SR.Xml_ExpectAttType);
				break;
			}
			if (await ReadDataAsync().ConfigureAwait(continueOnCapturedContext: false) == 0)
			{
				Throw(_curPos, System.SR.Xml_IncompleteDtdContent);
			}
		}
	}

	private async Task<Token> ScanLiteralAsync(LiteralType literalType)
	{
		char quoteChar = _chars[_curPos];
		char replaceChar = ((literalType == LiteralType.AttributeValue) ? ' ' : '\n');
		int startQuoteEntityId = _currentEntityId;
		_literalLineInfo.Set(LineNo, LinePos);
		_curPos++;
		_tokenStartPos = _curPos;
		_stringBuilder.Length = 0;
		while (true)
		{
			if (XmlCharType.IsAttributeValueChar(_chars[_curPos]) && _chars[_curPos] != '%')
			{
				_curPos++;
				continue;
			}
			if (_chars[_curPos] == quoteChar && _currentEntityId == startQuoteEntityId)
			{
				break;
			}
			int num = _curPos - _tokenStartPos;
			if (num > 0)
			{
				_stringBuilder.Append(_chars, _tokenStartPos, num);
				_tokenStartPos = _curPos;
			}
			switch (_chars[_curPos])
			{
			case '"':
			case '\'':
			case '>':
				_curPos++;
				continue;
			case '\n':
				_curPos++;
				if (Normalize)
				{
					_stringBuilder.Append(replaceChar);
					_tokenStartPos = _curPos;
				}
				_readerAdapter.OnNewLine(_curPos);
				continue;
			case '\r':
				if (_chars[_curPos + 1] == '\n')
				{
					if (Normalize)
					{
						if (literalType == LiteralType.AttributeValue)
						{
							_stringBuilder.Append(_readerAdapter.IsEntityEolNormalized ? "  " : " ");
						}
						else
						{
							_stringBuilder.Append(_readerAdapter.IsEntityEolNormalized ? "\r\n" : "\n");
						}
						_tokenStartPos = _curPos + 2;
						SaveParsingBuffer();
						_readerAdapter.CurrentPosition++;
					}
					_curPos += 2;
				}
				else
				{
					if (_curPos + 1 == _charsUsed)
					{
						break;
					}
					_curPos++;
					if (Normalize)
					{
						_stringBuilder.Append(replaceChar);
						_tokenStartPos = _curPos;
					}
				}
				_readerAdapter.OnNewLine(_curPos);
				continue;
			case '\t':
				if (literalType == LiteralType.AttributeValue && Normalize)
				{
					_stringBuilder.Append(' ');
					_tokenStartPos++;
				}
				_curPos++;
				continue;
			case '<':
				if (literalType == LiteralType.AttributeValue)
				{
					Throw(_curPos, System.SR.Xml_BadAttributeChar, XmlException.BuildCharExceptionArgs('<', '\0'));
				}
				_curPos++;
				continue;
			case '%':
				if (literalType != LiteralType.EntityReplText)
				{
					_curPos++;
					continue;
				}
				await HandleEntityReferenceAsync(paramEntity: true, inLiteral: true, literalType == LiteralType.AttributeValue).ConfigureAwait(continueOnCapturedContext: false);
				_tokenStartPos = _curPos;
				continue;
			case '&':
			{
				if (literalType == LiteralType.SystemOrPublicID)
				{
					_curPos++;
					continue;
				}
				if (_curPos + 1 == _charsUsed)
				{
					break;
				}
				if (_chars[_curPos + 1] == '#')
				{
					SaveParsingBuffer();
					int num2 = await _readerAdapter.ParseNumericCharRefAsync(SaveInternalSubsetValue ? _internalSubsetValueSb : null).ConfigureAwait(continueOnCapturedContext: false);
					LoadParsingBuffer();
					_stringBuilder.Append(_chars, _curPos, num2 - _curPos);
					_readerAdapter.CurrentPosition = num2;
					_tokenStartPos = num2;
					_curPos = num2;
					continue;
				}
				SaveParsingBuffer();
				if (literalType == LiteralType.AttributeValue)
				{
					int num3 = await _readerAdapter.ParseNamedCharRefAsync(expand: true, SaveInternalSubsetValue ? _internalSubsetValueSb : null).ConfigureAwait(continueOnCapturedContext: false);
					LoadParsingBuffer();
					if (num3 >= 0)
					{
						_stringBuilder.Append(_chars, _curPos, num3 - _curPos);
						_readerAdapter.CurrentPosition = num3;
						_tokenStartPos = num3;
						_curPos = num3;
					}
					else
					{
						await HandleEntityReferenceAsync(paramEntity: false, inLiteral: true, inAttribute: true).ConfigureAwait(continueOnCapturedContext: false);
						_tokenStartPos = _curPos;
					}
					continue;
				}
				int num4 = await _readerAdapter.ParseNamedCharRefAsync(expand: false, null).ConfigureAwait(continueOnCapturedContext: false);
				LoadParsingBuffer();
				if (num4 >= 0)
				{
					_tokenStartPos = _curPos;
					_curPos = num4;
					continue;
				}
				_stringBuilder.Append('&');
				_curPos++;
				_tokenStartPos = _curPos;
				XmlQualifiedName entityName = ScanEntityName();
				VerifyEntityReference(entityName, paramEntity: false, mustBeDeclared: false, inAttribute: false);
				continue;
			}
			default:
			{
				if (_curPos == _charsUsed)
				{
					break;
				}
				char ch = _chars[_curPos];
				if (XmlCharType.IsHighSurrogate(ch))
				{
					if (_curPos + 1 == _charsUsed)
					{
						break;
					}
					_curPos++;
					if (XmlCharType.IsLowSurrogate(_chars[_curPos]))
					{
						_curPos++;
						continue;
					}
				}
				ThrowInvalidChar(_chars, _charsUsed, _curPos);
				return Token.None;
			}
			}
			bool isEof = _readerAdapter.IsEof;
			bool flag = isEof;
			if (!flag)
			{
				flag = await ReadDataAsync().ConfigureAwait(continueOnCapturedContext: false) == 0;
			}
			if (flag && (literalType == LiteralType.SystemOrPublicID || !HandleEntityEnd(inLiteral: true)))
			{
				Throw(_curPos, System.SR.Xml_UnclosedQuote);
			}
			_tokenStartPos = _curPos;
		}
		if (_stringBuilder.Length > 0)
		{
			_stringBuilder.Append(_chars, _tokenStartPos, _curPos - _tokenStartPos);
		}
		_curPos++;
		_literalQuoteChar = quoteChar;
		return Token.Literal;
	}

	private async Task<Token> ScanNotation1Async()
	{
		switch (_chars[_curPos])
		{
		case 'P':
			if (!(await EatPublicKeywordAsync().ConfigureAwait(continueOnCapturedContext: false)))
			{
				Throw(_curPos, System.SR.Xml_ExpectExternalOrClose);
			}
			_nextScaningFunction = ScanningFunction.ClosingTag;
			_scanningFunction = ScanningFunction.PublicId1;
			return Token.PUBLIC;
		case 'S':
			if (!(await EatSystemKeywordAsync().ConfigureAwait(continueOnCapturedContext: false)))
			{
				Throw(_curPos, System.SR.Xml_ExpectExternalOrClose);
			}
			_nextScaningFunction = ScanningFunction.ClosingTag;
			_scanningFunction = ScanningFunction.SystemId;
			return Token.SYSTEM;
		default:
			Throw(_curPos, System.SR.Xml_ExpectExternalOrPublicId);
			return Token.None;
		}
	}

	private async Task<Token> ScanSystemIdAsync()
	{
		if (_chars[_curPos] != '"' && _chars[_curPos] != '\'')
		{
			ThrowUnexpectedToken(_curPos, "\"", "'");
		}
		await ScanLiteralAsync(LiteralType.SystemOrPublicID).ConfigureAwait(continueOnCapturedContext: false);
		_scanningFunction = _nextScaningFunction;
		return Token.Literal;
	}

	private async Task<Token> ScanEntity1Async()
	{
		if (_chars[_curPos] == '%')
		{
			_curPos++;
			_nextScaningFunction = ScanningFunction.Entity2;
			_scanningFunction = ScanningFunction.Name;
			return Token.Percent;
		}
		await ScanNameAsync().ConfigureAwait(continueOnCapturedContext: false);
		_scanningFunction = ScanningFunction.Entity2;
		return Token.Name;
	}

	private async Task<Token> ScanEntity2Async()
	{
		switch (_chars[_curPos])
		{
		case 'P':
			if (!(await EatPublicKeywordAsync().ConfigureAwait(continueOnCapturedContext: false)))
			{
				Throw(_curPos, System.SR.Xml_ExpectExternalOrClose);
			}
			_nextScaningFunction = ScanningFunction.Entity3;
			_scanningFunction = ScanningFunction.PublicId1;
			return Token.PUBLIC;
		case 'S':
			if (!(await EatSystemKeywordAsync().ConfigureAwait(continueOnCapturedContext: false)))
			{
				Throw(_curPos, System.SR.Xml_ExpectExternalOrClose);
			}
			_nextScaningFunction = ScanningFunction.Entity3;
			_scanningFunction = ScanningFunction.SystemId;
			return Token.SYSTEM;
		case '"':
		case '\'':
			await ScanLiteralAsync(LiteralType.EntityReplText).ConfigureAwait(continueOnCapturedContext: false);
			_scanningFunction = ScanningFunction.ClosingTag;
			return Token.Literal;
		default:
			Throw(_curPos, System.SR.Xml_ExpectExternalIdOrEntityValue);
			return Token.None;
		}
	}

	private async Task<Token> ScanEntity3Async()
	{
		if (_chars[_curPos] == 'N')
		{
			do
			{
				if (_charsUsed - _curPos >= 5)
				{
					if (_chars[_curPos + 1] != 'D' || _chars[_curPos + 2] != 'A' || _chars[_curPos + 3] != 'T' || _chars[_curPos + 4] != 'A')
					{
						break;
					}
					_curPos += 5;
					_scanningFunction = ScanningFunction.Name;
					_nextScaningFunction = ScanningFunction.ClosingTag;
					return Token.NData;
				}
			}
			while (await ReadDataAsync().ConfigureAwait(continueOnCapturedContext: false) != 0);
		}
		_scanningFunction = ScanningFunction.ClosingTag;
		return Token.None;
	}

	private async Task<Token> ScanPublicId1Async()
	{
		if (_chars[_curPos] != '"' && _chars[_curPos] != '\'')
		{
			ThrowUnexpectedToken(_curPos, "\"", "'");
		}
		await ScanLiteralAsync(LiteralType.SystemOrPublicID).ConfigureAwait(continueOnCapturedContext: false);
		_scanningFunction = ScanningFunction.PublicId2;
		return Token.Literal;
	}

	private async Task<Token> ScanPublicId2Async()
	{
		if (_chars[_curPos] != '"' && _chars[_curPos] != '\'')
		{
			_scanningFunction = _nextScaningFunction;
			return Token.None;
		}
		await ScanLiteralAsync(LiteralType.SystemOrPublicID).ConfigureAwait(continueOnCapturedContext: false);
		_scanningFunction = _nextScaningFunction;
		return Token.Literal;
	}

	private async Task<Token> ScanCondSection1Async()
	{
		if (_chars[_curPos] != 'I')
		{
			Throw(_curPos, System.SR.Xml_ExpectIgnoreOrInclude);
		}
		_curPos++;
		while (true)
		{
			if (_charsUsed - _curPos >= 5)
			{
				char c = _chars[_curPos];
				if (c == 'G')
				{
					if (_chars[_curPos + 1] != 'N' || _chars[_curPos + 2] != 'O' || _chars[_curPos + 3] != 'R' || _chars[_curPos + 4] != 'E' || XmlCharType.IsNameSingleChar(_chars[_curPos + 5]))
					{
						break;
					}
					_nextScaningFunction = ScanningFunction.CondSection3;
					_scanningFunction = ScanningFunction.CondSection2;
					_curPos += 5;
					return Token.IGNORE;
				}
				if (c != 'N')
				{
					break;
				}
				if (_charsUsed - _curPos >= 6)
				{
					if (_chars[_curPos + 1] != 'C' || _chars[_curPos + 2] != 'L' || _chars[_curPos + 3] != 'U' || _chars[_curPos + 4] != 'D' || _chars[_curPos + 5] != 'E' || XmlCharType.IsNameSingleChar(_chars[_curPos + 6]))
					{
						break;
					}
					_nextScaningFunction = ScanningFunction.SubsetContent;
					_scanningFunction = ScanningFunction.CondSection2;
					_curPos += 6;
					return Token.INCLUDE;
				}
			}
			if (await ReadDataAsync().ConfigureAwait(continueOnCapturedContext: false) == 0)
			{
				Throw(_curPos, System.SR.Xml_IncompleteDtdContent);
			}
		}
		Throw(_curPos - 1, System.SR.Xml_ExpectIgnoreOrInclude);
		return Token.None;
	}

	private async Task<Token> ScanCondSection3Async()
	{
		int ignoreSectionDepth = 0;
		while (true)
		{
			if (XmlCharType.IsTextChar(_chars[_curPos]) && _chars[_curPos] != ']')
			{
				_curPos++;
				continue;
			}
			switch (_chars[_curPos])
			{
			case '\t':
			case '"':
			case '&':
			case '\'':
				_curPos++;
				continue;
			case '\n':
				_curPos++;
				_readerAdapter.OnNewLine(_curPos);
				continue;
			case '\r':
				if (_chars[_curPos + 1] == '\n')
				{
					_curPos += 2;
				}
				else
				{
					if (_curPos + 1 >= _charsUsed && !_readerAdapter.IsEof)
					{
						break;
					}
					_curPos++;
				}
				_readerAdapter.OnNewLine(_curPos);
				continue;
			case '<':
				if (_charsUsed - _curPos >= 3)
				{
					if (_chars[_curPos + 1] != '!' || _chars[_curPos + 2] != '[')
					{
						_curPos++;
						continue;
					}
					ignoreSectionDepth++;
					_curPos += 3;
					continue;
				}
				break;
			case ']':
				if (_charsUsed - _curPos < 3)
				{
					break;
				}
				if (_chars[_curPos + 1] != ']' || _chars[_curPos + 2] != '>')
				{
					_curPos++;
					continue;
				}
				if (ignoreSectionDepth > 0)
				{
					ignoreSectionDepth--;
					_curPos += 3;
					continue;
				}
				_curPos += 3;
				_scanningFunction = ScanningFunction.SubsetContent;
				return Token.CondSectionEnd;
			default:
			{
				if (_curPos == _charsUsed)
				{
					break;
				}
				char ch = _chars[_curPos];
				if (XmlCharType.IsHighSurrogate(ch))
				{
					if (_curPos + 1 == _charsUsed)
					{
						break;
					}
					_curPos++;
					if (XmlCharType.IsLowSurrogate(_chars[_curPos]))
					{
						_curPos++;
						continue;
					}
				}
				ThrowInvalidChar(_chars, _charsUsed, _curPos);
				return Token.None;
			}
			}
			bool isEof = _readerAdapter.IsEof;
			bool flag = isEof;
			if (!flag)
			{
				flag = await ReadDataAsync().ConfigureAwait(continueOnCapturedContext: false) == 0;
			}
			if (flag)
			{
				if (HandleEntityEnd(inLiteral: false))
				{
					continue;
				}
				Throw(_curPos, System.SR.Xml_UnclosedConditionalSection);
			}
			_tokenStartPos = _curPos;
		}
	}

	private Task ScanNameAsync()
	{
		return ScanQNameAsync(isQName: false);
	}

	private Task ScanQNameAsync()
	{
		return ScanQNameAsync(SupportNamespaces);
	}

	private async Task ScanQNameAsync(bool isQName)
	{
		_tokenStartPos = _curPos;
		int colonOffset = -1;
		while (true)
		{
			if (XmlCharType.IsStartNCNameSingleChar(_chars[_curPos]) || _chars[_curPos] == ':')
			{
				_curPos++;
			}
			else if (_curPos + 1 >= _charsUsed)
			{
				if (await ReadDataInNameAsync().ConfigureAwait(continueOnCapturedContext: false))
				{
					continue;
				}
				Throw(_curPos, System.SR.Xml_UnexpectedEOF, "Name");
			}
			else
			{
				Throw(_curPos, System.SR.Xml_BadStartNameChar, XmlException.BuildCharExceptionArgs(_chars, _charsUsed, _curPos));
			}
			while (true)
			{
				if (XmlCharType.IsNCNameSingleChar(_chars[_curPos]))
				{
					_curPos++;
					continue;
				}
				if (_chars[_curPos] == ':')
				{
					if (isQName)
					{
						break;
					}
					_curPos++;
					continue;
				}
				if (_curPos == _charsUsed)
				{
					if (await ReadDataInNameAsync().ConfigureAwait(continueOnCapturedContext: false))
					{
						continue;
					}
					if (_tokenStartPos == _curPos)
					{
						Throw(_curPos, System.SR.Xml_UnexpectedEOF, "Name");
					}
				}
				_colonPos = ((colonOffset == -1) ? (-1) : (_tokenStartPos + colonOffset));
				return;
			}
			if (colonOffset != -1)
			{
				Throw(_curPos, System.SR.Xml_BadNameChar, XmlException.BuildCharExceptionArgs(':', '\0'));
			}
			colonOffset = _curPos - _tokenStartPos;
			_curPos++;
		}
	}

	private async Task<bool> ReadDataInNameAsync()
	{
		int offset = _curPos - _tokenStartPos;
		_curPos = _tokenStartPos;
		bool result = await ReadDataAsync().ConfigureAwait(continueOnCapturedContext: false) != 0;
		_tokenStartPos = _curPos;
		_curPos += offset;
		return result;
	}

	private async Task ScanNmtokenAsync()
	{
		_tokenStartPos = _curPos;
		int len;
		while (true)
		{
			if (XmlCharType.IsNCNameSingleChar(_chars[_curPos]) || _chars[_curPos] == ':')
			{
				_curPos++;
				continue;
			}
			if (_curPos < _charsUsed)
			{
				if (_curPos - _tokenStartPos == 0)
				{
					Throw(_curPos, System.SR.Xml_BadNameChar, XmlException.BuildCharExceptionArgs(_chars, _charsUsed, _curPos));
				}
				return;
			}
			len = _curPos - _tokenStartPos;
			_curPos = _tokenStartPos;
			if (await ReadDataAsync().ConfigureAwait(continueOnCapturedContext: false) == 0)
			{
				if (len > 0)
				{
					break;
				}
				Throw(_curPos, System.SR.Xml_UnexpectedEOF, "NmToken");
			}
			_tokenStartPos = _curPos;
			_curPos += len;
		}
		_tokenStartPos = _curPos;
		_curPos += len;
	}

	private async Task<bool> EatPublicKeywordAsync()
	{
		while (_charsUsed - _curPos < 6)
		{
			if (await ReadDataAsync().ConfigureAwait(continueOnCapturedContext: false) == 0)
			{
				return false;
			}
		}
		if (_chars[_curPos + 1] != 'U' || _chars[_curPos + 2] != 'B' || _chars[_curPos + 3] != 'L' || _chars[_curPos + 4] != 'I' || _chars[_curPos + 5] != 'C')
		{
			return false;
		}
		_curPos += 6;
		return true;
	}

	private async Task<bool> EatSystemKeywordAsync()
	{
		while (_charsUsed - _curPos < 6)
		{
			if (await ReadDataAsync().ConfigureAwait(continueOnCapturedContext: false) == 0)
			{
				return false;
			}
		}
		if (_chars[_curPos + 1] != 'Y' || _chars[_curPos + 2] != 'S' || _chars[_curPos + 3] != 'T' || _chars[_curPos + 4] != 'E' || _chars[_curPos + 5] != 'M')
		{
			return false;
		}
		_curPos += 6;
		return true;
	}

	private async Task<int> ReadDataAsync()
	{
		SaveParsingBuffer();
		int result = await _readerAdapter.ReadDataAsync().ConfigureAwait(continueOnCapturedContext: false);
		LoadParsingBuffer();
		return result;
	}

	private Task<bool> HandleEntityReferenceAsync(bool paramEntity, bool inLiteral, bool inAttribute)
	{
		_curPos++;
		return HandleEntityReferenceAsync(ScanEntityName(), paramEntity, inLiteral, inAttribute);
	}

	private async Task<bool> HandleEntityReferenceAsync(XmlQualifiedName entityName, bool paramEntity, bool inLiteral, bool inAttribute)
	{
		SaveParsingBuffer();
		if (paramEntity && ParsingInternalSubset && !ParsingTopLevelMarkup)
		{
			Throw(_curPos - entityName.Name.Length - 1, System.SR.Xml_InvalidParEntityRef);
		}
		SchemaEntity schemaEntity = VerifyEntityReference(entityName, paramEntity, mustBeDeclared: true, inAttribute);
		if (schemaEntity == null)
		{
			return false;
		}
		if (schemaEntity.ParsingInProgress)
		{
			Throw(_curPos - entityName.Name.Length - 1, paramEntity ? System.SR.Xml_RecursiveParEntity : System.SR.Xml_RecursiveGenEntity, entityName.Name);
		}
		int currentEntityId;
		if (schemaEntity.IsExternal)
		{
			(int, bool) tuple = await _readerAdapter.PushEntityAsync(schemaEntity).ConfigureAwait(continueOnCapturedContext: false);
			(currentEntityId, _) = tuple;
			if (!tuple.Item2)
			{
				return false;
			}
			_externalEntitiesDepth++;
		}
		else
		{
			if (schemaEntity.Text.Length == 0)
			{
				return false;
			}
			(int, bool) tuple3 = await _readerAdapter.PushEntityAsync(schemaEntity).ConfigureAwait(continueOnCapturedContext: false);
			(currentEntityId, _) = tuple3;
			if (!tuple3.Item2)
			{
				return false;
			}
		}
		_currentEntityId = currentEntityId;
		if (paramEntity && !inLiteral && _scanningFunction != ScanningFunction.ParamEntitySpace)
		{
			_savedScanningFunction = _scanningFunction;
			_scanningFunction = ScanningFunction.ParamEntitySpace;
		}
		LoadParsingBuffer();
		return true;
	}
}
