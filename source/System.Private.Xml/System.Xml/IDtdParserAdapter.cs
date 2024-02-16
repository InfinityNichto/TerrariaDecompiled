using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;

namespace System.Xml;

internal interface IDtdParserAdapter
{
	XmlNameTable NameTable { get; }

	IXmlNamespaceResolver NamespaceResolver { get; }

	Uri BaseUri { get; }

	char[] ParsingBuffer { get; }

	int ParsingBufferLength { get; }

	int CurrentPosition { get; set; }

	int LineNo { get; }

	int LineStartPosition { get; }

	bool IsEof { get; }

	int EntityStackLength { get; }

	bool IsEntityEolNormalized { get; }

	int ReadData();

	void OnNewLine(int pos);

	int ParseNumericCharRef(StringBuilder internalSubsetBuilder);

	int ParseNamedCharRef(bool expand, StringBuilder internalSubsetBuilder);

	void ParsePI(StringBuilder sb);

	void ParseComment(StringBuilder sb);

	bool PushEntity(IDtdEntityInfo entity, out int entityId);

	bool PopEntity(out IDtdEntityInfo oldEntity, out int newEntityId);

	bool PushExternalSubset(string systemId, string publicId);

	void PushInternalDtd(string baseUri, string internalDtd);

	void OnSystemId(string systemId, LineInfo keywordLineInfo, LineInfo systemLiteralLineInfo);

	void OnPublicId(string publicId, LineInfo keywordLineInfo, LineInfo publicLiteralLineInfo);

	[DoesNotReturn]
	void Throw(Exception e);

	Task<int> ReadDataAsync();

	Task<int> ParseNumericCharRefAsync(StringBuilder internalSubsetBuilder);

	Task<int> ParseNamedCharRefAsync(bool expand, StringBuilder internalSubsetBuilder);

	Task ParsePIAsync(StringBuilder sb);

	Task ParseCommentAsync(StringBuilder sb);

	Task<(int, bool)> PushEntityAsync(IDtdEntityInfo entity);

	Task<bool> PushExternalSubsetAsync(string systemId, string publicId);
}
