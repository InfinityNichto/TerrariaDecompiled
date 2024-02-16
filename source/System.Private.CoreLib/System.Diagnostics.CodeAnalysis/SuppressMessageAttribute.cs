namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
[Conditional("CODE_ANALYSIS")]
public sealed class SuppressMessageAttribute : Attribute
{
	public string Category { get; }

	public string CheckId { get; }

	public string? Scope { get; set; }

	public string? Target { get; set; }

	public string? MessageId { get; set; }

	public string? Justification { get; set; }

	public SuppressMessageAttribute(string category, string checkId)
	{
		Category = category;
		CheckId = checkId;
	}
}
