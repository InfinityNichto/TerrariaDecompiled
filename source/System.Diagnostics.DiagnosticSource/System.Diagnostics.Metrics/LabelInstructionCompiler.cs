using System.Collections.Concurrent;
using System.Collections.Generic;

namespace System.Diagnostics.Metrics;

internal static class LabelInstructionCompiler
{
	public static AggregatorLookupFunc<TAggregator> Create<TAggregator>(ref AggregatorStore<TAggregator> aggregatorStore, Func<TAggregator> createAggregatorFunc, ReadOnlySpan<KeyValuePair<string, object>> labels) where TAggregator : Aggregator
	{
		LabelInstruction[] array = Compile(labels);
		Array.Sort(array, (LabelInstruction a, LabelInstruction b) => string.CompareOrdinal(a.LabelName, b.LabelName));
		int expectedLabels = labels.Length;
		switch (array.Length)
		{
		case 0:
		{
			TAggregator defaultAggregator = aggregatorStore.GetAggregator();
			return delegate(ReadOnlySpan<KeyValuePair<string, object>> l, out TAggregator aggregator)
			{
				if (l.Length != expectedLabels)
				{
					aggregator = null;
					return false;
				}
				aggregator = defaultAggregator;
				return true;
			};
		}
		case 1:
		{
			StringSequence1 names2 = new StringSequence1(array[0].LabelName);
			ConcurrentDictionary<ObjectSequence1, TAggregator> labelValuesDictionary2 = aggregatorStore.GetLabelValuesDictionary<StringSequence1, ObjectSequence1>(in names2);
			LabelInstructionInterpretter<ObjectSequence1, TAggregator> object2 = new LabelInstructionInterpretter<ObjectSequence1, TAggregator>(expectedLabels, array, labelValuesDictionary2, createAggregatorFunc);
			return object2.GetAggregator;
		}
		case 2:
		{
			StringSequence2 names4 = new StringSequence2(array[0].LabelName, array[1].LabelName);
			ConcurrentDictionary<ObjectSequence2, TAggregator> labelValuesDictionary4 = aggregatorStore.GetLabelValuesDictionary<StringSequence2, ObjectSequence2>(in names4);
			LabelInstructionInterpretter<ObjectSequence2, TAggregator> object4 = new LabelInstructionInterpretter<ObjectSequence2, TAggregator>(expectedLabels, array, labelValuesDictionary4, createAggregatorFunc);
			return object4.GetAggregator;
		}
		case 3:
		{
			StringSequence3 names3 = new StringSequence3(array[0].LabelName, array[1].LabelName, array[2].LabelName);
			ConcurrentDictionary<ObjectSequence3, TAggregator> labelValuesDictionary3 = aggregatorStore.GetLabelValuesDictionary<StringSequence3, ObjectSequence3>(in names3);
			LabelInstructionInterpretter<ObjectSequence3, TAggregator> object3 = new LabelInstructionInterpretter<ObjectSequence3, TAggregator>(expectedLabels, array, labelValuesDictionary3, createAggregatorFunc);
			return object3.GetAggregator;
		}
		default:
		{
			string[] array2 = new string[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array2[i] = array[i].LabelName;
			}
			StringSequenceMany names = new StringSequenceMany(array2);
			ConcurrentDictionary<ObjectSequenceMany, TAggregator> labelValuesDictionary = aggregatorStore.GetLabelValuesDictionary<StringSequenceMany, ObjectSequenceMany>(in names);
			LabelInstructionInterpretter<ObjectSequenceMany, TAggregator> @object = new LabelInstructionInterpretter<ObjectSequenceMany, TAggregator>(expectedLabels, array, labelValuesDictionary, createAggregatorFunc);
			return @object.GetAggregator;
		}
		}
	}

	private static LabelInstruction[] Compile(ReadOnlySpan<KeyValuePair<string, object>> labels)
	{
		LabelInstruction[] array = new LabelInstruction[labels.Length];
		for (int i = 0; i < labels.Length; i++)
		{
			array[i] = new LabelInstruction(i, labels[i].Key);
		}
		return array;
	}
}
