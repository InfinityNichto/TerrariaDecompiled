namespace System.Net.Http.Headers;

internal sealed class TransferCodingHeaderParser : BaseHeaderParser
{
	private readonly Func<TransferCodingHeaderValue> _transferCodingCreator;

	internal static readonly TransferCodingHeaderParser SingleValueParser = new TransferCodingHeaderParser(supportsMultipleValues: false, CreateTransferCoding);

	internal static readonly TransferCodingHeaderParser MultipleValueParser = new TransferCodingHeaderParser(supportsMultipleValues: true, CreateTransferCoding);

	internal static readonly TransferCodingHeaderParser SingleValueWithQualityParser = new TransferCodingHeaderParser(supportsMultipleValues: false, CreateTransferCodingWithQuality);

	internal static readonly TransferCodingHeaderParser MultipleValueWithQualityParser = new TransferCodingHeaderParser(supportsMultipleValues: true, CreateTransferCodingWithQuality);

	private TransferCodingHeaderParser(bool supportsMultipleValues, Func<TransferCodingHeaderValue> transferCodingCreator)
		: base(supportsMultipleValues)
	{
		_transferCodingCreator = transferCodingCreator;
	}

	protected override int GetParsedValueLength(string value, int startIndex, object storeValue, out object parsedValue)
	{
		TransferCodingHeaderValue parsedValue2;
		int transferCodingLength = TransferCodingHeaderValue.GetTransferCodingLength(value, startIndex, _transferCodingCreator, out parsedValue2);
		parsedValue = parsedValue2;
		return transferCodingLength;
	}

	private static TransferCodingHeaderValue CreateTransferCoding()
	{
		return new TransferCodingHeaderValue();
	}

	private static TransferCodingHeaderValue CreateTransferCodingWithQuality()
	{
		return new TransferCodingWithQualityHeaderValue();
	}
}
