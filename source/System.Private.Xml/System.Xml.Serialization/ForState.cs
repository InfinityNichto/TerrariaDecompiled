using System.Reflection.Emit;

namespace System.Xml.Serialization;

internal sealed class ForState
{
	private readonly LocalBuilder _indexVar;

	private readonly Label _beginLabel;

	private readonly Label _testLabel;

	private readonly object _end;

	internal LocalBuilder Index => _indexVar;

	internal Label BeginLabel => _beginLabel;

	internal Label TestLabel => _testLabel;

	internal object End => _end;

	internal ForState(LocalBuilder indexVar, Label beginLabel, Label testLabel, object end)
	{
		_indexVar = indexVar;
		_beginLabel = beginLabel;
		_testLabel = testLabel;
		_end = end;
	}
}
