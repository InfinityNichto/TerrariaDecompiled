using System.Threading.Tasks;

namespace System.Xml;

internal interface IDtdParser
{
	IDtdInfo ParseInternalDtd(IDtdParserAdapter adapter, bool saveInternalSubset);

	IDtdInfo ParseFreeFloatingDtd(string baseUri, string docTypeName, string publicId, string systemId, string internalSubset, IDtdParserAdapter adapter);

	Task<IDtdInfo> ParseInternalDtdAsync(IDtdParserAdapter adapter, bool saveInternalSubset);

	Task<IDtdInfo> ParseFreeFloatingDtdAsync(string baseUri, string docTypeName, string publicId, string systemId, string internalSubset, IDtdParserAdapter adapter);
}
