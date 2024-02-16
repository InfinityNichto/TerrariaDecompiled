using System.Xml.Schema;

namespace System.Xml.Serialization;

internal sealed class AttributeAccessor : Accessor
{
	private bool _isSpecial;

	private bool _isList;

	internal bool IsSpecialXmlNamespace => _isSpecial;

	internal bool IsList
	{
		get
		{
			return _isList;
		}
		set
		{
			_isList = value;
		}
	}

	internal void CheckSpecial()
	{
		int num = Name.LastIndexOf(':');
		if (num >= 0)
		{
			if (!Name.StartsWith("xml:", StringComparison.Ordinal))
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.Xml_InvalidNameChars, Name));
			}
			Name = Name.Substring("xml:".Length);
			base.Namespace = "http://www.w3.org/XML/1998/namespace";
			_isSpecial = true;
		}
		else if (base.Namespace == "http://www.w3.org/XML/1998/namespace")
		{
			_isSpecial = true;
		}
		else
		{
			_isSpecial = false;
		}
		if (_isSpecial)
		{
			base.Form = XmlSchemaForm.Qualified;
		}
	}
}
