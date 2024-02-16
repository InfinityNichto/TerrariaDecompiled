namespace System.Xml;

internal interface IDtdParserAdapterWithValidation : IDtdParserAdapter
{
	bool DtdValidation { get; }

	IValidationEventHandling ValidationEventHandling { get; }
}
