using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Schema;

internal sealed class ChameleonKey
{
	internal string targetNS;

	internal Uri chameleonLocation;

	internal XmlSchema originalSchema;

	private int _hashCode;

	public ChameleonKey(string ns, XmlSchema originalSchema)
	{
		targetNS = ns;
		chameleonLocation = originalSchema.BaseUri;
		if (chameleonLocation.OriginalString.Length == 0)
		{
			this.originalSchema = originalSchema;
		}
	}

	public override int GetHashCode()
	{
		if (_hashCode == 0)
		{
			_hashCode = targetNS.GetHashCode() + chameleonLocation.GetHashCode() + ((originalSchema != null) ? originalSchema.GetHashCode() : 0);
		}
		return _hashCode;
	}

	public override bool Equals([NotNullWhen(true)] object obj)
	{
		if (this == obj)
		{
			return true;
		}
		if (obj is ChameleonKey chameleonKey)
		{
			if (targetNS.Equals(chameleonKey.targetNS) && chameleonLocation.Equals(chameleonKey.chameleonLocation))
			{
				return originalSchema == chameleonKey.originalSchema;
			}
			return false;
		}
		return false;
	}
}
