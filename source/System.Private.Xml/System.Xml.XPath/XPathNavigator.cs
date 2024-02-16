using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Schema;
using MS.Internal.Xml.XPath;

namespace System.Xml.XPath;

[DebuggerDisplay("{debuggerDisplayProxy}")]
public abstract class XPathNavigator : XPathItem, ICloneable, IXPathNavigable, IXmlNamespaceResolver
{
	private sealed class CheckValidityHelper
	{
		private bool _isValid;

		private readonly ValidationEventHandler _nextEventHandler;

		private readonly XPathNavigatorReader _reader;

		internal bool IsValid => _isValid;

		internal CheckValidityHelper(ValidationEventHandler nextEventHandler, XPathNavigatorReader reader)
		{
			_isValid = true;
			_nextEventHandler = nextEventHandler;
			_reader = reader;
		}

		internal void ValidationCallback(object sender, ValidationEventArgs args)
		{
			if (args.Severity == XmlSeverityType.Error)
			{
				_isValid = false;
			}
			XmlSchemaValidationException ex = args.Exception as XmlSchemaValidationException;
			if (ex != null && _reader != null)
			{
				ex.SetSourceObject(_reader.UnderlyingObject);
			}
			if (_nextEventHandler != null)
			{
				_nextEventHandler(sender, args);
			}
			else if (ex != null && args.Severity == XmlSeverityType.Error)
			{
				throw ex;
			}
		}
	}

	[DebuggerDisplay("{ToString()}")]
	internal struct DebuggerDisplayProxy
	{
		private readonly XPathNavigator _nav;

		public DebuggerDisplayProxy(XPathNavigator nav)
		{
			_nav = nav;
		}

		public override string ToString()
		{
			string text = _nav.NodeType.ToString();
			switch (_nav.NodeType)
			{
			case XPathNodeType.Element:
				text = text + ", Name=\"" + _nav.Name + "\"";
				break;
			case XPathNodeType.Attribute:
			case XPathNodeType.Namespace:
			case XPathNodeType.ProcessingInstruction:
				text = text + ", Name=\"" + _nav.Name + "\"";
				text = text + ", Value=\"" + XmlConvert.EscapeValueForDebuggerDisplay(_nav.Value) + "\"";
				break;
			case XPathNodeType.Text:
			case XPathNodeType.SignificantWhitespace:
			case XPathNodeType.Whitespace:
			case XPathNodeType.Comment:
				text = text + ", Value=\"" + XmlConvert.EscapeValueForDebuggerDisplay(_nav.Value) + "\"";
				break;
			}
			return text;
		}
	}

	internal static readonly XPathNavigatorKeyComparer comparer = new XPathNavigatorKeyComparer();

	internal static readonly char[] NodeTypeLetter = new char[10] { 'R', 'E', 'A', 'N', 'T', 'S', 'W', 'P', 'C', 'X' };

	internal static readonly char[] UniqueIdTbl = new char[32]
	{
		'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J',
		'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
		'U', 'V', 'W', 'X', 'Y', 'Z', '1', '2', '3', '4',
		'5', '6'
	};

	internal static readonly int[] ContentKindMasks = new int[10] { 1, 2, 0, 0, 112, 32, 64, 128, 256, 2147483635 };

	public sealed override bool IsNode => true;

	public override XmlSchemaType? XmlType
	{
		get
		{
			IXmlSchemaInfo schemaInfo = SchemaInfo;
			if (schemaInfo != null && schemaInfo.Validity == XmlSchemaValidity.Valid)
			{
				XmlSchemaType memberType = schemaInfo.MemberType;
				if (memberType != null)
				{
					return memberType;
				}
				return schemaInfo.SchemaType;
			}
			return null;
		}
	}

	public override object TypedValue
	{
		get
		{
			IXmlSchemaInfo schemaInfo = SchemaInfo;
			if (schemaInfo != null)
			{
				if (schemaInfo.Validity == XmlSchemaValidity.Valid)
				{
					XmlSchemaType xmlSchemaType = schemaInfo.MemberType;
					if (xmlSchemaType == null)
					{
						xmlSchemaType = schemaInfo.SchemaType;
					}
					if (xmlSchemaType != null)
					{
						XmlSchemaDatatype datatype = xmlSchemaType.Datatype;
						if (datatype != null)
						{
							return xmlSchemaType.ValueConverter.ChangeType(Value, datatype.ValueType, this);
						}
					}
				}
				else
				{
					XmlSchemaType xmlSchemaType = schemaInfo.SchemaType;
					if (xmlSchemaType != null)
					{
						XmlSchemaDatatype datatype = xmlSchemaType.Datatype;
						if (datatype != null)
						{
							return xmlSchemaType.ValueConverter.ChangeType(datatype.ParseValue(Value, NameTable, this), datatype.ValueType, this);
						}
					}
				}
			}
			return Value;
		}
	}

	public override Type ValueType
	{
		get
		{
			IXmlSchemaInfo schemaInfo = SchemaInfo;
			if (schemaInfo != null)
			{
				if (schemaInfo.Validity == XmlSchemaValidity.Valid)
				{
					XmlSchemaType xmlSchemaType = schemaInfo.MemberType;
					if (xmlSchemaType == null)
					{
						xmlSchemaType = schemaInfo.SchemaType;
					}
					if (xmlSchemaType != null)
					{
						XmlSchemaDatatype datatype = xmlSchemaType.Datatype;
						if (datatype != null)
						{
							return datatype.ValueType;
						}
					}
				}
				else
				{
					XmlSchemaType xmlSchemaType = schemaInfo.SchemaType;
					if (xmlSchemaType != null)
					{
						XmlSchemaDatatype datatype = xmlSchemaType.Datatype;
						if (datatype != null)
						{
							return datatype.ValueType;
						}
					}
				}
			}
			return typeof(string);
		}
	}

	public override bool ValueAsBoolean
	{
		get
		{
			IXmlSchemaInfo schemaInfo = SchemaInfo;
			if (schemaInfo != null)
			{
				if (schemaInfo.Validity == XmlSchemaValidity.Valid)
				{
					XmlSchemaType xmlSchemaType = schemaInfo.MemberType;
					if (xmlSchemaType == null)
					{
						xmlSchemaType = schemaInfo.SchemaType;
					}
					if (xmlSchemaType != null)
					{
						return xmlSchemaType.ValueConverter.ToBoolean(Value);
					}
				}
				else
				{
					XmlSchemaType xmlSchemaType = schemaInfo.SchemaType;
					if (xmlSchemaType != null)
					{
						XmlSchemaDatatype datatype = xmlSchemaType.Datatype;
						if (datatype != null)
						{
							return xmlSchemaType.ValueConverter.ToBoolean(datatype.ParseValue(Value, NameTable, this));
						}
					}
				}
			}
			return XmlUntypedConverter.Untyped.ToBoolean(Value);
		}
	}

