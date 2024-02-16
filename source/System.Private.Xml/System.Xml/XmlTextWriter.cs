using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;

namespace System.Xml;

[EditorBrowsable(EditorBrowsableState.Never)]
public class XmlTextWriter : XmlWriter
{
	private enum NamespaceState
	{
		Uninitialized,
		NotDeclaredButInScope,
		DeclaredButNotWrittenOut,
		DeclaredAndWrittenOut
	}

	private struct TagInfo
	{
		internal string name;

		internal string prefix;

		internal string defaultNs;

		internal NamespaceState defaultNsState;

		internal XmlSpace xmlSpace;

		internal string xmlLang;

		internal int prevNsTop;

		internal int prefixCount;

		internal bool mixed;

		internal void Init(int nsTop)
		{
			name = null;
			defaultNs = string.Empty;
			defaultNsState = NamespaceState.Uninitialized;
			xmlSpace = XmlSpace.None;
			xmlLang = null;
			prevNsTop = nsTop;
			prefixCount = 0;
			mixed = false;
		}
	}

	private struct Namespace
	{
		internal string prefix;

		internal string ns;

		internal bool declared;

		internal int prevNsIndex;

		internal void Set(string prefix, string ns, bool declared)
		{
			this.prefix = prefix;
			this.ns = ns;
			this.declared = declared;
			prevNsIndex = -1;
		}
	}

	private enum SpecialAttr
	{
		None,
		XmlSpace,
		XmlLang,
		XmlNs
	}

	private enum State
	{
		Start,
		Prolog,
		PostDTD,
		Element,
		Attribute,
		Content,
		AttrOnly,
		Epilog,
		Error,
		Closed
	}

	private enum Token
	{
		PI,
		Doctype,
		Comment,
		CData,
		StartElement,
		EndElement,
		LongEndElement,
		StartAttribute,
		EndAttribute,
		Content,
		Base64,
		RawData,
		Whitespace,
		Empty
	}

	private readonly TextWriter _textWriter;

	private readonly XmlTextEncoder _xmlEncoder;

	private readonly Encoding _encoding;

	private Formatting _formatting;

	private bool _indented;

	private int _indentation;

	private char[] _indentChars;

	private static readonly char[] s_defaultIndentChars = CreateDefaultIndentChars();

	private TagInfo[] _stack;

	private int _top;

	private State[] _stateTable;

	private State _currentState;

	private Token _lastToken;

	private XmlTextWriterBase64Encoder _base64Encoder;

	private char _quoteChar;

	private char _curQuoteChar;

	private bool _namespaces;

	private SpecialAttr _specialAttr;

	private string _prefixForXmlNs;

	private bool _flush;

	private Namespace[] _nsStack;

	private int _nsTop;

	private Dictionary<string, int> _nsHashtable;

	private bool _useNsHashtable;

	private static readonly string[] s_stateName = new string[10] { "Start", "Prolog", "PostDTD", "Element", "Attribute", "Content", "AttrOnly", "Epilog", "Error", "Closed" };

	private static readonly string[] s_tokenName = new string[14]
	{
		"PI", "Doctype", "Comment", "CData", "StartElement", "EndElement", "LongEndElement", "StartAttribute", "EndAttribute", "Content",
		"Base64", "RawData", "Whitespace", "Empty"
	};

	private static readonly State[] s_stateTableDefault = new State[104]
	{
		State.Prolog,
		State.Prolog,
		State.PostDTD,
		State.Content,
		State.Content,
		State.Content,
		State.Error,
		State.Epilog,
		State.PostDTD,
		State.PostDTD,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Prolog,
		State.Prolog,
		State.PostDTD,
		State.Content,
		State.Content,
		State.Content,
		State.Error,
		State.Epilog,
		State.Content,
		State.Content,
		State.Error,
		State.Content,
		State.Content,
		State.Content,
		State.Error,
		State.Epilog,
		State.Element,
		State.Element,
		State.Element,
		State.Element,
		State.Element,
		State.Element,
		State.Error,
		State.Element,
		State.Error,
		State.Error,
		State.Error,
		State.Content,
		State.Content,
		State.Content,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Content,
		State.Content,
		State.Content,
		State.Error,
		State.Error,
		State.AttrOnly,
		State.Error,
		State.Error,
		State.Attribute,
		State.Attribute,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Element,
		State.Error,
		State.Epilog,
		State.Error,
		State.Content,
		State.Content,
		State.Error,
		State.Content,
		State.Attribute,
		State.Content,
		State.Attribute,
		State.Epilog,
		State.Content,
		State.Content,
		State.Error,
		State.Content,
		State.Attribute,
		State.Content,
		State.Attribute,
		State.Epilog,
		State.Prolog,
		State.Prolog,
		State.PostDTD,
		State.Content,
		State.Attribute,
		State.Content,
		State.Attribute,
		State.Epilog,
		State.Prolog,
		State.Prolog,
		State.PostDTD,
		State.Content,
		State.Attribute,
		State.Content,
		State.Attribute,
		State.Epilog
	};

