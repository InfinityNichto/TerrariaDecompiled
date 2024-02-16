using System.Reflection.Emit;

namespace System.Runtime.Serialization;

internal sealed class ForState
{
	private readonly LocalBuilder _indexVar;

	private readonly Label _beginLabel;

	private readonly Label _testLabel;

	private Label _endLabel;

	private bool _requiresEndLabel;

	private readonly object _end;

	internal LocalBuilder Index => _indexVar;

	internal Label BeginLabel => _beginLabel;

	internal Label TestLabel => _testLabel;

	internal Label EndLabel
	{
		get
		{
			return _endLabel;
		}
		set
		{
			_endLabel = value;
		}
	}

	internal bool RequiresEndLabel
	{
		get
		{
			return _requiresEndLabel;
		}
		set
		{
			_requiresEndLabel = value;
		}
	}

	internal object End => _end;

	internal ForState(LocalBuilder indexVar, Label beginLabel, Label testLabel, object end)
	{
		_indexVar = indexVar;
		_beginLabel = beginLabel;
		_testLabel = testLabel;
		_end = end;
	}
}