	public override DateTime ValueAsDateTime
	{
		get
		{
			IXmlSchemaInfo schemaInfo = SchemaInfo;
			if (schemaInfo != null)
			{
				if (schemaInfo.Validity == XmlSchemaValidity.Valid)
				{
					XmlSchemaType xmlSchemaType = schemaInfo.MemberType;
					if (xmlSchemaType == null)
					{
						xmlSchemaType = schemaInfo.SchemaType;
					}
					if (xmlSchemaType != null)
					{
						return xmlSchemaType.ValueConverter.ToDateTime(Value);
					}
				}
				else
				{
					XmlSchemaType xmlSchemaType = schemaInfo.SchemaType;
					if (xmlSchemaType != null)
					{
						XmlSchemaDatatype datatype = xmlSchemaType.Datatype;
						if (datatype != null)
						{
							return xmlSchemaType.ValueConverter.ToDateTime(datatype.ParseValue(Value, NameTable, this));
						}
					}
				}
			}
			return XmlUntypedConverter.Untyped.ToDateTime(Value);
		}
	}

	public override double ValueAsDouble
	{
		get
		{
			IXmlSchemaInfo schemaInfo = SchemaInfo;
			if (schemaInfo != null)
			{
				if (schemaInfo.Validity == XmlSchemaValidity.Valid)
				{
					XmlSchemaType xmlSchemaType = schemaInfo.MemberType;
					if (xmlSchemaType == null)
					{
						xmlSchemaType = schemaInfo.SchemaType;
					}
					if (xmlSchemaType != null)
					{
						return xmlSchemaType.ValueConverter.ToDouble(Value);
					}
				}
				else
				{
					XmlSchemaType xmlSchemaType = schemaInfo.SchemaType;
					if (xmlSchemaType != null)
					{
						XmlSchemaDatatype datatype = xmlSchemaType.Datatype;
						if (datatype != null)
						{
							return xmlSchemaType.ValueConverter.ToDouble(datatype.ParseValue(Value, NameTable, this));
						}
					}
				}
			}
			return XmlUntypedConverter.Untyped.ToDouble(Value);
		}
	}

	public override int ValueAsInt
	{
		get
		{
			IXmlSchemaInfo schemaInfo = SchemaInfo;
			if (schemaInfo != null)
			{
				if (schemaInfo.Validity == XmlSchemaValidity.Valid)
				{
					XmlSchemaType xmlSchemaType = schemaInfo.MemberType;
					if (xmlSchemaType == null)
					{
						xmlSchemaType = schemaInfo.SchemaType;
					}
					if (xmlSchemaType != null)
					{
						return xmlSchemaType.ValueConverter.ToInt32(Value);
					}
				}
				else
				{
					XmlSchemaType xmlSchemaType = schemaInfo.SchemaType;
					if (xmlSchemaType != null)
					{
						XmlSchemaDatatype datatype = xmlSchemaType.Datatype;
						if (datatype != null)
						{
							return xmlSchemaType.ValueConverter.ToInt32(datatype.ParseValue(Value, NameTable, this));
						}
					}
				}
			}
			return XmlUntypedConverter.Untyped.ToInt32(Value);
		}
	}

	public override long ValueAsLong
	{
		get
		{
			IXmlSchemaInfo schemaInfo = SchemaInfo;
			if (schemaInfo != null)
			{
				if (schemaInfo.Validity == XmlSchemaValidity.Valid)
				{
					XmlSchemaType xmlSchemaType = schemaInfo.MemberType;
					if (xmlSchemaType == null)
					{
						xmlSchemaType = schemaInfo.SchemaType;
					}
					if (xmlSchemaType != null)
					{
						return xmlSchemaType.ValueConverter.ToInt64(Value);
					}
				}
				else
				{
					XmlSchemaType xmlSchemaType = schemaInfo.SchemaType;
					if (xmlSchemaType != null)
					{
						XmlSchemaDatatype datatype = xmlSchemaType.Datatype;
						if (datatype != null)
						{
							return xmlSchemaType.ValueConverter.ToInt64(datatype.ParseValue(Value, NameTable, this));
						}
					}
				}
			}
			return XmlUntypedConverter.Untyped.ToInt64(Value);
		}
	}

	public abstract XmlNameTable NameTable { get; }

	public static IEqualityComparer NavigatorComparer => comparer;

	public abstract XPathNodeType NodeType { get; }

	public abstract string LocalName { get; }

	public abstract string Name { get; }

	public abstract string NamespaceURI { get; }

	public abstract string Prefix { get; }

	public abstract string BaseURI { get; }

	public abstract bool IsEmptyElement { get; }

	public virtual string XmlLang
	{
		get
		{
			XPathNavigator xPathNavigator = Clone();
			do
			{
				if (xPathNavigator.MoveToAttribute("lang", "http://www.w3.org/XML/1998/namespace"))
				{
					return xPathNavigator.Value;
				}
			}
			while (xPathNavigator.MoveToParent());
			return string.Empty;
		}
	}

	public virtual object? UnderlyingObject => null;

	public virtual bool HasAttributes
	{
		get
		{
			if (!MoveToFirstAttribute())
			{
				return false;
			}
			MoveToParent();
			return true;
		}
	}

	public virtual bool HasChildren
	{
		get
		{
			if (MoveToFirstChild())
			{
				MoveToParent();
				return true;
			}
			return false;
		}
	}

	public virtual IXmlSchemaInfo? SchemaInfo => this as IXmlSchemaInfo;

	public virtual bool CanEdit => false;

	public virtual string OuterXml
	{
		get
		{
			if (NodeType == XPathNodeType.Attribute)
			{
				return Name + "=\"" + Value + "\"";
			}
			if (NodeType == XPathNodeType.Namespace)
			{
				if (LocalName.Length == 0)
				{
					return "xmlns=\"" + Value + "\"";
				}
				return "xmlns:" + LocalName + "=\"" + Value + "\"";
			}
			StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
			XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
			xmlWriterSettings.Indent = true;
			xmlWriterSettings.OmitXmlDeclaration = true;
			xmlWriterSettings.ConformanceLevel = ConformanceLevel.Auto;
			XmlWriter xmlWriter = XmlWriter.Create(stringWriter, xmlWriterSettings);
			try
			{
				xmlWriter.WriteNode(this, defattr: true);
			}
			finally
			{
				xmlWriter.Close();
			}
			return stringWriter.ToString();
		}
		set
		{
			ReplaceSelf(value);
		}
	}

