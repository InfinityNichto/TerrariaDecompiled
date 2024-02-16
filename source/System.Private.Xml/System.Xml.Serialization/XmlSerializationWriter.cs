using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml.Schema;

namespace System.Xml.Serialization;

public abstract class XmlSerializationWriter : XmlSerializationGeneratedCode
{
	internal sealed class TypeEntry
	{
		internal XmlSerializationWriteCallback callback;

		internal string typeNs;

		internal string typeName;

		internal Type type;
	}

	private XmlWriter _w;

	private XmlSerializerNamespaces _namespaces;

	private int _tempNamespacePrefix;

	private HashSet<int> _usedPrefixes;

	private Hashtable _references;

	private string _idBase;

	private int _nextId;

	private Hashtable _typeEntries;

	private ArrayList _referencesToWrite;

	private Hashtable _objectsInUse;

	private readonly string _aliasBase = "q";

	private bool _soap12;

	private bool _escapeName = true;

	protected bool EscapeName
	{
		get
		{
			return _escapeName;
		}
		set
		{
			_escapeName = value;
		}
	}

	protected XmlWriter Writer
	{
		get
		{
			return _w;
		}
		set
		{
			_w = value;
		}
	}

	protected ArrayList? Namespaces
	{
		get
		{
			if (_namespaces != null)
			{
				return _namespaces.NamespaceList;
			}
			return null;
		}
		set
		{
			if (value == null)
			{
				_namespaces = null;
				return;
			}
			XmlQualifiedName[] namespaces = (XmlQualifiedName[])value.ToArray(typeof(XmlQualifiedName));
			_namespaces = new XmlSerializerNamespaces(namespaces);
		}
	}

	internal void Init(XmlWriter w, XmlSerializerNamespaces namespaces, string encodingStyle, string idBase, TempAssembly tempAssembly)
	{
		_w = w;
		_namespaces = namespaces;
		_soap12 = encodingStyle == "http://www.w3.org/2003/05/soap-encoding";
		_idBase = idBase;
		Init(tempAssembly);
	}

	protected static byte[] FromByteArrayBase64(byte[] value)
	{
		return value;
	}

	protected static Assembly? ResolveDynamicAssembly(string assemblyFullName)
	{
		return DynamicAssemblies.Get(assemblyFullName);
	}

	[return: NotNullIfNotNull("value")]
	protected static string? FromByteArrayHex(byte[]? value)
	{
		return XmlCustomFormatter.FromByteArrayHex(value);
	}

	protected static string FromDateTime(DateTime value)
	{
		return XmlCustomFormatter.FromDateTime(value);
	}

	protected static string FromDate(DateTime value)
	{
		return XmlCustomFormatter.FromDate(value);
	}

	protected static string FromTime(DateTime value)
	{
		return XmlCustomFormatter.FromTime(value);
	}

	protected static string FromChar(char value)
	{
		return XmlCustomFormatter.FromChar(value);
	}

	protected static string FromEnum(long value, string[] values, long[] ids)
	{
		return XmlCustomFormatter.FromEnum(value, values, ids, null);
	}

	protected static string FromEnum(long value, string[] values, long[] ids, string typeName)
	{
		return XmlCustomFormatter.FromEnum(value, values, ids, typeName);
	}

	[return: NotNullIfNotNull("name")]
	protected static string? FromXmlName(string? name)
	{
		return XmlCustomFormatter.FromXmlName(name);
	}

	[return: NotNullIfNotNull("ncName")]
	protected static string? FromXmlNCName(string? ncName)
	{
		return XmlCustomFormatter.FromXmlNCName(ncName);
	}

	[return: NotNullIfNotNull("nmToken")]
	protected static string? FromXmlNmToken(string? nmToken)
	{
		return XmlCustomFormatter.FromXmlNmToken(nmToken);
	}

	[return: NotNullIfNotNull("nmTokens")]
	protected static string? FromXmlNmTokens(string? nmTokens)
	{
		return XmlCustomFormatter.FromXmlNmTokens(nmTokens);
	}

	protected void WriteXsiType(string name, string? ns)
	{
		WriteAttribute("type", "http://www.w3.org/2001/XMLSchema-instance", GetQualifiedName(name, ns));
	}

	[RequiresUnreferencedCode("calls GetPrimitiveTypeName")]
	private XmlQualifiedName GetPrimitiveTypeName(Type type)
	{
		return GetPrimitiveTypeName(type, throwIfUnknown: true);
	}

	[RequiresUnreferencedCode("calls CreateUnknownTypeException")]
	private XmlQualifiedName GetPrimitiveTypeName(Type type, bool throwIfUnknown)
	{
		XmlQualifiedName primitiveTypeNameInternal = GetPrimitiveTypeNameInternal(type);
		if (throwIfUnknown && primitiveTypeNameInternal == null)
		{
			throw CreateUnknownTypeException(type);
		}
		return primitiveTypeNameInternal;
	}

