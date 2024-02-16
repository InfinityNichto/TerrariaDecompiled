using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace System.Xml;

internal sealed class XmlWellFormedWriter : XmlWriter
{
	private enum State
	{
		Start = 0,
		TopLevel = 1,
		Document = 2,
		Element = 3,
		Content = 4,
		B64Content = 5,
		B64Attribute = 6,
		AfterRootEle = 7,
		Attribute = 8,
		SpecialAttr = 9,
		EndDocument = 10,
		RootLevelAttr = 11,
		RootLevelSpecAttr = 12,
		RootLevelB64Attr = 13,
		AfterRootLevelAttr = 14,
		Closed = 15,
		Error = 16,
		StartContent = 101,
		StartContentEle = 102,
		StartContentB64 = 103,
		StartDoc = 104,
		StartDocEle = 106,
		EndAttrSEle = 107,
		EndAttrEEle = 108,
		EndAttrSCont = 109,
		EndAttrSAttr = 111,
		PostB64Cont = 112,
		PostB64Attr = 113,
		PostB64RootAttr = 114,
		StartFragEle = 115,
		StartFragCont = 116,
		StartFragB64 = 117,
		StartRootLevelAttr = 118
	}

	private enum Token
	{
		StartDocument,
		EndDocument,
		PI,
		Comment,
		Dtd,
		StartElement,
		EndElement,
		StartAttribute,
		EndAttribute,
		Text,
		CData,
		AtomicValue,
		Base64,
		RawData,
		Whitespace
	}

	private sealed class NamespaceResolverProxy : IXmlNamespaceResolver
	{
		private readonly XmlWellFormedWriter _wfWriter;

		internal NamespaceResolverProxy(XmlWellFormedWriter wfWriter)
		{
			_wfWriter = wfWriter;
		}

		IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
		{
			throw new NotImplementedException();
		}

		string IXmlNamespaceResolver.LookupNamespace(string prefix)
		{
			return _wfWriter.LookupNamespace(prefix);
		}

		string IXmlNamespaceResolver.LookupPrefix(string namespaceName)
		{
			return _wfWriter.LookupPrefix(namespaceName);
		}
	}

	private struct ElementScope
	{
		internal int prevNSTop;

		internal string prefix;

		internal string localName;

		internal string namespaceUri;

		internal XmlSpace xmlSpace;

		internal string xmlLang;

		internal void Set(string prefix, string localName, string namespaceUri, int prevNSTop)
		{
			this.prevNSTop = prevNSTop;
			this.prefix = prefix;
			this.namespaceUri = namespaceUri;
			this.localName = localName;
			xmlSpace = (XmlSpace)(-1);
			xmlLang = null;
		}

		internal void WriteEndElement(XmlRawWriter rawWriter)
		{
			rawWriter.WriteEndElement(prefix, localName, namespaceUri);
		}

		internal void WriteFullEndElement(XmlRawWriter rawWriter)
		{
			rawWriter.WriteFullEndElement(prefix, localName, namespaceUri);
		}

		internal Task WriteEndElementAsync(XmlRawWriter rawWriter)
		{
			return rawWriter.WriteEndElementAsync(prefix, localName, namespaceUri);
		}

		internal Task WriteFullEndElementAsync(XmlRawWriter rawWriter)
		{
			return rawWriter.WriteFullEndElementAsync(prefix, localName, namespaceUri);
		}
	}

	private enum NamespaceKind
	{
		Written,
		NeedToWrite,
		Implied,
		Special
	}

	private struct Namespace
	{
		internal string prefix;

		internal string namespaceUri;

		internal NamespaceKind kind;

		internal int prevNsIndex;

		internal void Set(string prefix, string namespaceUri, NamespaceKind kind)
		{
			this.prefix = prefix;
			this.namespaceUri = namespaceUri;
			this.kind = kind;
			prevNsIndex = -1;
		}

		internal void WriteDecl(XmlWriter writer, XmlRawWriter rawWriter)
		{
			if (rawWriter != null)
			{
				rawWriter.WriteNamespaceDeclaration(prefix, namespaceUri);
				return;
			}
			if (prefix.Length == 0)
			{
				writer.WriteStartAttribute(string.Empty, "xmlns", "http://www.w3.org/2000/xmlns/");
			}
			else
			{
				writer.WriteStartAttribute("xmlns", prefix, "http://www.w3.org/2000/xmlns/");
			}
			writer.WriteString(namespaceUri);
			writer.WriteEndAttribute();
		}

