namespace System.Security.Cryptography;

public readonly struct PemFields
{
	public Range Location { get; }

	public Range Label { get; }

	public Range Base64Data { get; }

	public int DecodedDataLength { get; }

	internal PemFields(Range label, Range base64data, Range location, int decodedDataLength)
	{
		Location = location;
		DecodedDataLength = decodedDataLength;
		Base64Data = base64data;
		Label = label;
	}
}
