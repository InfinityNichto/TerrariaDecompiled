namespace System.Runtime.InteropServices;

[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
public sealed class ImportedFromTypeLibAttribute : Attribute
{
	public string Value { get; }

	public ImportedFromTypeLibAttribute(string tlbFile)
	{
		Value = tlbFile;
	}
}
