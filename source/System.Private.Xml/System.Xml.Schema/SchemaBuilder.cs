namespace System.Xml.Schema;

internal abstract class SchemaBuilder
{
	internal abstract bool ProcessElement(string prefix, string name, string ns);

	internal abstract void ProcessAttribute(string prefix, string name, string ns, string value);

	internal abstract bool IsContentParsed();

	internal abstract void ProcessMarkup(XmlNode[] markup);

	internal abstract void ProcessCData(string value);

	internal abstract void StartChildren();

	internal abstract void EndChildren();
}
