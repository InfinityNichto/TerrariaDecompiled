using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal abstract class SortHelper<TInputOutput>
{
	internal abstract TInputOutput[] Sort();
}
internal sealed class SortHelper<TInputOutput, TKey> : SortHelper<TInputOutput>, IDisposable
{
	private readonly QueryOperatorEnumerator<TInputOutput, TKey> _source;

	private readonly int _partitionCount;

	private readonly int _partitionIndex;

	private readonly QueryTaskGroupState _groupState;

	private readonly int[][] _sharedIndices;

	private readonly GrowingArray<TKey>[] _sharedKeys;

	private readonly TInputOutput[][] _sharedValues;

	private readonly Barrier[][] _sharedBarriers;

	private readonly OrdinalIndexState _indexState;

	private readonly IComparer<TKey> _keyComparer;

	private SortHelper(QueryOperatorEnumerator<TInputOutput, TKey> source, int partitionCount, int partitionIndex, QueryTaskGroupState groupState, int[][] sharedIndices, OrdinalIndexState indexState, IComparer<TKey> keyComparer, GrowingArray<TKey>[] sharedkeys, TInputOutput[][] sharedValues, Barrier[][] sharedBarriers)
	{
		_source = source;
		_partitionCount = partitionCount;
		_partitionIndex = partitionIndex;
		_groupState = groupState;
		_sharedIndices = sharedIndices;
		_indexState = indexState;
		_keyComparer = keyComparer;
		_sharedKeys = sharedkeys;
		_sharedValues = sharedValues;
		_sharedBarriers = sharedBarriers;
	}

	internal static SortHelper<TInputOutput, TKey>[] GenerateSortHelpers(PartitionedStream<TInputOutput, TKey> partitions, QueryTaskGroupState groupState)
	{
		int partitionCount = partitions.PartitionCount;
		SortHelper<TInputOutput, TKey>[] array = new SortHelper<TInputOutput, TKey>[partitionCount];
		int num = 1;
		int num2 = 0;
		while (num < partitionCount)
		{
			num2++;
			num <<= 1;
		}
		int[][] sharedIndices = new int[partitionCount][];
		GrowingArray<TKey>[] sharedkeys = new GrowingArray<TKey>[partitionCount];
		TInputOutput[][] sharedValues = new TInputOutput[partitionCount][];
		Barrier[][] array2 = JaggedArray<Barrier>.Allocate(num2, partitionCount);
		if (partitionCount > 1)
		{
			int num3 = 1;
			for (int i = 0; i < array2.Length; i++)
			{
				for (int j = 0; j < array2[i].Length; j++)
				{
					if (j % num3 == 0)
					{
						array2[i][j] = new Barrier(2);
					}
				}
				num3 *= 2;
			}
		}
		for (int k = 0; k < partitionCount; k++)
		{
			array[k] = new SortHelper<TInputOutput, TKey>(partitions[k], partitionCount, k, groupState, sharedIndices, partitions.OrdinalIndexState, partitions.KeyComparer, sharedkeys, sharedValues, array2);
		}
		return array;
	}

	public void Dispose()
	{
		if (_partitionIndex != 0)
		{
			return;
		}
		for (int i = 0; i < _sharedBarriers.Length; i++)
		{
			for (int j = 0; j < _sharedBarriers[i].Length; j++)
			{
				_sharedBarriers[i][j]?.Dispose();
			}
		}
	}

	internal override TInputOutput[] Sort()
	{
		GrowingArray<TKey> keys = null;
		List<TInputOutput> values = null;
		BuildKeysFromSource(ref keys, ref values);
		QuickSortIndicesInPlace(keys, values, _indexState);
		if (_partitionCount > 1)
		{
			MergeSortCooperatively();
		}
		return _sharedValues[_partitionIndex];
	}

	private void BuildKeysFromSource(ref GrowingArray<TKey> keys, ref List<TInputOutput> values)
	{
		values = new List<TInputOutput>();
		CancellationToken mergedCancellationToken = _groupState.CancellationState.MergedCancellationToken;
		try
		{
			TInputOutput currentElement = default(TInputOutput);
			TKey currentKey = default(TKey);
			bool flag = _source.MoveNext(ref currentElement, ref currentKey);
			if (keys == null)
			{
				keys = new GrowingArray<TKey>();
			}
			if (!flag)
			{
				return;
			}
			int num = 0;
			do
			{
				if ((num++ & 0x3F) == 0)
				{
					mergedCancellationToken.ThrowIfCancellationRequested();
				}
				keys.Add(currentKey);
				values.Add(currentElement);
			}
			while (_source.MoveNext(ref currentElement, ref currentKey));
		}
		finally
		{
			_source.Dispose();
		}
	}

	private void QuickSortIndicesInPlace(GrowingArray<TKey> keys, List<TInputOutput> values, OrdinalIndexState ordinalIndexState)
	{
		int[] array = new int[values.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = i;
		}
		if (array.Length > 1 && ordinalIndexState.IsWorseThan(OrdinalIndexState.Increasing))
		{
			QuickSort(0, array.Length - 1, keys.InternalArray, array, _groupState.CancellationState.MergedCancellationToken);
		}
		if (_partitionCount == 1)
		{
			TInputOutput[] array2 = new TInputOutput[values.Count];
			for (int j = 0; j < array.Length; j++)
			{
				array2[j] = values[array[j]];
			}
			_sharedValues[_partitionIndex] = array2;
		}
		else
		{
			_sharedIndices[_partitionIndex] = array;
			_sharedKeys[_partitionIndex] = keys;
			_sharedValues[_partitionIndex] = new TInputOutput[values.Count];
			values.CopyTo(_sharedValues[_partitionIndex]);
		}
	}

