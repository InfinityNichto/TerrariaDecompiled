using System.Diagnostics.CodeAnalysis;

namespace System.Xml;

internal sealed class XmlDataImplementation : XmlImplementation
{
	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public XmlDataImplementation()
	{
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
	public override XmlDocument CreateDocument()
	{
		return new XmlDataDocument(this);
	}
}
