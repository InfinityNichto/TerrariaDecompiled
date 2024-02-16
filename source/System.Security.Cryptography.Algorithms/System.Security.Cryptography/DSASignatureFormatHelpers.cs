namespace System.Security.Cryptography;

internal static class DSASignatureFormatHelpers
{
	internal static bool IsKnownValue(this DSASignatureFormat signatureFormat)
	{
		if (signatureFormat >= DSASignatureFormat.IeeeP1363FixedFieldConcatenation)
		{
			return signatureFormat <= DSASignatureFormat.Rfc3279DerSequence;
		}
		return false;
	}

	internal static Exception CreateUnknownValueException(DSASignatureFormat signatureFormat)
	{
		return new ArgumentOutOfRangeException("signatureFormat", System.SR.Format(System.SR.Cryptography_UnknownSignatureFormat, signatureFormat));
	}
}