	public virtual string InnerXml
	{
		get
		{
			switch (NodeType)
			{
			case XPathNodeType.Root:
			case XPathNodeType.Element:
			{
				StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
				XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
				xmlWriterSettings.Indent = true;
				xmlWriterSettings.OmitXmlDeclaration = true;
				xmlWriterSettings.ConformanceLevel = ConformanceLevel.Auto;
				XmlWriter xmlWriter = XmlWriter.Create(stringWriter, xmlWriterSettings);
				try
				{
					if (MoveToFirstChild())
					{
						do
						{
							xmlWriter.WriteNode(this, defattr: true);
						}
						while (MoveToNext());
						MoveToParent();
					}
				}
				finally
				{
					xmlWriter.Close();
				}
				return stringWriter.ToString();
			}
			case XPathNodeType.Attribute:
			case XPathNodeType.Namespace:
				return Value;
			default:
				return string.Empty;
			}
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			switch (NodeType)
			{
			case XPathNodeType.Root:
			case XPathNodeType.Element:
			{
				XPathNavigator xPathNavigator = CreateNavigator();
				while (xPathNavigator.MoveToFirstChild())
				{
					xPathNavigator.DeleteSelf();
				}
				if (value.Length != 0)
				{
					xPathNavigator.AppendChild(value);
				}
				break;
			}
			case XPathNodeType.Attribute:
				SetValue(value);
				break;
			default:
				throw new InvalidOperationException(System.SR.Xpn_BadPosition);
			}
		}
	}

	internal uint IndexInParent
	{
		get
		{
			XPathNavigator xPathNavigator = Clone();
			uint num = 0u;
			XPathNodeType nodeType = NodeType;
			if (nodeType != XPathNodeType.Attribute)
			{
				if (nodeType == XPathNodeType.Namespace)
				{
					while (xPathNavigator.MoveToNextNamespace())
					{
						num++;
					}
				}
				else
				{
					while (xPathNavigator.MoveToNext())
					{
						num++;
					}
				}
			}
			else
			{
				while (xPathNavigator.MoveToNextAttribute())
				{
					num++;
				}
			}
			return num;
		}
	}

	internal virtual string UniqueId
	{
		get
		{
			XPathNavigator xPathNavigator = Clone();
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(NodeTypeLetter[(int)NodeType]);
			while (true)
			{
				uint num = xPathNavigator.IndexInParent;
				if (!xPathNavigator.MoveToParent())
				{
					break;
				}
				if (num <= 31)
				{
					stringBuilder.Append(UniqueIdTbl[num]);
					continue;
				}
				stringBuilder.Append('0');
				do
				{
					stringBuilder.Append(UniqueIdTbl[num & 0x1F]);
					num >>= 5;
				}
				while (num != 0);
				stringBuilder.Append('0');
			}
			return stringBuilder.ToString();
		}
	}

	private object debuggerDisplayProxy => new DebuggerDisplayProxy(this);

	public override string ToString()
	{
		return Value;
	}

	public virtual void SetValue(string value)
	{
		throw new NotSupportedException();
	}

	public virtual void SetTypedValue(object typedValue)
	{
		if (typedValue == null)
		{
			throw new ArgumentNullException("typedValue");
		}
		XPathNodeType nodeType = NodeType;
		if ((uint)(nodeType - 1) > 1u)
		{
			throw new InvalidOperationException(System.SR.Xpn_BadPosition);
		}
		string text = null;
		IXmlSchemaInfo schemaInfo = SchemaInfo;
		if (schemaInfo != null)
		{
			XmlSchemaType schemaType = schemaInfo.SchemaType;
			if (schemaType != null)
			{
				text = schemaType.ValueConverter.ToString(typedValue, this);
				schemaType.Datatype?.ParseValue(text, NameTable, this);
			}
		}
		if (text == null)
		{
			text = XmlUntypedConverter.Untyped.ToString(typedValue, this);
		}
		SetValue(text);
	}

	public override object ValueAs(Type returnType, IXmlNamespaceResolver? nsResolver)
	{
		if (nsResolver == null)
		{
			nsResolver = this;
		}
		IXmlSchemaInfo schemaInfo = SchemaInfo;
		if (schemaInfo != null)
		{
			if (schemaInfo.Validity == XmlSchemaValidity.Valid)
			{
				XmlSchemaType xmlSchemaType = schemaInfo.MemberType;
				if (xmlSchemaType == null)
				{
					xmlSchemaType = schemaInfo.SchemaType;
				}
				if (xmlSchemaType != null)
				{
					return xmlSchemaType.ValueConverter.ChangeType(Value, returnType, nsResolver);
				}
			}
			else
			{
				XmlSchemaType xmlSchemaType = schemaInfo.SchemaType;
				if (xmlSchemaType != null)
				{
					XmlSchemaDatatype datatype = xmlSchemaType.Datatype;
					if (datatype != null)
					{
						return xmlSchemaType.ValueConverter.ChangeType(datatype.ParseValue(Value, NameTable, nsResolver), returnType, nsResolver);
					}
				}
			}
		}
		return XmlUntypedConverter.Untyped.ChangeType(Value, returnType, nsResolver);
	}

	object ICloneable.Clone()
	{
		return Clone();
	}

	public virtual XPathNavigator CreateNavigator()
	{
		return Clone();
	}

	public virtual string? LookupNamespace(string prefix)
	{
		if (prefix == null)
		{
			return null;
		}
		if (NodeType != XPathNodeType.Element)
		{
			XPathNavigator xPathNavigator = Clone();
			if (xPathNavigator.MoveToParent())
			{
				return xPathNavigator.LookupNamespace(prefix);
			}
		}
		else if (MoveToNamespace(prefix))
		{
			string value = Value;
			MoveToParent();
			return value;
		}
		if (prefix.Length == 0)
		{
			return string.Empty;
		}
		if (prefix == "xml")
		{
			return "http://www.w3.org/XML/1998/namespace";
		}
		if (prefix == "xmlns")
		{
			return "http://www.w3.org/2000/xmlns/";
		}
		return null;
	}

	public virtual string? LookupPrefix(string namespaceURI)
	{
		if (namespaceURI == null)
		{
			return null;
		}
		XPathNavigator xPathNavigator = Clone();
		if (NodeType != XPathNodeType.Element)
		{
			if (xPathNavigator.MoveToParent())
			{
				return xPathNavigator.LookupPrefix(namespaceURI);
			}
		}
		else if (xPathNavigator.MoveToFirstNamespace(XPathNamespaceScope.All))
		{
			do
			{
				if (namespaceURI == xPathNavigator.Value)
				{
					return xPathNavigator.LocalName;
				}
			}
			while (xPathNavigator.MoveToNextNamespace(XPathNamespaceScope.All));
		}
		if (namespaceURI == LookupNamespace(string.Empty))
		{
			return string.Empty;
		}
		if (namespaceURI == "http://www.w3.org/XML/1998/namespace")
		{
			return "xml";
		}
		if (namespaceURI == "http://www.w3.org/2000/xmlns/")
		{
			return "xmlns";
		}
		return null;
	}

