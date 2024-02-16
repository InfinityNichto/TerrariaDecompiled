using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.IlGen;

internal sealed class OptimizerPatterns : IQilAnnotation
{
	private static readonly int s_patternCount = Enum.GetValues(typeof(OptimizerPatternName)).Length;

	private int _patterns;

	private bool _isReadOnly;

	private object _arg0;

	private object _arg1;

	private object _arg2;

	private static volatile OptimizerPatterns s_zeroOrOneDefault;

	private static volatile OptimizerPatterns s_maybeManyDefault;

	private static volatile OptimizerPatterns s_dodDefault;

	public static OptimizerPatterns Read(QilNode nd)
	{
		OptimizerPatterns optimizerPatterns = ((nd.Annotation is XmlILAnnotation xmlILAnnotation) ? xmlILAnnotation.Patterns : null);
		if (optimizerPatterns == null)
		{
			if (!nd.XmlType.MaybeMany)
			{
				if (s_zeroOrOneDefault == null)
				{
					optimizerPatterns = new OptimizerPatterns();
					optimizerPatterns.AddPattern(OptimizerPatternName.IsDocOrderDistinct);
					optimizerPatterns.AddPattern(OptimizerPatternName.SameDepth);
					optimizerPatterns._isReadOnly = true;
					s_zeroOrOneDefault = optimizerPatterns;
				}
				else
				{
					optimizerPatterns = s_zeroOrOneDefault;
				}
			}
			else if (nd.XmlType.IsDod)
			{
				if (s_dodDefault == null)
				{
					optimizerPatterns = new OptimizerPatterns();
					optimizerPatterns.AddPattern(OptimizerPatternName.IsDocOrderDistinct);
					optimizerPatterns._isReadOnly = true;
					s_dodDefault = optimizerPatterns;
				}
				else
				{
					optimizerPatterns = s_dodDefault;
				}
			}
			else if (s_maybeManyDefault == null)
			{
				optimizerPatterns = new OptimizerPatterns();
				optimizerPatterns._isReadOnly = true;
				s_maybeManyDefault = optimizerPatterns;
			}
			else
			{
				optimizerPatterns = s_maybeManyDefault;
			}
		}
		return optimizerPatterns;
	}

	public static OptimizerPatterns Write(QilNode nd)
	{
		XmlILAnnotation xmlILAnnotation = XmlILAnnotation.Write(nd);
		OptimizerPatterns optimizerPatterns = xmlILAnnotation.Patterns;
		if (optimizerPatterns == null || optimizerPatterns._isReadOnly)
		{
			optimizerPatterns = (xmlILAnnotation.Patterns = new OptimizerPatterns());
			if (!nd.XmlType.MaybeMany)
			{
				optimizerPatterns.AddPattern(OptimizerPatternName.IsDocOrderDistinct);
				optimizerPatterns.AddPattern(OptimizerPatternName.SameDepth);
			}
			else if (nd.XmlType.IsDod)
			{
				optimizerPatterns.AddPattern(OptimizerPatternName.IsDocOrderDistinct);
			}
		}
		return optimizerPatterns;
	}

	public static void Inherit(QilNode ndSrc, QilNode ndDst, OptimizerPatternName pattern)
	{
		OptimizerPatterns optimizerPatterns = Read(ndSrc);
		if (optimizerPatterns.MatchesPattern(pattern))
		{
			OptimizerPatterns optimizerPatterns2 = Write(ndDst);
			optimizerPatterns2.AddPattern(pattern);
			switch (pattern)
			{
			case OptimizerPatternName.Step:
				optimizerPatterns2.AddArgument(OptimizerPatternArgument.StepNode, optimizerPatterns.GetArgument(OptimizerPatternArgument.StepNode));
				optimizerPatterns2.AddArgument(OptimizerPatternArgument.StepInput, optimizerPatterns.GetArgument(OptimizerPatternArgument.StepInput));
				break;
			case OptimizerPatternName.FilterElements:
				optimizerPatterns2.AddArgument(OptimizerPatternArgument.ElementQName, optimizerPatterns.GetArgument(OptimizerPatternArgument.ElementQName));
				break;
			case OptimizerPatternName.FilterContentKind:
				optimizerPatterns2.AddArgument(OptimizerPatternArgument.ElementQName, optimizerPatterns.GetArgument(OptimizerPatternArgument.ElementQName));
				break;
			case OptimizerPatternName.EqualityIndex:
				optimizerPatterns2.AddArgument(OptimizerPatternArgument.StepNode, optimizerPatterns.GetArgument(OptimizerPatternArgument.StepNode));
				optimizerPatterns2.AddArgument(OptimizerPatternArgument.StepInput, optimizerPatterns.GetArgument(OptimizerPatternArgument.StepInput));
				break;
			case OptimizerPatternName.DodReverse:
			case OptimizerPatternName.JoinAndDod:
				optimizerPatterns2.AddArgument(OptimizerPatternArgument.ElementQName, optimizerPatterns.GetArgument(OptimizerPatternArgument.ElementQName));
				break;
			case OptimizerPatternName.MaxPosition:
				optimizerPatterns2.AddArgument(OptimizerPatternArgument.ElementQName, optimizerPatterns.GetArgument(OptimizerPatternArgument.ElementQName));
				break;
			case OptimizerPatternName.SingleTextRtf:
				optimizerPatterns2.AddArgument(OptimizerPatternArgument.ElementQName, optimizerPatterns.GetArgument(OptimizerPatternArgument.ElementQName));
				break;
			case OptimizerPatternName.FilterAttributeKind:
			case OptimizerPatternName.IsDocOrderDistinct:
			case OptimizerPatternName.IsPositional:
			case OptimizerPatternName.SameDepth:
				break;
			}
		}
	}

	public void AddArgument(OptimizerPatternArgument argId, object arg)
	{
		switch ((int)argId)
		{
		case 0:
			_arg0 = arg;
			break;
		case 1:
			_arg1 = arg;
			break;
		case 2:
			_arg2 = arg;
			break;
		}
	}

	public object GetArgument(OptimizerPatternArgument argNum)
	{
		object result = null;
		switch ((int)argNum)
		{
		case 0:
			result = _arg0;
			break;
		case 1:
			result = _arg1;
			break;
		case 2:
			result = _arg2;
			break;
		}
		return result;
	}

	public void AddPattern(OptimizerPatternName pattern)
	{
		_patterns |= 1 << (int)pattern;
	}

	public bool MatchesPattern(OptimizerPatternName pattern)
	{
		return (_patterns & (1 << (int)pattern)) != 0;
	}

	public override string ToString()
	{
		string text = "";
		for (int i = 0; i < s_patternCount; i++)
		{
			if (MatchesPattern((OptimizerPatternName)i))
			{
				if (text.Length != 0)
				{
					text += ", ";
				}
				string text2 = text;
				OptimizerPatternName optimizerPatternName = (OptimizerPatternName)i;
				text = text2 + optimizerPatternName;
			}
		}
		return text;
	}
}
