using System.Collections.Concurrent;
using System.Collections.Generic;

namespace System.Diagnostics.Metrics;

internal class LabelInstructionInterpretter<TObjectSequence, TAggregator> where TObjectSequence : struct, IObjectSequence, IEquatable<TObjectSequence> where TAggregator : Aggregator
{
	private int _expectedLabelCount;

	private LabelInstruction[] _instructions;

	private ConcurrentDictionary<TObjectSequence, TAggregator> _valuesDict;

	private Func<TObjectSequence, TAggregator> _createAggregator;

	public LabelInstructionInterpretter(int expectedLabelCount, LabelInstruction[] instructions, ConcurrentDictionary<TObjectSequence, TAggregator> valuesDict, Func<TAggregator> createAggregator)
	{
		_expectedLabelCount = expectedLabelCount;
		_instructions = instructions;
		_valuesDict = valuesDict;
		_createAggregator = (TObjectSequence _) => createAggregator();
	}

	public bool GetAggregator(ReadOnlySpan<KeyValuePair<string, object>> labels, out TAggregator aggregator)
	{
		aggregator = null;
		if (labels.Length != _expectedLabelCount)
		{
			return false;
		}
		TObjectSequence val = default(TObjectSequence);
		if (val is ObjectSequenceMany)
		{
			val = (TObjectSequence)(object)new ObjectSequenceMany(new object[_expectedLabelCount]);
		}
		Span<object> span = val.AsSpan();
		for (int i = 0; i < _instructions.Length; i++)
		{
			LabelInstruction labelInstruction = _instructions[i];
			if (labelInstruction.LabelName != labels[labelInstruction.SourceIndex].Key)
			{
				return false;
			}
			span[i] = labels[labelInstruction.SourceIndex].Value;
		}
		if (!_valuesDict.TryGetValue(val, out aggregator))
		{
			aggregator = _createAggregator(val);
			if (aggregator == null)
			{
				return true;
			}
			aggregator = _valuesDict.GetOrAdd(val, aggregator);
		}
		return true;
	}
}
