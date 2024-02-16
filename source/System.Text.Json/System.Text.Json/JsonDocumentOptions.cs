namespace System.Text.Json;

public struct JsonDocumentOptions
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
			if ((int)value > 1)
			{
				throw new ArgumentOutOfRangeException("value", System.SR.JsonDocumentDoesNotSupportComments);
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

	internal JsonReaderOptions GetReaderOptions()
	{
		JsonReaderOptions result = default(JsonReaderOptions);
		result.AllowTrailingCommas = AllowTrailingCommas;
		result.CommentHandling = CommentHandling;
		result.MaxDepth = MaxDepth;
		return result;
	}
}
