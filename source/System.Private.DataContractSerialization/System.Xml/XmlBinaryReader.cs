using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;

namespace System.Xml;

internal sealed class XmlBinaryReader : XmlBaseReader, IXmlBinaryReaderInitializer
{
	private enum ArrayState
	{
		None,
		Element,
		Content
	}

	private bool _isTextWithEndElement;

	private bool _buffered;

	private ArrayState _arrayState;

	private int _arrayCount;

	private int _maxBytesPerRead;

	private XmlBinaryNodeType _arrayNodeType;

	public void SetInput(byte[] buffer, int offset, int count, IXmlDictionary dictionary, XmlDictionaryReaderQuotas quotas, XmlBinaryReaderSession session, OnXmlDictionaryReaderClose onClose)
	{
		if (buffer == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("buffer");
		}
		if (offset < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", System.SR.ValueMustBeNonNegative));
		}
		if (offset > buffer.Length)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", System.SR.Format(System.SR.OffsetExceedsBufferSize, buffer.Length)));
		}
		if (count < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", System.SR.ValueMustBeNonNegative));
		}
		if (count > buffer.Length - offset)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", System.SR.Format(System.SR.SizeExceedsRemainingBufferSpace, buffer.Length - offset)));
		}
		MoveToInitial(quotas, session, null);
		base.BufferReader.SetBuffer(buffer, offset, count, dictionary, session);
		_buffered = true;
	}

	public void SetInput(Stream stream, IXmlDictionary dictionary, XmlDictionaryReaderQuotas quotas, XmlBinaryReaderSession session, OnXmlDictionaryReaderClose onClose)
	{
		if (stream == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("stream");
		}
		MoveToInitial(quotas, session, null);
		base.BufferReader.SetBuffer(stream, dictionary, session);
		_buffered = false;
	}

	private void MoveToInitial(XmlDictionaryReaderQuotas quotas, XmlBinaryReaderSession session, OnXmlDictionaryReaderClose onClose)
	{
		MoveToInitial(quotas);
		_maxBytesPerRead = quotas.MaxBytesPerRead;
		_arrayState = ArrayState.None;
		_isTextWithEndElement = false;
	}

	public override void Close()
	{
		base.Close();
	}

	public override string ReadElementContentAsString()
	{
		if (base.Node.NodeType != XmlNodeType.Element)
		{
			MoveToStartElement();
		}
		if (!CanOptimizeReadElementContent())
		{
			return base.ReadElementContentAsString();
		}
		string text;
		switch (GetNodeType())
		{
		case XmlBinaryNodeType.Chars8TextWithEndElement:
			SkipNodeType();
			text = base.BufferReader.ReadUTF8String(ReadUInt8());
			ReadTextWithEndElement();
			break;
		case XmlBinaryNodeType.DictionaryTextWithEndElement:
			SkipNodeType();
			text = base.BufferReader.GetDictionaryString(ReadDictionaryKey()).Value;
			ReadTextWithEndElement();
			break;
		default:
			text = base.ReadElementContentAsString();
			break;
		}
		if (text.Length > Quotas.MaxStringContentLength)
		{
			XmlExceptionHelper.ThrowMaxStringContentLengthExceeded(this, Quotas.MaxStringContentLength);
		}
		return text;
	}

	public override bool ReadElementContentAsBoolean()
	{
		if (base.Node.NodeType != XmlNodeType.Element)
		{
			MoveToStartElement();
		}
		if (!CanOptimizeReadElementContent())
		{
			return base.ReadElementContentAsBoolean();
		}
		bool result;
		switch (GetNodeType())
		{
		case XmlBinaryNodeType.TrueTextWithEndElement:
			SkipNodeType();
			result = true;
			ReadTextWithEndElement();
			break;
		case XmlBinaryNodeType.FalseTextWithEndElement:
			SkipNodeType();
			result = false;
			ReadTextWithEndElement();
			break;
		case XmlBinaryNodeType.BoolTextWithEndElement:
			SkipNodeType();
			result = base.BufferReader.ReadUInt8() != 0;
			ReadTextWithEndElement();
			break;
		default:
			result = base.ReadElementContentAsBoolean();
			break;
		}
		return result;
	}

	public override int ReadElementContentAsInt()
	{
		if (base.Node.NodeType != XmlNodeType.Element)
		{
			MoveToStartElement();
		}
		if (!CanOptimizeReadElementContent())
		{
			return base.ReadElementContentAsInt();
		}
		int result;
		switch (GetNodeType())
		{
		case XmlBinaryNodeType.ZeroTextWithEndElement:
			SkipNodeType();
			result = 0;
			ReadTextWithEndElement();
			break;
		case XmlBinaryNodeType.OneTextWithEndElement:
			SkipNodeType();
			result = 1;
			ReadTextWithEndElement();
			break;
		case XmlBinaryNodeType.Int8TextWithEndElement:
			SkipNodeType();
			result = base.BufferReader.ReadInt8();
			ReadTextWithEndElement();
			break;
		case XmlBinaryNodeType.Int16TextWithEndElement:
			SkipNodeType();
			result = base.BufferReader.ReadInt16();
			ReadTextWithEndElement();
			break;
		case XmlBinaryNodeType.Int32TextWithEndElement:
			SkipNodeType();
			result = base.BufferReader.ReadInt32();
			ReadTextWithEndElement();
			break;
		default:
			result = base.ReadElementContentAsInt();
			break;
		}
		return result;
	}

	private bool CanOptimizeReadElementContent()
	{
		if (_arrayState == ArrayState.None)
		{
			return !base.Signing;
		}
		return false;
	}

	public override float ReadElementContentAsFloat()
	{
		if (base.Node.NodeType != XmlNodeType.Element)
		{
			MoveToStartElement();
		}
		if (CanOptimizeReadElementContent() && GetNodeType() == XmlBinaryNodeType.FloatTextWithEndElement)
		{
			SkipNodeType();
			float result = base.BufferReader.ReadSingle();
			ReadTextWithEndElement();
			return result;
		}
		return base.ReadElementContentAsFloat();
	}

	public override double ReadElementContentAsDouble()
	{
		if (base.Node.NodeType != XmlNodeType.Element)
		{
			MoveToStartElement();
		}
		if (CanOptimizeReadElementContent() && GetNodeType() == XmlBinaryNodeType.DoubleTextWithEndElement)
		{
			SkipNodeType();
			double result = base.BufferReader.ReadDouble();
			ReadTextWithEndElement();
			return result;
		}
		return base.ReadElementContentAsDouble();
	}

	public override decimal ReadElementContentAsDecimal()
	{
		if (base.Node.NodeType != XmlNodeType.Element)
		{
			MoveToStartElement();
		}
		if (CanOptimizeReadElementContent() && GetNodeType() == XmlBinaryNodeType.DecimalTextWithEndElement)
		{
			SkipNodeType();
			decimal result = base.BufferReader.ReadDecimal();
			ReadTextWithEndElement();
			return result;
		}
		return base.ReadElementContentAsDecimal();
	}

	public override DateTime ReadElementContentAsDateTime()
	{
		if (base.Node.NodeType != XmlNodeType.Element)
		{
			MoveToStartElement();
		}
		if (CanOptimizeReadElementContent() && GetNodeType() == XmlBinaryNodeType.DateTimeTextWithEndElement)
		{
			SkipNodeType();
			DateTime result = base.BufferReader.ReadDateTime();
			ReadTextWithEndElement();
			return result;
		}
		return base.ReadElementContentAsDateTime();
	}

	public override TimeSpan ReadElementContentAsTimeSpan()
	{
		if (base.Node.NodeType != XmlNodeType.Element)
		{
			MoveToStartElement();
		}
		if (CanOptimizeReadElementContent() && GetNodeType() == XmlBinaryNodeType.TimeSpanTextWithEndElement)
		{
			SkipNodeType();
			TimeSpan result = base.BufferReader.ReadTimeSpan();
			ReadTextWithEndElement();
			return result;
		}
		return base.ReadElementContentAsTimeSpan();
	}

	public override Guid ReadElementContentAsGuid()
	{
		if (base.Node.NodeType != XmlNodeType.Element)
		{
			MoveToStartElement();
		}
		if (CanOptimizeReadElementContent() && GetNodeType() == XmlBinaryNodeType.GuidTextWithEndElement)
		{
			SkipNodeType();
			Guid result = base.BufferReader.ReadGuid();
			ReadTextWithEndElement();
			return result;
		}
		return base.ReadElementContentAsGuid();
	}

	public override UniqueId ReadElementContentAsUniqueId()
	{
		if (base.Node.NodeType != XmlNodeType.Element)
		{
			MoveToStartElement();
		}
		if (CanOptimizeReadElementContent() && GetNodeType() == XmlBinaryNodeType.UniqueIdTextWithEndElement)
		{
			SkipNodeType();
			UniqueId result = base.BufferReader.ReadUniqueId();
			ReadTextWithEndElement();
			return result;
		}
		return base.ReadElementContentAsUniqueId();
	}

	public override bool TryGetBase64ContentLength(out int length)
	{
		length = 0;
		if (!_buffered)
		{
			return false;
		}
		if (_arrayState != 0)
		{
			return false;
		}
		if (!base.Node.Value.TryGetByteArrayLength(out var length2))
		{
			return false;
		}
		int offset = base.BufferReader.Offset;
		try
		{
			bool flag = false;
			while (!flag && !base.BufferReader.EndOfFile)
			{
				XmlBinaryNodeType nodeType = GetNodeType();
				SkipNodeType();
				int num;
				switch (nodeType)
				{
				case XmlBinaryNodeType.Bytes8TextWithEndElement:
					num = base.BufferReader.ReadUInt8();
					flag = true;
					break;
				case XmlBinaryNodeType.Bytes16TextWithEndElement:
					num = base.BufferReader.ReadUInt16();
					flag = true;
					break;
				case XmlBinaryNodeType.Bytes32TextWithEndElement:
					num = base.BufferReader.ReadUInt31();
					flag = true;
					break;
				case XmlBinaryNodeType.EndElement:
					num = 0;
					flag = true;
					break;
				case XmlBinaryNodeType.Bytes8Text:
					num = base.BufferReader.ReadUInt8();
					break;
				case XmlBinaryNodeType.Bytes16Text:
					num = base.BufferReader.ReadUInt16();
					break;
				case XmlBinaryNodeType.Bytes32Text:
					num = base.BufferReader.ReadUInt31();
					break;
				default:
					return false;
				}
				base.BufferReader.Advance(num);
				if (length2 > int.MaxValue - num)
				{
					return false;
				}
				length2 += num;
			}
			length = length2;
			return true;
		}
		finally
		{
			base.BufferReader.Offset = offset;
		}
	}

	private void ReadTextWithEndElement()
	{
		ExitScope();
		ReadNode();
	}

	private XmlAtomicTextNode MoveToAtomicTextWithEndElement()
	{
		_isTextWithEndElement = true;
		return MoveToAtomicText();
	}

	public override bool Read()
	{
		if (base.Node.ReadState == ReadState.Closed)
		{
			return false;
		}
		SignNode();
		if (_isTextWithEndElement)
		{
			_isTextWithEndElement = false;
			MoveToEndElement();
			return true;
		}
		if (_arrayState == ArrayState.Content)
		{
			if (_arrayCount != 0)
			{
				MoveToArrayElement();
				return true;
			}
			_arrayState = ArrayState.None;
		}
		if (base.Node.ExitScope)
		{
			ExitScope();
		}
		return ReadNode();
	}

	private bool ReadNode()
	{
		if (!_buffered)
		{
			base.BufferReader.SetWindow(base.ElementNode.BufferOffset, _maxBytesPerRead);
		}
		if (base.BufferReader.EndOfFile)
		{
			MoveToEndOfFile();
			return false;
		}
		XmlBinaryNodeType xmlBinaryNodeType;
		if (_arrayState == ArrayState.None)
		{
			xmlBinaryNodeType = GetNodeType();
			SkipNodeType();
		}
		else
		{
			xmlBinaryNodeType = _arrayNodeType;
			_arrayCount--;
			_arrayState = ArrayState.Content;
		}
		switch (xmlBinaryNodeType)
		{
		case XmlBinaryNodeType.MinElement:
		{
			XmlElementNode xmlElementNode = EnterScope();
			xmlElementNode.Prefix.SetValue(PrefixHandleType.Empty);
			ReadName(xmlElementNode.LocalName);
			ReadAttributes();
			xmlElementNode.Namespace = LookupNamespace(PrefixHandleType.Empty);
			xmlElementNode.BufferOffset = base.BufferReader.Offset;
			return true;
		}
		case XmlBinaryNodeType.Element:
		{
			XmlElementNode xmlElementNode = EnterScope();
			ReadName(xmlElementNode.Prefix);
			ReadName(xmlElementNode.LocalName);
			ReadAttributes();
			xmlElementNode.Namespace = LookupNamespace(xmlElementNode.Prefix);
			xmlElementNode.BufferOffset = base.BufferReader.Offset;
			return true;
		}
		case XmlBinaryNodeType.ShortDictionaryElement:
		{
			XmlElementNode xmlElementNode = EnterScope();
			xmlElementNode.Prefix.SetValue(PrefixHandleType.Empty);
			ReadDictionaryName(xmlElementNode.LocalName);
			ReadAttributes();
			xmlElementNode.Namespace = LookupNamespace(PrefixHandleType.Empty);
			xmlElementNode.BufferOffset = base.BufferReader.Offset;
			return true;
		}
		case XmlBinaryNodeType.DictionaryElement:
		{
			XmlElementNode xmlElementNode = EnterScope();
			ReadName(xmlElementNode.Prefix);
			ReadDictionaryName(xmlElementNode.LocalName);
			ReadAttributes();
			xmlElementNode.Namespace = LookupNamespace(xmlElementNode.Prefix);
			xmlElementNode.BufferOffset = base.BufferReader.Offset;
			return true;
		}
		case XmlBinaryNodeType.PrefixElementA:
		case XmlBinaryNodeType.PrefixElementB:
		case XmlBinaryNodeType.PrefixElementC:
		case XmlBinaryNodeType.PrefixElementD:
		case XmlBinaryNodeType.PrefixElementE:
		case XmlBinaryNodeType.PrefixElementF:
		case XmlBinaryNodeType.PrefixElementG:
		case XmlBinaryNodeType.PrefixElementH:
		case XmlBinaryNodeType.PrefixElementI:
		case XmlBinaryNodeType.PrefixElementJ:
		case XmlBinaryNodeType.PrefixElementK:
		case XmlBinaryNodeType.PrefixElementL:
		case XmlBinaryNodeType.PrefixElementM:
		case XmlBinaryNodeType.PrefixElementN:
		case XmlBinaryNodeType.PrefixElementO:
		case XmlBinaryNodeType.PrefixElementP:
		case XmlBinaryNodeType.PrefixElementQ:
		case XmlBinaryNodeType.PrefixElementR:
		case XmlBinaryNodeType.PrefixElementS:
		case XmlBinaryNodeType.PrefixElementT:
		case XmlBinaryNodeType.PrefixElementU:
		case XmlBinaryNodeType.PrefixElementV:
		case XmlBinaryNodeType.PrefixElementW:
		case XmlBinaryNodeType.PrefixElementX:
		case XmlBinaryNodeType.PrefixElementY:
		case XmlBinaryNodeType.PrefixElementZ:
		{
			XmlElementNode xmlElementNode = EnterScope();
			PrefixHandleType alphaPrefix = PrefixHandle.GetAlphaPrefix((int)(xmlBinaryNodeType - 94));
			xmlElementNode.Prefix.SetValue(alphaPrefix);
			ReadName(xmlElementNode.LocalName);
			ReadAttributes();
			xmlElementNode.Namespace = LookupNamespace(alphaPrefix);
			xmlElementNode.BufferOffset = base.BufferReader.Offset;
			return true;
		}
		case XmlBinaryNodeType.PrefixDictionaryElementA:
		case XmlBinaryNodeType.PrefixDictionaryElementB:
		case XmlBinaryNodeType.PrefixDictionaryElementC:
		case XmlBinaryNodeType.PrefixDictionaryElementD:
		case XmlBinaryNodeType.PrefixDictionaryElementE:
		case XmlBinaryNodeType.PrefixDictionaryElementF:
		case XmlBinaryNodeType.PrefixDictionaryElementG:
		case XmlBinaryNodeType.PrefixDictionaryElementH:
		case XmlBinaryNodeType.PrefixDictionaryElementI:
		case XmlBinaryNodeType.PrefixDictionaryElementJ:
		case XmlBinaryNodeType.PrefixDictionaryElementK:
		case XmlBinaryNodeType.PrefixDictionaryElementL:
		case XmlBinaryNodeType.PrefixDictionaryElementM:
		case XmlBinaryNodeType.PrefixDictionaryElementN:
		case XmlBinaryNodeType.PrefixDictionaryElementO:
		case XmlBinaryNodeType.PrefixDictionaryElementP:
		case XmlBinaryNodeType.PrefixDictionaryElementQ:
		case XmlBinaryNodeType.PrefixDictionaryElementR:
		case XmlBinaryNodeType.PrefixDictionaryElementS:
		case XmlBinaryNodeType.PrefixDictionaryElementT:
		case XmlBinaryNodeType.PrefixDictionaryElementU:
		case XmlBinaryNodeType.PrefixDictionaryElementV:
		case XmlBinaryNodeType.PrefixDictionaryElementW:
		case XmlBinaryNodeType.PrefixDictionaryElementX:
		case XmlBinaryNodeType.PrefixDictionaryElementY:
		case XmlBinaryNodeType.PrefixDictionaryElementZ:
		{
			XmlElementNode xmlElementNode = EnterScope();
			PrefixHandleType alphaPrefix = PrefixHandle.GetAlphaPrefix((int)(xmlBinaryNodeType - 68));
			xmlElementNode.Prefix.SetValue(alphaPrefix);
			ReadDictionaryName(xmlElementNode.LocalName);
			ReadAttributes();
			xmlElementNode.Namespace = LookupNamespace(alphaPrefix);
			xmlElementNode.BufferOffset = base.BufferReader.Offset;
			return true;
		}
		case XmlBinaryNodeType.EndElement:
			MoveToEndElement();
			return true;
		case XmlBinaryNodeType.Comment:
			ReadName(MoveToComment().Value);
			return true;
		case XmlBinaryNodeType.EmptyTextWithEndElement:
			MoveToAtomicTextWithEndElement().Value.SetValue(ValueHandleType.Empty);
			if (base.OutsideRootElement)
			{
				VerifyWhitespace();
			}
			return true;
		case XmlBinaryNodeType.ZeroTextWithEndElement:
			MoveToAtomicTextWithEndElement().Value.SetValue(ValueHandleType.Zero);
			if (base.OutsideRootElement)
			{
				VerifyWhitespace();
			}
			return true;
		case XmlBinaryNodeType.OneTextWithEndElement:
			MoveToAtomicTextWithEndElement().Value.SetValue(ValueHandleType.One);
			if (base.OutsideRootElement)
			{
				VerifyWhitespace();
			}
			return true;
		case XmlBinaryNodeType.TrueTextWithEndElement:
			MoveToAtomicTextWithEndElement().Value.SetValue(ValueHandleType.True);
			if (base.OutsideRootElement)
			{
				VerifyWhitespace();
			}
			return true;
		case XmlBinaryNodeType.FalseTextWithEndElement:
			MoveToAtomicTextWithEndElement().Value.SetValue(ValueHandleType.False);
			if (base.OutsideRootElement)
			{
				VerifyWhitespace();
			}
			return true;
		case XmlBinaryNodeType.BoolTextWithEndElement:
			MoveToAtomicTextWithEndElement().Value.SetValue((ReadUInt8() != 0) ? ValueHandleType.True : ValueHandleType.False);
			if (base.OutsideRootElement)
			{
				VerifyWhitespace();
			}
			return true;
		case XmlBinaryNodeType.Chars8TextWithEndElement:
			if (_buffered)
			{
				ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.UTF8, ReadUInt8());
			}
			else
			{
				ReadPartialUTF8Text(withEndElement: true, ReadUInt8());
			}
			return true;
		case XmlBinaryNodeType.Chars8Text:
			if (_buffered)
			{
				ReadText(MoveToComplexText(), ValueHandleType.UTF8, ReadUInt8());
			}
			else
			{
				ReadPartialUTF8Text(withEndElement: false, ReadUInt8());
			}
			return true;
		case XmlBinaryNodeType.Chars16TextWithEndElement:
			if (_buffered)
			{
				ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.UTF8, ReadUInt16());
			}
			else
			{
				ReadPartialUTF8Text(withEndElement: true, ReadUInt16());
			}
			return true;
		case XmlBinaryNodeType.Chars16Text:
			if (_buffered)
			{
				ReadText(MoveToComplexText(), ValueHandleType.UTF8, ReadUInt16());
			}
			else
			{
				ReadPartialUTF8Text(withEndElement: false, ReadUInt16());
			}
			return true;
		case XmlBinaryNodeType.Chars32TextWithEndElement:
			if (_buffered)
			{
				ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.UTF8, ReadUInt31());
			}
			else
			{
				ReadPartialUTF8Text(withEndElement: true, ReadUInt31());
			}
			return true;
		case XmlBinaryNodeType.Chars32Text:
			if (_buffered)
			{
				ReadText(MoveToComplexText(), ValueHandleType.UTF8, ReadUInt31());
			}
			else
			{
				ReadPartialUTF8Text(withEndElement: false, ReadUInt31());
			}
			return true;
		case XmlBinaryNodeType.UnicodeChars8TextWithEndElement:
			ReadUnicodeText(withEndElement: true, ReadUInt8());
			return true;
		case XmlBinaryNodeType.UnicodeChars8Text:
			ReadUnicodeText(withEndElement: false, ReadUInt8());
			return true;
		case XmlBinaryNodeType.UnicodeChars16TextWithEndElement:
			ReadUnicodeText(withEndElement: true, ReadUInt16());
			return true;
		case XmlBinaryNodeType.UnicodeChars16Text:
			ReadUnicodeText(withEndElement: false, ReadUInt16());
			return true;
		case XmlBinaryNodeType.UnicodeChars32TextWithEndElement:
			ReadUnicodeText(withEndElement: true, ReadUInt31());
			return true;
		case XmlBinaryNodeType.UnicodeChars32Text:
			ReadUnicodeText(withEndElement: false, ReadUInt31());
			return true;
		case XmlBinaryNodeType.Bytes8TextWithEndElement:
			if (_buffered)
			{
				ReadBinaryText(MoveToAtomicTextWithEndElement(), ReadUInt8());
			}
			else
			{
				ReadPartialBinaryText(withEndElement: true, ReadUInt8());
			}
			return true;
		case XmlBinaryNodeType.Bytes8Text:
			if (_buffered)
			{
				ReadBinaryText(MoveToComplexText(), ReadUInt8());
			}
			else
			{
				ReadPartialBinaryText(withEndElement: false, ReadUInt8());
			}
			return true;
		case XmlBinaryNodeType.Bytes16TextWithEndElement:
			if (_buffered)
			{
				ReadBinaryText(MoveToAtomicTextWithEndElement(), ReadUInt16());
			}
			else
			{
				ReadPartialBinaryText(withEndElement: true, ReadUInt16());
			}
			return true;
		case XmlBinaryNodeType.Bytes16Text:
			if (_buffered)
			{
				ReadBinaryText(MoveToComplexText(), ReadUInt16());
			}
			else
			{
				ReadPartialBinaryText(withEndElement: false, ReadUInt16());
			}
			return true;
		case XmlBinaryNodeType.Bytes32TextWithEndElement:
			if (_buffered)
			{
				ReadBinaryText(MoveToAtomicTextWithEndElement(), ReadUInt31());
			}
			else
			{
				ReadPartialBinaryText(withEndElement: true, ReadUInt31());
			}
			return true;
		case XmlBinaryNodeType.Bytes32Text:
			if (_buffered)
			{
				ReadBinaryText(MoveToComplexText(), ReadUInt31());
			}
			else
			{
				ReadPartialBinaryText(withEndElement: false, ReadUInt31());
			}
			return true;
		case XmlBinaryNodeType.DictionaryTextWithEndElement:
			MoveToAtomicTextWithEndElement().Value.SetDictionaryValue(ReadDictionaryKey());
			return true;
		case XmlBinaryNodeType.UniqueIdTextWithEndElement:
			ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.UniqueId, 16);
			return true;
		case XmlBinaryNodeType.GuidTextWithEndElement:
			ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.Guid, 16);
			return true;
		case XmlBinaryNodeType.DecimalTextWithEndElement:
			ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.Decimal, 16);
			return true;
		case XmlBinaryNodeType.Int8TextWithEndElement:
			ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.Int8, 1);
			return true;
		case XmlBinaryNodeType.Int16TextWithEndElement:
			ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.Int16, 2);
			return true;
		case XmlBinaryNodeType.Int32TextWithEndElement:
			ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.Int32, 4);
			return true;
		case XmlBinaryNodeType.Int64TextWithEndElement:
			ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.Int64, 8);
			return true;
		case XmlBinaryNodeType.UInt64TextWithEndElement:
			ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.UInt64, 8);
			return true;
		case XmlBinaryNodeType.FloatTextWithEndElement:
			ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.Single, 4);
			return true;
		case XmlBinaryNodeType.DoubleTextWithEndElement:
			ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.Double, 8);
			return true;
		case XmlBinaryNodeType.TimeSpanTextWithEndElement:
			ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.TimeSpan, 8);
			return true;
		case XmlBinaryNodeType.DateTimeTextWithEndElement:
			ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.DateTime, 8);
			return true;
		case XmlBinaryNodeType.QNameDictionaryTextWithEndElement:
			base.BufferReader.ReadQName(MoveToAtomicTextWithEndElement().Value);
			return true;
		case XmlBinaryNodeType.Array:
			ReadArray();
			return true;
		default:
			base.BufferReader.ReadValue(xmlBinaryNodeType, MoveToComplexText().Value);
			return true;
		}
	}

	private void VerifyWhitespace()
	{
		if (!base.Node.Value.IsWhitespace())
		{
			XmlExceptionHelper.ThrowInvalidBinaryFormat(this);
		}
	}

	private void ReadAttributes()
	{
		XmlBinaryNodeType nodeType = GetNodeType();
		if (nodeType >= XmlBinaryNodeType.MinAttribute && nodeType <= XmlBinaryNodeType.PrefixAttributeZ)
		{
			ReadAttributes2();
		}
	}

	private void ReadAttributes2()
	{
		int num = 0;
		if (_buffered)
		{
			num = base.BufferReader.Offset;
		}
		while (true)
		{
			XmlBinaryNodeType nodeType = GetNodeType();
			switch (nodeType)
			{
			case XmlBinaryNodeType.MinAttribute:
			{
				SkipNodeType();
				XmlAttributeNode xmlAttributeNode = AddAttribute();
				xmlAttributeNode.Prefix.SetValue(PrefixHandleType.Empty);
				ReadName(xmlAttributeNode.LocalName);
				ReadAttributeText(xmlAttributeNode.AttributeText);
				break;
			}
			case XmlBinaryNodeType.Attribute:
			{
				SkipNodeType();
				XmlAttributeNode xmlAttributeNode = AddAttribute();
				ReadName(xmlAttributeNode.Prefix);
				ReadName(xmlAttributeNode.LocalName);
				ReadAttributeText(xmlAttributeNode.AttributeText);
				FixXmlAttribute(xmlAttributeNode);
				break;
			}
			case XmlBinaryNodeType.ShortDictionaryAttribute:
			{
				SkipNodeType();
				XmlAttributeNode xmlAttributeNode = AddAttribute();
				xmlAttributeNode.Prefix.SetValue(PrefixHandleType.Empty);
				ReadDictionaryName(xmlAttributeNode.LocalName);
				ReadAttributeText(xmlAttributeNode.AttributeText);
				break;
			}
			case XmlBinaryNodeType.DictionaryAttribute:
			{
				SkipNodeType();
				XmlAttributeNode xmlAttributeNode = AddAttribute();
				ReadName(xmlAttributeNode.Prefix);
				ReadDictionaryName(xmlAttributeNode.LocalName);
				ReadAttributeText(xmlAttributeNode.AttributeText);
				break;
			}
			case XmlBinaryNodeType.XmlnsAttribute:
			{
				SkipNodeType();
				Namespace @namespace = AddNamespace();
				ReadName(@namespace.Prefix);
				ReadName(@namespace.Uri);
				XmlAttributeNode xmlAttributeNode = AddXmlnsAttribute(@namespace);
				break;
			}
			case XmlBinaryNodeType.ShortXmlnsAttribute:
			{
				SkipNodeType();
				Namespace @namespace = AddNamespace();
				@namespace.Prefix.SetValue(PrefixHandleType.Empty);
				ReadName(@namespace.Uri);
				XmlAttributeNode xmlAttributeNode = AddXmlnsAttribute(@namespace);
				break;
			}
			case XmlBinaryNodeType.ShortDictionaryXmlnsAttribute:
			{
				SkipNodeType();
				Namespace @namespace = AddNamespace();
				@namespace.Prefix.SetValue(PrefixHandleType.Empty);
				ReadDictionaryName(@namespace.Uri);
				XmlAttributeNode xmlAttributeNode = AddXmlnsAttribute(@namespace);
				break;
			}
			case XmlBinaryNodeType.DictionaryXmlnsAttribute:
			{
				SkipNodeType();
				Namespace @namespace = AddNamespace();
				ReadName(@namespace.Prefix);
				ReadDictionaryName(@namespace.Uri);
				XmlAttributeNode xmlAttributeNode = AddXmlnsAttribute(@namespace);
				break;
			}
			case XmlBinaryNodeType.PrefixDictionaryAttributeA:
			case XmlBinaryNodeType.PrefixDictionaryAttributeB:
			case XmlBinaryNodeType.PrefixDictionaryAttributeC:
			case XmlBinaryNodeType.PrefixDictionaryAttributeD:
			case XmlBinaryNodeType.PrefixDictionaryAttributeE:
			case XmlBinaryNodeType.PrefixDictionaryAttributeF:
			case XmlBinaryNodeType.PrefixDictionaryAttributeG:
			case XmlBinaryNodeType.PrefixDictionaryAttributeH:
			case XmlBinaryNodeType.PrefixDictionaryAttributeI:
			case XmlBinaryNodeType.PrefixDictionaryAttributeJ:
			case XmlBinaryNodeType.PrefixDictionaryAttributeK:
			case XmlBinaryNodeType.PrefixDictionaryAttributeL:
			case XmlBinaryNodeType.PrefixDictionaryAttributeM:
			case XmlBinaryNodeType.PrefixDictionaryAttributeN:
			case XmlBinaryNodeType.PrefixDictionaryAttributeO:
			case XmlBinaryNodeType.PrefixDictionaryAttributeP:
			case XmlBinaryNodeType.PrefixDictionaryAttributeQ:
			case XmlBinaryNodeType.PrefixDictionaryAttributeR:
			case XmlBinaryNodeType.PrefixDictionaryAttributeS:
			case XmlBinaryNodeType.PrefixDictionaryAttributeT:
			case XmlBinaryNodeType.PrefixDictionaryAttributeU:
			case XmlBinaryNodeType.PrefixDictionaryAttributeV:
			case XmlBinaryNodeType.PrefixDictionaryAttributeW:
			case XmlBinaryNodeType.PrefixDictionaryAttributeX:
			case XmlBinaryNodeType.PrefixDictionaryAttributeY:
			case XmlBinaryNodeType.PrefixDictionaryAttributeZ:
			{
				SkipNodeType();
				XmlAttributeNode xmlAttributeNode = AddAttribute();
				PrefixHandleType alphaPrefix = PrefixHandle.GetAlphaPrefix((int)(nodeType - 12));
				xmlAttributeNode.Prefix.SetValue(alphaPrefix);
				ReadDictionaryName(xmlAttributeNode.LocalName);
				ReadAttributeText(xmlAttributeNode.AttributeText);
				break;
			}
			case XmlBinaryNodeType.PrefixAttributeA:
			case XmlBinaryNodeType.PrefixAttributeB:
			case XmlBinaryNodeType.PrefixAttributeC:
			case XmlBinaryNodeType.PrefixAttributeD:
			case XmlBinaryNodeType.PrefixAttributeE:
			case XmlBinaryNodeType.PrefixAttributeF:
			case XmlBinaryNodeType.PrefixAttributeG:
			case XmlBinaryNodeType.PrefixAttributeH:
			case XmlBinaryNodeType.PrefixAttributeI:
			case XmlBinaryNodeType.PrefixAttributeJ:
			case XmlBinaryNodeType.PrefixAttributeK:
			case XmlBinaryNodeType.PrefixAttributeL:
			case XmlBinaryNodeType.PrefixAttributeM:
			case XmlBinaryNodeType.PrefixAttributeN:
			case XmlBinaryNodeType.PrefixAttributeO:
			case XmlBinaryNodeType.PrefixAttributeP:
			case XmlBinaryNodeType.PrefixAttributeQ:
			case XmlBinaryNodeType.PrefixAttributeR:
			case XmlBinaryNodeType.PrefixAttributeS:
			case XmlBinaryNodeType.PrefixAttributeT:
			case XmlBinaryNodeType.PrefixAttributeU:
			case XmlBinaryNodeType.PrefixAttributeV:
			case XmlBinaryNodeType.PrefixAttributeW:
			case XmlBinaryNodeType.PrefixAttributeX:
			case XmlBinaryNodeType.PrefixAttributeY:
			case XmlBinaryNodeType.PrefixAttributeZ:
			{
				SkipNodeType();
				XmlAttributeNode xmlAttributeNode = AddAttribute();
				PrefixHandleType alphaPrefix = PrefixHandle.GetAlphaPrefix((int)(nodeType - 38));
				xmlAttributeNode.Prefix.SetValue(alphaPrefix);
				ReadName(xmlAttributeNode.LocalName);
				ReadAttributeText(xmlAttributeNode.AttributeText);
				break;
			}
			default:
				ProcessAttributes();
				return;
			}
		}
	}

	private void ReadText(XmlTextNode textNode, ValueHandleType type, int length)
	{
		int offset = base.BufferReader.ReadBytes(length);
		textNode.Value.SetValue(type, offset, length);
		if (base.OutsideRootElement)
		{
			VerifyWhitespace();
		}
	}

	private void ReadBinaryText(XmlTextNode textNode, int length)
	{
		ReadText(textNode, ValueHandleType.Base64, length);
	}

	private void ReadPartialUTF8Text(bool withEndElement, int length)
	{
		int num = Math.Max(_maxBytesPerRead - 5, 0);
		if (length <= num)
		{
			if (withEndElement)
			{
				ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.UTF8, length);
			}
			else
			{
				ReadText(MoveToComplexText(), ValueHandleType.UTF8, length);
			}
			return;
		}
		int num2 = Math.Max(num - 5, 0);
		int num3 = base.BufferReader.ReadBytes(num2);
		int num4;
		for (num4 = num3 + num2 - 1; num4 >= num3; num4--)
		{
			byte @byte = base.BufferReader.GetByte(num4);
			if ((@byte & 0x80) == 0 || (@byte & 0xC0) == 192)
			{
				break;
			}
		}
		int num5 = num3 + num2 - num4;
		base.BufferReader.Offset = base.BufferReader.Offset - num5;
		num2 -= num5;
		MoveToComplexText().Value.SetValue(ValueHandleType.UTF8, num3, num2);
		if (base.OutsideRootElement)
		{
			VerifyWhitespace();
		}
		XmlBinaryNodeType nodeType = (withEndElement ? XmlBinaryNodeType.Chars32TextWithEndElement : XmlBinaryNodeType.Chars32Text);
		InsertNode(nodeType, length - num2);
	}

	private void ReadUnicodeText(bool withEndElement, int length)
	{
		if (((uint)length & (true ? 1u : 0u)) != 0)
		{
			XmlExceptionHelper.ThrowInvalidBinaryFormat(this);
		}
		if (_buffered)
		{
			if (withEndElement)
			{
				ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.Unicode, length);
			}
			else
			{
				ReadText(MoveToComplexText(), ValueHandleType.Unicode, length);
			}
		}
		else
		{
			ReadPartialUnicodeText(withEndElement, length);
		}
	}

	private void ReadPartialUnicodeText(bool withEndElement, int length)
	{
		int num = Math.Max(_maxBytesPerRead - 5, 0);
		if (length <= num)
		{
			if (withEndElement)
			{
				ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.Unicode, length);
			}
			else
			{
				ReadText(MoveToComplexText(), ValueHandleType.Unicode, length);
			}
			return;
		}
		int num2 = Math.Max(num - 5, 0);
		if (((uint)num2 & (true ? 1u : 0u)) != 0)
		{
			num2--;
		}
		int num3 = base.BufferReader.ReadBytes(num2);
		int num4 = 0;
		char c = (char)base.BufferReader.GetInt16(num3 + num2 - 2);
		if (c >= '\ud800' && c < '\udc00')
		{
			num4 = 2;
		}
		base.BufferReader.Offset = base.BufferReader.Offset - num4;
		num2 -= num4;
		MoveToComplexText().Value.SetValue(ValueHandleType.Unicode, num3, num2);
		if (base.OutsideRootElement)
		{
			VerifyWhitespace();
		}
		XmlBinaryNodeType nodeType = (withEndElement ? XmlBinaryNodeType.UnicodeChars32TextWithEndElement : XmlBinaryNodeType.UnicodeChars32Text);
		InsertNode(nodeType, length - num2);
	}

	private void ReadPartialBinaryText(bool withEndElement, int length)
	{
		int num = Math.Max(_maxBytesPerRead - 5, 0);
		if (length <= num)
		{
			if (withEndElement)
			{
				ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.Base64, length);
			}
			else
			{
				ReadText(MoveToComplexText(), ValueHandleType.Base64, length);
			}
			return;
		}
		int num2 = num;
		if (num2 > 3)
		{
			num2 -= num2 % 3;
		}
		ReadText(MoveToComplexText(), ValueHandleType.Base64, num2);
		XmlBinaryNodeType nodeType = (withEndElement ? XmlBinaryNodeType.Bytes32TextWithEndElement : XmlBinaryNodeType.Bytes32Text);
		InsertNode(nodeType, length - num2);
	}

	private void InsertNode(XmlBinaryNodeType nodeType, int length)
	{
		byte[] array = new byte[5]
		{
			(byte)nodeType,
			(byte)length,
			0,
			0,
			0
		};
		length >>= 8;
		array[2] = (byte)length;
		length >>= 8;
		array[3] = (byte)length;
		length >>= 8;
		array[4] = (byte)length;
		base.BufferReader.InsertBytes(array, 0, array.Length);
	}

	private void ReadAttributeText(XmlAttributeTextNode textNode)
	{
		XmlBinaryNodeType nodeType = GetNodeType();
		SkipNodeType();
		base.BufferReader.ReadValue(nodeType, textNode.Value);
	}

	private void ReadName(ValueHandle value)
	{
		int num = ReadMultiByteUInt31();
		int offset = base.BufferReader.ReadBytes(num);
		value.SetValue(ValueHandleType.UTF8, offset, num);
	}

	private void ReadName(StringHandle handle)
	{
		int num = ReadMultiByteUInt31();
		int offset = base.BufferReader.ReadBytes(num);
		handle.SetValue(offset, num);
	}

	private void ReadName(PrefixHandle prefix)
	{
		int num = ReadMultiByteUInt31();
		int offset = base.BufferReader.ReadBytes(num);
		prefix.SetValue(offset, num);
	}

	private void ReadDictionaryName(StringHandle s)
	{
		int value = ReadDictionaryKey();
		s.SetValue(value);
	}

	private XmlBinaryNodeType GetNodeType()
	{
		return base.BufferReader.GetNodeType();
	}

	private void SkipNodeType()
	{
		base.BufferReader.SkipNodeType();
	}

	private int ReadDictionaryKey()
	{
		return base.BufferReader.ReadDictionaryKey();
	}

	private int ReadMultiByteUInt31()
	{
		return base.BufferReader.ReadMultiByteUInt31();
	}

	private int ReadUInt8()
	{
		return base.BufferReader.ReadUInt8();
	}

	private int ReadUInt16()
	{
		return base.BufferReader.ReadUInt16();
	}

	private int ReadUInt31()
	{
		return base.BufferReader.ReadUInt31();
	}

	private bool IsValidArrayType(XmlBinaryNodeType nodeType)
	{
		switch (nodeType)
		{
		case XmlBinaryNodeType.Int16TextWithEndElement:
		case XmlBinaryNodeType.Int32TextWithEndElement:
		case XmlBinaryNodeType.Int64TextWithEndElement:
		case XmlBinaryNodeType.FloatTextWithEndElement:
		case XmlBinaryNodeType.DoubleTextWithEndElement:
		case XmlBinaryNodeType.DecimalTextWithEndElement:
		case XmlBinaryNodeType.DateTimeTextWithEndElement:
		case XmlBinaryNodeType.TimeSpanTextWithEndElement:
		case XmlBinaryNodeType.GuidTextWithEndElement:
		case XmlBinaryNodeType.BoolTextWithEndElement:
			return true;
		default:
			return false;
		}
	}

	private void ReadArray()
	{
		if (GetNodeType() == XmlBinaryNodeType.Array)
		{
			XmlExceptionHelper.ThrowInvalidBinaryFormat(this);
		}
		ReadNode();
		if (base.Node.NodeType != XmlNodeType.Element)
		{
			XmlExceptionHelper.ThrowInvalidBinaryFormat(this);
		}
		if (GetNodeType() == XmlBinaryNodeType.Array)
		{
			XmlExceptionHelper.ThrowInvalidBinaryFormat(this);
		}
		ReadNode();
		if (base.Node.NodeType != XmlNodeType.EndElement)
		{
			XmlExceptionHelper.ThrowInvalidBinaryFormat(this);
		}
		_arrayState = ArrayState.Element;
		_arrayNodeType = GetNodeType();
		if (!IsValidArrayType(_arrayNodeType))
		{
			XmlExceptionHelper.ThrowInvalidBinaryFormat(this);
		}
		SkipNodeType();
		_arrayCount = ReadMultiByteUInt31();
		if (_arrayCount == 0)
		{
			XmlExceptionHelper.ThrowInvalidBinaryFormat(this);
		}
		MoveToArrayElement();
	}

	private void MoveToArrayElement()
	{
		_arrayState = ArrayState.Element;
		MoveToNode(base.ElementNode);
	}

	private void SkipArrayElements(int count)
	{
		_arrayCount -= count;
		if (_arrayCount == 0)
		{
			_arrayState = ArrayState.None;
			ExitScope();
			ReadNode();
		}
	}

	public override bool IsStartArray([NotNullWhen(true)] out Type type)
	{
		type = null;
		if (_arrayState != ArrayState.Element)
		{
			return false;
		}
		switch (_arrayNodeType)
		{
		case XmlBinaryNodeType.BoolTextWithEndElement:
			type = typeof(bool);
			break;
		case XmlBinaryNodeType.Int16TextWithEndElement:
			type = typeof(short);
			break;
		case XmlBinaryNodeType.Int32TextWithEndElement:
			type = typeof(int);
			break;
		case XmlBinaryNodeType.Int64TextWithEndElement:
			type = typeof(long);
			break;
		case XmlBinaryNodeType.FloatTextWithEndElement:
			type = typeof(float);
			break;
		case XmlBinaryNodeType.DoubleTextWithEndElement:
			type = typeof(double);
			break;
		case XmlBinaryNodeType.DecimalTextWithEndElement:
			type = typeof(decimal);
			break;
		case XmlBinaryNodeType.DateTimeTextWithEndElement:
			type = typeof(DateTime);
			break;
		case XmlBinaryNodeType.GuidTextWithEndElement:
			type = typeof(Guid);
			break;
		case XmlBinaryNodeType.TimeSpanTextWithEndElement:
			type = typeof(TimeSpan);
			break;
		case XmlBinaryNodeType.UniqueIdTextWithEndElement:
			type = typeof(UniqueId);
			break;
		default:
			return false;
		}
		return true;
	}

	public override bool TryGetArrayLength(out int count)
	{
		count = 0;
		if (!_buffered)
		{
			return false;
		}
		if (_arrayState != ArrayState.Element)
		{
			return false;
		}
		count = _arrayCount;
		return true;
	}

	private bool IsStartArray(string localName, string namespaceUri, XmlBinaryNodeType nodeType)
	{
		if (IsStartElement(localName, namespaceUri) && _arrayState == ArrayState.Element && _arrayNodeType == nodeType)
		{
			return !base.Signing;
		}
		return false;
	}

	private bool IsStartArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, XmlBinaryNodeType nodeType)
	{
		if (IsStartElement(localName, namespaceUri) && _arrayState == ArrayState.Element && _arrayNodeType == nodeType)
		{
			return !base.Signing;
		}
		return false;
	}

	private void CheckArray(Array array, int offset, int count)
	{
		if (array == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("array"));
		}
		if (offset < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", System.SR.ValueMustBeNonNegative));
		}
		if (offset > array.Length)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", System.SR.Format(System.SR.OffsetExceedsBufferSize, array.Length)));
		}
		if (count < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", System.SR.ValueMustBeNonNegative));
		}
		if (count > array.Length - offset)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", System.SR.Format(System.SR.SizeExceedsRemainingBufferSpace, array.Length - offset)));
		}
	}

	private unsafe int ReadArray(bool[] array, int offset, int count)
	{
		CheckArray(array, offset, count);
		int num = Math.Min(count, _arrayCount);
		fixed (bool* ptr = &array[offset])
		{
			base.BufferReader.UnsafeReadArray((byte*)ptr, (byte*)(ptr + num));
		}
		SkipArrayElements(num);
		return num;
	}

	public override int ReadArray(string localName, string namespaceUri, bool[] array, int offset, int count)
	{
		if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.BoolTextWithEndElement))
		{
			return ReadArray(array, offset, count);
		}
		return base.ReadArray(localName, namespaceUri, array, offset, count);
	}

	public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, bool[] array, int offset, int count)
	{
		if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.BoolTextWithEndElement))
		{
			return ReadArray(array, offset, count);
		}
		return base.ReadArray(localName, namespaceUri, array, offset, count);
	}

	private unsafe int ReadArray(short[] array, int offset, int count)
	{
		CheckArray(array, offset, count);
		int num = Math.Min(count, _arrayCount);
		fixed (short* ptr = &array[offset])
		{
			base.BufferReader.UnsafeReadArray((byte*)ptr, (byte*)(ptr + num));
		}
		SkipArrayElements(num);
		return num;
	}

	public override int ReadArray(string localName, string namespaceUri, short[] array, int offset, int count)
	{
		if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.Int16TextWithEndElement) && BitConverter.IsLittleEndian)
		{
			return ReadArray(array, offset, count);
		}
		return base.ReadArray(localName, namespaceUri, array, offset, count);
	}

	public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, short[] array, int offset, int count)
	{
		if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.Int16TextWithEndElement) && BitConverter.IsLittleEndian)
		{
			return ReadArray(array, offset, count);
		}
		return base.ReadArray(localName, namespaceUri, array, offset, count);
	}

	private unsafe int ReadArray(int[] array, int offset, int count)
	{
		CheckArray(array, offset, count);
		int num = Math.Min(count, _arrayCount);
		fixed (int* ptr = &array[offset])
		{
			base.BufferReader.UnsafeReadArray((byte*)ptr, (byte*)(ptr + num));
		}
		SkipArrayElements(num);
		return num;
	}

	public override int ReadArray(string localName, string namespaceUri, int[] array, int offset, int count)
	{
		if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.Int32TextWithEndElement) && BitConverter.IsLittleEndian)
		{
			return ReadArray(array, offset, count);
		}
		return base.ReadArray(localName, namespaceUri, array, offset, count);
	}

	public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, int[] array, int offset, int count)
	{
		if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.Int32TextWithEndElement) && BitConverter.IsLittleEndian)
		{
			return ReadArray(array, offset, count);
		}
		return base.ReadArray(localName, namespaceUri, array, offset, count);
	}

	private unsafe int ReadArray(long[] array, int offset, int count)
	{
		CheckArray(array, offset, count);
		int num = Math.Min(count, _arrayCount);
		fixed (long* ptr = &array[offset])
		{
			base.BufferReader.UnsafeReadArray((byte*)ptr, (byte*)(ptr + num));
		}
		SkipArrayElements(num);
		return num;
	}

	public override int ReadArray(string localName, string namespaceUri, long[] array, int offset, int count)
	{
		if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.Int64TextWithEndElement) && BitConverter.IsLittleEndian)
		{
			return ReadArray(array, offset, count);
		}
		return base.ReadArray(localName, namespaceUri, array, offset, count);
	}

	public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, long[] array, int offset, int count)
	{
		if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.Int64TextWithEndElement) && BitConverter.IsLittleEndian)
		{
			return ReadArray(array, offset, count);
		}
		return base.ReadArray(localName, namespaceUri, array, offset, count);
	}

	private unsafe int ReadArray(float[] array, int offset, int count)
	{
		CheckArray(array, offset, count);
		int num = Math.Min(count, _arrayCount);
		fixed (float* ptr = &array[offset])
		{
			base.BufferReader.UnsafeReadArray((byte*)ptr, (byte*)(ptr + num));
		}
		SkipArrayElements(num);
		return num;
	}

	public override int ReadArray(string localName, string namespaceUri, float[] array, int offset, int count)
	{
		if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.FloatTextWithEndElement))
		{
			return ReadArray(array, offset, count);
		}
		return base.ReadArray(localName, namespaceUri, array, offset, count);
	}

	public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, float[] array, int offset, int count)
	{
		if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.FloatTextWithEndElement))
		{
			return ReadArray(array, offset, count);
		}
		return base.ReadArray(localName, namespaceUri, array, offset, count);
	}

	private unsafe int ReadArray(double[] array, int offset, int count)
	{
		CheckArray(array, offset, count);
		int num = Math.Min(count, _arrayCount);
		fixed (double* ptr = &array[offset])
		{
			base.BufferReader.UnsafeReadArray((byte*)ptr, (byte*)(ptr + num));
		}
		SkipArrayElements(num);
		return num;
	}

	public override int ReadArray(string localName, string namespaceUri, double[] array, int offset, int count)
	{
		if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.DoubleTextWithEndElement))
		{
			return ReadArray(array, offset, count);
		}
		return base.ReadArray(localName, namespaceUri, array, offset, count);
	}

	public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, double[] array, int offset, int count)
	{
		if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.DoubleTextWithEndElement))
		{
			return ReadArray(array, offset, count);
		}
		return base.ReadArray(localName, namespaceUri, array, offset, count);
	}

	private unsafe int ReadArray(decimal[] array, int offset, int count)
	{
		CheckArray(array, offset, count);
		int num = Math.Min(count, _arrayCount);
		fixed (decimal* ptr = &array[offset])
		{
			base.BufferReader.UnsafeReadArray((byte*)ptr, (byte*)(ptr + num));
		}
		SkipArrayElements(num);
		return num;
	}

	public override int ReadArray(string localName, string namespaceUri, decimal[] array, int offset, int count)
	{
		if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.DecimalTextWithEndElement))
		{
			return ReadArray(array, offset, count);
		}
		return base.ReadArray(localName, namespaceUri, array, offset, count);
	}

	public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, decimal[] array, int offset, int count)
	{
		if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.DecimalTextWithEndElement))
		{
			return ReadArray(array, offset, count);
		}
		return base.ReadArray(localName, namespaceUri, array, offset, count);
	}

	private int ReadArray(DateTime[] array, int offset, int count)
	{
		CheckArray(array, offset, count);
		int num = Math.Min(count, _arrayCount);
		for (int i = 0; i < num; i++)
		{
			array[offset + i] = base.BufferReader.ReadDateTime();
		}
		SkipArrayElements(num);
		return num;
	}

	public override int ReadArray(string localName, string namespaceUri, DateTime[] array, int offset, int count)
	{
		if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.DateTimeTextWithEndElement))
		{
			return ReadArray(array, offset, count);
		}
		return base.ReadArray(localName, namespaceUri, array, offset, count);
	}

	public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, DateTime[] array, int offset, int count)
	{
		if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.DateTimeTextWithEndElement))
		{
			return ReadArray(array, offset, count);
		}
		return base.ReadArray(localName, namespaceUri, array, offset, count);
	}

	private int ReadArray(Guid[] array, int offset, int count)
	{
		CheckArray(array, offset, count);
		int num = Math.Min(count, _arrayCount);
		for (int i = 0; i < num; i++)
		{
			array[offset + i] = base.BufferReader.ReadGuid();
		}
		SkipArrayElements(num);
		return num;
	}

	public override int ReadArray(string localName, string namespaceUri, Guid[] array, int offset, int count)
	{
		if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.GuidTextWithEndElement))
		{
			return ReadArray(array, offset, count);
		}
		return base.ReadArray(localName, namespaceUri, array, offset, count);
	}

	public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, Guid[] array, int offset, int count)
	{
		if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.GuidTextWithEndElement))
		{
			return ReadArray(array, offset, count);
		}
		return base.ReadArray(localName, namespaceUri, array, offset, count);
	}

	private int ReadArray(TimeSpan[] array, int offset, int count)
	{
		CheckArray(array, offset, count);
		int num = Math.Min(count, _arrayCount);
		for (int i = 0; i < num; i++)
		{
			array[offset + i] = base.BufferReader.ReadTimeSpan();
		}
		SkipArrayElements(num);
		return num;
	}

	public override int ReadArray(string localName, string namespaceUri, TimeSpan[] array, int offset, int count)
	{
		if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.TimeSpanTextWithEndElement))
		{
			return ReadArray(array, offset, count);
		}
		return base.ReadArray(localName, namespaceUri, array, offset, count);
	}

	public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, TimeSpan[] array, int offset, int count)
	{
		if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.TimeSpanTextWithEndElement))
		{
			return ReadArray(array, offset, count);
		}
		return base.ReadArray(localName, namespaceUri, array, offset, count);
	}

	protected override XmlSigningNodeWriter CreateSigningNodeWriter()
	{
		return new XmlSigningNodeWriter(text: false);
	}
}