		internal async Task WriteDeclAsync(XmlWriter writer, XmlRawWriter rawWriter)
		{
			if (rawWriter != null)
			{
				await rawWriter.WriteNamespaceDeclarationAsync(prefix, namespaceUri).ConfigureAwait(continueOnCapturedContext: false);
				return;
			}
			if (prefix.Length != 0)
			{
				await writer.WriteStartAttributeAsync("xmlns", prefix, "http://www.w3.org/2000/xmlns/").ConfigureAwait(continueOnCapturedContext: false);
			}
			else
			{
				await writer.WriteStartAttributeAsync(string.Empty, "xmlns", "http://www.w3.org/2000/xmlns/").ConfigureAwait(continueOnCapturedContext: false);
			}
			await writer.WriteStringAsync(namespaceUri).ConfigureAwait(continueOnCapturedContext: false);
			await writer.WriteEndAttributeAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private struct AttrName
	{
		internal string prefix;

		internal string namespaceUri;

		internal string localName;

		internal int prev;

		internal void Set(string prefix, string localName, string namespaceUri)
		{
			this.prefix = prefix;
			this.namespaceUri = namespaceUri;
			this.localName = localName;
			prev = 0;
		}

		internal bool IsDuplicate(string prefix, string localName, string namespaceUri)
		{
			if (this.localName == localName)
			{
				if (!(this.prefix == prefix))
				{
					return this.namespaceUri == namespaceUri;
				}
				return true;
			}
			return false;
		}
	}

	private enum SpecialAttribute
	{
		No,
		DefaultXmlns,
		PrefixedXmlns,
		XmlSpace,
		XmlLang
	}

	private sealed class AttributeValueCache
	{
		private enum ItemType
		{
			EntityRef,
			CharEntity,
			SurrogateCharEntity,
			Whitespace,
			String,
			StringChars,
			Raw,
			RawChars,
			ValueString
		}

		private sealed class Item
		{
			internal ItemType type;

			internal object data;

			internal Item(ItemType type, object data)
			{
				Set(type, data);
			}

			[MemberNotNull("type")]
			[MemberNotNull("data")]
			internal void Set(ItemType type, object data)
			{
				this.type = type;
				this.data = data;
			}
		}

		private sealed class BufferChunk
		{
			internal char[] buffer;

			internal int index;

			internal int count;

			internal BufferChunk(char[] buffer, int index, int count)
			{
				this.buffer = buffer;
				this.index = index;
				this.count = count;
			}
		}

		private StringBuilder _stringValue = new StringBuilder();

		private string _singleStringValue;

		private Item[] _items;

		private int _firstItem;

		private int _lastItem = -1;

		internal string StringValue
		{
			get
			{
				if (_singleStringValue != null)
				{
					return _singleStringValue;
				}
				return _stringValue.ToString();
			}
		}

		internal void WriteEntityRef(string name)
		{
			if (_singleStringValue != null)
			{
				StartComplexValue();
			}
			switch (name)
			{
			case "lt":
				_stringValue.Append('<');
				break;
			case "gt":
				_stringValue.Append('>');
				break;
			case "quot":
				_stringValue.Append('"');
				break;
			case "apos":
				_stringValue.Append('\'');
				break;
			case "amp":
				_stringValue.Append('&');
				break;
			default:
				_stringValue.Append('&');
				_stringValue.Append(name);
				_stringValue.Append(';');
				break;
			}
			AddItem(ItemType.EntityRef, name);
		}

		internal void WriteCharEntity(char ch)
		{
			if (_singleStringValue != null)
			{
				StartComplexValue();
			}
			_stringValue.Append(ch);
			AddItem(ItemType.CharEntity, ch);
		}

		internal void WriteSurrogateCharEntity(char lowChar, char highChar)
		{
			if (_singleStringValue != null)
			{
				StartComplexValue();
			}
			_stringValue.Append(highChar);
			_stringValue.Append(lowChar);
			AddItem(ItemType.SurrogateCharEntity, new char[2] { lowChar, highChar });
		}

		internal void WriteWhitespace(string ws)
		{
			if (_singleStringValue != null)
			{
				StartComplexValue();
			}
			_stringValue.Append(ws);
			AddItem(ItemType.Whitespace, ws);
		}

		internal void WriteString(string text)
		{
			if (_singleStringValue != null)
			{
				StartComplexValue();
			}
			else if (_lastItem == -1)
			{
				_singleStringValue = text;
				return;
			}
			_stringValue.Append(text);
			AddItem(ItemType.String, text);
		}

		internal void WriteChars(char[] buffer, int index, int count)
		{
			if (_singleStringValue != null)
			{
				StartComplexValue();
			}
			_stringValue.Append(buffer, index, count);
			AddItem(ItemType.StringChars, new BufferChunk(buffer, index, count));
		}

		internal void WriteRaw(char[] buffer, int index, int count)
		{
			if (_singleStringValue != null)
			{
				StartComplexValue();
			}
			_stringValue.Append(buffer, index, count);
			AddItem(ItemType.RawChars, new BufferChunk(buffer, index, count));
		}

		internal void WriteRaw(string data)
		{
			if (_singleStringValue != null)
			{
				StartComplexValue();
			}
			_stringValue.Append(data);
			AddItem(ItemType.Raw, data);
		}

		internal void WriteValue(string value)
		{
			if (_singleStringValue != null)
			{
				StartComplexValue();
			}
			_stringValue.Append(value);
			AddItem(ItemType.ValueString, value);
		}

		internal void Replay(XmlWriter writer)
		{
			if (_singleStringValue != null)
			{
				writer.WriteString(_singleStringValue);
				return;
			}
			for (int i = _firstItem; i <= _lastItem; i++)
			{
				Item item = _items[i];
				switch (item.type)
				{
				case ItemType.EntityRef:
					writer.WriteEntityRef((string)item.data);
					break;
				case ItemType.CharEntity:
					writer.WriteCharEntity((char)item.data);
					break;
				case ItemType.SurrogateCharEntity:
				{
					char[] array = (char[])item.data;
					writer.WriteSurrogateCharEntity(array[0], array[1]);
					break;
				}
				case ItemType.Whitespace:
					writer.WriteWhitespace((string)item.data);
					break;
				case ItemType.String:
					writer.WriteString((string)item.data);
					break;
				case ItemType.StringChars:
				{
					BufferChunk bufferChunk = (BufferChunk)item.data;
					writer.WriteChars(bufferChunk.buffer, bufferChunk.index, bufferChunk.count);
					break;
				}
				case ItemType.Raw:
					writer.WriteRaw((string)item.data);
					break;
				case ItemType.RawChars:
				{
					BufferChunk bufferChunk = (BufferChunk)item.data;
					writer.WriteChars(bufferChunk.buffer, bufferChunk.index, bufferChunk.count);
					break;
				}
				case ItemType.ValueString:
					writer.WriteValue((string)item.data);
					break;
				}
			}
		}

		internal void Trim()
		{
			if (_singleStringValue != null)
			{
				_singleStringValue = XmlConvert.TrimString(_singleStringValue);
				return;
			}
			string text = _stringValue.ToString();
			string text2 = XmlConvert.TrimString(text);
			if (text != text2)
			{
				_stringValue = new StringBuilder(text2);
			}
			int i;
			for (i = _firstItem; i == _firstItem && i <= _lastItem; i++)
			{
				Item item = _items[i];
				switch (item.type)
				{
				case ItemType.Whitespace:
					_firstItem++;
					break;
				case ItemType.String:
				case ItemType.Raw:
				case ItemType.ValueString:
					item.data = XmlConvert.TrimStringStart((string)item.data);
					if (((string)item.data).Length == 0)
					{
						_firstItem++;
					}
					break;
				case ItemType.StringChars:
				case ItemType.RawChars:
				{
					BufferChunk bufferChunk = (BufferChunk)item.data;
					int num = bufferChunk.index + bufferChunk.count;
					while (bufferChunk.index < num && XmlCharType.IsWhiteSpace(bufferChunk.buffer[bufferChunk.index]))
					{
						bufferChunk.index++;
						bufferChunk.count--;
					}
					if (bufferChunk.index == num)
					{
						_firstItem++;
					}
					break;
				}
				}
			}
			i = _lastItem;
			while (i == _lastItem && i >= _firstItem)
			{
				Item item2 = _items[i];
				switch (item2.type)
				{
				case ItemType.Whitespace:
					_lastItem--;
					break;
				case ItemType.String:
				case ItemType.Raw:
				case ItemType.ValueString:
					item2.data = XmlConvert.TrimStringEnd((string)item2.data);
					if (((string)item2.data).Length == 0)
					{
						_lastItem--;
					}
					break;
				case ItemType.StringChars:
				case ItemType.RawChars:
				{
					BufferChunk bufferChunk2 = (BufferChunk)item2.data;
					while (bufferChunk2.count > 0 && XmlCharType.IsWhiteSpace(bufferChunk2.buffer[bufferChunk2.index + bufferChunk2.count - 1]))
					{
						bufferChunk2.count--;
					}
					if (bufferChunk2.count == 0)
					{
						_lastItem--;
					}
					break;
				}
				}
				i--;
			}
		}

		internal void Clear()
		{
			_singleStringValue = null;
			_lastItem = -1;
			_firstItem = 0;
			_stringValue.Length = 0;
		}

		private void StartComplexValue()
		{
			_stringValue.Append(_singleStringValue);
			AddItem(ItemType.String, _singleStringValue);
			_singleStringValue = null;
		}

		private void AddItem(ItemType type, object data)
		{
			int num = _lastItem + 1;
			if (_items == null)
			{
				_items = new Item[4];
			}
			else if (_items.Length == num)
			{
				Item[] array = new Item[num * 2];
				Array.Copy(_items, array, num);
				_items = array;
			}
			if (_items[num] == null)
			{
				_items[num] = new Item(type, data);
			}
			else
			{
				_items[num].Set(type, data);
			}
			_lastItem = num;
		}

		internal async Task ReplayAsync(XmlWriter writer)
		{
			if (_singleStringValue != null)
			{
				await writer.WriteStringAsync(_singleStringValue).ConfigureAwait(continueOnCapturedContext: false);
				return;
			}
			for (int i = _firstItem; i <= _lastItem; i++)
			{
				Item item = _items[i];
				switch (item.type)
				{
				case ItemType.EntityRef:
					await writer.WriteEntityRefAsync((string)item.data).ConfigureAwait(continueOnCapturedContext: false);
					break;
				case ItemType.CharEntity:
					await writer.WriteCharEntityAsync((char)item.data).ConfigureAwait(continueOnCapturedContext: false);
					break;
				case ItemType.SurrogateCharEntity:
				{
					char[] array = (char[])item.data;
					await writer.WriteSurrogateCharEntityAsync(array[0], array[1]).ConfigureAwait(continueOnCapturedContext: false);
					break;
				}
				case ItemType.Whitespace:
					await writer.WriteWhitespaceAsync((string)item.data).ConfigureAwait(continueOnCapturedContext: false);
					break;
				case ItemType.String:
					await writer.WriteStringAsync((string)item.data).ConfigureAwait(continueOnCapturedContext: false);
					break;
				case ItemType.StringChars:
				{
					BufferChunk bufferChunk = (BufferChunk)item.data;
					await writer.WriteCharsAsync(bufferChunk.buffer, bufferChunk.index, bufferChunk.count).ConfigureAwait(continueOnCapturedContext: false);
					break;
				}
				case ItemType.Raw:
					await writer.WriteRawAsync((string)item.data).ConfigureAwait(continueOnCapturedContext: false);
					break;
				case ItemType.RawChars:
				{
					BufferChunk bufferChunk = (BufferChunk)item.data;
					await writer.WriteCharsAsync(bufferChunk.buffer, bufferChunk.index, bufferChunk.count).ConfigureAwait(continueOnCapturedContext: false);
					break;
				}
				case ItemType.ValueString:
					await writer.WriteStringAsync((string)item.data).ConfigureAwait(continueOnCapturedContext: false);
					break;
				}
			}
		}
	}

	private readonly XmlWriter _writer;

	private readonly XmlRawWriter _rawWriter;

	private readonly IXmlNamespaceResolver _predefinedNamespaces;

	private Namespace[] _nsStack;

	private int _nsTop;

	private Dictionary<string, int> _nsHashtable;

	private bool _useNsHashtable;

	private ElementScope[] _elemScopeStack;

	private int _elemTop;

	private AttrName[] _attrStack;

	private int _attrCount;

	private Dictionary<string, int> _attrHashTable;

	private SpecialAttribute _specAttr;

	private AttributeValueCache _attrValueCache;

	private string _curDeclPrefix;

	private State[] _stateTable;

	private State _currentState;

	private readonly bool _checkCharacters;

	private readonly bool _omitDuplNamespaces;

	private readonly bool _writeEndDocumentOnClose;

	private ConformanceLevel _conformanceLevel;

	private bool _dtdWritten;

	private bool _xmlDeclFollows;

	internal static readonly string[] stateName = new string[17]
	{
		"Start", "TopLevel", "Document", "Element Start Tag", "Element Content", "Element Content", "Attribute", "EndRootElement", "Attribute", "Special Attribute",
		"End Document", "Root Level Attribute Value", "Root Level Special Attribute Value", "Root Level Base64 Attribute Value", "After Root Level Attribute", "Closed", "Error"
	};

	internal static readonly string[] tokenName = new string[15]
	{
		"StartDocument", "EndDocument", "PI", "Comment", "DTD", "StartElement", "EndElement", "StartAttribute", "EndAttribute", "Text",
		"CDATA", "Atomic value", "Base64", "RawData", "Whitespace"
	};

	private static readonly WriteState[] s_state2WriteState = new WriteState[17]
	{
		WriteState.Start,
		WriteState.Prolog,
		WriteState.Prolog,
		WriteState.Element,
		WriteState.Content,
		WriteState.Content,
		WriteState.Attribute,
		WriteState.Content,
		WriteState.Attribute,
		WriteState.Attribute,
		WriteState.Content,
		WriteState.Attribute,
		WriteState.Attribute,
		WriteState.Attribute,
		WriteState.Attribute,
		WriteState.Closed,
		WriteState.Error
	};

	private static readonly State[] s_stateTableDocument = new State[240]
	{
		State.Document,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.PostB64Cont,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.PostB64Cont,
		State.Error,
		State.EndDocument,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.StartDoc,
		State.TopLevel,
		State.Document,
		State.StartContent,
		State.Content,
		State.PostB64Cont,
		State.PostB64Attr,
		State.AfterRootEle,
		State.EndAttrSCont,
		State.EndAttrSCont,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.StartDoc,
		State.TopLevel,
		State.Document,
		State.StartContent,
		State.Content,
		State.PostB64Cont,
		State.PostB64Attr,
		State.AfterRootEle,
		State.EndAttrSCont,
		State.EndAttrSCont,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.StartDoc,
		State.TopLevel,
		State.Document,
		State.Error,
		State.Error,
		State.PostB64Cont,
		State.PostB64Attr,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.StartDocEle,
		State.Element,
		State.Element,
		State.StartContentEle,
		State.Element,
		State.PostB64Cont,
		State.PostB64Attr,
		State.Error,
		State.EndAttrSEle,
		State.EndAttrSEle,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.StartContent,
		State.Content,
		State.PostB64Cont,
		State.PostB64Attr,
		State.Error,
		State.EndAttrEEle,
		State.EndAttrEEle,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Attribute,
		State.Error,
		State.PostB64Cont,
		State.PostB64Attr,
		State.Error,
		State.EndAttrSAttr,
		State.EndAttrSAttr,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.PostB64Cont,
		State.PostB64Attr,
		State.Error,
		State.Element,
		State.Element,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.StartContent,
		State.Content,
		State.PostB64Cont,
		State.PostB64Attr,
		State.Error,
		State.Attribute,
		State.SpecialAttr,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.StartContent,
		State.Content,
		State.PostB64Cont,
		State.PostB64Attr,
		State.Error,
		State.EndAttrSCont,
		State.EndAttrSCont,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.StartContent,
		State.Content,
		State.PostB64Cont,
		State.PostB64Attr,
		State.Error,
		State.Attribute,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.StartContentB64,
		State.B64Content,
		State.B64Content,
		State.B64Attribute,
		State.Error,
		State.B64Attribute,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.StartDoc,
		State.Error,
		State.Document,
		State.StartContent,
		State.Content,
		State.PostB64Cont,
		State.PostB64Attr,
		State.AfterRootEle,
		State.Attribute,
		State.SpecialAttr,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.StartDoc,
		State.TopLevel,
		State.Document,
		State.StartContent,
		State.Content,
		State.PostB64Cont,
		State.PostB64Attr,
		State.AfterRootEle,
		State.Attribute,
		State.SpecialAttr,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error
	};

	private static readonly State[] s_stateTableAuto = new State[240]
	{
		State.Document,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.PostB64Cont,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.PostB64Cont,
		State.Error,
		State.EndDocument,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.TopLevel,
		State.TopLevel,
		State.Error,
		State.StartContent,
		State.Content,
		State.PostB64Cont,
		State.PostB64Attr,
		State.AfterRootEle,
		State.EndAttrSCont,
		State.EndAttrSCont,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.TopLevel,
		State.TopLevel,
		State.Error,
		State.StartContent,
		State.Content,
		State.PostB64Cont,
		State.PostB64Attr,
		State.AfterRootEle,
		State.EndAttrSCont,
		State.EndAttrSCont,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.StartDoc,
		State.TopLevel,
		State.Error,
		State.Error,
		State.Error,
		State.PostB64Cont,
		State.PostB64Attr,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.StartFragEle,
		State.Element,
		State.Error,
		State.StartContentEle,
		State.Element,
		State.PostB64Cont,
		State.PostB64Attr,
		State.Element,
		State.EndAttrSEle,
		State.EndAttrSEle,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.StartContent,
		State.Content,
		State.PostB64Cont,
		State.PostB64Attr,
		State.Error,
		State.EndAttrEEle,
		State.EndAttrEEle,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.RootLevelAttr,
		State.Error,
		State.Error,
		State.Attribute,
		State.Error,
		State.PostB64Cont,
		State.PostB64Attr,
		State.Error,
		State.EndAttrSAttr,
		State.EndAttrSAttr,
		State.Error,
		State.StartRootLevelAttr,
		State.StartRootLevelAttr,
		State.PostB64RootAttr,
		State.RootLevelAttr,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.PostB64Cont,
		State.PostB64Attr,
		State.Error,
		State.Element,
		State.Element,
		State.Error,
		State.AfterRootLevelAttr,
		State.AfterRootLevelAttr,
		State.PostB64RootAttr,
		State.Error,
		State.Error,
		State.StartFragCont,
		State.StartFragCont,
		State.Error,
		State.StartContent,
		State.Content,
		State.PostB64Cont,
		State.PostB64Attr,
		State.Content,
		State.Attribute,
		State.SpecialAttr,
		State.Error,
		State.RootLevelAttr,
		State.RootLevelSpecAttr,
		State.PostB64RootAttr,
		State.Error,
		State.Error,
		State.StartFragCont,
		State.StartFragCont,
		State.Error,
		State.StartContent,
		State.Content,
		State.PostB64Cont,
		State.PostB64Attr,
		State.Content,
		State.EndAttrSCont,
		State.EndAttrSCont,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.StartFragCont,
		State.StartFragCont,
		State.Error,
		State.StartContent,
		State.Content,
		State.PostB64Cont,
		State.PostB64Attr,
		State.Content,
		State.Attribute,
		State.Error,
		State.Error,
		State.RootLevelAttr,
		State.Error,
		State.PostB64RootAttr,
		State.Error,
		State.Error,
		State.StartFragB64,
		State.StartFragB64,
		State.Error,
		State.StartContentB64,
		State.B64Content,
		State.B64Content,
		State.B64Attribute,
		State.B64Content,
		State.B64Attribute,
		State.Error,
		State.Error,
		State.RootLevelB64Attr,
		State.Error,
		State.RootLevelB64Attr,
		State.Error,
		State.Error,
		State.StartFragCont,
		State.TopLevel,
		State.Error,
		State.StartContent,
		State.Content,
		State.PostB64Cont,
		State.PostB64Attr,
		State.Content,
		State.Attribute,
		State.SpecialAttr,
		State.Error,
		State.RootLevelAttr,
		State.RootLevelSpecAttr,
		State.PostB64RootAttr,
		State.AfterRootLevelAttr,
		State.Error,
		State.TopLevel,
		State.TopLevel,
		State.Error,
		State.StartContent,
		State.Content,
		State.PostB64Cont,
		State.PostB64Attr,
		State.AfterRootEle,
		State.Attribute,
		State.SpecialAttr,
		State.Error,
		State.RootLevelAttr,
		State.RootLevelSpecAttr,
		State.PostB64RootAttr,
		State.AfterRootLevelAttr,
		State.Error
	};

	public override WriteState WriteState
	{
		get
		{
			if (_currentState <= State.Error)
			{
				return s_state2WriteState[(int)_currentState];
			}
			return WriteState.Error;
		}
	}

	public override XmlWriterSettings Settings
	{
		get
		{
			XmlWriterSettings settings = _writer.Settings;
			settings.ReadOnly = false;
			settings.ConformanceLevel = _conformanceLevel;
			if (_omitDuplNamespaces)
			{
				settings.NamespaceHandling |= NamespaceHandling.OmitDuplicates;
			}
			settings.WriteEndDocumentOnClose = _writeEndDocumentOnClose;
			settings.ReadOnly = true;
			return settings;
		}
	}

	public override XmlSpace XmlSpace
	{
		get
		{
			int num = _elemTop;
			while (num >= 0 && _elemScopeStack[num].xmlSpace == (XmlSpace)(-1))
			{
				num--;
			}
			return _elemScopeStack[num].xmlSpace;
		}
	}

	public override string XmlLang
	{
		get
		{
			int num = _elemTop;
			while (num > 0 && _elemScopeStack[num].xmlLang == null)
			{
				num--;
			}
			return _elemScopeStack[num].xmlLang;
		}
	}

	internal XmlRawWriter RawWriter => _rawWriter;

	private bool SaveAttrValue => _specAttr != SpecialAttribute.No;

	private bool InBase64
	{
		get
		{
			if (_currentState != State.B64Content && _currentState != State.B64Attribute)
			{
				return _currentState == State.RootLevelB64Attr;
			}
			return true;
		}
	}

	private bool IsClosedOrErrorState => _currentState >= State.Closed;

	internal XmlWellFormedWriter(XmlWriter writer, XmlWriterSettings settings)
	{
		_writer = writer;
		_rawWriter = writer as XmlRawWriter;
		_predefinedNamespaces = writer as IXmlNamespaceResolver;
		if (_rawWriter != null)
		{
			_rawWriter.NamespaceResolver = new NamespaceResolverProxy(this);
		}
		_checkCharacters = settings.CheckCharacters;
		_omitDuplNamespaces = (settings.NamespaceHandling & NamespaceHandling.OmitDuplicates) != 0;
		_writeEndDocumentOnClose = settings.WriteEndDocumentOnClose;
		_conformanceLevel = settings.ConformanceLevel;
		_stateTable = ((_conformanceLevel == ConformanceLevel.Document) ? s_stateTableDocument : s_stateTableAuto);
		_currentState = State.Start;
		_nsStack = new Namespace[8];
		_nsStack[0].Set("xmlns", "http://www.w3.org/2000/xmlns/", NamespaceKind.Special);
		_nsStack[1].Set("xml", "http://www.w3.org/XML/1998/namespace", NamespaceKind.Special);
		if (_predefinedNamespaces == null)
		{
			_nsStack[2].Set(string.Empty, string.Empty, NamespaceKind.Implied);
		}
		else
		{
			string text = _predefinedNamespaces.LookupNamespace(string.Empty);
			_nsStack[2].Set(string.Empty, (text == null) ? string.Empty : text, NamespaceKind.Implied);
		}
		_nsTop = 2;
		_elemScopeStack = new ElementScope[8];
		_elemScopeStack[0].Set(string.Empty, string.Empty, string.Empty, _nsTop);
		_elemScopeStack[0].xmlSpace = XmlSpace.None;
		_elemScopeStack[0].xmlLang = null;
		_elemTop = 0;
		_attrStack = new AttrName[8];
	}

	public override void WriteStartDocument()
	{
		WriteStartDocumentImpl(XmlStandalone.Omit);
	}

	public override void WriteStartDocument(bool standalone)
	{
		WriteStartDocumentImpl(standalone ? XmlStandalone.Yes : XmlStandalone.No);
	}

	public override void WriteEndDocument()
	{
		try
		{
			while (_elemTop > 0)
			{
				WriteEndElement();
			}
			State currentState = _currentState;
			AdvanceState(Token.EndDocument);
			if (currentState != State.AfterRootEle)
			{
				throw new ArgumentException(System.SR.Xml_NoRoot);
			}
			if (_rawWriter == null)
			{
				_writer.WriteEndDocument();
			}
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteDocType(string name, string pubid, string sysid, string subset)
	{
		try
		{
			if (name == null || name.Length == 0)
			{
				throw new ArgumentException(System.SR.Xml_EmptyName);
			}
			XmlConvert.VerifyQName(name, ExceptionType.XmlException);
			if (_conformanceLevel == ConformanceLevel.Fragment)
			{
				throw new InvalidOperationException(System.SR.Xml_DtdNotAllowedInFragment);
			}
			AdvanceState(Token.Dtd);
			if (_dtdWritten)
			{
				_currentState = State.Error;
				throw new InvalidOperationException(System.SR.Xml_DtdAlreadyWritten);
			}
			if (_conformanceLevel == ConformanceLevel.Auto)
			{
				_conformanceLevel = ConformanceLevel.Document;
				_stateTable = s_stateTableDocument;
			}
			if (_checkCharacters)
			{
				int invCharIndex;
				if (pubid != null && (invCharIndex = XmlCharType.IsPublicId(pubid)) >= 0)
				{
					string xml_InvalidCharacter = System.SR.Xml_InvalidCharacter;
					object[] args = XmlException.BuildCharExceptionArgs(pubid, invCharIndex);
					throw new ArgumentException(System.SR.Format(xml_InvalidCharacter, args), "pubid");
				}
				if (sysid != null && (invCharIndex = XmlCharType.IsOnlyCharData(sysid)) >= 0)
				{
					string xml_InvalidCharacter2 = System.SR.Xml_InvalidCharacter;
					object[] args = XmlException.BuildCharExceptionArgs(sysid, invCharIndex);
					throw new ArgumentException(System.SR.Format(xml_InvalidCharacter2, args), "sysid");
				}
				if (subset != null && (invCharIndex = XmlCharType.IsOnlyCharData(subset)) >= 0)
				{
					string xml_InvalidCharacter3 = System.SR.Xml_InvalidCharacter;
					object[] args = XmlException.BuildCharExceptionArgs(subset, invCharIndex);
					throw new ArgumentException(System.SR.Format(xml_InvalidCharacter3, args), "subset");
				}
			}
			_writer.WriteDocType(name, pubid, sysid, subset);
			_dtdWritten = true;
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteStartElement(string prefix, string localName, string ns)
	{
		try
		{
			if (localName == null || localName.Length == 0)
			{
				throw new ArgumentException(System.SR.Xml_EmptyLocalName);
			}
			CheckNCName(localName);
			AdvanceState(Token.StartElement);
			if (prefix == null)
			{
				if (ns != null)
				{
					prefix = LookupPrefix(ns);
				}
				if (prefix == null)
				{
					prefix = string.Empty;
				}
			}
			else if (prefix.Length > 0)
			{
				CheckNCName(prefix);
				if (ns == null)
				{
					ns = LookupNamespace(prefix);
				}
				if (ns == null || (ns != null && ns.Length == 0))
				{
					throw new ArgumentException(System.SR.Xml_PrefixForEmptyNs);
				}
			}
			if (ns == null)
			{
				ns = LookupNamespace(prefix);
				if (ns == null)
				{
					ns = string.Empty;
				}
			}
			if (_elemTop == 0 && _rawWriter != null)
			{
				_rawWriter.OnRootElement(_conformanceLevel);
			}
			_writer.WriteStartElement(prefix, localName, ns);
			int num = ++_elemTop;
			if (num == _elemScopeStack.Length)
			{
				ElementScope[] array = new ElementScope[num * 2];
				Array.Copy(_elemScopeStack, array, num);
				_elemScopeStack = array;
			}
			_elemScopeStack[num].Set(prefix, localName, ns, _nsTop);
			PushNamespaceImplicit(prefix, ns);
			if (_attrCount >= 14)
			{
				_attrHashTable.Clear();
			}
			_attrCount = 0;
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteEndElement()
	{
		try
		{
			AdvanceState(Token.EndElement);
			int elemTop = _elemTop;
			if (elemTop == 0)
			{
				throw new XmlException(System.SR.Xml_NoStartTag, string.Empty);
			}
			if (_rawWriter != null)
			{
				_elemScopeStack[elemTop].WriteEndElement(_rawWriter);
			}
			else
			{
				_writer.WriteEndElement();
			}
			int prevNSTop = _elemScopeStack[elemTop].prevNSTop;
			if (_useNsHashtable && prevNSTop < _nsTop)
			{
				PopNamespaces(prevNSTop + 1, _nsTop);
			}
			_nsTop = prevNSTop;
			if ((_elemTop = elemTop - 1) == 0)
			{
				if (_conformanceLevel == ConformanceLevel.Document)
				{
					_currentState = State.AfterRootEle;
				}
				else
				{
					_currentState = State.TopLevel;
				}
			}
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteFullEndElement()
	{
		try
		{
			AdvanceState(Token.EndElement);
			int elemTop = _elemTop;
			if (elemTop == 0)
			{
				throw new XmlException(System.SR.Xml_NoStartTag, string.Empty);
			}
			if (_rawWriter != null)
			{
				_elemScopeStack[elemTop].WriteFullEndElement(_rawWriter);
			}
			else
			{
				_writer.WriteFullEndElement();
			}
			int prevNSTop = _elemScopeStack[elemTop].prevNSTop;
			if (_useNsHashtable && prevNSTop < _nsTop)
			{
				PopNamespaces(prevNSTop + 1, _nsTop);
			}
			_nsTop = prevNSTop;
			if ((_elemTop = elemTop - 1) == 0)
			{
				if (_conformanceLevel == ConformanceLevel.Document)
				{
					_currentState = State.AfterRootEle;
				}
				else
				{
					_currentState = State.TopLevel;
				}
			}
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteStartAttribute(string prefix, string localName, string namespaceName)
	{
		try
		{
			if (localName == null || localName.Length == 0)
			{
				if (!(prefix == "xmlns"))
				{
					throw new ArgumentException(System.SR.Xml_EmptyLocalName);
				}
				localName = "xmlns";
				prefix = string.Empty;
			}
			CheckNCName(localName);
			AdvanceState(Token.StartAttribute);
			if (prefix == null)
			{
				if (namespaceName != null && (!(localName == "xmlns") || !(namespaceName == "http://www.w3.org/2000/xmlns/")))
				{
					prefix = LookupPrefix(namespaceName);
				}
				if (prefix == null)
				{
					prefix = string.Empty;
				}
			}
			if (namespaceName == null)
			{
				if (prefix.Length > 0)
				{
					namespaceName = LookupNamespace(prefix);
				}
				if (namespaceName == null)
				{
					namespaceName = string.Empty;
				}
			}
			if (prefix.Length == 0)
			{
				if (localName[0] != 'x' || !(localName == "xmlns"))
				{
					if (namespaceName.Length > 0)
					{
						prefix = LookupPrefix(namespaceName);
						if (prefix == null || prefix.Length == 0)
						{
							prefix = GeneratePrefix();
						}
					}
					goto IL_01fd;
				}
				if (namespaceName.Length > 0 && namespaceName != "http://www.w3.org/2000/xmlns/")
				{
					throw new ArgumentException(System.SR.Xml_XmlnsPrefix);
				}
				_curDeclPrefix = string.Empty;
				SetSpecialAttribute(SpecialAttribute.DefaultXmlns);
			}
			else
			{
				if (prefix[0] != 'x')
				{
					goto IL_01c9;
				}
				if (prefix == "xmlns")
				{
					if (namespaceName.Length > 0 && namespaceName != "http://www.w3.org/2000/xmlns/")
					{
						throw new ArgumentException(System.SR.Xml_XmlnsPrefix);
					}
					_curDeclPrefix = localName;
					SetSpecialAttribute(SpecialAttribute.PrefixedXmlns);
				}
				else
				{
					if (!(prefix == "xml"))
					{
						goto IL_01c9;
					}
					if (namespaceName.Length > 0 && namespaceName != "http://www.w3.org/XML/1998/namespace")
					{
						throw new ArgumentException(System.SR.Xml_XmlPrefix);
					}
					if (!(localName == "space"))
					{
						if (!(localName == "lang"))
						{
							goto IL_01c9;
						}
						SetSpecialAttribute(SpecialAttribute.XmlLang);
					}
					else
					{
						SetSpecialAttribute(SpecialAttribute.XmlSpace);
					}
				}
			}
			goto IL_020d;
			IL_01c9:
			CheckNCName(prefix);
			if (namespaceName.Length == 0)
			{
				prefix = string.Empty;
			}
			else
			{
				string text = LookupLocalNamespace(prefix);
				if (text != null && text != namespaceName)
				{
					prefix = GeneratePrefix();
				}
			}
			goto IL_01fd;
			IL_01fd:
			if (prefix.Length != 0)
			{
				PushNamespaceImplicit(prefix, namespaceName);
			}
			goto IL_020d;
			IL_020d:
			AddAttribute(prefix, localName, namespaceName);
			if (_specAttr == SpecialAttribute.No)
			{
				_writer.WriteStartAttribute(prefix, localName, namespaceName);
			}
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
			AdvanceState(Token.EndAttribute);
			if (_specAttr != 0)
			{
				switch (_specAttr)
				{
				case SpecialAttribute.DefaultXmlns:
				{
					string stringValue = _attrValueCache.StringValue;
					if (PushNamespaceExplicit(string.Empty, stringValue))
					{
						if (_rawWriter != null)
						{
							if (_rawWriter.SupportsNamespaceDeclarationInChunks)
							{
								_rawWriter.WriteStartNamespaceDeclaration(string.Empty);
								_attrValueCache.Replay(_rawWriter);
								_rawWriter.WriteEndNamespaceDeclaration();
							}
							else
							{
								_rawWriter.WriteNamespaceDeclaration(string.Empty, stringValue);
							}
						}
						else
						{
							_writer.WriteStartAttribute(string.Empty, "xmlns", "http://www.w3.org/2000/xmlns/");
							_attrValueCache.Replay(_writer);
							_writer.WriteEndAttribute();
						}
					}
					_curDeclPrefix = null;
					break;
				}
				case SpecialAttribute.PrefixedXmlns:
				{
					string stringValue = _attrValueCache.StringValue;
					if (stringValue.Length == 0)
					{
						throw new ArgumentException(System.SR.Xml_PrefixForEmptyNs);
					}
					if (stringValue == "http://www.w3.org/2000/xmlns/" || (stringValue == "http://www.w3.org/XML/1998/namespace" && _curDeclPrefix != "xml"))
					{
						throw new ArgumentException(System.SR.Xml_CanNotBindToReservedNamespace);
					}
					if (PushNamespaceExplicit(_curDeclPrefix, stringValue))
					{
						if (_rawWriter != null)
						{
							if (_rawWriter.SupportsNamespaceDeclarationInChunks)
							{
								_rawWriter.WriteStartNamespaceDeclaration(_curDeclPrefix);
								_attrValueCache.Replay(_rawWriter);
								_rawWriter.WriteEndNamespaceDeclaration();
							}
							else
							{
								_rawWriter.WriteNamespaceDeclaration(_curDeclPrefix, stringValue);
							}
						}
						else
						{
							_writer.WriteStartAttribute("xmlns", _curDeclPrefix, "http://www.w3.org/2000/xmlns/");
							_attrValueCache.Replay(_writer);
							_writer.WriteEndAttribute();
						}
					}
					_curDeclPrefix = null;
					break;
				}
				case SpecialAttribute.XmlSpace:
				{
					_attrValueCache.Trim();
					string stringValue = _attrValueCache.StringValue;
					if (stringValue == "default")
					{
						_elemScopeStack[_elemTop].xmlSpace = XmlSpace.Default;
					}
					else
					{
						if (!(stringValue == "preserve"))
						{
							throw new ArgumentException(System.SR.Format(System.SR.Xml_InvalidXmlSpace, stringValue));
						}
						_elemScopeStack[_elemTop].xmlSpace = XmlSpace.Preserve;
					}
					_writer.WriteStartAttribute("xml", "space", "http://www.w3.org/XML/1998/namespace");
					_attrValueCache.Replay(_writer);
					_writer.WriteEndAttribute();
					break;
				}
				case SpecialAttribute.XmlLang:
				{
					string stringValue = _attrValueCache.StringValue;
					_elemScopeStack[_elemTop].xmlLang = stringValue;
					_writer.WriteStartAttribute("xml", "lang", "http://www.w3.org/XML/1998/namespace");
					_attrValueCache.Replay(_writer);
					_writer.WriteEndAttribute();
					break;
				}
				}
				_specAttr = SpecialAttribute.No;
				_attrValueCache.Clear();
			}
			else
			{
				_writer.WriteEndAttribute();
			}
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteCData(string text)
	{
		try
		{
			if (text == null)
			{
				text = string.Empty;
			}
			AdvanceState(Token.CData);
			_writer.WriteCData(text);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteComment(string text)
	{
		try
		{
			if (text == null)
			{
				text = string.Empty;
			}
			AdvanceState(Token.Comment);
			_writer.WriteComment(text);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteProcessingInstruction(string name, string text)
	{
		try
		{
			if (name == null || name.Length == 0)
			{
				throw new ArgumentException(System.SR.Xml_EmptyName);
			}
			CheckNCName(name);
			if (text == null)
			{
				text = string.Empty;
			}
			if (name.Length == 3 && string.Equals(name, "xml", StringComparison.OrdinalIgnoreCase))
			{
				if (_currentState != 0)
				{
					throw new ArgumentException((_conformanceLevel == ConformanceLevel.Document) ? System.SR.Xml_DupXmlDecl : System.SR.Xml_CannotWriteXmlDecl);
				}
				_xmlDeclFollows = true;
				AdvanceState(Token.PI);
				if (_rawWriter != null)
				{
					_rawWriter.WriteXmlDeclaration(text);
				}
				else
				{
					_writer.WriteProcessingInstruction(name, text);
				}
			}
			else
			{
				AdvanceState(Token.PI);
				_writer.WriteProcessingInstruction(name, text);
			}
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
			if (name == null || name.Length == 0)
			{
				throw new ArgumentException(System.SR.Xml_EmptyName);
			}
			CheckNCName(name);
			AdvanceState(Token.Text);
			if (SaveAttrValue)
			{
				_attrValueCache.WriteEntityRef(name);
			}
			else
			{
				_writer.WriteEntityRef(name);
			}
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
			if (char.IsSurrogate(ch))
			{
				throw new ArgumentException(System.SR.Xml_InvalidSurrogateMissingLowChar);
			}
			AdvanceState(Token.Text);
			if (SaveAttrValue)
			{
				_attrValueCache.WriteCharEntity(ch);
			}
			else
			{
				_writer.WriteCharEntity(ch);
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
			if (!char.IsSurrogatePair(highChar, lowChar))
			{
				throw XmlConvert.CreateInvalidSurrogatePairException(lowChar, highChar);
			}
			AdvanceState(Token.Text);
			if (SaveAttrValue)
			{
				_attrValueCache.WriteSurrogateCharEntity(lowChar, highChar);
			}
			else
			{
				_writer.WriteSurrogateCharEntity(lowChar, highChar);
			}
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteWhitespace(string ws)
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
			AdvanceState(Token.Whitespace);
			if (SaveAttrValue)
			{
				_attrValueCache.WriteWhitespace(ws);
			}
			else
			{
				_writer.WriteWhitespace(ws);
			}
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteString(string text)
	{
		try
		{
			if (text != null)
			{
				AdvanceState(Token.Text);
				if (SaveAttrValue)
				{
					_attrValueCache.WriteString(text);
				}
				else
				{
					_writer.WriteString(text);
				}
			}
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
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			if (count > buffer.Length - index)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			AdvanceState(Token.Text);
			if (SaveAttrValue)
			{
				_attrValueCache.WriteChars(buffer, index, count);
			}
			else
			{
				_writer.WriteChars(buffer, index, count);
			}
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
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			if (count > buffer.Length - index)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			AdvanceState(Token.RawData);
			if (SaveAttrValue)
			{
				_attrValueCache.WriteRaw(buffer, index, count);
			}
			else
			{
				_writer.WriteRaw(buffer, index, count);
			}
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
			if (data != null)
			{
				AdvanceState(Token.RawData);
				if (SaveAttrValue)
				{
					_attrValueCache.WriteRaw(data);
				}
				else
				{
					_writer.WriteRaw(data);
				}
			}
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
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			if (count > buffer.Length - index)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			AdvanceState(Token.Base64);
			_writer.WriteBase64(buffer, index, count);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void Close()
	{
		if (_currentState == State.Closed)
		{
			return;
		}
		try
		{
			if (_writeEndDocumentOnClose)
			{
				while (_currentState != State.Error && _elemTop > 0)
				{
					WriteEndElement();
				}
			}
			else if (_currentState != State.Error && _elemTop > 0)
			{
				try
				{
					AdvanceState(Token.EndElement);
				}
				catch
				{
					_currentState = State.Error;
					throw;
				}
			}
			if (InBase64 && _rawWriter != null)
			{
				_rawWriter.WriteEndBase64();
			}
			_writer.Flush();
		}
		finally
		{
			try
			{
				if (_rawWriter != null)
				{
					_rawWriter.Close(WriteState);
				}
				else
				{
					_writer.Close();
				}
			}
			finally
			{
				_currentState = State.Closed;
			}
		}
	}

	public override void Flush()
	{
		try
		{
			_writer.Flush();
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override string LookupPrefix(string ns)
	{
		try
		{
			if (ns == null)
			{
				throw new ArgumentNullException("ns");
			}
			for (int num = _nsTop; num >= 0; num--)
			{
				if (_nsStack[num].namespaceUri == ns)
				{
					string prefix = _nsStack[num].prefix;
					for (num++; num <= _nsTop; num++)
					{
						if (_nsStack[num].prefix == prefix)
						{
							return null;
						}
					}
					return prefix;
				}
			}
			return (_predefinedNamespaces != null) ? _predefinedNamespaces.LookupPrefix(ns) : null;
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteQualifiedName(string localName, string ns)
	{
		try
		{
			if (localName == null || localName.Length == 0)
			{
				throw new ArgumentException(System.SR.Xml_EmptyLocalName);
			}
			CheckNCName(localName);
			AdvanceState(Token.Text);
			string text = string.Empty;
			if (ns != null && ns.Length != 0)
			{
				text = LookupPrefix(ns);
				if (text == null)
				{
					if (_currentState != State.Attribute)
					{
						throw new ArgumentException(System.SR.Format(System.SR.Xml_UndefNamespace, ns));
					}
					text = GeneratePrefix();
					PushNamespaceImplicit(text, ns);
				}
			}
			if (SaveAttrValue || _rawWriter == null)
			{
				if (text.Length != 0)
				{
					WriteString(text);
					WriteString(":");
				}
				WriteString(localName);
			}
			else
			{
				_rawWriter.WriteQualifiedName(text, localName, ns);
			}
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteValue(bool value)
	{
		try
		{
			AdvanceState(Token.AtomicValue);
			_writer.WriteValue(value);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteValue(DateTime value)
	{
		try
		{
			AdvanceState(Token.AtomicValue);
			_writer.WriteValue(value);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteValue(DateTimeOffset value)
	{
		try
		{
			AdvanceState(Token.AtomicValue);
			_writer.WriteValue(value);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteValue(double value)
	{
		try
		{
			AdvanceState(Token.AtomicValue);
			_writer.WriteValue(value);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteValue(float value)
	{
		try
		{
			AdvanceState(Token.AtomicValue);
			_writer.WriteValue(value);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteValue(decimal value)
	{
		try
		{
			AdvanceState(Token.AtomicValue);
			_writer.WriteValue(value);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteValue(int value)
	{
		try
		{
			AdvanceState(Token.AtomicValue);
			_writer.WriteValue(value);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteValue(long value)
	{
		try
		{
			AdvanceState(Token.AtomicValue);
			_writer.WriteValue(value);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteValue(string value)
	{
		try
		{
			if (value != null)
			{
				if (SaveAttrValue)
				{
					AdvanceState(Token.Text);
					_attrValueCache.WriteValue(value);
				}
				else
				{
					AdvanceState(Token.AtomicValue);
					_writer.WriteValue(value);
				}
			}
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteValue(object value)
	{
		try
		{
			if (SaveAttrValue && value is string)
			{
				AdvanceState(Token.Text);
				_attrValueCache.WriteValue((string)value);
			}
			else
			{
				AdvanceState(Token.AtomicValue);
				_writer.WriteValue(value);
			}
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override void WriteBinHex(byte[] buffer, int index, int count)
	{
		if (IsClosedOrErrorState)
		{
			throw new InvalidOperationException(System.SR.Xml_ClosedOrError);
		}
		try
		{
			AdvanceState(Token.Text);
			base.WriteBinHex(buffer, index, count);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	private void SetSpecialAttribute(SpecialAttribute special)
	{
		_specAttr = special;
		if (State.Attribute == _currentState)
		{
			_currentState = State.SpecialAttr;
		}
		else if (State.RootLevelAttr == _currentState)
		{
			_currentState = State.RootLevelSpecAttr;
		}
		if (_attrValueCache == null)
		{
			_attrValueCache = new AttributeValueCache();
		}
	}

	private void WriteStartDocumentImpl(XmlStandalone standalone)
	{
		try
		{
			AdvanceState(Token.StartDocument);
			if (_conformanceLevel == ConformanceLevel.Auto)
			{
				_conformanceLevel = ConformanceLevel.Document;
				_stateTable = s_stateTableDocument;
			}
			else if (_conformanceLevel == ConformanceLevel.Fragment)
			{
				throw new InvalidOperationException(System.SR.Xml_CannotStartDocumentOnFragment);
			}
			if (_rawWriter != null)
			{
				if (!_xmlDeclFollows)
				{
					_rawWriter.WriteXmlDeclaration(standalone);
				}
			}
			else
			{
				_writer.WriteStartDocument();
			}
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	private void StartFragment()
	{
		_conformanceLevel = ConformanceLevel.Fragment;
	}

	private void PushNamespaceImplicit(string prefix, string ns)
	{
		int num = LookupNamespaceIndex(prefix);
		NamespaceKind kind;
		if (num != -1)
		{
			if (num > _elemScopeStack[_elemTop].prevNSTop)
			{
				if (_nsStack[num].namespaceUri != ns)
				{
					throw new XmlException(System.SR.Xml_RedefinePrefix, new string[3]
					{
						prefix,
						_nsStack[num].namespaceUri,
						ns
					});
				}
				return;
			}
			if (_nsStack[num].kind == NamespaceKind.Special)
			{
				if (!(prefix == "xml"))
				{
					throw new ArgumentException(System.SR.Xml_XmlnsPrefix);
				}
				if (ns != _nsStack[num].namespaceUri)
				{
					throw new ArgumentException(System.SR.Xml_XmlPrefix);
				}
				kind = NamespaceKind.Implied;
			}
			else
			{
				kind = ((!(_nsStack[num].namespaceUri == ns)) ? NamespaceKind.NeedToWrite : NamespaceKind.Implied);
			}
		}
		else
		{
			if ((ns == "http://www.w3.org/XML/1998/namespace" && prefix != "xml") || (ns == "http://www.w3.org/2000/xmlns/" && prefix != "xmlns"))
			{
				throw new ArgumentException(System.SR.Format(System.SR.Xml_NamespaceDeclXmlXmlns, prefix));
			}
			if (_predefinedNamespaces != null)
			{
				string text = _predefinedNamespaces.LookupNamespace(prefix);
				kind = ((!(text == ns)) ? NamespaceKind.NeedToWrite : NamespaceKind.Implied);
			}
			else
			{
				kind = NamespaceKind.NeedToWrite;
			}
		}
		AddNamespace(prefix, ns, kind);
	}

	private bool PushNamespaceExplicit(string prefix, string ns)
	{
		bool result = true;
		int num = LookupNamespaceIndex(prefix);
		if (num != -1)
		{
			if (num > _elemScopeStack[_elemTop].prevNSTop)
			{
				if (_nsStack[num].namespaceUri != ns)
				{
					throw new XmlException(System.SR.Xml_RedefinePrefix, new string[3]
					{
						prefix,
						_nsStack[num].namespaceUri,
						ns
					});
				}
				NamespaceKind kind = _nsStack[num].kind;
				if (kind == NamespaceKind.Written)
				{
					throw DupAttrException((prefix.Length == 0) ? string.Empty : "xmlns", (prefix.Length == 0) ? "xmlns" : prefix);
				}
				if (_omitDuplNamespaces && kind != NamespaceKind.NeedToWrite)
				{
					result = false;
				}
				_nsStack[num].kind = NamespaceKind.Written;
				return result;
			}
			if (_nsStack[num].namespaceUri == ns && _omitDuplNamespaces)
			{
				result = false;
			}
		}
		else if (_predefinedNamespaces != null)
		{
			string text = _predefinedNamespaces.LookupNamespace(prefix);
			if (text == ns && _omitDuplNamespaces)
			{
				result = false;
			}
		}
		if ((ns == "http://www.w3.org/XML/1998/namespace" && prefix != "xml") || (ns == "http://www.w3.org/2000/xmlns/" && prefix != "xmlns"))
		{
			throw new ArgumentException(System.SR.Format(System.SR.Xml_NamespaceDeclXmlXmlns, prefix));
		}
		if (prefix.Length > 0 && prefix[0] == 'x')
		{
			if (prefix == "xml")
			{
				if (ns != "http://www.w3.org/XML/1998/namespace")
				{
					throw new ArgumentException(System.SR.Xml_XmlPrefix);
				}
			}
			else if (prefix == "xmlns")
			{
				throw new ArgumentException(System.SR.Xml_XmlnsPrefix);
			}
		}
		AddNamespace(prefix, ns, NamespaceKind.Written);
		return result;
	}

	private void AddNamespace(string prefix, string ns, NamespaceKind kind)
	{
		int num = ++_nsTop;
		if (num == _nsStack.Length)
		{
			Namespace[] array = new Namespace[num * 2];
			Array.Copy(_nsStack, array, num);
			_nsStack = array;
		}
		_nsStack[num].Set(prefix, ns, kind);
		if (_useNsHashtable)
		{
			AddToNamespaceHashtable(_nsTop);
		}
		else if (_nsTop == 16)
		{
			_nsHashtable = new Dictionary<string, int>();
			for (int i = 0; i <= _nsTop; i++)
			{
				AddToNamespaceHashtable(i);
			}
			_useNsHashtable = true;
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

	private int LookupNamespaceIndex(string prefix)
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

	private static XmlException DupAttrException(string prefix, string localName)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (prefix.Length > 0)
		{
			stringBuilder.Append(prefix);
			stringBuilder.Append(':');
		}
		stringBuilder.Append(localName);
		return new XmlException(System.SR.Xml_DupAttributeName, stringBuilder.ToString());
	}

	private void AdvanceState(Token token)
	{
		if (_currentState >= State.Closed)
		{
			if (_currentState == State.Closed || _currentState == State.Error)
			{
				throw new InvalidOperationException(System.SR.Xml_ClosedOrError);
			}
			throw new InvalidOperationException(System.SR.Format(System.SR.Xml_WrongToken, tokenName[(int)token], GetStateName(_currentState)));
		}
		State state;
		while (true)
		{
			state = _stateTable[(int)(((int)token << 4) + _currentState)];
			switch (state)
			{
			case State.Error:
				ThrowInvalidStateTransition(token, _currentState);
				break;
			case State.StartContent:
				StartElementContent();
				state = State.Content;
				break;
			case State.StartContentEle:
				StartElementContent();
				state = State.Element;
				break;
			case State.StartContentB64:
				StartElementContent();
				state = State.B64Content;
				break;
			case State.StartDoc:
				WriteStartDocument();
				state = State.Document;
				break;
			case State.StartDocEle:
				WriteStartDocument();
				state = State.Element;
				break;
			case State.EndAttrSEle:
				WriteEndAttribute();
				StartElementContent();
				state = State.Element;
				break;
			case State.EndAttrEEle:
				WriteEndAttribute();
				StartElementContent();
				state = State.Content;
				break;
			case State.EndAttrSCont:
				WriteEndAttribute();
				StartElementContent();
				state = State.Content;
				break;
			case State.EndAttrSAttr:
				WriteEndAttribute();
				state = State.Attribute;
				break;
			case State.PostB64Cont:
				if (_rawWriter != null)
				{
					_rawWriter.WriteEndBase64();
				}
				_currentState = State.Content;
				continue;
			case State.PostB64Attr:
				if (_rawWriter != null)
				{
					_rawWriter.WriteEndBase64();
				}
				_currentState = State.Attribute;
				continue;
			case State.PostB64RootAttr:
				if (_rawWriter != null)
				{
					_rawWriter.WriteEndBase64();
				}
				_currentState = State.RootLevelAttr;
				continue;
			case State.StartFragEle:
				StartFragment();
				state = State.Element;
				break;
			case State.StartFragCont:
				StartFragment();
				state = State.Content;
				break;
			case State.StartFragB64:
				StartFragment();
				state = State.B64Content;
				break;
			case State.StartRootLevelAttr:
				WriteEndAttribute();
				state = State.RootLevelAttr;
				break;
			}
			break;
		}
		_currentState = state;
	}

	private void StartElementContent()
	{
		int prevNSTop = _elemScopeStack[_elemTop].prevNSTop;
		for (int num = _nsTop; num > prevNSTop; num--)
		{
			if (_nsStack[num].kind == NamespaceKind.NeedToWrite)
			{
				_nsStack[num].WriteDecl(_writer, _rawWriter);
			}
		}
		if (_rawWriter != null)
		{
			_rawWriter.StartElementContent();
		}
	}

	private static string GetStateName(State state)
	{
		if (state >= State.Error)
		{
			return "Error";
		}
		return stateName[(int)state];
	}

	internal string LookupNamespace(string prefix)
	{
		for (int num = _nsTop; num >= 0; num--)
		{
			if (_nsStack[num].prefix == prefix)
			{
				return _nsStack[num].namespaceUri;
			}
		}
		if (_predefinedNamespaces == null)
		{
			return null;
		}
		return _predefinedNamespaces.LookupNamespace(prefix);
	}

	private string LookupLocalNamespace(string prefix)
	{
		for (int num = _nsTop; num > _elemScopeStack[_elemTop].prevNSTop; num--)
		{
			if (_nsStack[num].prefix == prefix)
			{
				return _nsStack[num].namespaceUri;
			}
		}
		return null;
	}

	private string GeneratePrefix()
	{
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(1, 1, invariantCulture);
		handler.AppendLiteral("p");
		handler.AppendFormatted(_nsTop - 2, "d");
		string text = string.Create(provider, ref handler);
		if (LookupNamespace(text) == null)
		{
			return text;
		}
		int num = 0;
		string text2;
		do
		{
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider2 = invariantCulture;
			DefaultInterpolatedStringHandler handler2 = new DefaultInterpolatedStringHandler(0, 2, invariantCulture);
			handler2.AppendFormatted(text);
			handler2.AppendFormatted(num);
			text2 = string.Create(provider2, ref handler2);
			num++;
		}
		while (LookupNamespace(text2) != null);
		return text2;
	}

	private void CheckNCName(string ncname)
	{
		int length = ncname.Length;
		if (XmlCharType.IsStartNCNameSingleChar(ncname[0]))
		{
			for (int i = 1; i < length; i++)
			{
				if (!XmlCharType.IsNCNameSingleChar(ncname[i]))
				{
					throw InvalidCharsException(ncname, i);
				}
			}
			return;
		}
		throw InvalidCharsException(ncname, 0);
	}

	private static Exception InvalidCharsException(string name, int badCharIndex)
	{
		string[] array = XmlException.BuildCharExceptionArgs(name, badCharIndex);
		string[] array2 = new string[3]
		{
			name,
			array[0],
			array[1]
		};
		string xml_InvalidNameCharsDetail = System.SR.Xml_InvalidNameCharsDetail;
		object[] args = array2;
		return new ArgumentException(System.SR.Format(xml_InvalidNameCharsDetail, args));
	}

	private void ThrowInvalidStateTransition(Token token, State currentState)
	{
		string text = System.SR.Format(System.SR.Xml_WrongToken, tokenName[(int)token], GetStateName(currentState));
		if ((currentState == State.Start || currentState == State.AfterRootEle) && _conformanceLevel == ConformanceLevel.Document)
		{
			throw new InvalidOperationException(text + " " + System.SR.Xml_ConformanceLevelFragment);
		}
		throw new InvalidOperationException(text);
	}

	private void AddAttribute(string prefix, string localName, string namespaceName)
	{
		int num = _attrCount++;
		if (num == _attrStack.Length)
		{
			AttrName[] array = new AttrName[num * 2];
			Array.Copy(_attrStack, array, num);
			_attrStack = array;
		}
		_attrStack[num].Set(prefix, localName, namespaceName);
		if (_attrCount < 14)
		{
			for (int i = 0; i < num; i++)
			{
				if (_attrStack[i].IsDuplicate(prefix, localName, namespaceName))
				{
					throw DupAttrException(prefix, localName);
				}
			}
			return;
		}
		if (_attrCount == 14)
		{
			if (_attrHashTable == null)
			{
				_attrHashTable = new Dictionary<string, int>();
			}
			for (int j = 0; j < num; j++)
			{
				AddToAttrHashTable(j);
			}
		}
		AddToAttrHashTable(num);
		int prev;
		for (prev = _attrStack[num].prev; prev > 0; prev = _attrStack[prev].prev)
		{
			prev--;
			if (_attrStack[prev].IsDuplicate(prefix, localName, namespaceName))
			{
				throw DupAttrException(prefix, localName);
			}
		}
	}

	private void AddToAttrHashTable(int attributeIndex)
	{
		string localName = _attrStack[attributeIndex].localName;
		int count = _attrHashTable.Count;
		_attrHashTable[localName] = 0;
		if (count == _attrHashTable.Count)
		{
			int num = attributeIndex - 1;
			while (num >= 0 && !(_attrStack[num].localName == localName))
			{
				num--;
			}
			_attrStack[attributeIndex].prev = num + 1;
		}
	}

	public override Task WriteStartDocumentAsync()
	{
		return WriteStartDocumentImplAsync(XmlStandalone.Omit);
	}

	public override Task WriteStartDocumentAsync(bool standalone)
	{
		return WriteStartDocumentImplAsync(standalone ? XmlStandalone.Yes : XmlStandalone.No);
	}

	public override async Task WriteEndDocumentAsync()
	{
		_ = 2;
		try
		{
			while (_elemTop > 0)
			{
				await WriteEndElementAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			State prevState = _currentState;
			await AdvanceStateAsync(Token.EndDocument).ConfigureAwait(continueOnCapturedContext: false);
			if (prevState != State.AfterRootEle)
			{
				throw new ArgumentException(System.SR.Xml_NoRoot);
			}
			if (_rawWriter == null)
			{
				await _writer.WriteEndDocumentAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override async Task WriteDocTypeAsync(string name, string pubid, string sysid, string subset)
	{
		_ = 1;
		try
		{
			if (name == null || name.Length == 0)
			{
				throw new ArgumentException(System.SR.Xml_EmptyName);
			}
			XmlConvert.VerifyQName(name, ExceptionType.XmlException);
			if (_conformanceLevel == ConformanceLevel.Fragment)
			{
				throw new InvalidOperationException(System.SR.Xml_DtdNotAllowedInFragment);
			}
			await AdvanceStateAsync(Token.Dtd).ConfigureAwait(continueOnCapturedContext: false);
			if (_dtdWritten)
			{
				_currentState = State.Error;
				throw new InvalidOperationException(System.SR.Xml_DtdAlreadyWritten);
			}
			if (_conformanceLevel == ConformanceLevel.Auto)
			{
				_conformanceLevel = ConformanceLevel.Document;
				_stateTable = s_stateTableDocument;
			}
			if (_checkCharacters)
			{
				int invCharIndex;
				if (pubid != null && (invCharIndex = XmlCharType.IsPublicId(pubid)) >= 0)
				{
					string xml_InvalidCharacter = System.SR.Xml_InvalidCharacter;
					object[] args = XmlException.BuildCharExceptionArgs(pubid, invCharIndex);
					throw new ArgumentException(System.SR.Format(xml_InvalidCharacter, args), "pubid");
				}
				if (sysid != null && (invCharIndex = XmlCharType.IsOnlyCharData(sysid)) >= 0)
				{
					string xml_InvalidCharacter2 = System.SR.Xml_InvalidCharacter;
					object[] args = XmlException.BuildCharExceptionArgs(sysid, invCharIndex);
					throw new ArgumentException(System.SR.Format(xml_InvalidCharacter2, args), "sysid");
				}
				if (subset != null && (invCharIndex = XmlCharType.IsOnlyCharData(subset)) >= 0)
				{
					string xml_InvalidCharacter3 = System.SR.Xml_InvalidCharacter;
					object[] args = XmlException.BuildCharExceptionArgs(subset, invCharIndex);
					throw new ArgumentException(System.SR.Format(xml_InvalidCharacter3, args), "subset");
				}
			}
			await _writer.WriteDocTypeAsync(name, pubid, sysid, subset).ConfigureAwait(continueOnCapturedContext: false);
			_dtdWritten = true;
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	private Task TryReturnTask(Task task)
	{
		if (task.IsSuccess())
		{
			return Task.CompletedTask;
		}
		return _TryReturnTask(task);
	}

	private async Task _TryReturnTask(Task task)
	{
		try
		{
			await task.ConfigureAwait(continueOnCapturedContext: false);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	private Task SequenceRun<TArg>(Task task, Func<TArg, Task> nextTaskFun, TArg arg)
	{
		if (task.IsSuccess())
		{
			return TryReturnTask(nextTaskFun(arg));
		}
		return _SequenceRun(task, nextTaskFun, arg);
	}

	private async Task _SequenceRun<TArg>(Task task, Func<TArg, Task> nextTaskFun, TArg arg)
	{
		_ = 1;
		try
		{
			await task.ConfigureAwait(continueOnCapturedContext: false);
			await nextTaskFun(arg).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override Task WriteStartElementAsync(string prefix, string localName, string ns)
	{
		try
		{
			if (localName == null || localName.Length == 0)
			{
				throw new ArgumentException(System.SR.Xml_EmptyLocalName);
			}
			CheckNCName(localName);
			Task task = AdvanceStateAsync(Token.StartElement);
			if (task.IsSuccess())
			{
				return WriteStartElementAsync_NoAdvanceState(prefix, localName, ns);
			}
			return WriteStartElementAsync_NoAdvanceState(task, prefix, localName, ns);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	private Task WriteStartElementAsync_NoAdvanceState(string prefix, string localName, string ns)
	{
		try
		{
			if (prefix == null)
			{
				if (ns != null)
				{
					prefix = LookupPrefix(ns);
				}
				if (prefix == null)
				{
					prefix = string.Empty;
				}
			}
			else if (prefix.Length > 0)
			{
				CheckNCName(prefix);
				if (ns == null)
				{
					ns = LookupNamespace(prefix);
				}
				if (ns == null || (ns != null && ns.Length == 0))
				{
					throw new ArgumentException(System.SR.Xml_PrefixForEmptyNs);
				}
			}
			if (ns == null)
			{
				ns = LookupNamespace(prefix);
				if (ns == null)
				{
					ns = string.Empty;
				}
			}
			if (_elemTop == 0 && _rawWriter != null)
			{
				_rawWriter.OnRootElement(_conformanceLevel);
			}
			Task task = _writer.WriteStartElementAsync(prefix, localName, ns);
			if (task.IsSuccess())
			{
				WriteStartElementAsync_FinishWrite(prefix, localName, ns);
				return Task.CompletedTask;
			}
			return WriteStartElementAsync_FinishWrite(task, prefix, localName, ns);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	private async Task WriteStartElementAsync_NoAdvanceState(Task task, string prefix, string localName, string ns)
	{
		_ = 1;
		try
		{
			await task.ConfigureAwait(continueOnCapturedContext: false);
			await WriteStartElementAsync_NoAdvanceState(prefix, localName, ns).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	private void WriteStartElementAsync_FinishWrite(string prefix, string localName, string ns)
	{
		try
		{
			int num = ++_elemTop;
			if (num == _elemScopeStack.Length)
			{
				ElementScope[] array = new ElementScope[num * 2];
				Array.Copy(_elemScopeStack, array, num);
				_elemScopeStack = array;
			}
			_elemScopeStack[num].Set(prefix, localName, ns, _nsTop);
			PushNamespaceImplicit(prefix, ns);
			if (_attrCount >= 14)
			{
				_attrHashTable.Clear();
			}
			_attrCount = 0;
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	private async Task WriteStartElementAsync_FinishWrite(Task t, string prefix, string localName, string ns)
	{
		try
		{
			await t.ConfigureAwait(continueOnCapturedContext: false);
			WriteStartElementAsync_FinishWrite(prefix, localName, ns);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override Task WriteEndElementAsync()
	{
		try
		{
			Task task = AdvanceStateAsync(Token.EndElement);
			return SequenceRun(task, (XmlWellFormedWriter thisRef) => thisRef.WriteEndElementAsync_NoAdvanceState(), this);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	private Task WriteEndElementAsync_NoAdvanceState()
	{
		try
		{
			int elemTop = _elemTop;
			if (elemTop == 0)
			{
				throw new XmlException(System.SR.Xml_NoStartTag, string.Empty);
			}
			Task task = ((_rawWriter == null) ? _writer.WriteEndElementAsync() : _elemScopeStack[elemTop].WriteEndElementAsync(_rawWriter));
			return SequenceRun(task, (XmlWellFormedWriter thisRef) => thisRef.WriteEndElementAsync_FinishWrite(), this);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	private Task WriteEndElementAsync_FinishWrite()
	{
		try
		{
			int elemTop = _elemTop;
			int prevNSTop = _elemScopeStack[elemTop].prevNSTop;
			if (_useNsHashtable && prevNSTop < _nsTop)
			{
				PopNamespaces(prevNSTop + 1, _nsTop);
			}
			_nsTop = prevNSTop;
			if ((_elemTop = elemTop - 1) == 0)
			{
				if (_conformanceLevel == ConformanceLevel.Document)
				{
					_currentState = State.AfterRootEle;
				}
				else
				{
					_currentState = State.TopLevel;
				}
			}
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
		return Task.CompletedTask;
	}

	public override Task WriteFullEndElementAsync()
	{
		try
		{
			Task task = AdvanceStateAsync(Token.EndElement);
			return SequenceRun(task, (XmlWellFormedWriter thisRef) => thisRef.WriteFullEndElementAsync_NoAdvanceState(), this);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	private Task WriteFullEndElementAsync_NoAdvanceState()
	{
		try
		{
			int elemTop = _elemTop;
			if (elemTop == 0)
			{
				throw new XmlException(System.SR.Xml_NoStartTag, string.Empty);
			}
			Task task = ((_rawWriter == null) ? _writer.WriteFullEndElementAsync() : _elemScopeStack[elemTop].WriteFullEndElementAsync(_rawWriter));
			return SequenceRun(task, (XmlWellFormedWriter thisRef) => thisRef.WriteEndElementAsync_FinishWrite(), this);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	protected internal override Task WriteStartAttributeAsync(string prefix, string localName, string namespaceName)
	{
		try
		{
			if (localName == null || localName.Length == 0)
			{
				if (!(prefix == "xmlns"))
				{
					throw new ArgumentException(System.SR.Xml_EmptyLocalName);
				}
				localName = "xmlns";
				prefix = string.Empty;
			}
			CheckNCName(localName);
			Task task = AdvanceStateAsync(Token.StartAttribute);
			if (task.IsSuccess())
			{
				return WriteStartAttributeAsync_NoAdvanceState(prefix, localName, namespaceName);
			}
			return WriteStartAttributeAsync_NoAdvanceState(task, prefix, localName, namespaceName);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	private Task WriteStartAttributeAsync_NoAdvanceState(string prefix, string localName, string namespaceName)
	{
		try
		{
			if (prefix == null)
			{
				if (namespaceName != null && (!(localName == "xmlns") || !(namespaceName == "http://www.w3.org/2000/xmlns/")))
				{
					prefix = LookupPrefix(namespaceName);
				}
				if (prefix == null)
				{
					prefix = string.Empty;
				}
			}
			if (namespaceName == null)
			{
				if (prefix.Length > 0)
				{
					namespaceName = LookupNamespace(prefix);
				}
				if (namespaceName == null)
				{
					namespaceName = string.Empty;
				}
			}
			if (prefix.Length == 0)
			{
				if (localName[0] != 'x' || !(localName == "xmlns"))
				{
					if (namespaceName.Length > 0)
					{
						prefix = LookupPrefix(namespaceName);
						if (prefix == null || prefix.Length == 0)
						{
							prefix = GeneratePrefix();
						}
					}
					goto IL_01bc;
				}
				if (namespaceName.Length > 0 && namespaceName != "http://www.w3.org/2000/xmlns/")
				{
					throw new ArgumentException(System.SR.Xml_XmlnsPrefix);
				}
				_curDeclPrefix = string.Empty;
				SetSpecialAttribute(SpecialAttribute.DefaultXmlns);
			}
			else
			{
				if (prefix[0] != 'x')
				{
					goto IL_0188;
				}
				if (prefix == "xmlns")
				{
					if (namespaceName.Length > 0 && namespaceName != "http://www.w3.org/2000/xmlns/")
					{
						throw new ArgumentException(System.SR.Xml_XmlnsPrefix);
					}
					_curDeclPrefix = localName;
					SetSpecialAttribute(SpecialAttribute.PrefixedXmlns);
				}
				else
				{
					if (!(prefix == "xml"))
					{
						goto IL_0188;
					}
					if (namespaceName.Length > 0 && namespaceName != "http://www.w3.org/XML/1998/namespace")
					{
						throw new ArgumentException(System.SR.Xml_XmlPrefix);
					}
					if (!(localName == "space"))
					{
						if (!(localName == "lang"))
						{
							goto IL_0188;
						}
						SetSpecialAttribute(SpecialAttribute.XmlLang);
					}
					else
					{
						SetSpecialAttribute(SpecialAttribute.XmlSpace);
					}
				}
			}
			goto IL_01cc;
			IL_0188:
			CheckNCName(prefix);
			if (namespaceName.Length == 0)
			{
				prefix = string.Empty;
			}
			else
			{
				string text = LookupLocalNamespace(prefix);
				if (text != null && text != namespaceName)
				{
					prefix = GeneratePrefix();
				}
			}
			goto IL_01bc;
			IL_01bc:
			if (prefix.Length != 0)
			{
				PushNamespaceImplicit(prefix, namespaceName);
			}
			goto IL_01cc;
			IL_01cc:
			AddAttribute(prefix, localName, namespaceName);
			if (_specAttr == SpecialAttribute.No)
			{
				return TryReturnTask(_writer.WriteStartAttributeAsync(prefix, localName, namespaceName));
			}
			return Task.CompletedTask;
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	private async Task WriteStartAttributeAsync_NoAdvanceState(Task task, string prefix, string localName, string namespaceName)
	{
		_ = 1;
		try
		{
			await task.ConfigureAwait(continueOnCapturedContext: false);
			await WriteStartAttributeAsync_NoAdvanceState(prefix, localName, namespaceName).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	protected internal override Task WriteEndAttributeAsync()
	{
		try
		{
			Task task = AdvanceStateAsync(Token.EndAttribute);
			return SequenceRun(task, (XmlWellFormedWriter thisRef) => thisRef.WriteEndAttributeAsync_NoAdvance(), this);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	private Task WriteEndAttributeAsync_NoAdvance()
	{
		try
		{
			if (_specAttr != 0)
			{
				return WriteEndAttributeAsync_SepcialAtt();
			}
			return TryReturnTask(_writer.WriteEndAttributeAsync());
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	private async Task WriteEndAttributeAsync_SepcialAtt()
	{
		_ = 19;
		try
		{
			switch (_specAttr)
			{
			case SpecialAttribute.DefaultXmlns:
			{
				string stringValue = _attrValueCache.StringValue;
				if (PushNamespaceExplicit(string.Empty, stringValue))
				{
					if (_rawWriter == null)
					{
						await _writer.WriteStartAttributeAsync(string.Empty, "xmlns", "http://www.w3.org/2000/xmlns/").ConfigureAwait(continueOnCapturedContext: false);
						await _attrValueCache.ReplayAsync(_writer).ConfigureAwait(continueOnCapturedContext: false);
						await _writer.WriteEndAttributeAsync().ConfigureAwait(continueOnCapturedContext: false);
					}
					else if (!_rawWriter.SupportsNamespaceDeclarationInChunks)
					{
						await _rawWriter.WriteNamespaceDeclarationAsync(string.Empty, stringValue).ConfigureAwait(continueOnCapturedContext: false);
					}
					else
					{
						await _rawWriter.WriteStartNamespaceDeclarationAsync(string.Empty).ConfigureAwait(continueOnCapturedContext: false);
						await _attrValueCache.ReplayAsync(_rawWriter).ConfigureAwait(continueOnCapturedContext: false);
						await _rawWriter.WriteEndNamespaceDeclarationAsync().ConfigureAwait(continueOnCapturedContext: false);
					}
				}
				_curDeclPrefix = null;
				break;
			}
			case SpecialAttribute.PrefixedXmlns:
			{
				string stringValue = _attrValueCache.StringValue;
				if (stringValue.Length == 0)
				{
					throw new ArgumentException(System.SR.Xml_PrefixForEmptyNs);
				}
				if (stringValue == "http://www.w3.org/2000/xmlns/" || (stringValue == "http://www.w3.org/XML/1998/namespace" && _curDeclPrefix != "xml"))
				{
					throw new ArgumentException(System.SR.Xml_CanNotBindToReservedNamespace);
				}
				if (PushNamespaceExplicit(_curDeclPrefix, stringValue))
				{
					if (_rawWriter == null)
					{
						await _writer.WriteStartAttributeAsync("xmlns", _curDeclPrefix, "http://www.w3.org/2000/xmlns/").ConfigureAwait(continueOnCapturedContext: false);
						await _attrValueCache.ReplayAsync(_writer).ConfigureAwait(continueOnCapturedContext: false);
						await _writer.WriteEndAttributeAsync().ConfigureAwait(continueOnCapturedContext: false);
					}
					else if (!_rawWriter.SupportsNamespaceDeclarationInChunks)
					{
						await _rawWriter.WriteNamespaceDeclarationAsync(_curDeclPrefix, stringValue).ConfigureAwait(continueOnCapturedContext: false);
					}
					else
					{
						await _rawWriter.WriteStartNamespaceDeclarationAsync(_curDeclPrefix).ConfigureAwait(continueOnCapturedContext: false);
						await _attrValueCache.ReplayAsync(_rawWriter).ConfigureAwait(continueOnCapturedContext: false);
						await _rawWriter.WriteEndNamespaceDeclarationAsync().ConfigureAwait(continueOnCapturedContext: false);
					}
				}
				_curDeclPrefix = null;
				break;
			}
			case SpecialAttribute.XmlSpace:
			{
				_attrValueCache.Trim();
				string stringValue = _attrValueCache.StringValue;
				if (stringValue == "default")
				{
					_elemScopeStack[_elemTop].xmlSpace = XmlSpace.Default;
				}
				else
				{
					if (!(stringValue == "preserve"))
					{
						throw new ArgumentException(System.SR.Format(System.SR.Xml_InvalidXmlSpace, stringValue));
					}
					_elemScopeStack[_elemTop].xmlSpace = XmlSpace.Preserve;
				}
				await _writer.WriteStartAttributeAsync("xml", "space", "http://www.w3.org/XML/1998/namespace").ConfigureAwait(continueOnCapturedContext: false);
				await _attrValueCache.ReplayAsync(_writer).ConfigureAwait(continueOnCapturedContext: false);
				await _writer.WriteEndAttributeAsync().ConfigureAwait(continueOnCapturedContext: false);
				break;
			}
			case SpecialAttribute.XmlLang:
			{
				string stringValue = _attrValueCache.StringValue;
				_elemScopeStack[_elemTop].xmlLang = stringValue;
				await _writer.WriteStartAttributeAsync("xml", "lang", "http://www.w3.org/XML/1998/namespace").ConfigureAwait(continueOnCapturedContext: false);
				await _attrValueCache.ReplayAsync(_writer).ConfigureAwait(continueOnCapturedContext: false);
				await _writer.WriteEndAttributeAsync().ConfigureAwait(continueOnCapturedContext: false);
				break;
			}
			}
			_specAttr = SpecialAttribute.No;
			_attrValueCache.Clear();
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override async Task WriteCDataAsync(string text)
	{
		_ = 1;
		try
		{
			if (text == null)
			{
				text = string.Empty;
			}
			await AdvanceStateAsync(Token.CData).ConfigureAwait(continueOnCapturedContext: false);
			await _writer.WriteCDataAsync(text).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override async Task WriteCommentAsync(string text)
	{
		_ = 1;
		try
		{
			if (text == null)
			{
				text = string.Empty;
			}
			await AdvanceStateAsync(Token.Comment).ConfigureAwait(continueOnCapturedContext: false);
			await _writer.WriteCommentAsync(text).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override async Task WriteProcessingInstructionAsync(string name, string text)
	{
		_ = 4;
		try
		{
			if (name == null || name.Length == 0)
			{
				throw new ArgumentException(System.SR.Xml_EmptyName);
			}
			CheckNCName(name);
			if (text == null)
			{
				text = string.Empty;
			}
			if (name.Length != 3 || !string.Equals(name, "xml", StringComparison.OrdinalIgnoreCase))
			{
				await AdvanceStateAsync(Token.PI).ConfigureAwait(continueOnCapturedContext: false);
				await _writer.WriteProcessingInstructionAsync(name, text).ConfigureAwait(continueOnCapturedContext: false);
				return;
			}
			if (_currentState != 0)
			{
				throw new ArgumentException((_conformanceLevel == ConformanceLevel.Document) ? System.SR.Xml_DupXmlDecl : System.SR.Xml_CannotWriteXmlDecl);
			}
			_xmlDeclFollows = true;
			await AdvanceStateAsync(Token.PI).ConfigureAwait(continueOnCapturedContext: false);
			if (_rawWriter != null)
			{
				await _rawWriter.WriteXmlDeclarationAsync(text).ConfigureAwait(continueOnCapturedContext: false);
			}
			else
			{
				await _writer.WriteProcessingInstructionAsync(name, text).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override async Task WriteEntityRefAsync(string name)
	{
		_ = 1;
		try
		{
			if (name == null || name.Length == 0)
			{
				throw new ArgumentException(System.SR.Xml_EmptyName);
			}
			CheckNCName(name);
			await AdvanceStateAsync(Token.Text).ConfigureAwait(continueOnCapturedContext: false);
			if (SaveAttrValue)
			{
				_attrValueCache.WriteEntityRef(name);
			}
			else
			{
				await _writer.WriteEntityRefAsync(name).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override async Task WriteCharEntityAsync(char ch)
	{
		_ = 1;
		try
		{
			if (char.IsSurrogate(ch))
			{
				throw new ArgumentException(System.SR.Xml_InvalidSurrogateMissingLowChar);
			}
			await AdvanceStateAsync(Token.Text).ConfigureAwait(continueOnCapturedContext: false);
			if (SaveAttrValue)
			{
				_attrValueCache.WriteCharEntity(ch);
			}
			else
			{
				await _writer.WriteCharEntityAsync(ch).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override async Task WriteSurrogateCharEntityAsync(char lowChar, char highChar)
	{
		_ = 1;
		try
		{
			if (!char.IsSurrogatePair(highChar, lowChar))
			{
				throw XmlConvert.CreateInvalidSurrogatePairException(lowChar, highChar);
			}
			await AdvanceStateAsync(Token.Text).ConfigureAwait(continueOnCapturedContext: false);
			if (SaveAttrValue)
			{
				_attrValueCache.WriteSurrogateCharEntity(lowChar, highChar);
			}
			else
			{
				await _writer.WriteSurrogateCharEntityAsync(lowChar, highChar).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override async Task WriteWhitespaceAsync(string ws)
	{
		_ = 1;
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
			await AdvanceStateAsync(Token.Whitespace).ConfigureAwait(continueOnCapturedContext: false);
			if (SaveAttrValue)
			{
				_attrValueCache.WriteWhitespace(ws);
			}
			else
			{
				await _writer.WriteWhitespaceAsync(ws).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override Task WriteStringAsync(string text)
	{
		try
		{
			if (text == null)
			{
				return Task.CompletedTask;
			}
			Task task = AdvanceStateAsync(Token.Text);
			if (task.IsSuccess())
			{
				return WriteStringAsync_NoAdvanceState(text);
			}
			return WriteStringAsync_NoAdvanceState(task, text);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	private Task WriteStringAsync_NoAdvanceState(string text)
	{
		try
		{
			if (SaveAttrValue)
			{
				_attrValueCache.WriteString(text);
				return Task.CompletedTask;
			}
			return TryReturnTask(_writer.WriteStringAsync(text));
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	private async Task WriteStringAsync_NoAdvanceState(Task task, string text)
	{
		_ = 1;
		try
		{
			await task.ConfigureAwait(continueOnCapturedContext: false);
			await WriteStringAsync_NoAdvanceState(text).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override async Task WriteCharsAsync(char[] buffer, int index, int count)
	{
		_ = 1;
		try
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			if (count > buffer.Length - index)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			await AdvanceStateAsync(Token.Text).ConfigureAwait(continueOnCapturedContext: false);
			if (SaveAttrValue)
			{
				_attrValueCache.WriteChars(buffer, index, count);
			}
			else
			{
				await _writer.WriteCharsAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override async Task WriteRawAsync(char[] buffer, int index, int count)
	{
		_ = 1;
		try
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			if (count > buffer.Length - index)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			await AdvanceStateAsync(Token.RawData).ConfigureAwait(continueOnCapturedContext: false);
			if (SaveAttrValue)
			{
				_attrValueCache.WriteRaw(buffer, index, count);
			}
			else
			{
				await _writer.WriteRawAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override async Task WriteRawAsync(string data)
	{
		_ = 1;
		try
		{
			if (data != null)
			{
				await AdvanceStateAsync(Token.RawData).ConfigureAwait(continueOnCapturedContext: false);
				if (SaveAttrValue)
				{
					_attrValueCache.WriteRaw(data);
				}
				else
				{
					await _writer.WriteRawAsync(data).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override Task WriteBase64Async(byte[] buffer, int index, int count)
	{
		try
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			if (count > buffer.Length - index)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			Task task = AdvanceStateAsync(Token.Base64);
			if (task.IsSuccess())
			{
				return TryReturnTask(_writer.WriteBase64Async(buffer, index, count));
			}
			return WriteBase64Async_NoAdvanceState(task, buffer, index, count);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	private async Task WriteBase64Async_NoAdvanceState(Task task, byte[] buffer, int index, int count)
	{
		_ = 1;
		try
		{
			await task.ConfigureAwait(continueOnCapturedContext: false);
			await _writer.WriteBase64Async(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override async Task FlushAsync()
	{
		try
		{
			await _writer.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override async Task WriteQualifiedNameAsync(string localName, string ns)
	{
		_ = 4;
		try
		{
			if (localName == null || localName.Length == 0)
			{
				throw new ArgumentException(System.SR.Xml_EmptyLocalName);
			}
			CheckNCName(localName);
			await AdvanceStateAsync(Token.Text).ConfigureAwait(continueOnCapturedContext: false);
			string text = string.Empty;
			if (ns != null && ns.Length != 0)
			{
				text = LookupPrefix(ns);
				if (text == null)
				{
					if (_currentState != State.Attribute)
					{
						throw new ArgumentException(System.SR.Format(System.SR.Xml_UndefNamespace, ns));
					}
					text = GeneratePrefix();
					PushNamespaceImplicit(text, ns);
				}
			}
			if (SaveAttrValue || _rawWriter == null)
			{
				if (text.Length != 0)
				{
					await WriteStringAsync(text).ConfigureAwait(continueOnCapturedContext: false);
					await WriteStringAsync(":").ConfigureAwait(continueOnCapturedContext: false);
				}
				await WriteStringAsync(localName).ConfigureAwait(continueOnCapturedContext: false);
			}
			else
			{
				await _rawWriter.WriteQualifiedNameAsync(text, localName, ns).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	public override async Task WriteBinHexAsync(byte[] buffer, int index, int count)
	{
		if (IsClosedOrErrorState)
		{
			throw new InvalidOperationException(System.SR.Xml_ClosedOrError);
		}
		try
		{
			await AdvanceStateAsync(Token.Text).ConfigureAwait(continueOnCapturedContext: false);
			await base.WriteBinHexAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	private async Task WriteStartDocumentImplAsync(XmlStandalone standalone)
	{
		_ = 2;
		try
		{
			await AdvanceStateAsync(Token.StartDocument).ConfigureAwait(continueOnCapturedContext: false);
			if (_conformanceLevel == ConformanceLevel.Auto)
			{
				_conformanceLevel = ConformanceLevel.Document;
				_stateTable = s_stateTableDocument;
			}
			else if (_conformanceLevel == ConformanceLevel.Fragment)
			{
				throw new InvalidOperationException(System.SR.Xml_CannotStartDocumentOnFragment);
			}
			if (_rawWriter != null)
			{
				if (!_xmlDeclFollows)
				{
					await _rawWriter.WriteXmlDeclarationAsync(standalone).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			else
			{
				await _writer.WriteStartDocumentAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch
		{
			_currentState = State.Error;
			throw;
		}
	}

	private Task AdvanceStateAsync_ReturnWhenFinish(Task task, State newState)
	{
		if (task.IsSuccess())
		{
			_currentState = newState;
			return Task.CompletedTask;
		}
		return _AdvanceStateAsync_ReturnWhenFinish(task, newState);
	}

	private async Task _AdvanceStateAsync_ReturnWhenFinish(Task task, State newState)
	{
		await task.ConfigureAwait(continueOnCapturedContext: false);
		_currentState = newState;
	}

	private Task AdvanceStateAsync_ContinueWhenFinish(Task task, State newState, Token token)
	{
		if (task.IsSuccess())
		{
			_currentState = newState;
			return AdvanceStateAsync(token);
		}
		return _AdvanceStateAsync_ContinueWhenFinish(task, newState, token);
	}

	private async Task _AdvanceStateAsync_ContinueWhenFinish(Task task, State newState, Token token)
	{
		await task.ConfigureAwait(continueOnCapturedContext: false);
		_currentState = newState;
		await AdvanceStateAsync(token).ConfigureAwait(continueOnCapturedContext: false);
	}

	private Task AdvanceStateAsync(Token token)
	{
		if (_currentState >= State.Closed)
		{
			if (_currentState == State.Closed || _currentState == State.Error)
			{
				throw new InvalidOperationException(System.SR.Xml_ClosedOrError);
			}
			throw new InvalidOperationException(System.SR.Format(System.SR.Xml_WrongToken, tokenName[(int)token], GetStateName(_currentState)));
		}
		State state;
		while (true)
		{
			state = _stateTable[(int)(((int)token << 4) + _currentState)];
			switch (state)
			{
			case State.Error:
				ThrowInvalidStateTransition(token, _currentState);
				break;
			case State.StartContent:
				return AdvanceStateAsync_ReturnWhenFinish(StartElementContentAsync(), State.Content);
			case State.StartContentEle:
				return AdvanceStateAsync_ReturnWhenFinish(StartElementContentAsync(), State.Element);
			case State.StartContentB64:
				return AdvanceStateAsync_ReturnWhenFinish(StartElementContentAsync(), State.B64Content);
			case State.StartDoc:
				return AdvanceStateAsync_ReturnWhenFinish(WriteStartDocumentAsync(), State.Document);
			case State.StartDocEle:
				return AdvanceStateAsync_ReturnWhenFinish(WriteStartDocumentAsync(), State.Element);
			case State.EndAttrSEle:
			{
				Task task = SequenceRun(WriteEndAttributeAsync(), (XmlWellFormedWriter thisRef) => thisRef.StartElementContentAsync(), this);
				return AdvanceStateAsync_ReturnWhenFinish(task, State.Element);
			}
			case State.EndAttrEEle:
			{
				Task task = SequenceRun(WriteEndAttributeAsync(), (XmlWellFormedWriter thisRef) => thisRef.StartElementContentAsync(), this);
				return AdvanceStateAsync_ReturnWhenFinish(task, State.Content);
			}
			case State.EndAttrSCont:
			{
				Task task = SequenceRun(WriteEndAttributeAsync(), (XmlWellFormedWriter thisRef) => thisRef.StartElementContentAsync(), this);
				return AdvanceStateAsync_ReturnWhenFinish(task, State.Content);
			}
			case State.EndAttrSAttr:
				return AdvanceStateAsync_ReturnWhenFinish(WriteEndAttributeAsync(), State.Attribute);
			case State.PostB64Cont:
				if (_rawWriter != null)
				{
					return AdvanceStateAsync_ContinueWhenFinish(_rawWriter.WriteEndBase64Async(), State.Content, token);
				}
				_currentState = State.Content;
				continue;
			case State.PostB64Attr:
				if (_rawWriter != null)
				{
					return AdvanceStateAsync_ContinueWhenFinish(_rawWriter.WriteEndBase64Async(), State.Attribute, token);
				}
				_currentState = State.Attribute;
				continue;
			case State.PostB64RootAttr:
				if (_rawWriter != null)
				{
					return AdvanceStateAsync_ContinueWhenFinish(_rawWriter.WriteEndBase64Async(), State.RootLevelAttr, token);
				}
				_currentState = State.RootLevelAttr;
				continue;
			case State.StartFragEle:
				StartFragment();
				state = State.Element;
				break;
			case State.StartFragCont:
				StartFragment();
				state = State.Content;
				break;
			case State.StartFragB64:
				StartFragment();
				state = State.B64Content;
				break;
			case State.StartRootLevelAttr:
				return AdvanceStateAsync_ReturnWhenFinish(WriteEndAttributeAsync(), State.RootLevelAttr);
			}
			break;
		}
		_currentState = state;
		return Task.CompletedTask;
	}

	private async Task StartElementContentAsync_WithNS()
	{
		int start = _elemScopeStack[_elemTop].prevNSTop;
		for (int i = _nsTop; i > start; i--)
		{
			if (_nsStack[i].kind == NamespaceKind.NeedToWrite)
			{
				await _nsStack[i].WriteDeclAsync(_writer, _rawWriter).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		if (_rawWriter != null)
		{
			_rawWriter.StartElementContent();
		}
	}

	private Task StartElementContentAsync()
	{
		if (_nsTop > _elemScopeStack[_elemTop].prevNSTop)
		{
			return StartElementContentAsync_WithNS();
		}
		if (_rawWriter != null)
		{
			_rawWriter.StartElementContent();
		}
		return Task.CompletedTask;
	}

	protected override async ValueTask DisposeAsyncCore()
	{
		if (_currentState == State.Closed)
		{
			return;
		}
		try
		{
			if (_writeEndDocumentOnClose)
			{
				while (_currentState != State.Error && _elemTop > 0)
				{
					await WriteEndElementAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			else if (_currentState != State.Error && _elemTop > 0)
			{
				try
				{
					await AdvanceStateAsync(Token.EndElement).ConfigureAwait(continueOnCapturedContext: false);
				}
				catch
				{
					_currentState = State.Error;
					throw;
				}
			}
			if (InBase64 && _rawWriter != null)
			{
				await _rawWriter.WriteEndBase64Async().ConfigureAwait(continueOnCapturedContext: false);
			}
			await _writer.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			try
			{
				if (_rawWriter == null)
				{
					await _writer.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				else
				{
					await _rawWriter.DisposeAsyncCore(WriteState).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			finally
			{
				_currentState = State.Closed;
			}
		}
	}
}