	public virtual IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope scope)
	{
		XPathNodeType nodeType = NodeType;
		if ((nodeType != XPathNodeType.Element && scope != XmlNamespaceScope.Local) || nodeType == XPathNodeType.Attribute || nodeType == XPathNodeType.Namespace)
		{
			XPathNavigator xPathNavigator = Clone();
			if (xPathNavigator.MoveToParent())
			{
				return xPathNavigator.GetNamespacesInScope(scope);
			}
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		if (scope == XmlNamespaceScope.All)
		{
			dictionary["xml"] = "http://www.w3.org/XML/1998/namespace";
		}
		if (MoveToFirstNamespace((XPathNamespaceScope)scope))
		{
			do
			{
				string localName = LocalName;
				string value = Value;
				if (localName.Length != 0 || value.Length != 0 || scope == XmlNamespaceScope.Local)
				{
					dictionary[localName] = value;
				}
			}
			while (MoveToNextNamespace((XPathNamespaceScope)scope));
			MoveToParent();
		}
		return dictionary;
	}

	public abstract XPathNavigator Clone();

	public virtual XmlReader ReadSubtree()
	{
		XPathNodeType nodeType = NodeType;
		if ((uint)nodeType > 1u)
		{
			throw new InvalidOperationException(System.SR.Xpn_BadPosition);
		}
		return CreateReader();
	}

	public virtual void WriteSubtree(XmlWriter writer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		writer.WriteNode(this, defattr: true);
	}

	public virtual string GetAttribute(string localName, string namespaceURI)
	{
		if (!MoveToAttribute(localName, namespaceURI))
		{
			return "";
		}
		string value = Value;
		MoveToParent();
		return value;
	}

	public virtual bool MoveToAttribute(string localName, string namespaceURI)
	{
		if (MoveToFirstAttribute())
		{
			do
			{
				if (localName == LocalName && namespaceURI == NamespaceURI)
				{
					return true;
				}
			}
			while (MoveToNextAttribute());
			MoveToParent();
		}
		return false;
	}

	public abstract bool MoveToFirstAttribute();

	public abstract bool MoveToNextAttribute();

	public virtual string GetNamespace(string name)
	{
		if (!MoveToNamespace(name))
		{
			if (name == "xml")
			{
				return "http://www.w3.org/XML/1998/namespace";
			}
			if (name == "xmlns")
			{
				return "http://www.w3.org/2000/xmlns/";
			}
			return string.Empty;
		}
		string value = Value;
		MoveToParent();
		return value;
	}

	public virtual bool MoveToNamespace(string name)
	{
		if (MoveToFirstNamespace(XPathNamespaceScope.All))
		{
			do
			{
				if (name == LocalName)
				{
					return true;
				}
			}
			while (MoveToNextNamespace(XPathNamespaceScope.All));
			MoveToParent();
		}
		return false;
	}

	public abstract bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope);

	public abstract bool MoveToNextNamespace(XPathNamespaceScope namespaceScope);

	public bool MoveToFirstNamespace()
	{
		return MoveToFirstNamespace(XPathNamespaceScope.All);
	}

	public bool MoveToNextNamespace()
	{
		return MoveToNextNamespace(XPathNamespaceScope.All);
	}

	public abstract bool MoveToNext();

	public abstract bool MoveToPrevious();

	public virtual bool MoveToFirst()
	{
		XPathNodeType nodeType = NodeType;
		if ((uint)(nodeType - 2) <= 1u)
		{
			return false;
		}
		if (!MoveToParent())
		{
			return false;
		}
		return MoveToFirstChild();
	}

	public abstract bool MoveToFirstChild();

	public abstract bool MoveToParent();

	public virtual void MoveToRoot()
	{
		while (MoveToParent())
		{
		}
	}

	public abstract bool MoveTo(XPathNavigator other);

	public abstract bool MoveToId(string id);

	public virtual bool MoveToChild(string localName, string namespaceURI)
	{
		if (MoveToFirstChild())
		{
			do
			{
				if (NodeType == XPathNodeType.Element && localName == LocalName && namespaceURI == NamespaceURI)
				{
					return true;
				}
			}
			while (MoveToNext());
			MoveToParent();
		}
		return false;
	}

	public virtual bool MoveToChild(XPathNodeType type)
	{
		if (MoveToFirstChild())
		{
			int contentKindMask = GetContentKindMask(type);
			do
			{
				if (((1 << (int)NodeType) & contentKindMask) != 0)
				{
					return true;
				}
			}
			while (MoveToNext());
			MoveToParent();
		}
		return false;
	}

	public virtual bool MoveToFollowing(string localName, string namespaceURI)
	{
		return MoveToFollowing(localName, namespaceURI, null);
	}

	public virtual bool MoveToFollowing(string localName, string namespaceURI, XPathNavigator? end)
	{
		XPathNavigator other = Clone();
		if (end != null)
		{
			XPathNodeType nodeType = end.NodeType;
			if ((uint)(nodeType - 2) <= 1u)
			{
				end = end.Clone();
				end.MoveToNonDescendant();
			}
		}
		XPathNodeType nodeType2 = NodeType;
		if ((uint)(nodeType2 - 2) <= 1u && !MoveToParent())
		{
			return false;
		}
		do
		{
			if (!MoveToFirstChild())
			{
				while (!MoveToNext())
				{
					if (!MoveToParent())
					{
						MoveTo(other);
						return false;
					}
				}
			}
			if (end != null && IsSamePosition(end))
			{
				MoveTo(other);
				return false;
			}
		}
		while (NodeType != XPathNodeType.Element || localName != LocalName || namespaceURI != NamespaceURI);
		return true;
	}

	public virtual bool MoveToFollowing(XPathNodeType type)
	{
		return MoveToFollowing(type, null);
	}

	public virtual bool MoveToFollowing(XPathNodeType type, XPathNavigator? end)
	{
		XPathNavigator other = Clone();
		int contentKindMask = GetContentKindMask(type);
		if (end != null)
		{
			XPathNodeType nodeType = end.NodeType;
			if ((uint)(nodeType - 2) <= 1u)
			{
				end = end.Clone();
				end.MoveToNonDescendant();
			}
		}
		XPathNodeType nodeType2 = NodeType;
		if ((uint)(nodeType2 - 2) <= 1u && !MoveToParent())
		{
			return false;
		}
		do
		{
			if (!MoveToFirstChild())
			{
				while (!MoveToNext())
				{
					if (!MoveToParent())
					{
						MoveTo(other);
						return false;
					}
				}
			}
			if (end != null && IsSamePosition(end))
			{
				MoveTo(other);
				return false;
			}
		}
		while (((1 << (int)NodeType) & contentKindMask) == 0);
		return true;
	}

	public virtual bool MoveToNext(string localName, string namespaceURI)
	{
		XPathNavigator other = Clone();
		while (MoveToNext())
		{
			if (NodeType == XPathNodeType.Element && localName == LocalName && namespaceURI == NamespaceURI)
			{
				return true;
			}
		}
		MoveTo(other);
		return false;
	}

	public virtual bool MoveToNext(XPathNodeType type)
	{
		XPathNavigator other = Clone();
		int contentKindMask = GetContentKindMask(type);
		while (MoveToNext())
		{
			if (((1 << (int)NodeType) & contentKindMask) != 0)
			{
				return true;
			}
		}
		MoveTo(other);
		return false;
	}

	public abstract bool IsSamePosition(XPathNavigator other);

	public virtual bool IsDescendant([NotNullWhen(true)] XPathNavigator? nav)
	{
		if (nav != null)
		{
			nav = nav.Clone();
			while (nav.MoveToParent())
			{
				if (nav.IsSamePosition(this))
				{
					return true;
				}
			}
		}
		return false;
	}

	public virtual XmlNodeOrder ComparePosition(XPathNavigator? nav)
	{
		if (nav == null)
		{
			return XmlNodeOrder.Unknown;
		}
		if (IsSamePosition(nav))
		{
			return XmlNodeOrder.Same;
		}
		XPathNavigator xPathNavigator = Clone();
		XPathNavigator xPathNavigator2 = nav.Clone();
		int num = GetDepth(xPathNavigator.Clone());
		int num2 = GetDepth(xPathNavigator2.Clone());
		if (num > num2)
		{
			while (num > num2)
			{
				xPathNavigator.MoveToParent();
				num--;
			}
			if (xPathNavigator.IsSamePosition(xPathNavigator2))
			{
				return XmlNodeOrder.After;
			}
		}
		if (num2 > num)
		{
			while (num2 > num)
			{
				xPathNavigator2.MoveToParent();
				num2--;
			}
			if (xPathNavigator.IsSamePosition(xPathNavigator2))
			{
				return XmlNodeOrder.Before;
			}
		}
		XPathNavigator xPathNavigator3 = xPathNavigator.Clone();
		XPathNavigator xPathNavigator4 = xPathNavigator2.Clone();
		while (true)
		{
			if (!xPathNavigator3.MoveToParent() || !xPathNavigator4.MoveToParent())
			{
				return XmlNodeOrder.Unknown;
			}
			if (xPathNavigator3.IsSamePosition(xPathNavigator4))
			{
				break;
			}
			xPathNavigator.MoveToParent();
			xPathNavigator2.MoveToParent();
		}
		_ = xPathNavigator.GetType().ToString() != "Microsoft.VisualStudio.Modeling.StoreNavigator";
		return CompareSiblings(xPathNavigator, xPathNavigator2);
	}

	public virtual bool CheckValidity(XmlSchemaSet schemas, ValidationEventHandler validationEventHandler)
	{
		XmlSchemaType xmlSchemaType = null;
		XmlSchemaElement xmlSchemaElement = null;
		XmlSchemaAttribute xmlSchemaAttribute = null;
		switch (NodeType)
		{
		case XPathNodeType.Root:
			if (schemas == null)
			{
				throw new InvalidOperationException(System.SR.XPathDocument_MissingSchemas);
			}
			xmlSchemaType = null;
			break;
		case XPathNodeType.Element:
		{
			if (schemas == null)
			{
				throw new InvalidOperationException(System.SR.XPathDocument_MissingSchemas);
			}
			IXmlSchemaInfo schemaInfo = SchemaInfo;
			if (schemaInfo != null)
			{
				xmlSchemaType = schemaInfo.SchemaType;
				xmlSchemaElement = schemaInfo.SchemaElement;
			}
			if (xmlSchemaType == null && xmlSchemaElement == null)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XPathDocument_NotEnoughSchemaInfo, null));
			}
			break;
		}
		case XPathNodeType.Attribute:
		{
			if (schemas == null)
			{
				throw new InvalidOperationException(System.SR.XPathDocument_MissingSchemas);
			}
			IXmlSchemaInfo schemaInfo = SchemaInfo;
			if (schemaInfo != null)
			{
				xmlSchemaType = schemaInfo.SchemaType;
				xmlSchemaAttribute = schemaInfo.SchemaAttribute;
			}
			if (xmlSchemaType == null && xmlSchemaAttribute == null)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XPathDocument_NotEnoughSchemaInfo, null));
			}
			break;
		}
		default:
			throw new InvalidOperationException(System.SR.Format(System.SR.XPathDocument_ValidateInvalidNodeType, null));
		}
		XPathNavigatorReader reader = (XPathNavigatorReader)CreateReader();
		CheckValidityHelper checkValidityHelper = new CheckValidityHelper(validationEventHandler, reader);
		validationEventHandler = checkValidityHelper.ValidationCallback;
		XmlReader validatingReader = GetValidatingReader(reader, schemas, validationEventHandler, xmlSchemaType, xmlSchemaElement, xmlSchemaAttribute);
		while (validatingReader.Read())
		{
		}
		return checkValidityHelper.IsValid;
	}

	private XmlReader GetValidatingReader(XmlReader reader, XmlSchemaSet schemas, ValidationEventHandler validationEvent, XmlSchemaType schemaType, XmlSchemaElement schemaElement, XmlSchemaAttribute schemaAttribute)
	{
		if (schemaAttribute != null)
		{
			return schemaAttribute.Validate(reader, null, schemas, validationEvent);
		}
		if (schemaElement != null)
		{
			return schemaElement.Validate(reader, null, schemas, validationEvent);
		}
		if (schemaType != null)
		{
			return schemaType.Validate(reader, null, schemas, validationEvent);
		}
		XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
		xmlReaderSettings.ConformanceLevel = ConformanceLevel.Auto;
		xmlReaderSettings.ValidationType = ValidationType.Schema;
		xmlReaderSettings.Schemas = schemas;
		xmlReaderSettings.ValidationEventHandler += validationEvent;
		return XmlReader.Create(reader, xmlReaderSettings);
	}

	public virtual XPathExpression Compile(string xpath)
	{
		return XPathExpression.Compile(xpath);
	}

	public virtual XPathNavigator? SelectSingleNode(string xpath)
	{
		return SelectSingleNode(XPathExpression.Compile(xpath));
	}

	public virtual XPathNavigator? SelectSingleNode(string xpath, IXmlNamespaceResolver? resolver)
	{
		return SelectSingleNode(XPathExpression.Compile(xpath, resolver));
	}

	public virtual XPathNavigator? SelectSingleNode(XPathExpression expression)
	{
		XPathNodeIterator xPathNodeIterator = Select(expression);
		if (xPathNodeIterator.MoveNext())
		{
			return xPathNodeIterator.Current;
		}
		return null;
	}

	public virtual XPathNodeIterator Select(string xpath)
	{
		return Select(XPathExpression.Compile(xpath));
	}

	public virtual XPathNodeIterator Select(string xpath, IXmlNamespaceResolver? resolver)
	{
		return Select(XPathExpression.Compile(xpath, resolver));
	}

	public virtual XPathNodeIterator Select(XPathExpression expr)
	{
		if (!(Evaluate(expr) is XPathNodeIterator result))
		{
			throw XPathException.Create(System.SR.Xp_NodeSetExpected);
		}
		return result;
	}

	public virtual object Evaluate(string xpath)
	{
		return Evaluate(XPathExpression.Compile(xpath), null);
	}

	public virtual object Evaluate(string xpath, IXmlNamespaceResolver? resolver)
	{
		return Evaluate(XPathExpression.Compile(xpath, resolver));
	}

	public virtual object Evaluate(XPathExpression expr)
	{
		return Evaluate(expr, null);
	}

	public virtual object Evaluate(XPathExpression expr, XPathNodeIterator? context)
	{
		if (!(expr is CompiledXpathExpr compiledXpathExpr))
		{
			throw XPathException.Create(System.SR.Xp_BadQueryObject);
		}
		Query query = Query.Clone(compiledXpathExpr.QueryTree);
		query.Reset();
		if (context == null)
		{
			context = new XPathSingletonIterator(Clone(), moved: true);
		}
		object obj = query.Evaluate(context);
		if (obj is XPathNodeIterator)
		{
			return new XPathSelectionIterator(context.Current, query);
		}
		return obj;
	}

	public virtual bool Matches(XPathExpression expr)
	{
		if (!(expr is CompiledXpathExpr compiledXpathExpr))
		{
			throw XPathException.Create(System.SR.Xp_BadQueryObject);
		}
		Query query = Query.Clone(compiledXpathExpr.QueryTree);
		try
		{
			return query.MatchNode(this) != null;
		}
		catch (XPathException)
		{
			throw XPathException.Create(System.SR.Xp_InvalidPattern, compiledXpathExpr.Expression);
		}
	}

	public virtual bool Matches(string xpath)
	{
		return Matches(CompileMatchPattern(xpath));
	}

	public virtual XPathNodeIterator SelectChildren(XPathNodeType type)
	{
		return new XPathChildIterator(Clone(), type);
	}

	public virtual XPathNodeIterator SelectChildren(string name, string namespaceURI)
	{
		return new XPathChildIterator(Clone(), name, namespaceURI);
	}

	public virtual XPathNodeIterator SelectAncestors(XPathNodeType type, bool matchSelf)
	{
		return new XPathAncestorIterator(Clone(), type, matchSelf);
	}

	public virtual XPathNodeIterator SelectAncestors(string name, string namespaceURI, bool matchSelf)
	{
		return new XPathAncestorIterator(Clone(), name, namespaceURI, matchSelf);
	}

	public virtual XPathNodeIterator SelectDescendants(XPathNodeType type, bool matchSelf)
	{
		return new XPathDescendantIterator(Clone(), type, matchSelf);
	}

	public virtual XPathNodeIterator SelectDescendants(string name, string namespaceURI, bool matchSelf)
	{
		return new XPathDescendantIterator(Clone(), name, namespaceURI, matchSelf);
	}

	public virtual XmlWriter PrependChild()
	{
		throw new NotSupportedException();
	}

	public virtual XmlWriter AppendChild()
	{
		throw new NotSupportedException();
	}

	public virtual XmlWriter InsertAfter()
	{
		throw new NotSupportedException();
	}

	public virtual XmlWriter InsertBefore()
	{
		throw new NotSupportedException();
	}

	public virtual XmlWriter CreateAttributes()
	{
		throw new NotSupportedException();
	}

	public virtual XmlWriter ReplaceRange(XPathNavigator lastSiblingToReplace)
	{
		throw new NotSupportedException();
	}

	public virtual void ReplaceSelf(string newNode)
	{
		XmlReader newNode2 = CreateContextReader(newNode, fromCurrentNode: false);
		ReplaceSelf(newNode2);
	}

	public virtual void ReplaceSelf(XmlReader newNode)
	{
		if (newNode == null)
		{
			throw new ArgumentNullException("newNode");
		}
		XPathNodeType nodeType = NodeType;
		if (nodeType == XPathNodeType.Root || nodeType == XPathNodeType.Attribute || nodeType == XPathNodeType.Namespace)
		{
			throw new InvalidOperationException(System.SR.Xpn_BadPosition);
		}
		XmlWriter xmlWriter = ReplaceRange(this);
		BuildSubtree(newNode, xmlWriter);
		xmlWriter.Close();
	}

	public virtual void ReplaceSelf(XPathNavigator newNode)
	{
		if (newNode == null)
		{
			throw new ArgumentNullException("newNode");
		}
		XmlReader newNode2 = newNode.CreateReader();
		ReplaceSelf(newNode2);
	}

	public virtual void AppendChild(string newChild)
	{
		XmlReader newChild2 = CreateContextReader(newChild, fromCurrentNode: true);
		AppendChild(newChild2);
	}

	public virtual void AppendChild(XmlReader newChild)
	{
		if (newChild == null)
		{
			throw new ArgumentNullException("newChild");
		}
		XmlWriter xmlWriter = AppendChild();
		BuildSubtree(newChild, xmlWriter);
		xmlWriter.Close();
	}

	public virtual void AppendChild(XPathNavigator newChild)
	{
		if (newChild == null)
		{
			throw new ArgumentNullException("newChild");
		}
		if (!IsValidChildType(newChild.NodeType))
		{
			throw new InvalidOperationException(System.SR.Xpn_BadPosition);
		}
		XmlReader newChild2 = newChild.CreateReader();
		AppendChild(newChild2);
	}

	public virtual void PrependChild(string newChild)
	{
		XmlReader newChild2 = CreateContextReader(newChild, fromCurrentNode: true);
		PrependChild(newChild2);
	}

	public virtual void PrependChild(XmlReader newChild)
	{
		if (newChild == null)
		{
			throw new ArgumentNullException("newChild");
		}
		XmlWriter xmlWriter = PrependChild();
		BuildSubtree(newChild, xmlWriter);
		xmlWriter.Close();
	}

	public virtual void PrependChild(XPathNavigator newChild)
	{
		if (newChild == null)
		{
			throw new ArgumentNullException("newChild");
		}
		if (!IsValidChildType(newChild.NodeType))
		{
			throw new InvalidOperationException(System.SR.Xpn_BadPosition);
		}
		XmlReader newChild2 = newChild.CreateReader();
		PrependChild(newChild2);
	}

	public virtual void InsertBefore(string newSibling)
	{
		XmlReader newSibling2 = CreateContextReader(newSibling, fromCurrentNode: false);
		InsertBefore(newSibling2);
	}

	public virtual void InsertBefore(XmlReader newSibling)
	{
		if (newSibling == null)
		{
			throw new ArgumentNullException("newSibling");
		}
		XmlWriter xmlWriter = InsertBefore();
		BuildSubtree(newSibling, xmlWriter);
		xmlWriter.Close();
	}

	public virtual void InsertBefore(XPathNavigator newSibling)
	{
		if (newSibling == null)
		{
			throw new ArgumentNullException("newSibling");
		}
		if (!IsValidSiblingType(newSibling.NodeType))
		{
			throw new InvalidOperationException(System.SR.Xpn_BadPosition);
		}
		XmlReader newSibling2 = newSibling.CreateReader();
		InsertBefore(newSibling2);
	}

	public virtual void InsertAfter(string newSibling)
	{
		XmlReader newSibling2 = CreateContextReader(newSibling, fromCurrentNode: false);
		InsertAfter(newSibling2);
	}

	public virtual void InsertAfter(XmlReader newSibling)
	{
		if (newSibling == null)
		{
			throw new ArgumentNullException("newSibling");
		}
		XmlWriter xmlWriter = InsertAfter();
		BuildSubtree(newSibling, xmlWriter);
		xmlWriter.Close();
	}

	public virtual void InsertAfter(XPathNavigator newSibling)
	{
		if (newSibling == null)
		{
			throw new ArgumentNullException("newSibling");
		}
		if (!IsValidSiblingType(newSibling.NodeType))
		{
			throw new InvalidOperationException(System.SR.Xpn_BadPosition);
		}
		XmlReader newSibling2 = newSibling.CreateReader();
		InsertAfter(newSibling2);
	}

	public virtual void DeleteRange(XPathNavigator lastSiblingToDelete)
	{
		throw new NotSupportedException();
	}

	public virtual void DeleteSelf()
	{
		DeleteRange(this);
	}

	public virtual void PrependChildElement(string prefix, string localName, string namespaceURI, string value)
	{
		XmlWriter xmlWriter = PrependChild();
		xmlWriter.WriteStartElement(prefix, localName, namespaceURI);
		if (value != null)
		{
			xmlWriter.WriteString(value);
		}
		xmlWriter.WriteEndElement();
		xmlWriter.Close();
	}

	public virtual void AppendChildElement(string prefix, string localName, string namespaceURI, string value)
	{
		XmlWriter xmlWriter = AppendChild();
		xmlWriter.WriteStartElement(prefix, localName, namespaceURI);
		if (value != null)
		{
			xmlWriter.WriteString(value);
		}
		xmlWriter.WriteEndElement();
		xmlWriter.Close();
	}

	public virtual void InsertElementBefore(string prefix, string localName, string namespaceURI, string value)
	{
		XmlWriter xmlWriter = InsertBefore();
		xmlWriter.WriteStartElement(prefix, localName, namespaceURI);
		if (value != null)
		{
			xmlWriter.WriteString(value);
		}
		xmlWriter.WriteEndElement();
		xmlWriter.Close();
	}

	public virtual void InsertElementAfter(string prefix, string localName, string namespaceURI, string value)
	{
		XmlWriter xmlWriter = InsertAfter();
		xmlWriter.WriteStartElement(prefix, localName, namespaceURI);
		if (value != null)
		{
			xmlWriter.WriteString(value);
		}
		xmlWriter.WriteEndElement();
		xmlWriter.Close();
	}

	public virtual void CreateAttribute(string prefix, string localName, string namespaceURI, string value)
	{
		XmlWriter xmlWriter = CreateAttributes();
		xmlWriter.WriteStartAttribute(prefix, localName, namespaceURI);
		if (value != null)
		{
			xmlWriter.WriteString(value);
		}
		xmlWriter.WriteEndAttribute();
		xmlWriter.Close();
	}

	internal bool MoveToPrevious(string localName, string namespaceURI)
	{
		XPathNavigator other = Clone();
		string text = ((localName != null) ? NameTable.Get(localName) : null);
		while (MoveToPrevious())
		{
			if (NodeType == XPathNodeType.Element && text == LocalName && namespaceURI == NamespaceURI)
			{
				return true;
			}
		}
		MoveTo(other);
		return false;
	}

	internal bool MoveToPrevious(XPathNodeType type)
	{
		XPathNavigator other = Clone();
		int contentKindMask = GetContentKindMask(type);
		while (MoveToPrevious())
		{
			if (((1 << (int)NodeType) & contentKindMask) != 0)
			{
				return true;
			}
		}
		MoveTo(other);
		return false;
	}

	internal bool MoveToNonDescendant()
	{
		if (NodeType == XPathNodeType.Root)
		{
			return false;
		}
		if (MoveToNext())
		{
			return true;
		}
		XPathNavigator xPathNavigator = Clone();
		if (!MoveToParent())
		{
			return false;
		}
		XPathNodeType nodeType = xPathNavigator.NodeType;
		if ((uint)(nodeType - 2) <= 1u && MoveToFirstChild())
		{
			return true;
		}
		while (!MoveToNext())
		{
			if (!MoveToParent())
			{
				MoveTo(xPathNavigator);
				return false;
			}
		}
		return true;
	}

	private static XPathExpression CompileMatchPattern(string xpath)
	{
		bool needContext;
		Query query = new QueryBuilder().BuildPatternQuery(xpath, out needContext);
		return new CompiledXpathExpr(query, xpath, needContext);
	}

	private static int GetDepth(XPathNavigator nav)
	{
		int num = 0;
		while (nav.MoveToParent())
		{
			num++;
		}
		return num;
	}

	private XmlNodeOrder CompareSiblings(XPathNavigator n1, XPathNavigator n2)
	{
		int num = 0;
		switch (n1.NodeType)
		{
		case XPathNodeType.Attribute:
			num++;
			break;
		default:
			num += 2;
			break;
		case XPathNodeType.Namespace:
			break;
		}
		switch (n2.NodeType)
		{
		case XPathNodeType.Namespace:
			if (num != 0)
			{
				break;
			}
			while (n1.MoveToNextNamespace())
			{
				if (n1.IsSamePosition(n2))
				{
					return XmlNodeOrder.Before;
				}
			}
			break;
		case XPathNodeType.Attribute:
			num--;
			if (num != 0)
			{
				break;
			}
			while (n1.MoveToNextAttribute())
			{
				if (n1.IsSamePosition(n2))
				{
					return XmlNodeOrder.Before;
				}
			}
			break;
		default:
			num -= 2;
			if (num != 0)
			{
				break;
			}
			while (n1.MoveToNext())
			{
				if (n1.IsSamePosition(n2))
				{
					return XmlNodeOrder.Before;
				}
			}
			break;
		}
		if (num >= 0)
		{
			return XmlNodeOrder.After;
		}
		return XmlNodeOrder.Before;
	}

	internal static XmlNamespaceManager GetNamespaces(IXmlNamespaceResolver resolver)
	{
		XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(new NameTable());
		IDictionary<string, string> namespacesInScope = resolver.GetNamespacesInScope(XmlNamespaceScope.All);
		foreach (KeyValuePair<string, string> item in namespacesInScope)
		{
			if (item.Key != "xmlns")
			{
				xmlNamespaceManager.AddNamespace(item.Key, item.Value);
			}
		}
		return xmlNamespaceManager;
	}

	internal static int GetContentKindMask(XPathNodeType type)
	{
		return ContentKindMasks[(int)type];
	}

	internal static int GetKindMask(XPathNodeType type)
	{
		return type switch
		{
			XPathNodeType.All => int.MaxValue, 
			XPathNodeType.Text => 112, 
			_ => 1 << (int)type, 
		};
	}

	internal static bool IsText(XPathNodeType type)
	{
		return (uint)(type - 4) <= 2u;
	}

	private bool IsValidChildType(XPathNodeType type)
	{
		switch (NodeType)
		{
		case XPathNodeType.Root:
			if (type == XPathNodeType.Element || (uint)(type - 5) <= 3u)
			{
				return true;
			}
			break;
		case XPathNodeType.Element:
			if (type == XPathNodeType.Element || (uint)(type - 4) <= 4u)
			{
				return true;
			}
			break;
		}
		return false;
	}

	private bool IsValidSiblingType(XPathNodeType type)
	{
		XPathNodeType nodeType = NodeType;
		if ((nodeType == XPathNodeType.Element || (uint)(nodeType - 4) <= 4u) && (type == XPathNodeType.Element || (uint)(type - 4) <= 4u))
		{
			return true;
		}
		return false;
	}

	private XmlReader CreateReader()
	{
		return XPathNavigatorReader.Create(this);
	}

	private XmlReader CreateContextReader(string xml, bool fromCurrentNode)
	{
		if (xml == null)
		{
			throw new ArgumentNullException("xml");
		}
		XPathNavigator xPathNavigator = CreateNavigator();
		XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(NameTable);
		if (!fromCurrentNode)
		{
			xPathNavigator.MoveToParent();
		}
		if (xPathNavigator.MoveToFirstNamespace(XPathNamespaceScope.All))
		{
			do
			{
				xmlNamespaceManager.AddNamespace(xPathNavigator.LocalName, xPathNavigator.Value);
			}
			while (xPathNavigator.MoveToNextNamespace(XPathNamespaceScope.All));
		}
		XmlParserContext context = new XmlParserContext(NameTable, xmlNamespaceManager, null, XmlSpace.Default);
		XmlTextReader xmlTextReader = new XmlTextReader(xml, XmlNodeType.Element, context);
		xmlTextReader.WhitespaceHandling = WhitespaceHandling.Significant;
		return xmlTextReader;
	}

	internal void BuildSubtree(XmlReader reader, XmlWriter writer)
	{
		string text = "http://www.w3.org/2000/xmlns/";
		ReadState readState = reader.ReadState;
		if (readState != 0 && readState != ReadState.Interactive)
		{
			throw new ArgumentException(System.SR.Xml_InvalidOperation, "reader");
		}
		int num = 0;
		if (readState == ReadState.Initial)
		{
			if (!reader.Read())
			{
				return;
			}
			num++;
		}
		do
		{
			switch (reader.NodeType)
			{
			case XmlNodeType.Element:
			{
				writer.WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
				bool isEmptyElement = reader.IsEmptyElement;
				while (reader.MoveToNextAttribute())
				{
					if ((object)reader.NamespaceURI == text)
					{
						if (reader.Prefix.Length == 0)
						{
							writer.WriteAttributeString("", "xmlns", text, reader.Value);
						}
						else
						{
							writer.WriteAttributeString("xmlns", reader.LocalName, text, reader.Value);
						}
					}
					else
					{
						writer.WriteStartAttribute(reader.Prefix, reader.LocalName, reader.NamespaceURI);
						writer.WriteString(reader.Value);
						writer.WriteEndAttribute();
					}
				}
				reader.MoveToElement();
				if (isEmptyElement)
				{
					writer.WriteEndElement();
				}
				else
				{
					num++;
				}
				break;
			}
			case XmlNodeType.EndElement:
				writer.WriteFullEndElement();
				num--;
				break;
			case XmlNodeType.Text:
			case XmlNodeType.CDATA:
				writer.WriteString(reader.Value);
				break;
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
				writer.WriteString(reader.Value);
				break;
			case XmlNodeType.Comment:
				writer.WriteComment(reader.Value);
				break;
			case XmlNodeType.ProcessingInstruction:
				writer.WriteProcessingInstruction(reader.LocalName, reader.Value);
				break;
			case XmlNodeType.EntityReference:
				reader.ResolveEntity();
				break;
			case XmlNodeType.Attribute:
				if ((object)reader.NamespaceURI == text)
				{
					if (reader.Prefix.Length == 0)
					{
						writer.WriteAttributeString("", "xmlns", text, reader.Value);
					}
					else
					{
						writer.WriteAttributeString("xmlns", reader.LocalName, text, reader.Value);
					}
				}
				else
				{
					writer.WriteStartAttribute(reader.Prefix, reader.LocalName, reader.NamespaceURI);
					writer.WriteString(reader.Value);
					writer.WriteEndAttribute();
				}
				break;
			}
		}
		while (reader.Read() && num > 0);
	}
}
