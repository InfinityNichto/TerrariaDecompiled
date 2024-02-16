using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;

namespace System.Xml;

internal static class XmlExceptionHelper
{
	[DoesNotReturn]
	private static void ThrowXmlException(XmlDictionaryReader reader, string res)
	{
		ThrowXmlException(reader, res, null);
	}

	[DoesNotReturn]
	public static void ThrowXmlException(XmlDictionaryReader reader, string res, string arg1)
	{
		ThrowXmlException(reader, res, arg1, null);
	}

	[DoesNotReturn]
	private static void ThrowXmlException(XmlDictionaryReader reader, string res, string arg1, string arg2)
	{
		ThrowXmlException(reader, res, arg1, arg2, null);
	}

	[DoesNotReturn]
	private static void ThrowXmlException(XmlDictionaryReader reader, string res, string arg1, string arg2, string arg3)
	{
		string text = System.SR.Format(res, arg1, arg2, arg3);
		if (reader is IXmlLineInfo xmlLineInfo && xmlLineInfo.HasLineInfo())
		{
			text = text + " " + System.SR.Format(System.SR.XmlLineInfo, xmlLineInfo.LineNumber, xmlLineInfo.LinePosition);
		}
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(text));
	}

	[DoesNotReturn]
	public static void ThrowXmlException(XmlDictionaryReader reader, XmlException exception)
	{
		string text = exception.Message;
		if (reader is IXmlLineInfo xmlLineInfo && xmlLineInfo.HasLineInfo())
		{
			text = text + " " + System.SR.Format(System.SR.XmlLineInfo, xmlLineInfo.LineNumber, xmlLineInfo.LinePosition);
		}
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(text));
	}

	private static string GetName(string prefix, string localName)
	{
		if (prefix.Length == 0)
		{
			return localName;
		}
		return prefix + ":" + localName;
	}

	private static string GetWhatWasFound(XmlDictionaryReader reader)
	{
		if (reader.EOF)
		{
			return System.SR.XmlFoundEndOfFile;
		}
		switch (reader.NodeType)
		{
		case XmlNodeType.Element:
			return System.SR.Format(System.SR.XmlFoundElement, GetName(reader.Prefix, reader.LocalName), reader.NamespaceURI);
		case XmlNodeType.EndElement:
			return System.SR.Format(System.SR.XmlFoundEndElement, GetName(reader.Prefix, reader.LocalName), reader.NamespaceURI);
		case XmlNodeType.Text:
		case XmlNodeType.Whitespace:
		case XmlNodeType.SignificantWhitespace:
			return System.SR.Format(System.SR.XmlFoundText, reader.Value);
		case XmlNodeType.Comment:
			return System.SR.Format(System.SR.XmlFoundComment, reader.Value);
		case XmlNodeType.CDATA:
			return System.SR.Format(System.SR.XmlFoundCData, reader.Value);
		default:
			return System.SR.Format(System.SR.XmlFoundNodeType, reader.NodeType);
		}
	}

	[DoesNotReturn]
	public static void ThrowStartElementExpected(XmlDictionaryReader reader)
	{
		ThrowXmlException(reader, System.SR.XmlStartElementExpected, GetWhatWasFound(reader));
	}

	[DoesNotReturn]
	public static void ThrowStartElementExpected(XmlDictionaryReader reader, string name)
	{
		ThrowXmlException(reader, System.SR.XmlStartElementNameExpected, name, GetWhatWasFound(reader));
	}

	[DoesNotReturn]
	public static void ThrowStartElementExpected(XmlDictionaryReader reader, string localName, string ns)
	{
		ThrowXmlException(reader, System.SR.XmlStartElementLocalNameNsExpected, localName, ns, GetWhatWasFound(reader));
	}

	[DoesNotReturn]
	public static void ThrowStartElementExpected(XmlDictionaryReader reader, XmlDictionaryString localName, XmlDictionaryString ns)
	{
		ThrowStartElementExpected(reader, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(ns));
	}

	[DoesNotReturn]
	public static void ThrowFullStartElementExpected(XmlDictionaryReader reader)
	{
		ThrowXmlException(reader, System.SR.XmlFullStartElementExpected, GetWhatWasFound(reader));
	}

	[DoesNotReturn]
	public static void ThrowFullStartElementExpected(XmlDictionaryReader reader, string name)
	{
		ThrowXmlException(reader, System.SR.XmlFullStartElementNameExpected, name, GetWhatWasFound(reader));
	}

	[DoesNotReturn]
	public static void ThrowFullStartElementExpected(XmlDictionaryReader reader, string localName, string ns)
	{
		ThrowXmlException(reader, System.SR.XmlFullStartElementLocalNameNsExpected, localName, ns, GetWhatWasFound(reader));
	}

	[DoesNotReturn]
	public static void ThrowFullStartElementExpected(XmlDictionaryReader reader, XmlDictionaryString localName, XmlDictionaryString ns)
	{
		ThrowFullStartElementExpected(reader, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(ns));
	}

	[DoesNotReturn]
	public static void ThrowEndElementExpected(XmlDictionaryReader reader, string localName, string ns)
	{
		ThrowXmlException(reader, System.SR.XmlEndElementExpected, localName, ns, GetWhatWasFound(reader));
	}

	[DoesNotReturn]
	public static void ThrowMaxArrayLengthExceeded(XmlDictionaryReader reader, int maxArrayLength)
	{
		ThrowXmlException(reader, System.SR.XmlMaxArrayLengthExceeded, maxArrayLength.ToString(NumberFormatInfo.CurrentInfo));
	}

	[DoesNotReturn]
	public static void ThrowMaxBytesPerReadExceeded(XmlDictionaryReader reader, int maxBytesPerRead)
	{
		ThrowXmlException(reader, System.SR.XmlMaxBytesPerReadExceeded, maxBytesPerRead.ToString(NumberFormatInfo.CurrentInfo));
	}

	[DoesNotReturn]
	public static void ThrowMaxDepthExceeded(XmlDictionaryReader reader, int maxDepth)
	{
		ThrowXmlException(reader, System.SR.XmlMaxDepthExceeded, maxDepth.ToString());
	}

	[DoesNotReturn]
	public static void ThrowMaxStringContentLengthExceeded(XmlDictionaryReader reader, int maxStringContentLength)
	{
		ThrowXmlException(reader, System.SR.XmlMaxStringContentLengthExceeded, maxStringContentLength.ToString(NumberFormatInfo.CurrentInfo));
	}

	[DoesNotReturn]
	public static void ThrowBase64DataExpected(XmlDictionaryReader reader)
	{
		ThrowXmlException(reader, System.SR.XmlBase64DataExpected, GetWhatWasFound(reader));
	}

	[DoesNotReturn]
	public static void ThrowUndefinedPrefix(XmlDictionaryReader reader, string prefix)
	{
		ThrowXmlException(reader, System.SR.XmlUndefinedPrefix, prefix);
	}

	[DoesNotReturn]
	public static void ThrowProcessingInstructionNotSupported(XmlDictionaryReader reader)
	{
		ThrowXmlException(reader, System.SR.XmlProcessingInstructionNotSupported);
	}

	[DoesNotReturn]
	public static void ThrowInvalidXml(XmlDictionaryReader reader, byte b)
	{
		ThrowXmlException(reader, System.SR.XmlInvalidXmlByte, b.ToString("X2", CultureInfo.InvariantCulture));
	}

	[DoesNotReturn]
	public static void ThrowUnexpectedEndOfFile(XmlDictionaryReader reader)
	{
		ThrowXmlException(reader, System.SR.XmlUnexpectedEndOfFile, ((XmlBaseReader)reader).GetOpenElements());
	}

	[DoesNotReturn]
	public static void ThrowUnexpectedEndElement(XmlDictionaryReader reader)
	{
		ThrowXmlException(reader, System.SR.XmlUnexpectedEndElement);
	}

	[DoesNotReturn]
	public static void ThrowTokenExpected(XmlDictionaryReader reader, string expected, char found)
	{
		ThrowXmlException(reader, System.SR.XmlTokenExpected, expected, found.ToString());
	}

	[DoesNotReturn]
	public static void ThrowTokenExpected(XmlDictionaryReader reader, string expected, string found)
	{
		ThrowXmlException(reader, System.SR.XmlTokenExpected, expected, found);
	}

	[DoesNotReturn]
	public static void ThrowInvalidCharRef(XmlDictionaryReader reader)
	{
		ThrowXmlException(reader, System.SR.XmlInvalidCharRef);
	}

	[DoesNotReturn]
	public static void ThrowTagMismatch(XmlDictionaryReader reader, string expectedPrefix, string expectedLocalName, string foundPrefix, string foundLocalName)
	{
		ThrowXmlException(reader, System.SR.XmlTagMismatch, GetName(expectedPrefix, expectedLocalName), GetName(foundPrefix, foundLocalName));
	}

	[DoesNotReturn]
	public static void ThrowDuplicateXmlnsAttribute(XmlDictionaryReader reader, string localName, string ns)
	{
		string text = ((localName.Length != 0) ? ("xmlns:" + localName) : "xmlns");
		ThrowXmlException(reader, System.SR.XmlDuplicateAttribute, text, text, ns);
	}

	[DoesNotReturn]
	public static void ThrowDuplicateAttribute(XmlDictionaryReader reader, string prefix1, string prefix2, string localName, string ns)
	{
		ThrowXmlException(reader, System.SR.XmlDuplicateAttribute, GetName(prefix1, localName), GetName(prefix2, localName), ns);
	}

	[DoesNotReturn]
	public static void ThrowInvalidBinaryFormat(XmlDictionaryReader reader)
	{
		ThrowXmlException(reader, System.SR.XmlInvalidFormat);
	}

	[DoesNotReturn]
	public static void ThrowInvalidRootData(XmlDictionaryReader reader)
	{
		ThrowXmlException(reader, System.SR.XmlInvalidRootData);
	}

	[DoesNotReturn]
	public static void ThrowMultipleRootElements(XmlDictionaryReader reader)
	{
		ThrowXmlException(reader, System.SR.XmlMultipleRootElements);
	}

	[DoesNotReturn]
	public static void ThrowDeclarationNotFirst(XmlDictionaryReader reader)
	{
		ThrowXmlException(reader, System.SR.XmlDeclNotFirst);
	}

	[DoesNotReturn]
	public static void ThrowConversionOverflow(XmlDictionaryReader reader, string value, string type)
	{
		ThrowXmlException(reader, System.SR.XmlConversionOverflow, value, type);
	}

	[DoesNotReturn]
	public static void ThrowXmlDictionaryStringIDOutOfRange(XmlDictionaryReader reader)
	{
		ThrowXmlException(reader, System.SR.XmlDictionaryStringIDRange, 0.ToString(NumberFormatInfo.CurrentInfo), 536870911.ToString(NumberFormatInfo.CurrentInfo));
	}

	[DoesNotReturn]
	public static void ThrowXmlDictionaryStringIDUndefinedStatic(XmlDictionaryReader reader, int key)
	{
		ThrowXmlException(reader, System.SR.XmlDictionaryStringIDUndefinedStatic, key.ToString(NumberFormatInfo.CurrentInfo));
	}

	[DoesNotReturn]
	public static void ThrowXmlDictionaryStringIDUndefinedSession(XmlDictionaryReader reader, int key)
	{
		ThrowXmlException(reader, System.SR.XmlDictionaryStringIDUndefinedSession, key.ToString(NumberFormatInfo.CurrentInfo));
	}

	[DoesNotReturn]
	public static void ThrowEmptyNamespace(XmlDictionaryReader reader)
	{
		ThrowXmlException(reader, System.SR.XmlEmptyNamespaceRequiresNullPrefix);
	}

	public static XmlException CreateConversionException(string type, Exception exception)
	{
		return new XmlException(System.SR.Format(System.SR.XmlInvalidConversionWithoutValue, type), exception);
	}

	public static XmlException CreateConversionException(string value, string type, Exception exception)
	{
		return new XmlException(System.SR.Format(System.SR.XmlInvalidConversion, value, type), exception);
	}

	public static XmlException CreateEncodingException(byte[] buffer, int offset, int count, Exception exception)
	{
		return CreateEncodingException(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false).GetString(buffer, offset, count), exception);
	}

	public static XmlException CreateEncodingException(string value, Exception exception)
	{
		return new XmlException(System.SR.Format(System.SR.XmlInvalidUTF8Bytes, value), exception);
	}
}