	private void MergeSortCooperatively()
	{
		CancellationToken mergedCancellationToken = _groupState.CancellationState.MergedCancellationToken;
		int num = _sharedBarriers.Length;
		for (int i = 0; i < num; i++)
		{
			bool flag = i == num - 1;
			int num2 = ComputePartnerIndex(i);
			if (num2 >= _partitionCount)
			{
				continue;
			}
			int[] array = _sharedIndices[_partitionIndex];
			GrowingArray<TKey> growingArray = _sharedKeys[_partitionIndex];
			TKey[] internalArray = growingArray.InternalArray;
			TInputOutput[] array2 = _sharedValues[_partitionIndex];
			_sharedBarriers[i][Math.Min(_partitionIndex, num2)].SignalAndWait(mergedCancellationToken);
			if (_partitionIndex < num2)
			{
				int[] array3 = _sharedIndices[num2];
				TKey[] internalArray2 = _sharedKeys[num2].InternalArray;
				TInputOutput[] array4 = _sharedValues[num2];
				_sharedIndices[num2] = array;
				_sharedKeys[num2] = growingArray;
				_sharedValues[num2] = array2;
				int num3 = array2.Length;
				int num4 = array4.Length;
				int num5 = num3 + num4;
				int[] array5 = null;
				TInputOutput[] array6 = new TInputOutput[num5];
				if (!flag)
				{
					array5 = new int[num5];
				}
				_sharedIndices[_partitionIndex] = array5;
				_sharedKeys[_partitionIndex] = growingArray;
				_sharedValues[_partitionIndex] = array6;
				_sharedBarriers[i][_partitionIndex].SignalAndWait(mergedCancellationToken);
				int num6 = (num5 + 1) / 2;
				int j = 0;
				int num7 = 0;
				int num8 = 0;
				for (; j < num6; j++)
				{
					if ((j & 0x3F) == 0)
					{
						mergedCancellationToken.ThrowIfCancellationRequested();
					}
					if (num7 < num3 && (num8 >= num4 || _keyComparer.Compare(internalArray[array[num7]], internalArray2[array3[num8]]) <= 0))
					{
						if (flag)
						{
							array6[j] = array2[array[num7]];
						}
						else
						{
							array5[j] = array[num7];
						}
						num7++;
					}
					else
					{
						if (flag)
						{
							array6[j] = array4[array3[num8]];
						}
						else
						{
							array5[j] = num3 + array3[num8];
						}
						num8++;
					}
				}
				if (!flag && num3 > 0)
				{
					Array.Copy(array2, array6, num3);
				}
				_sharedBarriers[i][_partitionIndex].SignalAndWait(mergedCancellationToken);
				continue;
			}
			_sharedBarriers[i][num2].SignalAndWait(mergedCancellationToken);
			int[] array7 = _sharedIndices[_partitionIndex];
			TKey[] internalArray3 = _sharedKeys[_partitionIndex].InternalArray;
			TInputOutput[] array8 = _sharedValues[_partitionIndex];
			int[] array9 = _sharedIndices[num2];
			GrowingArray<TKey> growingArray2 = _sharedKeys[num2];
			TInputOutput[] array10 = _sharedValues[num2];
			int num9 = array8.Length;
			int num10 = array2.Length;
			int num11 = num9 + num10;
			int num12 = (num11 + 1) / 2;
			int num13 = num11 - 1;
			int num14 = num9 - 1;
			int num15 = num10 - 1;
			while (num13 >= num12)
			{
				if ((num13 & 0x3F) == 0)
				{
					mergedCancellationToken.ThrowIfCancellationRequested();
				}
				if (num14 >= 0 && (num15 < 0 || _keyComparer.Compare(internalArray3[array7[num14]], internalArray[array[num15]]) > 0))
				{
					if (flag)
					{
						array10[num13] = array8[array7[num14]];
					}
					else
					{
						array9[num13] = array7[num14];
					}
					num14--;
				}
				else
				{
					if (flag)
					{
						array10[num13] = array2[array[num15]];
					}
					else
					{
						array9[num13] = num9 + array[num15];
					}
					num15--;
				}
				num13--;
			}
			if (!flag && array2.Length != 0)
			{
				growingArray2.CopyFrom(internalArray, array2.Length);
				Array.Copy(array2, 0, array10, num9, array2.Length);
			}
			_sharedBarriers[i][num2].SignalAndWait(mergedCancellationToken);
			break;
		}
	}

	private int ComputePartnerIndex(int phase)
	{
		int num = 1 << phase;
		return _partitionIndex + ((_partitionIndex % (num * 2) == 0) ? num : (-num));
	}

	private void QuickSort(int left, int right, TKey[] keys, int[] indices, CancellationToken cancelToken)
	{
		if (right - left > 63)
		{
			cancelToken.ThrowIfCancellationRequested();
		}
		do
		{
			int num = left;
			int num2 = right;
			int num3 = indices[num + (num2 - num >> 1)];
			TKey y = keys[num3];
			while (true)
			{
				if (_keyComparer.Compare(keys[indices[num]], y) < 0)
				{
					num++;
					continue;
				}
				while (_keyComparer.Compare(keys[indices[num2]], y) > 0)
				{
					num2--;
				}
				if (num > num2)
				{
					break;
				}
				if (num < num2)
				{
					int num4 = indices[num];
					indices[num] = indices[num2];
					indices[num2] = num4;
				}
				num++;
				num2--;
				if (num > num2)
				{
					break;
				}
			}
			if (num2 - left <= right - num)
			{
				if (left < num2)
				{
					QuickSort(left, num2, keys, indices, cancelToken);
				}
				left = num;
			}
			else
			{
				if (num < right)
				{
					QuickSort(num, right, keys, indices, cancelToken);
				}
				right = num2;
			}
		}
		while (left < right);
	}
}