	private static readonly State[] s_stateTableDocument = new State[104]
	{
		State.Error,
		State.Prolog,
		State.PostDTD,
		State.Content,
		State.Content,
		State.Content,
		State.Error,
		State.Epilog,
		State.Error,
		State.PostDTD,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Prolog,
		State.PostDTD,
		State.Content,
		State.Content,
		State.Content,
		State.Error,
		State.Epilog,
		State.Error,
		State.Error,
		State.Error,
		State.Content,
		State.Content,
		State.Content,
		State.Error,
		State.Error,
		State.Error,
		State.Element,
		State.Element,
		State.Element,
		State.Element,
		State.Element,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Content,
		State.Content,
		State.Content,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Content,
		State.Content,
		State.Content,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Attribute,
		State.Attribute,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Element,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Content,
		State.Attribute,
		State.Content,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Content,
		State.Attribute,
		State.Content,
		State.Error,
		State.Error,
		State.Error,
		State.Prolog,
		State.PostDTD,
		State.Content,
		State.Attribute,
		State.Content,
		State.Error,
		State.Epilog,
		State.Error,
		State.Prolog,
		State.PostDTD,
		State.Content,
		State.Attribute,
		State.Content,
		State.Error,
		State.Epilog
	};

	private static readonly char[] s_selfClosingTagOpen = new char[2] { '<', '/' };

	private static readonly char[] s_closeTagEnd = new char[3] { ' ', '/', '>' };

	public Stream? BaseStream
	{
		get
		{
			if (_textWriter is StreamWriter streamWriter)
			{
				return streamWriter.BaseStream;
			}
			return null;
		}
	}

	public bool Namespaces
	{
		get
		{
			return _namespaces;
		}
		set
		{
			if (_currentState != 0)
			{
				throw new InvalidOperationException(System.SR.Xml_NotInWriteState);
			}
			_namespaces = value;
		}
	}

	public Formatting Formatting
	{
		get
		{
			return _formatting;
		}
		set
		{
			_formatting = value;
			_indented = value == Formatting.Indented;
		}
	}

	public int Indentation
	{
		get
		{
			return _indentation;
		}
		set
		{
			if (value < 0)
			{
				throw new ArgumentException(System.SR.Xml_InvalidIndentation);
			}
			_indentation = value;
		}
	}

	public char IndentChar
	{
		get
		{
			return _indentChars[0];
		}
		set
		{
			if (value == ' ')
			{
				_indentChars = s_defaultIndentChars;
				return;
			}
			if (_indentChars == s_defaultIndentChars)
			{
				_indentChars = new char[64];
			}
			for (int i = 0; i < 64; i++)
			{
				_indentChars[i] = value;
			}
		}
	}

	public char QuoteChar
	{
		get
		{
			return _quoteChar;
		}
		set
		{
			if (value != '"' && value != '\'')
			{
				throw new ArgumentException(System.SR.Xml_InvalidQuote);
			}
			_quoteChar = value;
			_xmlEncoder.QuoteChar = value;
		}
	}

	public override WriteState WriteState
	{
		get
		{
			switch (_currentState)
			{
			case State.Start:
				return WriteState.Start;
			case State.Prolog:
			case State.PostDTD:
				return WriteState.Prolog;
			case State.Element:
				return WriteState.Element;
			case State.Attribute:
			case State.AttrOnly:
				return WriteState.Attribute;
			case State.Content:
			case State.Epilog:
				return WriteState.Content;
			case State.Error:
				return WriteState.Error;
			case State.Closed:
				return WriteState.Closed;
			default:
				return WriteState.Error;
			}
		}
	}

	public override XmlSpace XmlSpace
	{
		get
		{
			for (int num = _top; num > 0; num--)
			{
				XmlSpace xmlSpace = _stack[num].xmlSpace;
				if (xmlSpace != 0)
				{
					return xmlSpace;
				}
			}
			return XmlSpace.None;
		}
	}

	public override string? XmlLang
	{
		get
		{
			for (int num = _top; num > 0; num--)
			{
				string xmlLang = _stack[num].xmlLang;
				if (xmlLang != null)
				{
					return xmlLang;
				}
			}
			return null;
		}
	}

	private static char[] CreateDefaultIndentChars()
	{
		char[] array = new char[64];
		Array.Fill(array, ' ');
		return array;
	}

