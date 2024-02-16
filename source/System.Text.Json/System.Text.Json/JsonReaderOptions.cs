namespace System.Text.Json;

public struct JsonReaderOptions
{
	private int _maxDepth;

	private JsonCommentHandling _commentHandling;

	public JsonCommentHandling CommentHandling
	{
		readonly get
		{
			return _commentHandling;
		}
		set
		{
			if ((int)value > 2)
			{
				throw ThrowHelper.GetArgumentOutOfRangeException_CommentEnumMustBeInRange("value");
			}
			_commentHandling = value;
		}
	}

	public int MaxDepth
	{
		readonly get
		{
			return _maxDepth;
		}
		set
		{
			if (value < 0)
			{
				throw ThrowHelper.GetArgumentOutOfRangeException_MaxDepthMustBePositive("value");
			}
			_maxDepth = value;
		}
	}

	public bool AllowTrailingCommas { get; set; }
}
