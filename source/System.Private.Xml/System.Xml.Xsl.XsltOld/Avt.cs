using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Xml.Xsl.XsltOld;

internal sealed class Avt
{
	private readonly string _constAvt;

	private readonly TextEvent[] _events;

	[MemberNotNullWhen(false, "_events")]
	public bool IsConstant
	{
		[MemberNotNullWhen(false, "_events")]
		get
		{
			return _events == null;
		}
	}

	private Avt(string constAvt)
	{
		_constAvt = constAvt;
	}

	private Avt(ArrayList eventList)
	{
		_events = new TextEvent[eventList.Count];
		for (int i = 0; i < eventList.Count; i++)
		{
			_events[i] = (TextEvent)eventList[i];
		}
	}

	internal string Evaluate(Processor processor, ActionFrame frame)
	{
		if (IsConstant)
		{
			return _constAvt;
		}
		StringBuilder sharedStringBuilder = processor.GetSharedStringBuilder();
		for (int i = 0; i < _events.Length; i++)
		{
			sharedStringBuilder.Append(_events[i].Evaluate(processor, frame));
		}
		processor.ReleaseSharedStringBuilder();
		return sharedStringBuilder.ToString();
	}

	internal static Avt CompileAvt(Compiler compiler, string avtText)
	{
		bool constant;
		ArrayList eventList = compiler.CompileAvt(avtText, out constant);
		if (!constant)
		{
			return new Avt(eventList);
		}
		return new Avt(avtText);
	}
}