	private XmlTextWriter()
	{
		_namespaces = true;
		_formatting = Formatting.None;
		_indentation = 2;
		_indentChars = s_defaultIndentChars;
		_nsStack = new Namespace[8];
		_nsTop = -1;
		_stack = new TagInfo[10];
		_top = 0;
		_stack[_top].Init(-1);
		_quoteChar = '"';
		_stateTable = s_stateTableDefault;
		_currentState = State.Start;
		_lastToken = Token.Empty;
	}

	public XmlTextWriter(Stream w, Encoding? encoding)
		: this()
	{
		_encoding = encoding;
		if (encoding != null)
		{
			_textWriter = new StreamWriter(w, encoding);
		}
		else
		{
			_textWriter = new StreamWriter(w);
		}
		_xmlEncoder = new XmlTextEncoder(_textWriter);
		_xmlEncoder.QuoteChar = _quoteChar;
	}

	public XmlTextWriter(string filename, Encoding? encoding)
		: this(new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read), encoding)
	{
	}

	public XmlTextWriter(TextWriter w)
		: this()
	{
		_textWriter = w;
		_encoding = w.Encoding;
		_xmlEncoder = new XmlTextEncoder(w);
		_xmlEncoder.QuoteChar = _quoteChar;
	}

	public override void WriteStartDocument()
	{
		StartDocument(-1);
	}

	public override void WriteStartDocument(bool standalone)
	{
		StartDocument(standalone ? 1 : 0);
	}

	public override void WriteEndDocument()
	{
		try
		{
			AutoCompleteAll();
			if (_currentState != State.Epilog)
			{
				if (_currentState == State.Closed)
				{
					throw new ArgumentException(System.SR.Xml_ClosedOrError);
				}
				throw new ArgumentException(System.SR.Xml_NoRoot);
			}
			_stateTable = s_stateTableDefault;
			_currentState = State.Start;
			_lastToken = Token.Empty;
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteDocType(string name, string? pubid, string? sysid, string? subset)
	{
		try
		{
			ValidateName(name, isNCName: false);
			AutoComplete(Token.Doctype);
			_textWriter.Write("<!DOCTYPE ");
			_textWriter.Write(name);
			if (pubid != null)
			{
				_textWriter.Write(" PUBLIC " + _quoteChar);
				_textWriter.Write(pubid);
				_textWriter.Write(_quoteChar + " " + _quoteChar);
				_textWriter.Write(sysid);
				_textWriter.Write(_quoteChar);
			}
			else if (sysid != null)
			{
				_textWriter.Write(" SYSTEM " + _quoteChar);
				_textWriter.Write(sysid);
				_textWriter.Write(_quoteChar);
			}
			if (subset != null)
			{
				_textWriter.Write("[");
				_textWriter.Write(subset);
				_textWriter.Write("]");
			}
			_textWriter.Write('>');
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteStartElement(string? prefix, string localName, string? ns)
	{
		try
		{
			AutoComplete(Token.StartElement);
			PushStack();
			_textWriter.Write('<');
			if (_namespaces)
			{
				_stack[_top].defaultNs = _stack[_top - 1].defaultNs;
				if (_stack[_top - 1].defaultNsState != 0)
				{
					_stack[_top].defaultNsState = NamespaceState.NotDeclaredButInScope;
				}
				_stack[_top].mixed = _stack[_top - 1].mixed;
				if (ns == null)
				{
					if (prefix != null && prefix.Length != 0 && LookupNamespace(prefix) == -1)
					{
						throw new ArgumentException(System.SR.Xml_UndefPrefix);
					}
				}
				else if (prefix == null)
				{
					string text = FindPrefix(ns);
					if (text != null)
					{
						prefix = text;
					}
					else
					{
						PushNamespace(null, ns, declared: false);
					}
				}
				else if (prefix.Length == 0)
				{
					PushNamespace(null, ns, declared: false);
				}
				else
				{
					if (ns.Length == 0)
					{
						prefix = null;
					}
					VerifyPrefixXml(prefix, ns);
					PushNamespace(prefix, ns, declared: false);
				}
				_stack[_top].prefix = null;
				if (prefix != null && prefix.Length != 0)
				{
					_stack[_top].prefix = prefix;
					_textWriter.Write(prefix);
					_textWriter.Write(':');
				}
			}
			else if ((ns != null && ns.Length != 0) || (prefix != null && prefix.Length != 0))
			{
				throw new ArgumentException(System.SR.Xml_NoNamespaces);
			}
			_stack[_top].name = localName;
			_textWriter.Write(localName);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteEndElement()
	{
		InternalWriteEndElement(longFormat: false);
	}

	public override void WriteFullEndElement()
	{
		InternalWriteEndElement(longFormat: true);
	}

	public override void WriteStartAttribute(string? prefix, string localName, string? ns)
	{
		try
		{
			AutoComplete(Token.StartAttribute);
			_specialAttr = SpecialAttr.None;
			if (_namespaces)
			{
				if (prefix != null && prefix.Length == 0)
				{
					prefix = null;
				}
				if (ns == "http://www.w3.org/2000/xmlns/" && prefix == null && localName != "xmlns")
				{
					prefix = "xmlns";
				}
				if (prefix == "xml")
				{
					if (localName == "lang")
					{
						_specialAttr = SpecialAttr.XmlLang;
					}
					else if (localName == "space")
					{
						_specialAttr = SpecialAttr.XmlSpace;
					}
				}
				else if (prefix == "xmlns")
				{
					if ("http://www.w3.org/2000/xmlns/" != ns && ns != null)
					{
						throw new ArgumentException(System.SR.Xml_XmlnsBelongsToReservedNs);
					}
					if (localName == null || localName.Length == 0)
					{
						localName = prefix;
						prefix = null;
						_prefixForXmlNs = null;
					}
					else
					{
						_prefixForXmlNs = localName;
					}
					_specialAttr = SpecialAttr.XmlNs;
				}
				else if (prefix == null && localName == "xmlns")
				{
					if ("http://www.w3.org/2000/xmlns/" != ns && ns != null)
					{
						throw new ArgumentException(System.SR.Xml_XmlnsBelongsToReservedNs);
					}
					_specialAttr = SpecialAttr.XmlNs;
					_prefixForXmlNs = null;
				}
				else if (ns == null)
				{
					if (prefix != null && LookupNamespace(prefix) == -1)
					{
						throw new ArgumentException(System.SR.Xml_UndefPrefix);
					}
				}
				else if (ns.Length == 0)
				{
					prefix = string.Empty;
				}
				else
				{
					VerifyPrefixXml(prefix, ns);
					if (prefix != null && LookupNamespaceInCurrentScope(prefix) != -1)
					{
						prefix = null;
					}
					string text = FindPrefix(ns);
					if (text != null && (prefix == null || prefix == text))
					{
						prefix = text;
					}
					else
					{
						if (prefix == null)
						{
							prefix = GeneratePrefix();
						}
						PushNamespace(prefix, ns, declared: false);
					}
				}
				if (prefix != null && prefix.Length != 0)
				{
					_textWriter.Write(prefix);
					_textWriter.Write(':');
				}
			}
			else
			{
				if ((ns != null && ns.Length != 0) || (prefix != null && prefix.Length != 0))
				{
					throw new ArgumentException(System.SR.Xml_NoNamespaces);
				}
				if (localName == "xml:lang")
				{
					_specialAttr = SpecialAttr.XmlLang;
				}
				else if (localName == "xml:space")
				{
					_specialAttr = SpecialAttr.XmlSpace;
				}
			}
			_xmlEncoder.StartAttribute(_specialAttr != SpecialAttr.None);
			_textWriter.Write(localName);
			_textWriter.Write('=');
			if (_curQuoteChar != _quoteChar)
			{
				_curQuoteChar = _quoteChar;
				_xmlEncoder.QuoteChar = _quoteChar;
			}
			_textWriter.Write(_curQuoteChar);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteEndAttribute()
	{
		try
		{
			AutoComplete(Token.EndAttribute);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteCData(string? text)
	{
		try
		{
			AutoComplete(Token.CData);
			if (text != null && text.Contains("]]>"))
			{
				throw new ArgumentException(System.SR.Xml_InvalidCDataChars);
			}
			_textWriter.Write("<![CDATA[");
			if (text != null)
			{
				_xmlEncoder.WriteRawWithSurrogateChecking(text);
			}
			_textWriter.Write("]]>");
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteComment(string? text)
	{
		try
		{
			if (text != null && (text.Contains("--") || (text.Length != 0 && text[text.Length - 1] == '-')))
			{
				throw new ArgumentException(System.SR.Xml_InvalidCommentChars);
			}
			AutoComplete(Token.Comment);
			_textWriter.Write("<!--");
			if (text != null)
			{
				_xmlEncoder.WriteRawWithSurrogateChecking(text);
			}
			_textWriter.Write("-->");
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteProcessingInstruction(string name, string? text)
	{
		try
		{
			if (text != null && text.Contains("?>"))
			{
				throw new ArgumentException(System.SR.Xml_InvalidPiChars);
			}
			if (string.Equals(name, "xml", StringComparison.OrdinalIgnoreCase) && _stateTable == s_stateTableDocument)
			{
				throw new ArgumentException(System.SR.Xml_DupXmlDecl);
			}
			AutoComplete(Token.PI);
			InternalWriteProcessingInstruction(name, text);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteEntityRef(string name)
	{
		try
		{
			ValidateName(name, isNCName: false);
			AutoComplete(Token.Content);
			_xmlEncoder.WriteEntityRef(name);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteCharEntity(char ch)
	{
		try
		{
			AutoComplete(Token.Content);
			_xmlEncoder.WriteCharEntity(ch);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteWhitespace(string? ws)
	{
		try
		{
			if (ws == null)
			{
				ws = string.Empty;
			}
			if (!XmlCharType.IsOnlyWhitespace(ws))
			{
				throw new ArgumentException(System.SR.Xml_NonWhitespace);
			}
			AutoComplete(Token.Whitespace);
			_xmlEncoder.Write(ws);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteString(string? text)
	{
		try
		{
			if (text != null && text.Length != 0)
			{
				AutoComplete(Token.Content);
				_xmlEncoder.Write(text);
			}
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteSurrogateCharEntity(char lowChar, char highChar)
	{
		try
		{
			AutoComplete(Token.Content);
			_xmlEncoder.WriteSurrogateCharEntity(lowChar, highChar);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteChars(char[] buffer, int index, int count)
	{
		try
		{
			AutoComplete(Token.Content);
			_xmlEncoder.Write(buffer, index, count);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteRaw(char[] buffer, int index, int count)
	{
		try
		{
			AutoComplete(Token.RawData);
			_xmlEncoder.WriteRaw(buffer, index, count);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteRaw(string data)
	{
		try
		{
			AutoComplete(Token.RawData);
			_xmlEncoder.WriteRawWithSurrogateChecking(data);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteBase64(byte[] buffer, int index, int count)
	{
		try
		{
			if (!_flush)
			{
				AutoComplete(Token.Base64);
			}
			_flush = true;
			if (_base64Encoder == null)
			{
				_base64Encoder = new XmlTextWriterBase64Encoder(_xmlEncoder);
			}
			_base64Encoder.Encode(buffer, index, count);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteBinHex(byte[] buffer, int index, int count)
	{
		try
		{
			AutoComplete(Token.Content);
			BinHexEncoder.Encode(buffer, index, count, this);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void Close()
	{
		try
		{
			AutoCompleteAll();
		}
		catch
		{
		}
		finally
		{
			_currentState = State.Closed;
			_textWriter.Dispose();
		}
	}

	public override void Flush()
	{
		_textWriter.Flush();
	}

	public override void WriteName(string name)
	{
		try
		{
			AutoComplete(Token.Content);
			InternalWriteName(name, isNCName: false);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteQualifiedName(string localName, string? ns)
	{
		try
		{
			AutoComplete(Token.Content);
			if (_namespaces)
			{
				if (ns != null && ns.Length != 0 && ns != _stack[_top].defaultNs)
				{
					string text = FindPrefix(ns);
					if (text == null)
					{
						if (_currentState != State.Attribute)
						{
							throw new ArgumentException(System.SR.Format(System.SR.Xml_UndefNamespace, ns));
						}
						text = GeneratePrefix();
						PushNamespace(text, ns, declared: false);
					}
					if (text.Length != 0)
					{
						InternalWriteName(text, isNCName: true);
						_textWriter.Write(':');
					}
				}
			}
			else if (ns != null && ns.Length != 0)
			{
				throw new ArgumentException(System.SR.Xml_NoNamespaces);
			}
			InternalWriteName(localName, isNCName: true);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override string? LookupPrefix(string ns)
	{
		if (ns == null || ns.Length == 0)
		{
			throw new ArgumentException(System.SR.Xml_EmptyName);
		}
		string text = FindPrefix(ns);
		if (text == null && ns == _stack[_top].defaultNs)
		{
			text = string.Empty;
		}
		return text;
	}

	public override void WriteNmToken(string name)
	{
		try
		{
			AutoComplete(Token.Content);
			if (name == null || name.Length == 0)
			{
				throw new ArgumentException(System.SR.Xml_EmptyName);
			}
			if (!ValidateNames.IsNmtokenNoNamespaces(name))
			{
				throw new ArgumentException(System.SR.Format(System.SR.Xml_InvalidNameChars, name));
			}
			_textWriter.Write(name);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	private void StartDocument(int standalone)
	{
		try
		{
			if (_currentState != 0)
			{
				throw new InvalidOperationException(System.SR.Xml_NotTheFirst);
			}
			_stateTable = s_stateTableDocument;
			_currentState = State.Prolog;
			StringBuilder stringBuilder = new StringBuilder(128);
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(11, 2, stringBuilder2);
			handler.AppendLiteral("version=");
			handler.AppendFormatted(_quoteChar);
			handler.AppendLiteral("1.0");
			handler.AppendFormatted(_quoteChar);
			stringBuilder2.Append(ref handler);
			if (_encoding != null)
			{
				stringBuilder.Append(" encoding=");
				stringBuilder.Append(_quoteChar);
				stringBuilder.Append(_encoding.WebName);
				stringBuilder.Append(_quoteChar);
			}
			if (standalone >= 0)
			{
				stringBuilder.Append(" standalone=");
				stringBuilder.Append(_quoteChar);
				stringBuilder.Append((standalone == 0) ? "no" : "yes");
				stringBuilder.Append(_quoteChar);
			}
			InternalWriteProcessingInstruction("xml", stringBuilder.ToString());
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	private void AutoComplete(Token token)
	{
		if (_currentState == State.Closed)
		{
			throw new InvalidOperationException(System.SR.Xml_Closed);
		}
		if (_currentState == State.Error)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.Xml_WrongToken, s_tokenName[(int)token], s_stateName[8]));
		}
		State state = _stateTable[(int)((int)token * 8 + _currentState)];
		if (state == State.Error)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.Xml_WrongToken, s_tokenName[(int)token], s_stateName[(int)_currentState]));
		}
		switch (token)
		{
		case Token.Doctype:
			if (_indented && _currentState != 0)
			{
				Indent(beforeEndElement: false);
			}
			break;
		case Token.PI:
		case Token.Comment:
		case Token.CData:
		case Token.StartElement:
			if (_currentState == State.Attribute)
			{
				WriteEndAttributeQuote();
				WriteEndStartTag(empty: false);
			}
			else if (_currentState == State.Element)
			{
				WriteEndStartTag(empty: false);
			}
			if (token == Token.CData)
			{
				_stack[_top].mixed = true;
			}
			else if (_indented && _currentState != 0)
			{
				Indent(beforeEndElement: false);
			}
			break;
		case Token.EndElement:
		case Token.LongEndElement:
			if (_flush)
			{
				FlushEncoders();
			}
			if (_currentState == State.Attribute)
			{
				WriteEndAttributeQuote();
			}
			if (_currentState == State.Content)
			{
				token = Token.LongEndElement;
			}
			else
			{
				WriteEndStartTag(token == Token.EndElement);
			}
			if (s_stateTableDocument == _stateTable && _top == 1)
			{
				state = State.Epilog;
			}
			break;
		case Token.StartAttribute:
			if (_flush)
			{
				FlushEncoders();
			}
			if (_currentState == State.Attribute)
			{
				WriteEndAttributeQuote();
				_textWriter.Write(' ');
			}
			else if (_currentState == State.Element)
			{
				_textWriter.Write(' ');
			}
			break;
		case Token.EndAttribute:
			if (_flush)
			{
				FlushEncoders();
			}
			WriteEndAttributeQuote();
			break;
		case Token.Content:
		case Token.RawData:
		case Token.Whitespace:
			if (_flush)
			{
				FlushEncoders();
			}
			goto case Token.Base64;
		case Token.Base64:
			if (_currentState == State.Element && _lastToken != Token.Content)
			{
				WriteEndStartTag(empty: false);
			}
			if (state == State.Content)
			{
				_stack[_top].mixed = true;
			}
			break;
		default:
			throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
		}
		_currentState = state;
		_lastToken = token;
	}

	private void AutoCompleteAll()
	{
		if (_flush)
		{
			FlushEncoders();
		}
		while (_top > 0)
		{
			WriteEndElement();
		}
	}

	private void InternalWriteEndElement(bool longFormat)
	{
		try
		{
			if (_top <= 0)
			{
				throw new InvalidOperationException(System.SR.Xml_NoStartTag);
			}
			AutoComplete(longFormat ? Token.LongEndElement : Token.EndElement);
			if (_lastToken == Token.LongEndElement)
			{
				if (_indented)
				{
					Indent(beforeEndElement: true);
				}
				_textWriter.Write(s_selfClosingTagOpen);
				if (_namespaces && _stack[_top].prefix != null)
				{
					_textWriter.Write(_stack[_top].prefix);
					_textWriter.Write(':');
				}
				_textWriter.Write(_stack[_top].name);
				_textWriter.Write('>');
			}
			int prevNsTop = _stack[_top].prevNsTop;
			if (_useNsHashtable && prevNsTop < _nsTop)
			{
				PopNamespaces(prevNsTop + 1, _nsTop);
			}
			_nsTop = prevNsTop;
			_top--;
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	private void WriteEndStartTag(bool empty)
	{
		_xmlEncoder.StartAttribute(cacheAttrValue: false);
		for (int num = _nsTop; num > _stack[_top].prevNsTop; num--)
		{
			if (!_nsStack[num].declared)
			{
				_textWriter.Write(" xmlns:");
				_textWriter.Write(_nsStack[num].prefix);
				_textWriter.Write('=');
				_textWriter.Write(_quoteChar);
				_xmlEncoder.Write(_nsStack[num].ns);
				_textWriter.Write(_quoteChar);
			}
		}
		if (_stack[_top].defaultNs != _stack[_top - 1].defaultNs && _stack[_top].defaultNsState == NamespaceState.DeclaredButNotWrittenOut)
		{
			_textWriter.Write(" xmlns=");
			_textWriter.Write(_quoteChar);
			_xmlEncoder.Write(_stack[_top].defaultNs);
			_textWriter.Write(_quoteChar);
			_stack[_top].defaultNsState = NamespaceState.DeclaredAndWrittenOut;
		}
		_xmlEncoder.EndAttribute();
		if (empty)
		{
			_textWriter.Write(s_closeTagEnd);
		}
		else
		{
			_textWriter.Write('>');
		}
	}

	private void WriteEndAttributeQuote()
	{
		if (_specialAttr != 0)
		{
			HandleSpecialAttribute();
		}
		_xmlEncoder.EndAttribute();
		_textWriter.Write(_curQuoteChar);
	}

	private void Indent(bool beforeEndElement)
	{
		if (_top == 0)
		{
			_textWriter.WriteLine();
		}
		else
		{
			if (_stack[_top].mixed)
			{
				return;
			}
			_textWriter.WriteLine();
			int num = (beforeEndElement ? (_top - 1) : _top) * _indentation;
			if (num <= _indentChars.Length)
			{
				_textWriter.Write(_indentChars, 0, num);
				return;
			}
			while (num > 0)
			{
				_textWriter.Write(_indentChars, 0, Math.Min(num, _indentChars.Length));
				num -= _indentChars.Length;
			}
		}
	}

	private void PushNamespace(string prefix, string ns, bool declared)
	{
		if ("http://www.w3.org/2000/xmlns/" == ns)
		{
			throw new ArgumentException(System.SR.Xml_CanNotBindToReservedNamespace);
		}
		if (prefix == null)
		{
			switch (_stack[_top].defaultNsState)
			{
			default:
				return;
			case NamespaceState.Uninitialized:
			case NamespaceState.NotDeclaredButInScope:
				_stack[_top].defaultNs = ns;
				break;
			case NamespaceState.DeclaredButNotWrittenOut:
				break;
			}
			_stack[_top].defaultNsState = (declared ? NamespaceState.DeclaredAndWrittenOut : NamespaceState.DeclaredButNotWrittenOut);
			return;
		}
		if (prefix.Length != 0 && ns.Length == 0)
		{
			throw new ArgumentException(System.SR.Xml_PrefixForEmptyNs);
		}
		int num = LookupNamespace(prefix);
		if (num != -1 && _nsStack[num].ns == ns)
		{
			if (declared)
			{
				_nsStack[num].declared = true;
			}
			return;
		}
		if (declared && num != -1 && num > _stack[_top].prevNsTop)
		{
			_nsStack[num].declared = true;
		}
		AddNamespace(prefix, ns, declared);
	}

	private void AddNamespace(string prefix, string ns, bool declared)
	{
		int num = ++_nsTop;
		if (num == _nsStack.Length)
		{
			Namespace[] array = new Namespace[num * 2];
			Array.Copy(_nsStack, array, num);
			_nsStack = array;
		}
		_nsStack[num].Set(prefix, ns, declared);
		if (_useNsHashtable)
		{
			AddToNamespaceHashtable(num);
		}
		else if (num == 16)
		{
			_nsHashtable = new Dictionary<string, int>();
			_useNsHashtable = true;
			for (int i = 0; i <= num; i++)
			{
				AddToNamespaceHashtable(i);
			}
		}
	}

	private void AddToNamespaceHashtable(int namespaceIndex)
	{
		string prefix = _nsStack[namespaceIndex].prefix;
		if (_nsHashtable.TryGetValue(prefix, out var value))
		{
			_nsStack[namespaceIndex].prevNsIndex = value;
		}
		_nsHashtable[prefix] = namespaceIndex;
	}

	private void PopNamespaces(int indexFrom, int indexTo)
	{
		for (int num = indexTo; num >= indexFrom; num--)
		{
			if (_nsStack[num].prevNsIndex == -1)
			{
				_nsHashtable.Remove(_nsStack[num].prefix);
			}
			else
			{
				_nsHashtable[_nsStack[num].prefix] = _nsStack[num].prevNsIndex;
			}
		}
	}

	private string GeneratePrefix()
	{
		return string.Concat(str3: (_stack[_top].prefixCount++ + 1).ToString("d", CultureInfo.InvariantCulture), str0: "d", str1: _top.ToString("d", CultureInfo.InvariantCulture), str2: "p");
	}

	private void InternalWriteProcessingInstruction(string name, string text)
	{
		_textWriter.Write("<?");
		ValidateName(name, isNCName: false);
		_textWriter.Write(name);
		_textWriter.Write(' ');
		if (text != null)
		{
			_xmlEncoder.WriteRawWithSurrogateChecking(text);
		}
		_textWriter.Write("?>");
	}

	private int LookupNamespace(string prefix)
	{
		if (_useNsHashtable)
		{
			if (_nsHashtable.TryGetValue(prefix, out var value))
			{
				return value;
			}
		}
		else
		{
			for (int num = _nsTop; num >= 0; num--)
			{
				if (_nsStack[num].prefix == prefix)
				{
					return num;
				}
			}
		}
		return -1;
	}

	private int LookupNamespaceInCurrentScope(string prefix)
	{
		if (_useNsHashtable)
		{
			if (_nsHashtable.TryGetValue(prefix, out var value) && value > _stack[_top].prevNsTop)
			{
				return value;
			}
		}
		else
		{
			for (int num = _nsTop; num > _stack[_top].prevNsTop; num--)
			{
				if (_nsStack[num].prefix == prefix)
				{
					return num;
				}
			}
		}
		return -1;
	}

	private string FindPrefix(string ns)
	{
		for (int num = _nsTop; num >= 0; num--)
		{
			if (_nsStack[num].ns == ns && LookupNamespace(_nsStack[num].prefix) == num)
			{
				return _nsStack[num].prefix;
			}
		}
		return null;
	}

	private void InternalWriteName(string name, bool isNCName)
	{
		ValidateName(name, isNCName);
		_textWriter.Write(name);
	}

	private void ValidateName(string name, bool isNCName)
	{
		if (name == null || name.Length == 0)
		{
			throw new ArgumentException(System.SR.Xml_EmptyName);
		}
		int length = name.Length;
		if (_namespaces)
		{
			int num = -1;
			int num2 = ValidateNames.ParseNCName(name);
			while (true)
			{
				if (num2 == length)
				{
					return;
				}
				if (name[num2] != ':' || isNCName || num != -1 || num2 <= 0 || num2 + 1 >= length)
				{
					break;
				}
				num = num2;
				num2++;
				num2 += ValidateNames.ParseNmtoken(name, num2);
			}
		}
		else if (ValidateNames.IsNameNoNamespaces(name))
		{
			return;
		}
		throw new ArgumentException(System.SR.Format(System.SR.Xml_InvalidNameChars, name));
	}

	private void HandleSpecialAttribute()
	{
		string attributeValue = _xmlEncoder.AttributeValue;
		switch (_specialAttr)
		{
		case SpecialAttr.XmlLang:
			_stack[_top].xmlLang = attributeValue;
			break;
		case SpecialAttr.XmlSpace:
			attributeValue = XmlConvert.TrimString(attributeValue);
			if (attributeValue == "default")
			{
				_stack[_top].xmlSpace = XmlSpace.Default;
				break;
			}
			if (attributeValue == "preserve")
			{
				_stack[_top].xmlSpace = XmlSpace.Preserve;
				break;
			}
			throw new ArgumentException(System.SR.Format(System.SR.Xml_InvalidXmlSpace, attributeValue));
		case SpecialAttr.XmlNs:
			VerifyPrefixXml(_prefixForXmlNs, attributeValue);
			PushNamespace(_prefixForXmlNs, attributeValue, declared: true);
			break;
		}
	}

	private void VerifyPrefixXml(string prefix, string ns)
	{
		if (prefix != null && prefix.Length == 3 && (prefix[0] == 'x' || prefix[0] == 'X') && (prefix[1] == 'm' || prefix[1] == 'M') && (prefix[2] == 'l' || prefix[2] == 'L') && "http://www.w3.org/XML/1998/namespace" != ns)
		{
			throw new ArgumentException(System.SR.Xml_InvalidPrefix);
		}
	}

	private void PushStack()
	{
		if (_top == _stack.Length - 1)
		{
			TagInfo[] array = new TagInfo[_stack.Length + 10];
			if (_top > 0)
			{
				Array.Copy(_stack, array, _top + 1);
			}
			_stack = array;
		}
		_top++;
		_stack[_top].Init(_nsTop);
	}

	private void FlushEncoders()
	{
		if (_base64Encoder != null)
		{
			_base64Encoder.Flush();
		}
		_flush = false;
	}
}
