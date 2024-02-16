using System.Globalization;
using System.Runtime.Serialization;
using System.Xml.Xsl.Xslt;

namespace System.Xml.Xsl;

[Serializable]
internal class XslLoadException : XslTransformException
{
	private ISourceLineInfo _lineInfo;

	public override string SourceUri
	{
		get
		{
			if (_lineInfo == null)
			{
				return null;
			}
			return _lineInfo.Uri;
		}
	}

	public override int LineNumber
	{
		get
		{
			if (_lineInfo == null)
			{
				return 0;
			}
			return _lineInfo.Start.Line;
		}
	}

	public override int LinePosition
	{
		get
		{
			if (_lineInfo == null)
			{
				return 0;
			}
			return _lineInfo.Start.Pos;
		}
	}

	internal XslLoadException(string res, params string[] args)
		: base(null, res, args)
	{
	}

	internal XslLoadException(Exception inner, ISourceLineInfo lineInfo)
		: base(inner, System.SR.Xslt_CompileError2, (string[])null)
	{
		SetSourceLineInfo(lineInfo);
	}

	internal XslLoadException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		if ((bool)info.GetValue("hasLineInfo", typeof(bool)))
		{
			string uriString = (string)info.GetValue("Uri", typeof(string));
			int startLine = (int)info.GetValue("StartLine", typeof(int));
			int startPos = (int)info.GetValue("StartPos", typeof(int));
			int endLine = (int)info.GetValue("EndLine", typeof(int));
			int endPos = (int)info.GetValue("EndPos", typeof(int));
			_lineInfo = new SourceLineInfo(uriString, startLine, startPos, endLine, endPos);
		}
	}

	internal XslLoadException(CompilerError error)
		: base(System.SR.Xml_UserException, error.ErrorText)
	{
		int line = error.Line;
		int num = error.Column;
		if (line == 0)
		{
			num = 0;
		}
		else if (num == 0)
		{
			num = 1;
		}
		SetSourceLineInfo(new SourceLineInfo(error.FileName, line, num, line, num));
	}

	internal void SetSourceLineInfo(ISourceLineInfo lineInfo)
	{
		_lineInfo = lineInfo;
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("hasLineInfo", _lineInfo != null);
		if (_lineInfo != null)
		{
			info.AddValue("Uri", _lineInfo.Uri, typeof(string));
			info.AddValue("StartLine", _lineInfo.Start.Line, typeof(int));
			info.AddValue("StartPos", _lineInfo.Start.Pos, typeof(int));
			info.AddValue("EndLine", _lineInfo.End.Line, typeof(int));
			info.AddValue("EndPos", _lineInfo.End.Pos, typeof(int));
		}
	}

	private static string AppendLineInfoMessage(string message, ISourceLineInfo lineInfo)
	{
		if (lineInfo != null)
		{
			string fileName = SourceLineInfo.GetFileName(lineInfo.Uri);
			string text = XslTransformException.CreateMessage(System.SR.Xml_ErrorFilePosition, fileName, lineInfo.Start.Line.ToString(CultureInfo.InvariantCulture), lineInfo.Start.Pos.ToString(CultureInfo.InvariantCulture));
			if (text != null && text.Length > 0)
			{
				if (message.Length > 0 && !XmlCharType.IsWhiteSpace(message[message.Length - 1]))
				{
					message += " ";
				}
				message += text;
			}
		}
		return message;
	}

	internal static string CreateMessage(ISourceLineInfo lineInfo, string res, params string[] args)
	{
		return AppendLineInfoMessage(XslTransformException.CreateMessage(res, args), lineInfo);
	}

	internal override string FormatDetailedMessage()
	{
		return AppendLineInfoMessage(Message, _lineInfo);
	}
}