	internal static XmlQualifiedName GetPrimitiveTypeNameInternal(Type type)
	{
		string ns = "http://www.w3.org/2001/XMLSchema";
		string name;
		switch (Type.GetTypeCode(type))
		{
		case TypeCode.String:
			name = "string";
			break;
		case TypeCode.Int32:
			name = "int";
			break;
		case TypeCode.Boolean:
			name = "boolean";
			break;
		case TypeCode.Int16:
			name = "short";
			break;
		case TypeCode.Int64:
			name = "long";
			break;
		case TypeCode.Single:
			name = "float";
			break;
		case TypeCode.Double:
			name = "double";
			break;
		case TypeCode.Decimal:
			name = "decimal";
			break;
		case TypeCode.DateTime:
			name = "dateTime";
			break;
		case TypeCode.Byte:
			name = "unsignedByte";
			break;
		case TypeCode.SByte:
			name = "byte";
			break;
		case TypeCode.UInt16:
			name = "unsignedShort";
			break;
		case TypeCode.UInt32:
			name = "unsignedInt";
			break;
		case TypeCode.UInt64:
			name = "unsignedLong";
			break;
		case TypeCode.Char:
			name = "char";
			ns = "http://microsoft.com/wsdl/types/";
			break;
		default:
			if (type == typeof(XmlQualifiedName))
			{
				name = "QName";
				break;
			}
			if (type == typeof(byte[]))
			{
				name = "base64Binary";
				break;
			}
			if (type == typeof(Guid))
			{
				name = "guid";
				ns = "http://microsoft.com/wsdl/types/";
				break;
			}
			if (type == typeof(TimeSpan))
			{
				name = "TimeSpan";
				ns = "http://microsoft.com/wsdl/types/";
				break;
			}
			if (type == typeof(DateTimeOffset))
			{
				name = "dateTimeOffset";
				ns = "http://microsoft.com/wsdl/types/";
				break;
			}
			if (type == typeof(XmlNode[]))
			{
				name = "anyType";
				break;
			}
			return null;
		}
		return new XmlQualifiedName(name, ns);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	protected void WriteTypedPrimitive(string? name, string? ns, object o, bool xsiType)
	{
		string text = null;
		string ns2 = "http://www.w3.org/2001/XMLSchema";
		bool flag = true;
		bool flag2 = false;
		Type type = o.GetType();
		bool flag3 = false;
		string text2;
		switch (Type.GetTypeCode(type))
		{
		case TypeCode.String:
			text = (string)o;
			text2 = "string";
			flag = false;
			break;
		case TypeCode.Int32:
			text = XmlConvert.ToString((int)o);
			text2 = "int";
			break;
		case TypeCode.Boolean:
			text = XmlConvert.ToString((bool)o);
			text2 = "boolean";
			break;
		case TypeCode.Int16:
			text = XmlConvert.ToString((short)o);
			text2 = "short";
			break;
		case TypeCode.Int64:
			text = XmlConvert.ToString((long)o);
			text2 = "long";
			break;
		case TypeCode.Single:
			text = XmlConvert.ToString((float)o);
			text2 = "float";
			break;
		case TypeCode.Double:
			text = XmlConvert.ToString((double)o);
			text2 = "double";
			break;
		case TypeCode.Decimal:
			text = XmlConvert.ToString((decimal)o);
			text2 = "decimal";
			break;
		case TypeCode.DateTime:
			text = FromDateTime((DateTime)o);
			text2 = "dateTime";
			break;
		case TypeCode.Char:
			text = FromChar((char)o);
			text2 = "char";
			ns2 = "http://microsoft.com/wsdl/types/";
			break;
		case TypeCode.Byte:
			text = XmlConvert.ToString((byte)o);
			text2 = "unsignedByte";
			break;
		case TypeCode.SByte:
			text = XmlConvert.ToString((sbyte)o);
			text2 = "byte";
			break;
		case TypeCode.UInt16:
			text = XmlConvert.ToString((ushort)o);
			text2 = "unsignedShort";
			break;
		case TypeCode.UInt32:
			text = XmlConvert.ToString((uint)o);
			text2 = "unsignedInt";
			break;
		case TypeCode.UInt64:
			text = XmlConvert.ToString((ulong)o);
			text2 = "unsignedLong";
			break;
		default:
			if (type == typeof(XmlQualifiedName))
			{
				text2 = "QName";
				flag3 = true;
				if (name == null)
				{
					_w.WriteStartElement(text2, ns2);
				}
				else
				{
					_w.WriteStartElement(name, ns);
				}
				text = FromXmlQualifiedName((XmlQualifiedName)o, ignoreEmpty: false);
				break;
			}
			if (type == typeof(byte[]))
			{
				text = string.Empty;
				flag2 = true;
				text2 = "base64Binary";
				break;
			}
			if (type == typeof(Guid))
			{
				text = XmlConvert.ToString((Guid)o);
				text2 = "guid";
				ns2 = "http://microsoft.com/wsdl/types/";
				break;
			}
			if (type == typeof(TimeSpan))
			{
				text = XmlConvert.ToString((TimeSpan)o);
				text2 = "TimeSpan";
				ns2 = "http://microsoft.com/wsdl/types/";
				break;
			}
			if (type == typeof(DateTimeOffset))
			{
				text = XmlConvert.ToString((DateTimeOffset)o);
				text2 = "dateTimeOffset";
				ns2 = "http://microsoft.com/wsdl/types/";
				break;
			}
			if (typeof(XmlNode[]).IsAssignableFrom(type))
			{
				if (name == null)
				{
					_w.WriteStartElement("anyType", "http://www.w3.org/2001/XMLSchema");
				}
				else
				{
					_w.WriteStartElement(name, ns);
				}
				XmlNode[] array = (XmlNode[])o;
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] != null)
					{
						array[i].WriteTo(_w);
					}
				}
				_w.WriteEndElement();
				return;
			}
			throw CreateUnknownTypeException(type);
		}
		if (!flag3)
		{
			if (name == null)
			{
				_w.WriteStartElement(text2, ns2);
			}
			else
			{
				_w.WriteStartElement(name, ns);
			}
		}
		if (xsiType)
		{
			WriteXsiType(text2, ns2);
		}
		if (text == null)
		{
			_w.WriteAttributeString("nil", "http://www.w3.org/2001/XMLSchema-instance", "true");
		}
		else if (flag2)
		{
			XmlCustomFormatter.WriteArrayBase64(_w, (byte[])o, 0, ((byte[])o).Length);
		}
		else if (flag)
		{
			_w.WriteRaw(text);
		}
		else
		{
			_w.WriteString(text);
		}
		_w.WriteEndElement();
	}

	private string GetQualifiedName(string name, string ns)
	{
		if (ns == null || ns.Length == 0)
		{
			return name;
		}
		string text = _w.LookupPrefix(ns);
		if (text == null)
		{
			if (ns == "http://www.w3.org/XML/1998/namespace")
			{
				text = "xml";
			}
			else
			{
				text = NextPrefix();
				WriteAttribute("xmlns", text, null, ns);
			}
		}
		else if (text.Length == 0)
		{
			return name;
		}
		return text + ":" + name;
	}

	protected string? FromXmlQualifiedName(XmlQualifiedName? xmlQualifiedName)
	{
		return FromXmlQualifiedName(xmlQualifiedName, ignoreEmpty: true);
	}

	protected string? FromXmlQualifiedName(XmlQualifiedName? xmlQualifiedName, bool ignoreEmpty)
	{
		if (xmlQualifiedName == null)
		{
			return null;
		}
		if (xmlQualifiedName.IsEmpty && ignoreEmpty)
		{
			return null;
		}
		return GetQualifiedName(EscapeName ? XmlConvert.EncodeLocalName(xmlQualifiedName.Name) : xmlQualifiedName.Name, xmlQualifiedName.Namespace);
	}

	protected void WriteStartElement(string name)
	{
		WriteStartElement(name, null, null, writePrefixed: false, null);
	}

	protected void WriteStartElement(string name, string? ns)
	{
		WriteStartElement(name, ns, null, writePrefixed: false, null);
	}

	protected void WriteStartElement(string name, string? ns, bool writePrefixed)
	{
		WriteStartElement(name, ns, null, writePrefixed, null);
	}

	protected void WriteStartElement(string name, string? ns, object? o)
	{
		WriteStartElement(name, ns, o, writePrefixed: false, null);
	}

	protected void WriteStartElement(string name, string? ns, object? o, bool writePrefixed)
	{
		WriteStartElement(name, ns, o, writePrefixed, null);
	}

	protected void WriteStartElement(string name, string? ns, object? o, bool writePrefixed, XmlSerializerNamespaces? xmlns)
	{
		if (o != null && _objectsInUse != null)
		{
			if (_objectsInUse.ContainsKey(o))
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlCircularReference, o.GetType().FullName));
			}
			_objectsInUse.Add(o, o);
		}
		string prefix = null;
		bool flag = false;
		if (_namespaces != null)
		{
			_namespaces.TryLookupPrefix(ns, out prefix);
			if (_namespaces.TryLookupNamespace("", out var ns2))
			{
				if (string.IsNullOrEmpty(ns2))
				{
					flag = true;
				}
				if (ns != ns2)
				{
					writePrefixed = true;
				}
			}
			_usedPrefixes = ListUsedPrefixes(_namespaces, _aliasBase);
		}
		if (writePrefixed && prefix == null && ns != null && ns.Length > 0)
		{
			prefix = _w.LookupPrefix(ns);
			if (prefix == null || prefix.Length == 0)
			{
				prefix = NextPrefix();
			}
		}
		if (prefix == null)
		{
			xmlns?.TryLookupPrefix(ns, out prefix);
		}
		if (flag && prefix == null && ns != null && ns.Length != 0)
		{
			prefix = NextPrefix();
		}
		_w.WriteStartElement(prefix, name, ns);
		if (_namespaces != null)
		{
			foreach (XmlQualifiedName namespace2 in _namespaces.Namespaces)
			{
				string name2 = namespace2.Name;
				string @namespace = namespace2.Namespace;
				if (name2.Length == 0 && (@namespace == null || @namespace.Length == 0))
				{
					continue;
				}
				if (@namespace == null || @namespace.Length == 0)
				{
					if (name2.Length > 0)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidXmlns, name2));
					}
					WriteAttribute("xmlns", name2, null, @namespace);
				}
				else if (_w.LookupPrefix(@namespace) == null)
				{
					if (prefix == null && name2.Length == 0)
					{
						break;
					}
					WriteAttribute("xmlns", name2, null, @namespace);
				}
			}
		}
		WriteNamespaceDeclarations(xmlns);
	}

	private HashSet<int> ListUsedPrefixes(XmlSerializerNamespaces nsList, string prefix)
	{
		HashSet<int> hashSet = new HashSet<int>();
		int length = prefix.Length;
		foreach (XmlQualifiedName @namespace in nsList.Namespaces)
		{
			if (@namespace.Name.Length <= length)
			{
				continue;
			}
			string name = @namespace.Name;
			if (name.Length <= length || name.Length > length + "2147483647".Length || !name.StartsWith(prefix, StringComparison.Ordinal))
			{
				continue;
			}
			bool flag = true;
			for (int i = length; i < name.Length; i++)
			{
				if (!char.IsDigit(name, i))
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				long num = long.Parse(name.AsSpan(length), NumberStyles.Integer, CultureInfo.InvariantCulture);
				if (num <= int.MaxValue)
				{
					int item = (int)num;
					hashSet.Add(item);
				}
			}
		}
		if (hashSet.Count > 0)
		{
			return hashSet;
		}
		return null;
	}

	protected void WriteNullTagEncoded(string? name)
	{
		WriteNullTagEncoded(name, null);
	}

	protected void WriteNullTagEncoded(string? name, string? ns)
	{
		if (name != null && name.Length != 0)
		{
			WriteStartElement(name, ns, null, writePrefixed: true);
			_w.WriteAttributeString("nil", "http://www.w3.org/2001/XMLSchema-instance", "true");
			_w.WriteEndElement();
		}
	}

	protected void WriteNullTagLiteral(string? name)
	{
		WriteNullTagLiteral(name, null);
	}

	protected void WriteNullTagLiteral(string? name, string? ns)
	{
		if (name != null && name.Length != 0)
		{
			WriteStartElement(name, ns, null, writePrefixed: false);
			_w.WriteAttributeString("nil", "http://www.w3.org/2001/XMLSchema-instance", "true");
			_w.WriteEndElement();
		}
	}

	protected void WriteEmptyTag(string? name)
	{
		WriteEmptyTag(name, null);
	}

	protected void WriteEmptyTag(string? name, string? ns)
	{
		if (name != null && name.Length != 0)
		{
			WriteStartElement(name, ns, null, writePrefixed: false);
			_w.WriteEndElement();
		}
	}

	protected void WriteEndElement()
	{
		_w.WriteEndElement();
	}

	protected void WriteEndElement(object? o)
	{
		_w.WriteEndElement();
		if (o != null && _objectsInUse != null)
		{
			_objectsInUse.Remove(o);
		}
	}

	protected void WriteSerializable(IXmlSerializable? serializable, string name, string ns, bool isNullable)
	{
		WriteSerializable(serializable, name, ns, isNullable, wrapped: true);
	}

	protected void WriteSerializable(IXmlSerializable? serializable, string name, string? ns, bool isNullable, bool wrapped)
	{
		if (serializable == null)
		{
			if (isNullable)
			{
				WriteNullTagLiteral(name, ns);
			}
			return;
		}
		if (wrapped)
		{
			_w.WriteStartElement(name, ns);
		}
		serializable.WriteXml(_w);
		if (wrapped)
		{
			_w.WriteEndElement();
		}
	}

	protected void WriteNullableStringEncoded(string name, string? ns, string? value, XmlQualifiedName? xsiType)
	{
		if (value == null)
		{
			WriteNullTagEncoded(name, ns);
		}
		else
		{
			WriteElementString(name, ns, value, xsiType);
		}
	}

	protected void WriteNullableStringLiteral(string name, string? ns, string? value)
	{
		if (value == null)
		{
			WriteNullTagLiteral(name, ns);
		}
		else
		{
			WriteElementString(name, ns, value, null);
		}
	}

	protected void WriteNullableStringEncodedRaw(string name, string? ns, string? value, XmlQualifiedName? xsiType)
	{
		if (value == null)
		{
			WriteNullTagEncoded(name, ns);
		}
		else
		{
			WriteElementStringRaw(name, ns, value, xsiType);
		}
	}

	protected void WriteNullableStringEncodedRaw(string name, string? ns, byte[]? value, XmlQualifiedName? xsiType)
	{
		if (value == null)
		{
			WriteNullTagEncoded(name, ns);
		}
		else
		{
			WriteElementStringRaw(name, ns, value, xsiType);
		}
	}

	protected void WriteNullableStringLiteralRaw(string name, string? ns, string? value)
	{
		if (value == null)
		{
			WriteNullTagLiteral(name, ns);
		}
		else
		{
			WriteElementStringRaw(name, ns, value, null);
		}
	}

	protected void WriteNullableStringLiteralRaw(string name, string? ns, byte[]? value)
	{
		if (value == null)
		{
			WriteNullTagLiteral(name, ns);
		}
		else
		{
			WriteElementStringRaw(name, ns, value, null);
		}
	}

	protected void WriteNullableQualifiedNameEncoded(string name, string? ns, XmlQualifiedName? value, XmlQualifiedName? xsiType)
	{
		if (value == null)
		{
			WriteNullTagEncoded(name, ns);
		}
		else
		{
			WriteElementQualifiedName(name, ns, value, xsiType);
		}
	}

	protected void WriteNullableQualifiedNameLiteral(string name, string? ns, XmlQualifiedName? value)
	{
		if (value == null)
		{
			WriteNullTagLiteral(name, ns);
		}
		else
		{
			WriteElementQualifiedName(name, ns, value, null);
		}
	}

	protected void WriteElementEncoded(XmlNode? node, string name, string? ns, bool isNullable, bool any)
	{
		if (node == null)
		{
			if (isNullable)
			{
				WriteNullTagEncoded(name, ns);
			}
		}
		else
		{
			WriteElement(node, name, ns, isNullable, any);
		}
	}

	protected void WriteElementLiteral(XmlNode? node, string name, string? ns, bool isNullable, bool any)
	{
		if (node == null)
		{
			if (isNullable)
			{
				WriteNullTagLiteral(name, ns);
			}
		}
		else
		{
			WriteElement(node, name, ns, isNullable, any);
		}
	}

	private void WriteElement(XmlNode node, string name, string ns, bool isNullable, bool any)
	{
		if (typeof(XmlAttribute).IsAssignableFrom(node.GetType()))
		{
			throw new InvalidOperationException(System.SR.XmlNoAttributeHere);
		}
		if (node is XmlDocument)
		{
			node = ((XmlDocument)node).DocumentElement;
			if (node == null)
			{
				if (isNullable)
				{
					WriteNullTagEncoded(name, ns);
				}
				return;
			}
		}
		if (any)
		{
			if (node is XmlElement && name != null && name.Length > 0 && (node.LocalName != name || node.NamespaceURI != ns))
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlElementNameMismatch, node.LocalName, node.NamespaceURI, name, ns));
			}
		}
		else
		{
			_w.WriteStartElement(name, ns);
		}
		node.WriteTo(_w);
		if (!any)
		{
			_w.WriteEndElement();
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	protected Exception CreateUnknownTypeException(object o)
	{
		return CreateUnknownTypeException(o.GetType());
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	protected Exception CreateUnknownTypeException(Type type)
	{
		if (typeof(IXmlSerializable).IsAssignableFrom(type))
		{
			return new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidSerializable, type.FullName));
		}
		TypeDesc typeDesc = new TypeScope().GetTypeDesc(type);
		if (!typeDesc.IsStructLike)
		{
			return new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidUseOfType, type.FullName));
		}
		return new InvalidOperationException(System.SR.Format(System.SR.XmlUnxpectedType, type.FullName));
	}

	protected Exception CreateMismatchChoiceException(string value, string elementName, string enumValue)
	{
		return new InvalidOperationException(System.SR.Format(System.SR.XmlChoiceMismatchChoiceException, elementName, value, enumValue));
	}

	protected Exception CreateUnknownAnyElementException(string name, string ns)
	{
		return new InvalidOperationException(System.SR.Format(System.SR.XmlUnknownAnyElement, name, ns));
	}

	protected Exception CreateInvalidChoiceIdentifierValueException(string type, string identifier)
	{
		return new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidChoiceIdentifierValue, type, identifier));
	}

	protected Exception CreateChoiceIdentifierValueException(string value, string identifier, string name, string ns)
	{
		return new InvalidOperationException(System.SR.Format(System.SR.XmlChoiceIdentifierMismatch, value, identifier, name, ns));
	}

	protected Exception CreateInvalidEnumValueException(object value, string typeName)
	{
		return new InvalidOperationException(System.SR.Format(System.SR.XmlUnknownConstant, value, typeName));
	}

	protected Exception CreateInvalidAnyTypeException(object o)
	{
		return CreateInvalidAnyTypeException(o.GetType());
	}

	protected Exception CreateInvalidAnyTypeException(Type type)
	{
		return new InvalidOperationException(System.SR.Format(System.SR.XmlIllegalAnyElement, type.FullName));
	}

	protected void WriteReferencingElement(string n, string? ns, object? o)
	{
		WriteReferencingElement(n, ns, o, isNullable: false);
	}

	protected void WriteReferencingElement(string n, string? ns, object? o, bool isNullable)
	{
		if (o == null)
		{
			if (isNullable)
			{
				WriteNullTagEncoded(n, ns);
			}
			return;
		}
		WriteStartElement(n, ns, null, writePrefixed: true);
		if (_soap12)
		{
			_w.WriteAttributeString("ref", "http://www.w3.org/2003/05/soap-encoding", GetId(o, addToReferencesList: true));
		}
		else
		{
			_w.WriteAttributeString("href", "#" + GetId(o, addToReferencesList: true));
		}
		_w.WriteEndElement();
	}

	private bool IsIdDefined(object o)
	{
		if (_references != null)
		{
			return _references.Contains(o);
		}
		return false;
	}

	private string GetId(object o, bool addToReferencesList)
	{
		if (_references == null)
		{
			_references = new Hashtable();
			_referencesToWrite = new ArrayList();
		}
		string text = (string)_references[o];
		if (text == null)
		{
			string idBase = _idBase;
			int num = ++_nextId;
			text = idBase + "id" + num.ToString(CultureInfo.InvariantCulture);
			_references.Add(o, text);
			if (addToReferencesList)
			{
				_referencesToWrite.Add(o);
			}
		}
		return text;
	}

	protected void WriteId(object o)
	{
		WriteId(o, addToReferencesList: true);
	}

	private void WriteId(object o, bool addToReferencesList)
	{
		if (_soap12)
		{
			_w.WriteAttributeString("id", "http://www.w3.org/2003/05/soap-encoding", GetId(o, addToReferencesList));
		}
		else
		{
			_w.WriteAttributeString("id", GetId(o, addToReferencesList));
		}
	}

	protected void WriteXmlAttribute(XmlNode node)
	{
		WriteXmlAttribute(node, null);
	}

	protected void WriteXmlAttribute(XmlNode node, object? container)
	{
		if (!(node is XmlAttribute xmlAttribute))
		{
			throw new InvalidOperationException(System.SR.XmlNeedAttributeHere);
		}
		if (xmlAttribute.Value != null)
		{
			if (xmlAttribute.NamespaceURI == "http://schemas.xmlsoap.org/wsdl/" && xmlAttribute.LocalName == "arrayType")
			{
				string dims;
				XmlQualifiedName xmlQualifiedName = TypeScope.ParseWsdlArrayType(xmlAttribute.Value, out dims, (container is XmlSchemaObject) ? ((XmlSchemaObject)container) : null);
				string value = FromXmlQualifiedName(xmlQualifiedName, ignoreEmpty: true) + dims;
				WriteAttribute("arrayType", "http://schemas.xmlsoap.org/wsdl/", value);
			}
			else
			{
				WriteAttribute(xmlAttribute.Name, xmlAttribute.NamespaceURI, xmlAttribute.Value);
			}
		}
	}

	protected void WriteAttribute(string localName, string? ns, string? value)
	{
		if (value == null || !(localName != "xmlns") || localName.StartsWith("xmlns:", StringComparison.Ordinal))
		{
			return;
		}
		int num = localName.IndexOf(':');
		if (num < 0)
		{
			if (ns == "http://www.w3.org/XML/1998/namespace")
			{
				string text = _w.LookupPrefix(ns);
				if (text == null || text.Length == 0)
				{
					text = "xml";
				}
				_w.WriteAttributeString(text, localName, ns, value);
			}
			else
			{
				_w.WriteAttributeString(localName, ns, value);
			}
		}
		else
		{
			string prefix = localName.Substring(0, num);
			_w.WriteAttributeString(prefix, localName.Substring(num + 1), ns, value);
		}
	}

	protected void WriteAttribute(string localName, string ns, byte[]? value)
	{
		if (value == null || !(localName != "xmlns") || localName.StartsWith("xmlns:", StringComparison.Ordinal))
		{
			return;
		}
		int num = localName.IndexOf(':');
		if (num < 0)
		{
			if (ns == "http://www.w3.org/XML/1998/namespace")
			{
				string text = _w.LookupPrefix(ns);
				if (text == null || text.Length == 0)
				{
					text = "xml";
				}
				_w.WriteStartAttribute("xml", localName, ns);
			}
			else
			{
				_w.WriteStartAttribute(null, localName, ns);
			}
		}
		else
		{
			string prefix = _w.LookupPrefix(ns);
			_w.WriteStartAttribute(prefix, localName.Substring(num + 1), ns);
		}
		XmlCustomFormatter.WriteArrayBase64(_w, value, 0, value.Length);
		_w.WriteEndAttribute();
	}

	protected void WriteAttribute(string localName, string? value)
	{
		if (value != null)
		{
			_w.WriteAttributeString(localName, null, value);
		}
	}

	protected void WriteAttribute(string localName, byte[]? value)
	{
		if (value != null)
		{
			_w.WriteStartAttribute(null, localName, null);
			XmlCustomFormatter.WriteArrayBase64(_w, value, 0, value.Length);
			_w.WriteEndAttribute();
		}
	}

	protected void WriteAttribute(string? prefix, string localName, string? ns, string? value)
	{
		if (value != null)
		{
			_w.WriteAttributeString(prefix, localName, null, value);
		}
	}

	protected void WriteValue(string? value)
	{
		if (value != null)
		{
			_w.WriteString(value);
		}
	}

	protected void WriteValue(byte[]? value)
	{
		if (value != null)
		{
			XmlCustomFormatter.WriteArrayBase64(_w, value, 0, value.Length);
		}
	}

	protected void WriteStartDocument()
	{
		if (_w.WriteState == WriteState.Start)
		{
			_w.WriteStartDocument();
		}
	}

	protected void WriteElementString(string localName, string? value)
	{
		WriteElementString(localName, null, value, null);
	}

	protected void WriteElementString(string localName, string? ns, string? value)
	{
		WriteElementString(localName, ns, value, null);
	}

	protected void WriteElementString(string localName, string? value, XmlQualifiedName? xsiType)
	{
		WriteElementString(localName, null, value, xsiType);
	}

	protected void WriteElementString(string localName, string? ns, string? value, XmlQualifiedName? xsiType)
	{
		if (value != null)
		{
			if (xsiType == null)
			{
				_w.WriteElementString(localName, ns, value);
				return;
			}
			_w.WriteStartElement(localName, ns);
			WriteXsiType(xsiType.Name, xsiType.Namespace);
			_w.WriteString(value);
			_w.WriteEndElement();
		}
	}

	protected void WriteElementStringRaw(string localName, string? value)
	{
		WriteElementStringRaw(localName, null, value, null);
	}

	protected void WriteElementStringRaw(string localName, byte[]? value)
	{
		WriteElementStringRaw(localName, null, value, null);
	}

	protected void WriteElementStringRaw(string localName, string? ns, string? value)
	{
		WriteElementStringRaw(localName, ns, value, null);
	}

	protected void WriteElementStringRaw(string localName, string? ns, byte[]? value)
	{
		WriteElementStringRaw(localName, ns, value, null);
	}

	protected void WriteElementStringRaw(string localName, string? value, XmlQualifiedName? xsiType)
	{
		WriteElementStringRaw(localName, null, value, xsiType);
	}

	protected void WriteElementStringRaw(string localName, byte[]? value, XmlQualifiedName? xsiType)
	{
		WriteElementStringRaw(localName, null, value, xsiType);
	}

	protected void WriteElementStringRaw(string localName, string? ns, string? value, XmlQualifiedName? xsiType)
	{
		if (value != null)
		{
			_w.WriteStartElement(localName, ns);
			if (xsiType != null)
			{
				WriteXsiType(xsiType.Name, xsiType.Namespace);
			}
			_w.WriteRaw(value);
			_w.WriteEndElement();
		}
	}

	protected void WriteElementStringRaw(string localName, string? ns, byte[]? value, XmlQualifiedName? xsiType)
	{
		if (value != null)
		{
			_w.WriteStartElement(localName, ns);
			if (xsiType != null)
			{
				WriteXsiType(xsiType.Name, xsiType.Namespace);
			}
			XmlCustomFormatter.WriteArrayBase64(_w, value, 0, value.Length);
			_w.WriteEndElement();
		}
	}

	protected void WriteRpcResult(string name, string? ns)
	{
		if (_soap12)
		{
			WriteElementQualifiedName("result", "http://www.w3.org/2003/05/soap-rpc", new XmlQualifiedName(name, ns), null);
		}
	}

	protected void WriteElementQualifiedName(string localName, XmlQualifiedName? value)
	{
		WriteElementQualifiedName(localName, null, value, null);
	}

	protected void WriteElementQualifiedName(string localName, XmlQualifiedName? value, XmlQualifiedName? xsiType)
	{
		WriteElementQualifiedName(localName, null, value, xsiType);
	}

	protected void WriteElementQualifiedName(string localName, string? ns, XmlQualifiedName? value)
	{
		WriteElementQualifiedName(localName, ns, value, null);
	}

	protected void WriteElementQualifiedName(string localName, string? ns, XmlQualifiedName? value, XmlQualifiedName? xsiType)
	{
		if (!(value == null))
		{
			if (value.Namespace == null || value.Namespace.Length == 0)
			{
				WriteStartElement(localName, ns, null, writePrefixed: true);
				WriteAttribute("xmlns", "");
			}
			else
			{
				_w.WriteStartElement(localName, ns);
			}
			if (xsiType != null)
			{
				WriteXsiType(xsiType.Name, xsiType.Namespace);
			}
			_w.WriteString(FromXmlQualifiedName(value, ignoreEmpty: false));
			_w.WriteEndElement();
		}
	}

	protected void AddWriteCallback(Type type, string typeName, string? typeNs, XmlSerializationWriteCallback callback)
	{
		TypeEntry typeEntry = new TypeEntry();
		typeEntry.typeName = typeName;
		typeEntry.typeNs = typeNs;
		typeEntry.type = type;
		typeEntry.callback = callback;
		_typeEntries[type] = typeEntry;
	}

	[RequiresUnreferencedCode("calls GetArrayElementType")]
	private void WriteArray(string name, string ns, object o, Type type)
	{
		Type arrayElementType = TypeScope.GetArrayElementType(type, null);
		StringBuilder stringBuilder = new StringBuilder();
		if (!_soap12)
		{
			while ((arrayElementType.IsArray || typeof(IEnumerable).IsAssignableFrom(arrayElementType)) && GetPrimitiveTypeName(arrayElementType, throwIfUnknown: false) == null)
			{
				arrayElementType = TypeScope.GetArrayElementType(arrayElementType, null);
				stringBuilder.Append("[]");
			}
		}
		string text;
		string ns2;
		if (arrayElementType == typeof(object))
		{
			text = "anyType";
			ns2 = "http://www.w3.org/2001/XMLSchema";
		}
		else
		{
			TypeEntry typeEntry = GetTypeEntry(arrayElementType);
			if (typeEntry != null)
			{
				text = typeEntry.typeName;
				ns2 = typeEntry.typeNs;
			}
			else if (_soap12)
			{
				XmlQualifiedName primitiveTypeName = GetPrimitiveTypeName(arrayElementType, throwIfUnknown: false);
				if (primitiveTypeName != null)
				{
					text = primitiveTypeName.Name;
					ns2 = primitiveTypeName.Namespace;
				}
				else
				{
					Type baseType = arrayElementType.BaseType;
					while (baseType != null)
					{
						typeEntry = GetTypeEntry(baseType);
						if (typeEntry != null)
						{
							break;
						}
						baseType = baseType.BaseType;
					}
					if (typeEntry != null)
					{
						text = typeEntry.typeName;
						ns2 = typeEntry.typeNs;
					}
					else
					{
						text = "anyType";
						ns2 = "http://www.w3.org/2001/XMLSchema";
					}
				}
			}
			else
			{
				XmlQualifiedName primitiveTypeName2 = GetPrimitiveTypeName(arrayElementType);
				text = primitiveTypeName2.Name;
				ns2 = primitiveTypeName2.Namespace;
			}
		}
		if (stringBuilder.Length > 0)
		{
			text += stringBuilder.ToString();
		}
		if (_soap12 && name != null && name.Length > 0)
		{
			WriteStartElement(name, ns, null, writePrefixed: false);
		}
		else
		{
			WriteStartElement("Array", "http://schemas.xmlsoap.org/soap/encoding/", null, writePrefixed: true);
		}
		WriteId(o, addToReferencesList: false);
		if (type.IsArray)
		{
			Array array = (Array)o;
			int length = array.Length;
			if (_soap12)
			{
				_w.WriteAttributeString("itemType", "http://www.w3.org/2003/05/soap-encoding", GetQualifiedName(text, ns2));
				_w.WriteAttributeString("arraySize", "http://www.w3.org/2003/05/soap-encoding", length.ToString(CultureInfo.InvariantCulture));
			}
			else
			{
				_w.WriteAttributeString("arrayType", "http://schemas.xmlsoap.org/soap/encoding/", $"{GetQualifiedName(text, ns2)}[{length}]");
			}
			for (int i = 0; i < length; i++)
			{
				WritePotentiallyReferencingElement("Item", "", array.GetValue(i), arrayElementType, suppressReference: false, isNullable: true);
			}
		}
		else
		{
			int num = (typeof(ICollection).IsAssignableFrom(type) ? ((ICollection)o).Count : (-1));
			if (_soap12)
			{
				_w.WriteAttributeString("itemType", "http://www.w3.org/2003/05/soap-encoding", GetQualifiedName(text, ns2));
				if (num >= 0)
				{
					_w.WriteAttributeString("arraySize", "http://www.w3.org/2003/05/soap-encoding", num.ToString(CultureInfo.InvariantCulture));
				}
			}
			else
			{
				string text2 = ((num >= 0) ? ("[" + num + "]") : "[]");
				_w.WriteAttributeString("arrayType", "http://schemas.xmlsoap.org/soap/encoding/", GetQualifiedName(text, ns2) + text2);
			}
			IEnumerator enumerator = ((IEnumerable)o).GetEnumerator();
			if (enumerator != null)
			{
				while (enumerator.MoveNext())
				{
					WritePotentiallyReferencingElement("Item", "", enumerator.Current, arrayElementType, suppressReference: false, isNullable: true);
				}
			}
		}
		_w.WriteEndElement();
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	protected void WritePotentiallyReferencingElement(string? n, string? ns, object? o)
	{
		WritePotentiallyReferencingElement(n, ns, o, null, suppressReference: false, isNullable: false);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	protected void WritePotentiallyReferencingElement(string? n, string? ns, object? o, Type? ambientType)
	{
		WritePotentiallyReferencingElement(n, ns, o, ambientType, suppressReference: false, isNullable: false);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	protected void WritePotentiallyReferencingElement(string n, string? ns, object? o, Type? ambientType, bool suppressReference)
	{
		WritePotentiallyReferencingElement(n, ns, o, ambientType, suppressReference, isNullable: false);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	protected void WritePotentiallyReferencingElement(string? n, string? ns, object? o, Type? ambientType, bool suppressReference, bool isNullable)
	{
		if (o == null)
		{
			if (isNullable)
			{
				WriteNullTagEncoded(n, ns);
			}
			return;
		}
		Type type = o.GetType();
		if (Type.GetTypeCode(type) == TypeCode.Object && !(o is Guid) && type != typeof(XmlQualifiedName) && !(o is XmlNode[]) && type != typeof(byte[]))
		{
			if ((suppressReference || _soap12) && !IsIdDefined(o))
			{
				WriteReferencedElement(n, ns, o, ambientType);
			}
			else if (n == null)
			{
				TypeEntry typeEntry = GetTypeEntry(type);
				WriteReferencingElement(typeEntry.typeName, typeEntry.typeNs, o, isNullable);
			}
			else
			{
				WriteReferencingElement(n, ns, o, isNullable);
			}
			return;
		}
		bool flag = type != ambientType && !type.IsEnum;
		TypeEntry typeEntry2 = GetTypeEntry(type);
		if (typeEntry2 != null)
		{
			if (n == null)
			{
				WriteStartElement(typeEntry2.typeName, typeEntry2.typeNs, null, writePrefixed: true);
			}
			else
			{
				WriteStartElement(n, ns, null, writePrefixed: true);
			}
			if (flag)
			{
				WriteXsiType(typeEntry2.typeName, typeEntry2.typeNs);
			}
			typeEntry2.callback(o);
			_w.WriteEndElement();
		}
		else
		{
			WriteTypedPrimitive(n, ns, o, flag);
		}
	}

	[RequiresUnreferencedCode("calls WriteReferencedElement")]
	private void WriteReferencedElement(object o, Type ambientType)
	{
		WriteReferencedElement(null, null, o, ambientType);
	}

	[RequiresUnreferencedCode("calls WriteArray")]
	private void WriteReferencedElement(string name, string ns, object o, Type ambientType)
	{
		if (name == null)
		{
			name = string.Empty;
		}
		Type type = o.GetType();
		if (type.IsArray || typeof(IEnumerable).IsAssignableFrom(type))
		{
			WriteArray(name, ns, o, type);
			return;
		}
		TypeEntry typeEntry = GetTypeEntry(type);
		if (typeEntry == null)
		{
			throw CreateUnknownTypeException(type);
		}
		WriteStartElement((name.Length == 0) ? typeEntry.typeName : name, (ns == null) ? typeEntry.typeNs : ns, null, writePrefixed: true);
		WriteId(o, addToReferencesList: false);
		if (ambientType != type)
		{
			WriteXsiType(typeEntry.typeName, typeEntry.typeNs);
		}
		typeEntry.callback(o);
		_w.WriteEndElement();
	}

	[RequiresUnreferencedCode("calls InitCallbacks")]
	private TypeEntry GetTypeEntry(Type t)
	{
		if (_typeEntries == null)
		{
			_typeEntries = new Hashtable();
			InitCallbacks();
		}
		return (TypeEntry)_typeEntries[t];
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	protected abstract void InitCallbacks();

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	protected void WriteReferencedElements()
	{
		if (_referencesToWrite != null)
		{
			for (int i = 0; i < _referencesToWrite.Count; i++)
			{
				WriteReferencedElement(_referencesToWrite[i], null);
			}
		}
	}

	protected void TopLevelElement()
	{
		_objectsInUse = new Hashtable();
	}

	protected void WriteNamespaceDeclarations(XmlSerializerNamespaces? xmlns)
	{
		if (xmlns != null)
		{
			foreach (XmlQualifiedName namespace2 in xmlns.Namespaces)
			{
				string name = namespace2.Name;
				string @namespace = namespace2.Namespace;
				if (_namespaces != null && _namespaces.TryLookupNamespace(name, out var ns) && ns != null && ns != @namespace)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlDuplicateNs, name, @namespace));
				}
				string text = ((@namespace == null || @namespace.Length == 0) ? null : Writer.LookupPrefix(@namespace));
				if (text == null || text != name)
				{
					WriteAttribute("xmlns", name, null, @namespace);
				}
			}
		}
		_namespaces = null;
	}

	private string NextPrefix()
	{
		if (_usedPrefixes == null)
		{
			string aliasBase = _aliasBase;
			int num = ++_tempNamespacePrefix;
			return aliasBase + num;
		}
		while (_usedPrefixes.Contains(++_tempNamespacePrefix))
		{
		}
		return _aliasBase + _tempNamespacePrefix;
	}
}
