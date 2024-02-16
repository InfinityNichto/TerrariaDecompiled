namespace System.Runtime.Serialization;

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class EnumMemberAttribute : Attribute
{
	private string _value;

	private bool _isValueSetExplicitly;

	public string? Value
	{
		get
		{
			return _value;
		}
		set
		{
			_value = value;
			_isValueSetExplicitly = true;
		}
	}

	public bool IsValueSetExplicitly => _isValueSetExplicitly;
}
