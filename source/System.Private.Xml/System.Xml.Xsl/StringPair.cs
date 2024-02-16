namespace System.Xml.Xsl;

internal struct StringPair
{
	private readonly string _left;

	private readonly string _right;

	public string Left => _left;

	public string Right => _right;

	public StringPair(string left, string right)
	{
		_left = left;
		_right = right;
	}
}
