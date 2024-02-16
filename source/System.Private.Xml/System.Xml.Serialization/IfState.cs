using System.Reflection.Emit;

namespace System.Xml.Serialization;

internal sealed class IfState
{
	private Label _elseBegin;

	private Label _endIf;

	internal Label EndIf
	{
		get
		{
			return _endIf;
		}
		set
		{
			_endIf = value;
		}
	}

	internal Label ElseBegin
	{
		get
		{
			return _elseBegin;
		}
		set
		{
			_elseBegin = value;
		}
	}
}
