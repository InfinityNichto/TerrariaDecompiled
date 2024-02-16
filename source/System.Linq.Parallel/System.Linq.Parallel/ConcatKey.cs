using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq.Parallel;

internal struct ConcatKey<TLeftKey, TRightKey>
{
	private sealed class ConcatKeyComparer : IComparer<ConcatKey<TLeftKey, TRightKey>>
	{
		private readonly IComparer<TLeftKey> _leftComparer;

		private readonly IComparer<TRightKey> _rightComparer;

		internal ConcatKeyComparer(IComparer<TLeftKey> leftComparer, IComparer<TRightKey> rightComparer)
		{
			_leftComparer = leftComparer;
			_rightComparer = rightComparer;
		}

		public int Compare(ConcatKey<TLeftKey, TRightKey> x, ConcatKey<TLeftKey, TRightKey> y)
		{
			if (x._isLeft != y._isLeft)
			{
				if (!x._isLeft)
				{
					return 1;
				}
				return -1;
			}
			if (x._isLeft)
			{
				return _leftComparer.Compare(x._leftKey, y._leftKey);
			}
			return _rightComparer.Compare(x._rightKey, y._rightKey);
		}
	}

	private readonly TLeftKey _leftKey;

	private readonly TRightKey _rightKey;

	private readonly bool _isLeft;

	private ConcatKey([AllowNull] TLeftKey leftKey, [AllowNull] TRightKey rightKey, bool isLeft)
	{
		_leftKey = leftKey;
		_rightKey = rightKey;
		_isLeft = isLeft;
	}

	internal static ConcatKey<TLeftKey, TRightKey> MakeLeft([AllowNull] TLeftKey leftKey)
	{
		return new ConcatKey<TLeftKey, TRightKey>(leftKey, default(TRightKey), isLeft: true);
	}

	internal static ConcatKey<TLeftKey, TRightKey> MakeRight([AllowNull] TRightKey rightKey)
	{
		return new ConcatKey<TLeftKey, TRightKey>(default(TLeftKey), rightKey, isLeft: false);
	}

	internal static IComparer<ConcatKey<TLeftKey, TRightKey>> MakeComparer(IComparer<TLeftKey> leftComparer, IComparer<TRightKey> rightComparer)
	{
		return new ConcatKeyComparer(leftComparer, rightComparer);
	}
}
