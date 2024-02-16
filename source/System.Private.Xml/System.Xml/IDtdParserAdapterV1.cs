namespace System.Xml;

internal interface IDtdParserAdapterV1 : IDtdParserAdapterWithValidation, IDtdParserAdapter
{
	bool V1CompatibilityMode { get; }

	bool Normalization { get; }

	bool Namespaces { get; }
}
